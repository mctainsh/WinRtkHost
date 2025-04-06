using System;
using System.IO;
using System.Net.NetworkInformation;

namespace WinRtkHost.Models
{
	static class Log
	{
		const string LOG_PREFIX = "WinRtkLog_";

		static internal string LogFileName { private set; get; } = string.Empty;
		static DateTime _day;
		static readonly object _lock = new object();
		static int _logLength = 0;
		static string _logFolder;
		static int _daysToKeep;
		static readonly DateTime _startTime = DateTime.Now;

		/// <summary>
		/// Enable logging with folder
		/// </summary>
		internal static void Setup(string logFolder, int logDaysToKeep)
		{
			if (string.IsNullOrEmpty(logFolder))
				_logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
			else
				_logFolder = logFolder;
			_daysToKeep = logDaysToKeep;
			Console.WriteLine("RTK Logs Location " + _logFolder);

		}

		internal static void Ln(string data) => WriteLine(data, true);
		internal static void Note(string data) => WriteLine(data, false);
		internal static void Data(string data) => WriteLine(data, true, false);

		/// <summary>
		/// Log the results to the console and to the log file
		/// </summary>
		internal static void WriteLn(string data, bool console = true) => Write(data + Environment.NewLine, console);
		internal static void Write(string data, bool console)
		{
			if (console)
				Console.Write(data);
			lock (_lock)
			{
				try
				{
					_logLength += data.Length;
					File.AppendAllText(LogFileName, data);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error writing to log file: " + ex);
				}
			}
		}
		static void WriteLine(string data, bool console, bool showDatePrefix = true)
		{
			var now = DateTime.Now;

			// Create the log folder at startup
			if (LogFileName.IsNullOrEmpty())
				Directory.CreateDirectory(_logFolder);

			// Roll over the log daily or when log gets too big
			if (_logLength > 100_000_000 || now.Date != _day.Date)
			{
				// Setup new log name
				_day = now;
				LogFileName = Path.Combine(_logFolder, $"{LOG_PREFIX}{now:yyyyMMdd_HHmmss}.txt");
				_logLength = 0;

				// Add log header
				AddLogHeader();

				// Purge old logs
				PurgeOldLogs();
			}

			// Write to disk and console
			Write((showDatePrefix ? now.ToString("HH:mm:ss.fff") + " > " : string.Empty) +
				data +
				Environment.NewLine, console);
		}

		/// <summary>
		/// Add header information to the start of the log
		/// </summary>
		private static void AddLogHeader()
		{
			var assembly = System.Reflection.Assembly.GetCallingAssembly();
			WriteLn($"*******************************************************************************");
			WriteLn($"* Windows RTK Host");
			WriteLn($"*   File         : {assembly.Location}");
			WriteLn($"*   Version      : {assembly.GetName()?.Version?.ToString()}");
			WriteLn($"* Times");
			WriteLn($"*    Local       : {DateTime.Now:dddd dd/MMMM/yyyy}");
			WriteLn($"*    UTC         : {DateTime.UtcNow:dd/MMMM/yyyy hh:mm tt}");
			WriteLn($"*    Uptime      : {(DateTime.Now-_startTime).TotalDays:0.00} days");
			WriteLn($"* Log");
			WriteLn($"*   File name    : {LogFileName}");
			WriteLn($"*   Days to keep : {_daysToKeep}");
			WriteLn($"* IP Addresses");
			try
			{
				foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
				{
					var ipProps = netInterface.GetIPProperties();
					foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
						if (addr.Address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
							WriteLn($"*                : {addr.Address,15} - {netInterface.Name}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
			WriteLn($"*******************************************************************************");
		}

		/// <summary>
		/// Get rid of the old log files
		/// </summary>
		private static void PurgeOldLogs()
		{
			try
			{
				// Find all the logs
				string[] sFileNames = Directory.GetFiles(_logFolder, LOG_PREFIX + "*.txt");
				foreach (string sFileName in sFileNames)
				{
					var dtExpired = DateTime.Now.AddDays(-_daysToKeep);
					try
					{
						var fi = new FileInfo(sFileName);
						if (fi.CreationTime < dtExpired)
							fi.Delete();
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
			}
		}
	}
}
