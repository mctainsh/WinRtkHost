using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
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

		public GpsCommandQueue(SerialPort port)
		{
			_port = port;
		}

		public string GetDeviceType() => _deviceType;
		public string GetDeviceFirmware() => _deviceFirmware;
		public string GetDeviceSerial() => _deviceSerial;

		/// <summary>
		/// Start the initialisation process for RTK messages
		/// </summary>
		public void StartRtkInitialiseProcess()
		{
			Log.Ln("GPS Queue Start RTK Initialise Process");
			_strings.Clear();

			if (Program.IsLC29H)
			{
				_strings.Add("PQTMVERNO");
				_strings.Add("PQTMCFGSVIN,W,1,43200,0,0,0,0");
				_strings.Add("PAIR432,1");
				_strings.Add("PAIR434,1");
				_strings.Add("PAIR436,1");
			}
			if (Program.IsUM980 || Program.IsUM982)
			{
				// Setup RTCM V3
				_strings.Add("VERSION");     // Used to determine device type
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
					_strings.Add("MODE BASE TIME 600 1");                             // Set base mode with 10 minute startup and 1m optimized save error
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
			Log.Ln("GPS Queue Start ASCII Initialise Process");
			_strings.Clear();

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

		public void CheckForVersion(string str)
		{
			if (!str.StartsWith("#VERSION"))
				return;

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

			string command = "CONFIG SIGNALGROUP 3 6"; // Assume for UM982
			if (_deviceType == "UM982")
			{
				Log.Ln("UM982 Detected");
			}
			else if (_deviceType == "UM980")
			{
				Log.Ln("UM980 Detected");
				command = "CONFIG SIGNALGROUP 2"; // for UM980
			}
			else
			{
				Log.Ln($"DANGER 303 Unknown Device '{_deviceType}' Detected in {str}");
			}
			_strings.Add(command);
		}

		/// <summary>
		/// ASCII string checksum verification
		/// </summary>
		public bool VerifyChecksumLC29H(string str)
		{
			int asteriskPos = str.LastIndexOf('*');
			if (asteriskPos == -1 || asteriskPos + 3 > str.Length)
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}");
				return false;
			}

			string data = str.Substring(1, asteriskPos - 1);
			string providedChecksumStr = str.Substring(asteriskPos + 1, 2);

			if (!uint.TryParse(providedChecksumStr, System.Globalization.NumberStyles.HexNumber, null, out uint providedChecksum))
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}");
				return false;
			}

			byte calculatedChecksum = CalculateChecksum(data);

			return calculatedChecksum == (byte)providedChecksum;
		}
		bool VerifyChecksumUM98x(string str)
		{
			int asteriskPos = str.LastIndexOf('*');
			if (asteriskPos == -1 || asteriskPos + 3 > str.Length)
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}. Invalid format");
				return false; // Invalid format
			}

			// Extract the data and the checksum
			string data = str.Substring(0, asteriskPos);
			string providedChecksumStr = str.Substring(asteriskPos + 1, 2);

			// Convert the provided checksum from hex to an integer
			if (!uint.TryParse(providedChecksumStr, System.Globalization.NumberStyles.HexNumber, null, out uint providedChecksum))
			{
				Log.Ln($"ERROR : GPS Checksum error in {str}. Invalid checksum");
				return false;
			}

			// Calculate the checksum of the data
			byte calculatedChecksum = CalculateChecksum(data);
			return calculatedChecksum == (byte)providedChecksum;
		}



		public bool IsCommandResponse(string str)
		{
			if (!_strings.Any())
				return false;

			if (str.StartsWith("$G"))
				return false;

			if (Program.IsLC29H)
				return ProcessLC29H(str);
			else if (Program.IsUM980 || Program.IsUM982)
				return ProcessUM98x(str);
			else
				Log.Ln($"ERROR : Unknown GPS type {str}");

			return true;
		}

		/// <summary>
		/// LC29H packet processing
		/// </summary>
		bool ProcessLC29H(string str)
		{
			if (!VerifyChecksumLC29H(str))
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
				return true;

			if (!VerifyChecksumUM98x(str))
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
			{
				Log.Ln("GPS Startup Commands Complete");
			}

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
