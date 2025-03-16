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
		/// Convert an array of bytes to a string of hex numbers and ASCII characters
		/// Format: "00 01 02 03 04 05 06 07-08 09 0a 0b 0c 0d 0e 0f 0123456789abcdef"
		///          0123456789012345678901234567890123456789012345678901234567890123
		///          0         1         2         3         4         5         6
		/// </summary>
		/// <param name="data">The byte array to convert</param>
		/// <param name="len">The length of the byte array</param>
		/// <returns>A formatted string representing the hex and ASCII values</returns>
		internal static string HexAsciDumpDetail(this byte[] data, int len)
		{
			if (len == 0)
				return string.Empty;

			var lines = new StringBuilder();
			const int SIZE = 66;
			var szText = new char[SIZE + 1];
			for (int n = 0; n < len; n++)
			{
				int index = n % 16;
				if (index == 0)
				{
					szText[SIZE-1] = '\0';
					if (n > 0)
						lines.Append(new string(szText).Trim('\0') + "\r\n");

					// Fill the szText with spaces
					for (int i = 0; i < SIZE; i++)
						szText[i] = ' ';
				}

				// Add the hex value to szText at position 3 * n
				var offset = 3 * index;
				var hexValue = data[n].ToString("x2");
				szText[offset] = hexValue[0];
				szText[offset + 1] = hexValue[1];
				szText[offset + 2] = ' ';
				szText[3 * 16 + 1 + index] = data[n] < 0x20 ? '·' : (char)data[n];
			}
			szText[SIZE-1] = '\0';
			lines.Append(new string(szText).Trim('\0'));
			return ("\r\n" + lines.ToString()).Indent(2);
		}

		internal static string Indent(this string str, int tab)
		{
			var tabStr = new string('\t', tab);
			return str.Replace("\n", "\n" + tabStr);
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

		/// <summary>
		/// Conver \r and \n to \\r and \\n
		/// </summary>
		internal static string ReplaceCrLfEncode(this string str)
		{
			string crlf = "\r\n";
			string newline = "\\r\\n";
			return str.Replace(crlf, newline);
		}

		internal static bool IsNullOrEmpty(this string str) => string.IsNullOrEmpty(str);
	}
}
