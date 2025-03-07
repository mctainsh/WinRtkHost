using System.Text;

namespace WinRtkHost.Models
{
	internal static class HandyString
	{
		/// <summary>
		/// Dump the data as a line of hex
		/// </summary>
		internal static string HexAsciDump(this byte[] data, int length)
		{
			var sb = new StringBuilder();
			for (int i = 0; i < length; i++)
				sb.AppendFormat("{0:X2} ", data[i]);
			return sb.ToString();
		}

		/// <summary>
		/// Check if all the charactors are printable including CR and LF
		/// </summary>
		internal static bool IsAllAscii(this byte[] data, int length)
		{
			if (length < 1)
				return false;
			for (int i = 0; i < length; i++)
			{
				var c = (char)data[i];
				if (c == '\r' || c == '\n')
					continue;
				if (c < 32 || c > 126)
					return false;
			}
			return true;
		}

		internal static string ReplaceCrLfEncode(this string str)
		{
			string crlf = "\r\n";
			string newline = "\\r\\n";
			return str.Replace(crlf, newline);
		}

		internal static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
	}
}
