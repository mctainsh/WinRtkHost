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
					Log.Ln($"No COM port specified (Using '{portName}')");
				}
				else if (!ports.Contains(portName))
				{
					portName = ports[0];
					Log.Ln($"Selected '{portName}' port not found (Using '{portName}')");
				}

				SerialPort port = RestartSerialPort(portName);

				var lastStatus = DateTime.Now; // Slow status timer

				// Loop until 'q' key is pressed
				while (true)
				{
					try
					{
						// Check the serial port is open
						if (!port.IsOpen)
						{
							Log.Ln("Port closed");
							System.Threading.Thread.Sleep(5_000);
							port = RestartSerialPort(portName);
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
		static SerialPort RestartSerialPort(string portName)
		{
			// Open serial port
			var port = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
			port.Open();
			Log.Ln($"Port {portName} opened");

			// Create the GPS parser
			if (_gpsParser != null)
				_gpsParser.Shutdown();
			_gpsParser = new GpsParser(port);
			return port;
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
