using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;

namespace WinRtkHost.Models.GPS
{
	public class GpsParser
	{
		const bool VERBOSE = true;
		const int MAX_BUFF = 2560;


		/// <summary>
		/// State of the build packet
		/// </summary>
		BuildState _buildState = BuildState.BuildStateNone;
		enum BuildState
		{
			BuildStateNone,
			BuildStateBinary,
			BuildStateAscii
		}

		/// <summary>
		/// Last time we receive a Binary RTK packet
		/// </summary>
		DateTime _timeOfLastRtkMessage;

		/// <summary>
		/// Last time we receive a ASCII packet
		/// </summary>
		DateTime _timeOfLastAsciiMessage;

		/// <summary>
		/// Array we are building binary RTK data or ASCII data in
		/// </summary>
		readonly byte[] _byteArray = new byte[MAX_BUFF + 1];
		int _binaryIndex = 0;
		int _binaryLength = 0;

		/// <summary>
		/// Number of ASCII packets we have received
		/// </summary>
		int _totalAsciiPackets = 0;

		/// <summary>
		/// Bytes that we have skipped
		/// </summary>
		readonly byte[] _skippedArray = new byte[MAX_BUFF + 2];
		int _skippedIndex = 0;

		/// <summary>
		/// Dictionary of the RTK packets we have received
		/// </summary>
		readonly Dictionary<int, int> _msgTypeTotals = new Dictionary<int, int>();

		/// <summary>
		/// Number of read errors
		/// </summary>
		int _readErrorCount = 0;
		int _missedBytesDuringError = 0;

		/// <summary>
		/// Biggest serial data packet we have received
		/// </summary>
		int _maxBufferSize = 0;

		/// <summary>
		/// Output command processing
		/// </summary>
		internal GpsCommandQueue CommandQueue { private set; get; }

		/// <summary>
		/// Average location builder
		/// </summary>
		readonly LocationAverage _locationAverage = new LocationAverage();

		/// <summary>
		/// Status of the GPS connection. False if we have not received a packet in the last 60 seconds
		/// </summary>
		public bool _gpsConnected = false;

		/// <summary>
		/// Serial port we are communicating on
		/// </summary>
		readonly SerialPort _port;

		/// <summary>
		/// List of socket connection to NTRIP casters we are pusing RTK data to
		/// </summary>
		internal List<NTRIPServer> NtripCasters { get; } = new List<NTRIPServer>();

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="port">Serial port to communicate on</param>
		public GpsParser(SerialPort port)
		{
			// Load NTRIP casters
			for (int index = 0; index < 1000; index++)
			{
				var caster = new NTRIPServer();
				if (!caster.LoadSettings(index))
					break;
				NtripCasters.Add(caster);
			}

			// Add receiver event handler
			_port = port;
			_port.DataReceived += OnSerialData;

			CommandQueue = new GpsCommandQueue(port);

			_timeOfLastRtkMessage = DateTime.Now;
			_timeOfLastAsciiMessage = DateTime.Now;

			// Don't initialise. Wait for GPS to timeout that way we won't lose datum
			//CommandQueue.StartRtkInitialiseProcess();
		}

		/// <summary>
		/// Socket summary
		/// </summary>
		override public string ToString()
		{
			const string NL = "\r\n\t";
			string counts = "";
			lock (_msgTypeTotals)
			{
				foreach (var item in _msgTypeTotals)
					counts += $"\t{item.Key} : {item.Value:N0}{NL}";
			}

			return $"GPS {_port.PortName} - {_gpsConnected}{NL}" +
				$"Read errors {_readErrorCount:N0}{NL}" +
				$"Max buff    {_maxBufferSize}{NL}" +
				$"ASCII ptks  {_totalAsciiPackets}{NL}" +
				$"{counts}";
		}

		/// <summary>
		/// Receove serial data from COM port
		/// </summary>
		void OnSerialData(object sender, SerialDataReceivedEventArgs e)
		{
			// Read available data from port
			var port = (SerialPort)sender;
			var bytes = port.BytesToRead;
			_maxBufferSize = Math.Max(_maxBufferSize, bytes);
			var data = new byte[bytes];
			port.Read(data, 0, bytes);

			// Process the data
			ProcessStream(data);
		}


		bool ProcessStream(byte[] pData)
		{
			var dataSize = pData.Length;
			for (int n = 0; n < dataSize; n++)
			{
				if (ProcessGpsSerialByte(pData[n]))
					continue;

				_buildState = BuildState.BuildStateNone;

				if (VERBOSE)
				{
					Log.Ln($"IN  BUFF {_binaryIndex} : {HexDump(_byteArray, _binaryIndex)}");
					Log.Ln($"IN  DATA {n} : {HexDump(pData, dataSize)}");
				}

				n += 1;

				if (_binaryIndex <= n)
				{
					n -= _binaryIndex;
				}
				else
				{
					int oldBufferSize = _binaryIndex - 1;
					int remainder = dataSize - n;
					int totalSize = oldBufferSize + remainder;

					if (totalSize > 0)
					{
						var pTempData = new byte[totalSize + 1];
						if (oldBufferSize > 0)
							Array.Copy(_byteArray, 1, pTempData, 0, oldBufferSize);
						if (remainder > 0)
							Array.Copy(pData, n, pTempData, oldBufferSize, remainder);

						pData = pTempData;
						n = -1;
						dataSize = totalSize;
					}
				}
				_binaryIndex = 0;

				if (VERBOSE)
				{
					Log.Ln($"OUT DATA {n} : {HexDump(pData, dataSize)}");
				}
			}
			return true;
		}

		/// <summary>
		/// Process a single byte from the GPS unit into the state machine
		/// </summary>
		/// <returns>True if we are still processing. False if bad data for type of packlet</returns>
		bool ProcessGpsSerialByte(byte ch)
		{
			switch (_buildState)
			{
				// Lookiung for the start of a packet
				case BuildState.BuildStateNone:
					switch (ch)
					{
						// Expect start of ASCII packet
						case (byte)'$':
						case (byte)'#':
							DumpSkippedBytes();
							_binaryIndex = 1;
							_byteArray[0] = ch;
							_buildState = BuildState.BuildStateAscii;
							return true;
						// Start of RTK binary packet
						case 0xD3:
							DumpSkippedBytes();
							_buildState = BuildState.BuildStateBinary;
							_binaryLength = 0;
							_binaryIndex = 1;
							_byteArray[0] = ch;
							return true;

						// Must be mid packet
						default:
							AddToSkipped(ch);
							return true;
					}

				// Building binary RTK packet
				case BuildState.BuildStateBinary:
					return BuildBinary(ch);

				// Building ASCII packet
				case BuildState.BuildStateAscii:
					return BuildAscii(ch);

				// Unknown state (Should not happen)
				default:
					AddToSkipped(ch);
					Log.Ln($"Unknown state {_buildState}");
					_buildState = BuildState.BuildStateNone;
					return true;
			}
		}

		/// <summary>
		/// Log all the byte we skipped (Should only every dump at startup)
		/// </summary>
		void DumpSkippedBytes()
		{
			if (VERBOSE && _skippedIndex > 0)
			{
				Log.Ln($"Skipped {_skippedIndex} : {HexDump(_skippedArray, _skippedIndex)}");
				_skippedIndex = 0;
			}
		}

		/// <summary>
		/// Keep track of all the skipped bytes
		/// </summary>
		void AddToSkipped(byte ch)
		{
			if (_skippedIndex >= MAX_BUFF)
			{
				Log.Ln("Skip buffer overflowed");
				_skippedIndex = 0;
			}
			_skippedArray[_skippedIndex++] = ch;
		}

		/// <summary>
		/// Building a binnary RTK packet
		/// </summary>
		bool BuildBinary(byte ch)
		{
			_byteArray[_binaryIndex++] = ch;

			if (_binaryIndex < 12)
				return true;

			if (_binaryLength == 0 && _binaryIndex >= 4)
			{
				uint lengthPrefix = GetUInt(8, 14 - 8);
				if (lengthPrefix != 0)
				{
					Log.Ln($"Binary length prefix too big {_byteArray[0]:X2} {_byteArray[1]:X2}");
					return false;
				}
				_binaryLength = (int)(GetUInt(14, 10) + 6);
				if (_binaryLength == 0 || _binaryLength >= MAX_BUFF)
				{
					Log.Ln($"Binary length too big {_binaryLength}");
					return false;
				}
				return true;
			}
			if (_binaryIndex >= MAX_BUFF)
			{
				Log.Ln($"Buffer overflow {_binaryIndex}");
				return false;
			}

			if (_binaryIndex >= _binaryLength)
			{
				uint parity = GetUInt((_binaryLength - 3) * 8, 24);
				uint calculated = Crc24.RtkCrc24( _byteArray, _binaryLength);
				uint type = GetUInt(24, 12);
				if (parity != calculated)
				{
					Log.Ln($"Checksum {type} ({parity:X6} != {calculated:X6}) [{_binaryIndex}] {HexDump(_byteArray, _binaryIndex)}");
					return false;
				}

				_gpsConnected = true;
				_timeOfLastRtkMessage = DateTime.Now;

				if (_missedBytesDuringError > 0)
				{
					_readErrorCount++;
					Log.Ln($" >> E: {_readErrorCount} - Skipped {_missedBytesDuringError}");
					_missedBytesDuringError = 0;
				}

				// Update the totals counts
				lock (_msgTypeTotals)
				{
					if (_msgTypeTotals.ContainsKey((int)type))
						_msgTypeTotals[(int)type]++;
					else
						_msgTypeTotals.Add((int)type, 1);
				}
				//Log.Ln($"GOOD {type}[{_binaryIndex}]");
				Console.Write($"\r{type}[{_binaryIndex}]    \r");

				// Send to NTRIP casters (Actually just queue)
				var sendData = new byte[_binaryLength];
				Array.Copy(_byteArray, sendData, _binaryLength);
				foreach (var _caster in NtripCasters)
					_caster.Send(sendData);

				_buildState = BuildState.BuildStateNone;
			}
			return true;
		}

		/// <summary>
		/// Process the ASCII data
		/// </summary>
		bool BuildAscii(byte ch)
		{
			if (ch == '\r')
				return true;

			if (ch == '\n')
			{
				_byteArray[_binaryIndex] = 0;
				ProcessAsciiLine(Encoding.ASCII.GetString(_byteArray, 0, _binaryIndex));
				_buildState = BuildState.BuildStateNone;
				return true;
			}

			if (_binaryIndex > 254)
			{
				Log.Ln($"ASCII Overflowing {HexAsciDump(_byteArray, _binaryIndex)}");
				_buildState = BuildState.BuildStateNone;
				return false;
			}

			_byteArray[_binaryIndex++] = ch;

			if (ch < 32 || ch > 126)
			{
				Log.Ln($"Non-ASCII {HexDump(_byteArray, _binaryIndex)}");
				_buildState = BuildState.BuildStateNone;
				return false;
			}
			return true;
		}

		void ProcessAsciiLine(string line)
		{
			if (line.Length < 1)
			{
				Log.Ln("W700 - GPS ASCII Too short");
				return;
			}

			_timeOfLastAsciiMessage = DateTime.Now;
			_totalAsciiPackets++;

			if (line.StartsWith("$GNGGA"))
			{
				//Log.Note($"GPS <- '{line}'");
				Log.Note(_locationAverage.ProcessGGALocation(line));
			}
			else
			{
				//Log.Ln($"GPS <- '{line}'");
			}

			CommandQueue.IsCommandResponse(line);
		}

		uint GetUInt(int pos, int len)
		{
			uint bits = 0;
			for (int i = pos; i < pos + len; i++)
				bits = (uint)((bits << 1) + (_byteArray[i / 8] >> 7 - i % 8 & 1u));
			return bits;
		}

		string HexDump(byte[] data, int length)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < length; i++)
			{
				sb.AppendFormat("{0:X2} ", data[i]);
			}
			return sb.ToString();
		}

		string HexAsciDump(byte[] data, int length)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < length; i++)
			{
				sb.AppendFormat("{0:X2} ", data[i]);
			}
			return sb.ToString();
		}

		/// <summary>
		/// Has comms stalled
		/// </summary>
		internal void CheckTimeouts()
		{
			// Check for Queue timeout
			if (CommandQueue.CheckForTimeouts())
				return;

			// Check for RTK timeout
			if ((DateTime.Now - _timeOfLastRtkMessage).TotalSeconds > 60)
			{
				_gpsConnected = false;
				Log.Ln("W700 - GPS RTK Timeout");
				CommandQueue.StartRtkInitialiseProcess();
				_timeOfLastRtkMessage = DateTime.Now;
			}

			// Check for GPS ASCII timeout
			if ((DateTime.Now - _timeOfLastAsciiMessage).TotalSeconds > 81)
			{
				_gpsConnected = false;
				Log.Ln("W701 - GPS ASCII Timeout");
				CommandQueue.StartAsciiProcess();
				_timeOfLastRtkMessage = DateTime.Now;
			}
		}

		/// <summary>
		/// Kill off all the servers
		/// </summary>
		internal string Shutdown()
		{
			foreach (var _caster in NtripCasters)
				_caster.Shutdown();
			return ToString();
		}

	}
}
