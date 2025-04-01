using System;
using System.IO;

namespace WinRtkHost.Models
{
	static class Log
	{
		static internal string LogFileName { private set; get; } = string.Empty;
		static DateTime _day;
		static readonly object _lock = new object();

		/// <summary>
		/// Log the results to the console and to the log file
		/// </summary>
		internal static void Write(string data, bool console)
		{
			if (console)
				Console.Write(data);
			lock (_lock)
			{
				try
				{
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
			const string LogFolder = "RtkLogs";
			if (LogFileName.IsNullOrEmpty())
				Directory.CreateDirectory(LogFolder);
			if (now.Date != _day.Date)
			{
				_day = now;
				LogFileName = $"{LogFolder}\\Log_{now:yyyyMMdd_HHmmss}.txt";
			}
			Write((showDatePrefix ? now.ToString("HH:mm:ss.fff") + " > " : string.Empty) +
				data +
				Environment.NewLine, console);
		}
		internal static void Ln(string data) => WriteLine(data, true);
		internal static void Note(string data) => WriteLine(data, false);
		internal static void Data(string data) => WriteLine(data, true, false);
	}
}
