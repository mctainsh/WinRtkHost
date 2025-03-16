using System;
using System.IO.Ports;
using System.Linq;
using WinRtkHost.Models;
using WinRtkHost.Models.GPS;
using WinRtkHost.Properties;

namespace WinRtkHost
{
	class Program
	{
		static GpsParser _gpsParser;

		internal static bool IsLC29H { private set; get; }
		internal static bool IsUM980 { private set; get; }
		internal static bool IsUM982 { private set; get; }
		internal static bool IsM20 { private set; get; }

		/// <summary>
		/// Main entry point
		/// </summary>
		static void Main()
		{
			try
			{
				IsLC29H = Settings.Default.GPSReceiverType == "LC29H";
				IsUM980 = Settings.Default.GPSReceiverType == "UM980";
				IsUM982 = Settings.Default.GPSReceiverType == "UM982";
				IsM20 = Settings.Default.GPSReceiverType == "M20";

				Log.Ln($"Starting receiver {Settings.Default.GPSReceiverType}\r\n" +
							$"\t M20   : {IsM20}\r\n" +
							$"\t LC29H : {IsLC29H}\r\n" +
							$"\t UM980 : {IsUM980}\r\n" +
							$"\t UM982 : {IsUM982}");

				if (!IsM20 && !IsLC29H && !IsUM980 && !IsUM982)
				{
					Log.Ln("Unknown receiver type");
					return;
				}

				var port = RestartSerialPort();

				var lastStatus = DateTime.Now; // Slow status timer

				// Loop until 'q' key is pressed
				while (true)
				{
					try
					{
						// Check the serial port is open
						while (port is null || !port.IsOpen)
						{
							Log.Ln("Port closed");
							System.Threading.Thread.Sleep(5_000);
							port = RestartSerialPort();
						}

						// Q to exit
						if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
							break;

						System.Threading.Thread.Sleep(100);

						// Check for timeouts
						_gpsParser.CheckTimeouts();

						// TODO : Every minute display the current stats
						if ((DateTime.Now - lastStatus).TotalSeconds > 60)
						{
							lastStatus = DateTime.Now;
							LogStatus(_gpsParser);
						}
					}
					catch (Exception ex)
					{
						Log.Ln("E932: In main loop " + ex.ToString());
					}
				}
				port.Close();
				_gpsParser.Shutdown();
				Log.Ln("EXIT Via key press!");
			}
			catch (Exception ex)
			{
				Log.Ln(ex.ToString());
			}
		}

		/// <summary>
		/// Restablish the serial port comminications
		/// </summary>
		static SerialPort RestartSerialPort()
		{
			// Get the COM port
			var portName = Settings.Default.ComPort;

			// List serial ports
			var portNames = SerialPort.GetPortNames();
			if (portNames.Length == 0)
			{
				Log.Ln("ERROR : No COM ports found");
				return null;
			}

			Log.Ln("Available COM Ports:");
			foreach (var p in portNames)
			{
				if (p == portName)
					Log.Ln("\t\t" + p + " (SELECTED)");
				else
					Log.Data("\t\t" + p);
			}
			if (portName.IsNullOrEmpty())
			{
				portName = portNames[0];
				Log.Ln($" - No COM port specified (Using '{portName}')");
			}
			else if (!portNames.Contains(portName))
			{
				Log.Ln($" - Selected '{portName}' port not found");
				return null;
			}

			// Open serial port
			try
			{
				var port = new SerialPort(portName,
					IsM20 ? 9600 : 115200,
					Parity.None, 8, StopBits.One);
				port.Open();
				if (!port.IsOpen)
				{
					Log.Ln($" - FAILED to open '{portName}'");
					return null;
				}
				Log.Ln($" - Port {portName} opened");

				// Create the GPS parser
				_gpsParser?.Shutdown();
				_gpsParser = new GpsParser(port);
				return port;
			}
			catch (Exception ex)
			{
				Log.Ln("E931: Error opening port " + ex.ToString());
				return null;
			}
		}

		/// <summary>
		/// Log the current system status
		/// </summary>
		private static void LogStatus(GpsParser gpsParser)
		{
			Log.Note($"=============================================");
			foreach (var s in gpsParser.NtripCasters)
				Log.Note(s.ToString());
			Log.Note(_gpsParser.ToString());
		}
	}
}
