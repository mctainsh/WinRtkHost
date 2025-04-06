using System;
using System.IO.Ports;
using System.Linq;
using System.ServiceProcess;
using WinRtkHost.Models;
using WinRtkHost.Models.GPS;
using WinRtkHost.Properties;

namespace WinRtkHost
{
	partial class RtkMainService : ServiceBase
	{
		internal static RtkMainService Instance { private set; get; }

		static GpsParser _gpsParser;

		/// <summary>
		/// Flags we are to terminate the service
		/// </summary>
		bool _keepRunning = true;

		public RtkMainService()
		{
			ServiceName = "WinRtkHostService";
			EventLog.Source = ServiceName;

			Instance = this;
			InitializeComponent();
		}

		/// <summary>
		/// Start if we are running in debug
		/// </summary>
		internal void TestingStart() => OnStart(null);
		internal void TestingStop() => OnStop();

		/// <summary>
		/// Start the service
		/// </summary>
		/// <param name="args"></param>
		protected override void OnStart(string[] args)
		{
			try
			{
				// Load the startup and restart configs
				new GpsCommandQueue(null);

				// Start the main worker thread
				Log.Ln("Starting main worker thread...");
				_keepRunning = true;
				var workerThread = new System.Threading.Thread(MainWorkerThread)
				{
					IsBackground = true
				};
				workerThread.Start();
				Log.Ln("Main worker thread started OK");
			}
			catch (Exception ex)
			{
				Log.Ln(ex.ToString());
				Stop();
			}
		}

		/// <summary>
		/// Terminate the background thread and terminate the service
		/// </summary>
		protected override void OnStop()
		{
			Log.Ln("Stopping service...");
			_keepRunning = false;
			_gpsParser.Shutdown();
		}

		/// <summary>
		/// Worker thread
		/// </summary>
		void MainWorkerThread()
		{
			var lastStatus = DateTime.Now; // Slow status timer

			var port = RestartSerialPort();

			while (_keepRunning)
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

					System.Threading.Thread.Sleep(100);

					// Check for timeouts
					_gpsParser.CheckTimeouts();

					// TODO : Every minute display the current stats
					if ((DateTime.Now - lastStatus).TotalSeconds > 60)
					{
						lastStatus = DateTime.Now;
						_gpsParser.LogStatus();
					}
				}
				catch (Exception ex)
				{
					Log.Ln("E932: In main loop " + ex.ToString());
				}
			}
			port.Close();
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
					115200,
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
				Log.Ln("E931: Error opening port " + ex.Message);
				return null;
			}
		}
	}
}
