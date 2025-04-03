using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Xml.Schema;
using WinRtkHost.Properties;

namespace WinRtkHost.Models.GPS
{
	public class GpsCommandQueue
	{
		readonly List<string> _strings = new List<string>();
		DateTime _timeSent;
		string _deviceType;
		string _deviceFirmware = "UNKNOWN";
		string _deviceSerial = "UNKNOWN";
		readonly SerialPort _port;

		public string GetDeviceType() => _deviceType;
		public string GetDeviceFirmware() => _deviceFirmware;
		public string GetDeviceSerial() => _deviceSerial;

		/// <summary>
		/// Constructor. Setup version request but don't run it
		/// </summary>
		public GpsCommandQueue(SerialPort port)
		{
			// Handy for debugging blank system (Query current state)
			_port = port;
			if (Program.IsM20)
			{
				//_strings.Add("UNLOGALL");		// Only do this for testing
				//_strings.Add("FRESET");		// ..
				_strings.Add("LOG LOGLIST");
				_strings.Add("LOG GPGSV ONTIME 10");
				//StartRtkInitialiseProcess();
			}
			if (Program.IsLC29H)
			{
				_strings.Add("PQTMVERNO");
			}
			if (Program.IsUM980 || Program.IsUM982)
			{
				//_strings.Add("UNLOG");
				_strings.Add("CONFIG");
				_strings.Add("VERSION");     // Used to determine device type											
				_strings.Add("GPGGA 1");
			}
			//_strings.Add("FRESET");
		}

		/// <summary>
		/// Start the initialisation process for RTK messages
		/// </summary>
		public void StartRtkInitialiseProcess()
		{
			// Don't do it if data in the queue
			if (_strings.Any())
			{
				Log.Ln("ERROR : GPS Queue Start RTK Initialise Process - Already has data");
				return;
			}

			Log.Ln("GPS Queue Start RTK Initialise Process");
			if (Program.IsM20)
			{
				_strings.Add("LOG LOGLIST");            // Record current state
				_strings.Add("FRESET");					// This resets out everything
				_strings.Add("LOG LOGLIST");			// Record default setting
				_strings.Add("UNLOGALL");				// Stop logging to clean up log
				_strings.Add("LOG LOGLIST");			// Record cleared default
				_strings.Add("RTKTYPE BASE");
				_strings.Add("LOG RTCM1005 ONTIME 1");
				_strings.Add("LOG RTCM1006 ONTIME 1");
				_strings.Add("LOG RTCM1019 ONTIME 1");
				_strings.Add("LOG RTCM1020 ONTIME 1");
				_strings.Add("LOG RTCM1033 ONTIME 1");
				_strings.Add("LOG RTCM1042 ONTIME 1");
				_strings.Add("LOG RTCM1044 ONTIME 1");
				_strings.Add("LOG RTCM1046 ONTIME 1");
				_strings.Add("LOG RTCM1048 ONTIME 1");
				_strings.Add("LOG RTCM1077 ONTIME 1");
				_strings.Add("LOG RTCM1087 ONTIME 1");
				_strings.Add("LOG RTCM1097 ONTIME 1");
				_strings.Add("LOG RTCM1107 ONTIME 1");
				_strings.Add("LOG RTCM1117 ONTIME 1");
				_strings.Add("LOG RTCM1127 ONTIME 1");
				_strings.Add("LOG RTCM1137 ONTIME 1");
				_strings.Add("LOG RTCM1230 ONTIME 1");
				_strings.Add("LOG GPGGA ONTIME 1");
				_strings.Add("INTERFACEMODE COM1 AUTO AUTO");
				_strings.Add("INTERFACEMODE COM3 AUTO AUTO");
				_strings.Add("PPSCONTROL ENABLE POSITIVE 1 100000");
				_strings.Add("FIX NONE");
				_strings.Add("SAVECONFIG");					// Save for later
				_strings.Add("REBOOT");
				_strings.Add("LOG LOGLIST");
			}
			if (Program.IsLC29H)
			{
				_strings.Add("PQTMCFGSVIN,W,1,43200,0,0,0,0");
				_strings.Add("PAIR432,1");
				_strings.Add("PAIR434,1");
				_strings.Add("PAIR436,1");
			}
			if (Program.IsUM980 || Program.IsUM982)
			{
				// Setup RTCM V3
				_strings.Add("RTCM1005 30"); // Base station antenna reference point (ARP) coordinates
				_strings.Add("RTCM1033 30"); // Receiver and antenna description
				_strings.Add("RTCM1077 1");  // GPS MSM7. The type 7 Multiple Signal Message format for the USA’s GPS system, popular.
				_strings.Add("RTCM1087 1");  // GLONASS MSM7. The type 7 Multiple Signal Message format for the Russian GLONASS system.
				_strings.Add("RTCM1097 1");  // Galileo MSM7. The type 7 Multiple Signal Message format for Europe’s Galileo system.
				_strings.Add("RTCM1117 1");  // QZSS MSM7. The type 7 Multiple Signal Message format for Japan’s QZSS system.
				_strings.Add("RTCM1127 1");  // BeiDou MSM7. The type 7 Multiple Signal Message format for China’s BeiDou system.
				_strings.Add("RTCM1137 1");  // NavIC MSM7. The type 7 Multiple Signal Message format for India’s NavIC system.	

				var address = Settings.Default.BaseStationAddress;
				if (address.IsNullOrEmpty())
					_strings.Add($"MODE BASE TIME {60 * 60} 0.01");                      // Set base mode with 6 hours startup and 1cm optimized save error
				else
					_strings.Add("MODE BASE " + Settings.Default.BaseStationAddress); // Set the precise coordinates of base station: latitude, longitude, height
			}
			if (Program.IsUM980)
			{
				_strings.Add("CONFIG SIGNALGROUP 2"); // Enable RTCM3
			}
			if (Program.IsUM982)
			{
				_strings.Add("CONFIG SIGNALGROUP 3 6"); // Enable RTCM3
			}

			SendTopCommand();
		}

		/// <summary>
		/// Start the initialisation process for ASCII messages
		/// </summary>
		public void StartAsciiProcess()
		{
			// Don't do it if data in the queue
			if (_strings.Any())
			{
				Log.Ln("ERROR : GPS Queue Start ASCII Initialise Process - Already has data");
				return;
			}

			Log.Ln("GPS Queue Start ASCII Initialise Process");
			if (Program.IsM20)
			{
				_strings.Add("LOG GPGGA ONTIME 1");
			}
			if (Program.IsLC29H)
			{
				// This should just work
			}
			if (Program.IsUM980 || Program.IsUM982)
			{
				_strings.Add("GPGGA 1");     // Used to determine device type
			}

			SendTopCommand();
		}

		/// <summary>
		/// Extract the version information for UM98x devices
		/// </summary>
		public void ProcessUM98VersionResponse(string str)
		{
			var sections = str.Split(';');
			if (sections.Length < 1)
			{
				Log.Ln($"DANGER 301 : Unknown sections '{str}' Detected");
				return;
			}

			var parts = sections[1].Split(',');
			if (parts.Length < 5)
			{
				Log.Ln($"DANGER 302 : Unknown split '{str}' Detected");
				return;
			}
			_deviceType = parts[0];
			_deviceFirmware = parts[1];
			var serialPart = parts[3].Split('-');
			_deviceSerial = serialPart[0];

			//string command = "CONFIG SIGNALGROUP 3 6"; // Assume for UM982
			if (_deviceType == "UM982")
			{
				Log.Ln("UM982 Detected");
			}
			else if (_deviceType == "UM980")
			{
				Log.Ln("UM980 Detected");
				//command = "CONFIG SIGNALGROUP 2"; // for UM980
			}
			else
			{
				Log.Ln($"DANGER 303 Unknown Device '{_deviceType}' Detected in {str}");
			}
			//_strings.Add(command);
		}

		/// <summary>
		/// ASCII string checksum verification
		/// </summary>
		/// <param name="messageStartIndex">The index of the first character of the message UM980 includes $</param>
		public bool VerifyNmeaChecksum(string str, int messageStartIndex)
		{
			int asteriskPos = str.LastIndexOf('*');
			if (asteriskPos == -1 || asteriskPos + 3 > str.Length)
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}");
				return false;
			}

			string data = str.Substring(messageStartIndex, asteriskPos - messageStartIndex);
			string providedChecksumStr = str.Substring(asteriskPos + 1, 2);

			if (!uint.TryParse(providedChecksumStr, System.Globalization.NumberStyles.HexNumber, null, out uint providedChecksum))
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}");
				return false;
			}

			byte calculatedChecksum = CalculateChecksum(data);

			return calculatedChecksum == (byte)providedChecksum;
		}


		/// <summary>
		/// Check if this is an ack for a command sent by me
		/// </summary>
		public bool IsCommandResponse(string str)
		{
			if (!_strings.Any())
				return false;

			if (str.StartsWith("$G"))
				return false;

			if (Program.IsM20)
				return ProcessM20(str);
			else if (Program.IsLC29H)
				return ProcessLC29H(str);
			else if (Program.IsUM980 || Program.IsUM982)
				return ProcessUM98x(str);
			else
				Log.Ln($"ERROR : Unknown GPS type {str}");

			return true;
		}

		/// <summary>
		/// Process M20 packet. A good response is
		///		<OK or 	<LOGLIST
		/// </summary>
		private bool ProcessM20(string str)
		{
			// Pre Ack message '[COM1]'
			if (str.StartsWith("[COM") && str.EndsWith("]"))
				return false;

			// Log list message
			if (str.StartsWith("<LOGLIST COM"))
				return false;

			// Normal ACK
			if (str == "<OK")
			{
				AcknowledgedQueueItem();
				return true;
			}

			// Response for FRESET
			if (str.Contains("Reboot Cause: FRESET") && _strings.FirstOrDefault()?.Contains("FRESET") == true)
			{
				AcknowledgedQueueItem();
				return true;
			}

			// Manual reboot
			if (str.Contains("Reboot Cause: MANUAL REBOOT") && _strings.FirstOrDefault()?.Contains("REBOOT") == true)
			{
				AcknowledgedQueueItem();
				return true;
			}

			Log.Ln($"ERROR : M20 response {str}");
			return false;
		}

		/// <summary>
		/// Item in the queue has been acklnowledged
		/// </summary>
		private void AcknowledgedQueueItem()
		{
			_strings.RemoveAt(0);
			if (!_strings.Any())
				Log.Ln("GPS Startup Commands Complete");
			SendTopCommand();
		}

		/// <summary>
		/// LC29H packet processing
		/// </summary>
		bool ProcessLC29H(string str)
		{
			if (!VerifyNmeaChecksum(str, 1))
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}");
				return false;
			}

			// Check for command match
			string match = "$" + _strings.First();

			if (str.StartsWith("$PQTMCFGSVIN,OK"))
			{
				Log.Ln($"GPS Configured : {str}");
			}
			else if (str.StartsWith("$PQTMCFGSVIN"))
			{
				Log.Ln($"ERROR GPS NOT Configured : {str}");
			}
			else if (str.StartsWith("$PAIR001"))
			{
				if (str.Length < 15)
				{
					Log.Ln($"ERROR : PAIR001 too short {str}");
					return false;
				}
				if (match.Length < 10)
				{
					Log.Ln($"ERROR : {str} too short for PAIR");
					return false;
				}
				if (match[5] != str[9] || match[6] != str[10] || match[7] != str[11])
				{
					Log.Ln($"ERROR : PAIR001 mismatch {str} and {match}");
					return false;
				}
			}
			else
			{
				if (!str.StartsWith(match))
					return false;
			}

			_strings.RemoveAt(0);

			//if (Program.IsUM980 || Program.IsUM982)
			//	CheckForVersion(str);
			//if (match.StartsWith("$PQTMVERNO"))
			//	CheckForVersion(str);

			if (!_strings.Any())
				Log.Ln("GPS Startup Commands Complete");

			SendTopCommand();
			return true;
		}

		/// <summary>
		/// LC29H packet processing
		/// </summary>
		bool ProcessUM98x(string str)
		{
			if (str.StartsWith("#VERSION"))
			{
				ProcessUM98VersionResponse(str);
				return true;
			}

			if (!VerifyNmeaChecksum(str, 0))
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}");
				return false;
			}

			string match = "$command,";

			// Check it start correctly
			if (!str.StartsWith(match))
				return false;

			// Check for a command match
			match += _strings.First();
			match += ",response: OK*";

			if (!str.StartsWith(match, StringComparison.OrdinalIgnoreCase))
				return false;

			// Clear the sent command
			_strings.Remove(_strings.First());

			if (!_strings.Any())
				Log.Ln("GPS UM98x Commands Complete");

			// Send next command
			SendTopCommand();
			return true;
		}

		/// <summary>
		/// Check for message timeouts
		/// </summary>
		public bool CheckForTimeouts()
		{
			if (!_strings.Any())
				return false;

			if ((DateTime.Now - _timeSent).TotalMilliseconds > 8000)
			{
				Log.Ln($"E940 - Timeout on {_strings.First()}");
				SendTopCommand();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Send the first item in the queue
		/// </summary>
		void SendTopCommand()
		{
			if (!_strings.Any())
				return;
			SendCommand(_strings.First());
			_timeSent = DateTime.Now;
		}

		//////////////////////////////////////////////////////////////////////////
		// Send a command to the GPS unit
		//	Builds 
		//		PQTMVERNO
		// into
		//		$PQTMVERNO*58\r\n
		// $PAIR432,1*22
		void SendCommand(string command)
		{
			string finalCommand;
			if (Program.IsLC29H)
			{
				// Make checksum
				byte checksum = CalculateChecksum(command);

				// Final string starting with $ + command + * + checksum in hex + \r\n
				finalCommand = $"${command}*{checksum:X2}\r\n";
			}
			else
			{
				// Final string starting with $ + command + * + checksum in hex + \r\n
				finalCommand = $"{command}\r\n";
			}

			try
			{
				Log.Ln($"GPS -> '{finalCommand.ReplaceCrLfEncode()}'");
				_port.Write(finalCommand);
			}
			catch (Exception ex)
			{
				Log.Ln("Error writing to serial port: " + ex.Message);
				_port.Close();
				Environment.Exit(1);
			}
		}

		byte CalculateChecksum(string data)
		{
			byte checksum = 0;
			foreach (char c in data)
			{
				checksum ^= (byte)c;
			}
			return checksum;
		}
	}
}
