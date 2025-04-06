using System;
using System.ServiceProcess;
using WinRtkHost.Models;
using WinRtkHost.Properties;

namespace WinRtkHost
{
	class Program
	{
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
				// Global exception handler
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

				// Setup logging
				var s = Settings.Default;
				Log.Setup(s.LogFolder, s.LogDaysToKeep);

				IsLC29H = s.GPSReceiverType == "LC29H";
				IsUM980 = s.GPSReceiverType == "UM980";
				IsUM982 = s.GPSReceiverType == "UM982";
				IsM20 = s.GPSReceiverType == "M20";

				Log.Ln($"Starting receiver '{s.GPSReceiverType}'\r\n" +
							$"\t M20   : {IsM20}\r\n" +
							$"\t LC29H : {IsLC29H}\r\n" +
							$"\t UM980 : {IsUM980}\r\n" +
							$"\t UM982 : {IsUM982}");

				if (!IsM20 && !IsLC29H && !IsUM980 && !IsUM982)
				{
					Log.Ln("Unknown receiver type");
					return;
				}

				// Launch the main service
				var service = new RtkMainService();
				var ServicesToRun = new ServiceBase[]
				{
					service
				};

#if DEBUG
				// vvvvvvvvvvvv  TESTING ONLY vvvvvvvvvvvv
				service.TestingStart();
				while (true)
				{
					System.Threading.Thread.Sleep(1000);
				}
				// ^^^^^^^^^^^^  TESTING ONLY ^^^^^^^^^^^^
#else
				ServiceBase.Run(ServicesToRun);

				// We are complete
				Log.Ln("Service completed");
#endif
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Log.Ln(ex.ToString());
			}
		}

		/// <summary>
		/// Global exception
		/// </summary>
		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Ln("CurrentDomain_UnhandledException *** EXCEPTION OBJECT *** " + e.ExceptionObject);
		}
	}
}
