using System;
using System.IO;

namespace WinRtkHost.Models
{
	static class Log
	{
		static internal string LogFileName { private set; get; }
		static DateTime _day;
		static object _lock = new object();

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
		static void WriteLine(string data, bool console)
		{
			var now = DateTime.Now;
			if (now.Day != _day.Day)
			{
				_day = now;
				LogFileName = $"RtkLogs\\Log_{now:yyyyMMdd_HHmmss}.txt";
				Directory.CreateDirectory("RtkLogs");
			}
			Write(now.ToString("HH:mm:ss.fff") + " > "+ data + Environment.NewLine, console);
		}
		internal static void Ln(string data) => WriteLine(data, true);
		internal static void Note(string data) => WriteLine(data, false);
		internal static void Data(string data)
		{
			var now = DateTime.Now;
			if (now.Day != _day.Day)
			{
				_day = now;
				LogFileName = $"Log_{now:yyyyMMdd_HHmmss}.txt";
			}
			Write(now.ToString("HH:mm:ss.fff") + " > "+ data + Environment.NewLine, true);
		}
	}
}
