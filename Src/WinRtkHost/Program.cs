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

		internal static bool IsLC29H => Settings.Default.GPSReceiverType == "LC29H";
		internal static bool IsUM980 => Settings.Default.GPSReceiverType == "UM980";
		internal static bool IsUM982 => Settings.Default.GPSReceiverType == "UM982";

		/// <summary>
		/// Main entry point
		/// </summary>
		static void Main()
		{
			try
			{
				Log.Ln($"Starting receiver {Settings.Default.GPSReceiverType}\r\n" +
					$"\t LC29H : {IsLC29H}\r\n" +
					$"\t UM980 : {IsUM980}\r\n" +
					$"\t UM982 : {IsUM982}");

				if (!IsLC29H && !IsUM980 && !IsUM982)
				{
					Log.Ln("Unknown receiver type");
					return;
				}

				// Get the COM port
				var portName = Settings.Default.ComPort;

				// List serial ports
				var ports = SerialPort.GetPortNames();
				if (ports.Length == 0)
				{
					Log.Ln("ERROR : No COM ports found");
					return;
				}

				Log.Ln("Available COM Ports:");
				foreach (var p in ports)
				{
					if (p == portName)
						Log.Ln("\t" + p + " (SELECTED)");
					else
						Log.Data("\t" + p);
				}
				if (portName.IsNullOrEmpty())
				{
					portName = ports[0];
					Log.Ln($"No COM port specified (Will use '{portName}')");
				}
				else if (!ports.Contains(portName))
				{
					portName = ports[0];
					Log.Ln($"Selected '{portName}' port not found (Will use '{portName}')");
				}

				// Open serial port
				var port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
				port.Open();
				Log.Ln($"Port {portName} opened");

				// Create the GPS parser
				_gpsParser = new GpsParser(port);

				var lastStatus = DateTime.Now; // Slow status timer

				// Loop until 'q' key is pressed
				while (true)
				{
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

					// Do nothing, just loop
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
