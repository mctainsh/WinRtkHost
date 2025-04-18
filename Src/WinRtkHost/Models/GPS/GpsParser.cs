/// <summary>
/// Show shifting array for new data
/// </summary>
//#define VERBOSE_SHIFT

using System;
using System.IO.Ports;
using System.Text;


namespace WinRtkHost.Models.GPS
{
	public class GpsParser
	{
		const int MAX_BUFF = 2560;

		internal const string NL = "\r\n\t";

		/// <summary>
		/// Process the RTCM packets here
		/// </summary>
		readonly RtcmParser _rtcmParser = new RtcmParser();

		/// <summary>
		/// State of the build packet
		/// </summary>
		BuildState _buildState = BuildState.BuildStateNone;
		enum BuildState
		{
			BuildStateNone,
			BuildStateBinary,
			BuildStateAscii,
			BuildStateRtkAscii,
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
		/// Number of read errors
		/// </summary>
		int _readErrorCount = 0;

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
		readonly static LocationAverage _locationAverage = new LocationAverage();

		/// <summary>
		/// Status of the GPS connection. False if we have not received a packet in the last 60 seconds
		/// </summary>
		public bool _gpsConnected = false;

		/// <summary>
		/// Serial port we are communicating on
		/// </summary>
		readonly SerialPort _port;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="port">Serial port to communicate on</param>
		public GpsParser(SerialPort port)
		{
			// Add receiver event handler
			_port = port;
			_port.DataReceived += OnSerialData;
			_port.ErrorReceived += OnSerialError;

			CommandQueue = new GpsCommandQueue(port);

			_timeOfLastRtkMessage = DateTime.Now;
			_timeOfLastAsciiMessage = DateTime.Now;

			// Don't initialise. Wait for GPS to timeout that way we won't lose datum
			//CommandQueue.StartRtkInitialiseProcess();
		}

		/// <summary>
		/// Serial port communication error
		/// </summary>
		private void OnSerialError(object sender, SerialErrorReceivedEventArgs e)
		{
			_readErrorCount++;
			Log.Ln($"E401 - Serial error {e.EventType}");
		}

		/// <summary>
		/// Socket summary
		/// </summary>
		public void LogStatus()
		{
			Log.Note($"=============================================");
			_rtcmParser.LogStatus();
			Log.Note($"GPS {_port.PortName} - {_gpsConnected}{NL}" +
				$"Read errors {_readErrorCount:N0}{NL}" +
				$"Max buff    {_maxBufferSize}{NL}" +
				$"ASCII ptks  {_totalAsciiPackets}{NL}" +
				$"Location    {_locationAverage.LogMeanAndStandardDeviations()}{NL}" +
				$"{_rtcmParser.GetMessageTypeCounts()}");
		}

		/// <summary>
		/// Receove serial data from COM port
		/// </summary>
		void OnSerialData(object sender, SerialDataReceivedEventArgs e)
		{
			try
			{
				// Read available data from port
				var port = (SerialPort)sender;
				var bytes = port.BytesToRead;
				_maxBufferSize = Math.Max(_maxBufferSize, bytes);
				var data = new byte[bytes];
				port.Read(data, 0, bytes);

				// Raw log
				//"C:\\Temp\\RawM20.bin".AppendBytesToFile(data);

				// Process the data
				ProcessStream(data);
			}
			catch (Exception ex)
			{
				Log.Ln($"E400 - Serial data error {ex}");
			}
		}

		/// <summary>
		/// Process what we have received from the GPS unit
		/// </summary>
		bool ProcessStream(byte[] pData)
		{
			var dataSize = pData.Length;
			for (int n = 0; n < dataSize; n++)
			{
				if (ProcessGpsSerialByte(pData[n]))
					continue;

				_buildState = BuildState.BuildStateNone;

#if VERBOSE_SHIFT
				Log.Ln($"IN  BUFF {_binaryIndex} : {HexDump(_byteArray, _binaryIndex)}");
				Log.Ln($"IN  DATA {n} : {HexDump(pData, dataSize)}");
#endif

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
#if VERBOSE_SHIFT
				Log.Ln($"OUT DATA {n} : {HexDump(pData, dataSize)}");
#endif
			}
			return true;
		}

		/// <summary>
		/// Process a single byte from the GPS unit into the state machine
		/// </summary>
		/// <returns>True if we are still processing. False if bad data for type of packlet</returns>
		bool ProcessGpsSerialByte(byte ch)
		{
			// Process the byte according to state machine
			switch (_buildState)
			{
				// Lookiung for the start of a packet
				case BuildState.BuildStateNone:
					switch (ch)
					{
						// Expect start of ASCII packet
						case (byte)'$':
						case (byte)'#':
						case (byte)'<':     // Added for M20
						case (byte)'[':     // Added for M20 Reboot
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

				// Building RTK ASCII packet
				case BuildState.BuildStateRtkAscii:
					return BuildRtkAscii(ch);

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
			if (_skippedIndex > 0)
			{
				Log.Ln($"Skipped {_skippedIndex} : {_skippedArray.HexAsciDumpDetail(_skippedIndex)}");
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

			// Check for text messages
			if (_binaryIndex == 3)
			{
				uint lengthPrefix = GetUInt(8, 14 - 8);
				if (lengthPrefix == 2)
				{
					_buildState = BuildState.BuildStateRtkAscii;
					return true;
				}
				if (lengthPrefix != 0)
				{
					Log.Ln($"Binary length prefix too big {_byteArray[0]:X2} {_byteArray[1]:X2} - {lengthPrefix}");
					return false;
				}
			}

			// Dont process if not big enough to hold prefix, length and checksum
			if (_binaryIndex < 5)
				return true;

			// Verify size (First time only)
			if (_binaryLength == 0 && _binaryIndex >= 4)
			{
				_binaryLength = (int)(GetUInt(14, 10) + 6);
				if (_binaryLength == 0 || _binaryLength >= MAX_BUFF)
				{
					Log.Ln($"Binary length too big {_binaryLength}");
					return false;
				}
				return true;
			}

			// Check for overflow
			if (_binaryIndex >= MAX_BUFF)
			{
				Log.Ln($"Buffer overflow {_binaryIndex}");
				return false;
			}

			// Have we reached the end of the packet
			if (_binaryIndex >= _binaryLength)
			{
				if (_rtcmParser.ProcessRtkPacket(_byteArray, _binaryLength))
				{
					// Packet complete OK
					_gpsConnected = true;
					_timeOfLastRtkMessage = DateTime.Now;
					_buildState = BuildState.BuildStateNone;
					return true;
				}
				// Corrupt packet
				return false;
			}

			// Continue processing packets
			return true;
		}

		/// <summary>
		/// Process the RTK ASCII data
		/// </summary>
		bool BuildRtkAscii(byte ch)
		{
			if (ch == '\r' || ch == '\n')
				return true;

			if (ch == '\0')
			{
				if (_binaryIndex < 2)
				{
					Log.Ln("RTK <- ''");
				}
				else
				{
					_byteArray[_binaryIndex] = 0;
					Log.Ln("RTK <- " + Encoding.ASCII.GetString(_byteArray, 2, _binaryIndex - 2));
				}
				_buildState = BuildState.BuildStateNone;
				return true;
			}

			if (_binaryIndex > 254)
			{
				Log.Ln($"RTK ASCII Overflowing {_byteArray.HexAsciDump(_binaryIndex)}");
				_buildState = BuildState.BuildStateNone;
				return false;
			}

			_byteArray[_binaryIndex++] = ch;

			if (ch < 32 || ch > 126)
			{
				Log.Ln($"RTK Non-ASCII {_byteArray.HexAsciDumpDetail(_binaryIndex)}");
				_buildState = BuildState.BuildStateNone;
				return false;
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
				Log.Ln($"ASCII Overflowing {_byteArray.HexAsciDumpDetail(_binaryIndex)}");
				_buildState = BuildState.BuildStateNone;
				return false;
			}

			_byteArray[_binaryIndex++] = ch;

			if (ch < 32 || ch > 126)
			{
				Log.Ln($"Non-ASCII {_byteArray.HexAsciDumpDetail(_binaryIndex)}");
				_buildState = BuildState.BuildStateNone;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Process the ASCII line of data
		/// </summary>
		void ProcessAsciiLine(string line)
		{
			if (line.Length < 1)
			{
				Log.Ln("W700 - GPS ASCII Too short");
				return;
			}

			_timeOfLastAsciiMessage = DateTime.Now;
			_totalAsciiPackets++;

			// TODO : Do I need to accept GPGGA for M20
			if (line.StartsWith("$GNGGA") || (Program.IsM20 && line.StartsWith("$GPGGA")))
			{
				//Log.Note($"GPS <- '{line}'");
				_locationAverage.ProcessGGALocation(line);
			}
			else if (line.StartsWith("$G"))
			{
				Log.Note($"GPS <- '{line}'");
			}
			else if (Program.IsM20 && (line.StartsWith("<    ") || line.Contains("System will reboot in") || line.StartsWith("#BEST")))
			{
				//M20 Result
				Log.Ln($"M20 '{line}'");
				return;
			}
			else
			{
				Log.Ln($"GPS <- '{line}'");
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
			if ((DateTime.Now - _timeOfLastAsciiMessage).TotalSeconds > 31)
			{
				_gpsConnected = false;
				Log.Ln("W701 - GPS ASCII Timeout");
				CommandQueue.StartAsciiProcess();
				_timeOfLastAsciiMessage = DateTime.Now;
			}
		}

		/// <summary>
		/// Kill off all the servers
		/// </summary>
		internal string Shutdown()
		{
			_rtcmParser.Shutdown();
			return ToString();
		}
	}
}
