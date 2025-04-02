using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace WinRtkHost.Models
{
	/// <summary>
	/// I should have named this differently. This class is responsible for managing 
	/// the connection to the NTRIP caster. It is a simple TCP/IP connection that 
	/// sends data to the caster.
	/// </summary>
	public class NTRIPServer
	{
		/// <summary>
		/// Retry interval for socket connection failures.
		/// </summary>
		static readonly int[] SOCKET_RETRY_INTERVALS_S = new int[] { 15, 30, 60, 300 };

		/// <summary>
		/// The number of times a reconnection has been attempted
		/// </summary>
		int _reconnectAttempt = 0;

		/// <summary>
		/// Flag keeps running till told to stop
		/// </summary>
		bool _keepAlive = true;

		/// <summary>
		/// Connection details read from the file
		/// </summary>
		string _sAddress;
		int _port;
		string _sCredential;
		string _sPassword;

		/// <summary>
		/// Socket
		/// </summary>
		Socket _client = null;

		bool _wasConnected = false;
		string _status;
		DateTime _wifiConnectTime;
		int _reconnects = 0;
		int _packetsSent = 0;
		double _maxSendTime = 0;

		/// <summary>
		/// Send time calculations
		/// </summary>
		readonly List<double> _sendMicroSeconds = new List<double>();

		/// <summary>
		/// Queue control
		/// </summary>
		readonly List<byte[]> _outboundQueue = new List<byte[]>();

		/// <summary>
		/// Load the settings from the file and start working thread
		/// </summary>
		/// <param name="index">Index of file on current directory</param>
		/// <returns>True if load occured</returns>
		public bool LoadSettings(int index)
		{
			string fileName = $"Caster{index}.txt";
			if (!File.Exists(fileName))
				return false;
			Log.Ln("Processing " + fileName);
			var llText = File.ReadAllText(fileName);

			var parts = llText.Split('\n');
			if (parts.Length > 3)
			{
				_sAddress = parts[0].Trim('\r');
				_port = int.Parse(parts[1].Trim('\r'));
				_sCredential = parts[2].Trim('\r');
				_sPassword = parts[3].Trim('\r');
				Log.Ln($" - Recovered\r\n\t Address  : {_sAddress}\r\n\t Port     : {_port}\r\n\t Mid/Cred : {_sCredential}\r\n\t Pass     : {_sPassword}");
			}
			else
			{
				Log.Ln($" - E341 - Cannot read saved Server settings {llText}");
			}

			// Start the main processing thread
			new System.Threading.Thread(() => Loop()).Start();
			return true;
		}


		/// <summary>
		/// Print socket details
		/// </summary>
		override public string ToString()
		{
			var ret = $"{_sAddress} - {_status}\r\n" +
				$"\tReconnects {_reconnects}\r\n" +
				$"\tSent       {_packetsSent:N0}\r\n" +
				$"\tMax Send   {_maxSendTime} ms\r\n" +
				$"\tSpeed      {AverageSendTime():N0} kbps";
			_maxSendTime = 0;
			return ret;
		}

		/// <summary>
		/// Process the socket connection in a seperate thread
		/// </summary>
		void Loop()
		{
			while (_keepAlive)
			{
				try
				{
					// Read from queue
					byte[] byteArray = null;
					lock (_outboundQueue)
					{
						if (_outboundQueue.Count > 0)
						{
							byteArray = _outboundQueue[0];
							_outboundQueue.RemoveAt(0);
						}
					}

					// Work or sleep
					if (byteArray != null)
						Loop(byteArray, byteArray.Length);
					else
						System.Threading.Thread.Sleep(1);
				}
				catch (Exception ex)
				{
					Log.Ln($"E500 - RTK {_sAddress} Loop error. {ex.ToString().Replace("\n", "\n\t\t")}");
					System.Threading.Thread.Sleep(1_000);
					CloseSocket();
				}
			}
		}

		/// <summary>
		/// Process the data here. 
		/// Note : If the soeket is disconnected, packet is dumped
		/// </summary>
		public void Loop(byte[] pBytes, int length)
		{
			if (_client?.Connected == true)
			{
				// Send the connected data
				ConnectedProcessing(pBytes, length);
			}
			else
			{
				// Not connected. Try to reconnect
				_wasConnected = false;
				_status = "Disconnected";
				Reconnect();
			}
		}

		/// <summary>
		/// Processing of the connected socket involves sending the new data
		/// </summary>
		void ConnectedProcessing(byte[] pBytes, int length)
		{
			if (!_wasConnected)
			{
				_reconnects++;
				_status = "Connected";
				_wasConnected = true;
			}

			ConnectedProcessingSend(pBytes, length);
			ConnectedProcessingReceive();
		}

		/// <summary>
		/// Send the data to the connected socket
		/// </summary>
		void ConnectedProcessingSend(byte[] pBytes, int length)
		{
			// Anything to send
			if (length < 1)
				return;

			//var startT = DateTime.Now;
			var stopwatch = Stopwatch.StartNew();

			// Clean up the send timer queue
			lock (_sendMicroSeconds)
			{
				while (_sendMicroSeconds.Count >= 3600)
					_sendMicroSeconds.RemoveAt(0);
			}

			try
			{
				// Keep a history of send timers
				var sent = _client.Send(pBytes, length, SocketFlags.None);
				if (sent != length)
					throw new Exception($"Incomplete send {length} != {sent}");

				//var time = (DateTime.Now - startT).TotalMilliseconds;
				stopwatch.Stop();
				var time = stopwatch.Elapsed.TotalMilliseconds;

				if (_maxSendTime == 0)
					_maxSendTime = time;
				else
					_maxSendTime = Math.Max(_maxSendTime, time);

				if (time != 0)
				{
					lock (_sendMicroSeconds)
					{
						_sendMicroSeconds.Add(length * 8.0 / time);
					}
				}
				_wifiConnectTime = DateTime.Now;
				_packetsSent++;

				// Record connections are going well
				if (_reconnectAttempt > 0)
				{
					_reconnectAttempt--;
					Log.Ln($"RTK {_sAddress} Reconnected OK.");
				}
			}
			catch (Exception ex)
			{
				Log.Ln($"E500 - RTK {_sAddress} Not connected. ({ex})");
				CloseSocket();
				_wasConnected = false;
			}
		}

		void CloseSocket()
		{
			try { _client?.Close(); } catch { }
			_client = null;
		}

		/// <summary>
		/// Process any received data
		/// </summary>
		void ConnectedProcessingReceive()
		{
			if (_client is null || _client.Available < 1)
				return;

			var buffer = new byte[_client.Available];
			var bytesRead = _client.Receive(buffer, _client.Available, SocketFlags.None);
			string str = "RECV. " + _sAddress + "\r\n";
			if (buffer.IsAllAscii(bytesRead))
				str += Encoding.ASCII.GetString(buffer, 0, bytesRead);
			else
				str += buffer.HexAsciDump(bytesRead);
			Log.Ln(str.Replace("\n", "\n\t\t"));
		}

		/// <summary>
		/// Safely read progress
		/// </summary>
		public double AverageSendTime()
		{
			lock (_sendMicroSeconds)
			{
				if (_sendMicroSeconds.Count < 1)
					return 0;
				double total = 0;
				foreach (double n in _sendMicroSeconds)
					total += n;
				return total / _sendMicroSeconds.Count;
			}
		}

		/// <summary>
		/// Reconnect. But only if we have not tried for a while
		/// </summary>
		/// <returns>False if retry was not attempted</returns>
		bool Reconnect()
		{
			// If we have tried recently, then do not try again
			if ((DateTime.Now - _wifiConnectTime).TotalSeconds < SOCKET_RETRY_INTERVALS_S[_reconnectAttempt])
				return false;

			// Clean out the send queue
			lock (_outboundQueue)
			{
				_outboundQueue.Clear();
			}

			// Record when we last tried
			_wifiConnectTime = DateTime.Now;

			// Record the attempt
			_reconnectAttempt++;
			if (_reconnectAttempt >= SOCKET_RETRY_INTERVALS_S.Length)
				_reconnectAttempt = SOCKET_RETRY_INTERVALS_S.Length - 1;


			Log.Ln($"RTK Connecting to {_sAddress} : {_port}. Try:{_reconnectAttempt}");

			try
			{
				_client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_client.Connect(_sAddress, _port);
			}
			catch (Exception ex)
			{
				Log.Ln($"E500 - RTK {_sAddress} Not connected. ({ex.Message})");
				CloseSocket();
				return false;
			}

			Log.Ln($"Connected {_sAddress} OK.");

			if (!WriteText($"SOURCE {_sPassword} {_sCredential}\r\n"))
				return false;
			if (!WriteText("Source-Agent: NTRIP UM98/ESP32_T_Display_SX\r\n"))
				return false;
			if (!WriteText("STR: \r\n"))
				return false;
			if (!WriteText("\r\n"))
				return false;
			return true;
		}

		bool WriteText(string str)
		{
			if (str is null)
				return true;

			string message = $"    -> '{str}'";
			Log.Ln(message.ReplaceCrLfEncode());

			byte[] data = Encoding.ASCII.GetBytes(str);
			_client.Send(data, data.Length, SocketFlags.None);
			return true;
		}

		/// <summary>
		/// Add this to the outbouind que for sending
		/// </summary>
		internal void Send(byte[] byteArray)
		{
			lock (_outboundQueue)
			{
				while (_outboundQueue.Count > 64)
					_outboundQueue.RemoveAt(0);
				_outboundQueue.Add(byteArray);
			}
		}

		/// <summary>
		/// Shutdown the socket and the thread
		/// </summary>
		internal void Shutdown()
		{
			_keepAlive = false;
			CloseSocket();
		}
	}
}