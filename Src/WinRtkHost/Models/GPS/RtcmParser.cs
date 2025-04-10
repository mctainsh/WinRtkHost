using System;
using System.Collections.Generic;

namespace WinRtkHost.Models.GPS
{
	internal class RtcmParser
	{
		/// <summary>
		/// Dictionary of the RTK packets we have received
		/// </summary>
		readonly Dictionary<int, int> _msgTypeTotals = new Dictionary<int, int>();

		/// <summary>
		/// List of socket connection to NTRIP casters we are pusing RTK data to
		/// </summary>
		internal List<NTRIPServer> NtripCasters { get; } = new List<NTRIPServer>();

		/// <summary>
		/// Current complete packet we are working on
		/// </summary>
		byte[] _data;

		/// <summary>
		/// Load config data for casters at startup
		/// </summary>
		internal RtcmParser()
		{
			// Load NTRIP casters
			var folder = AppDomain.CurrentDomain.BaseDirectory;
			for (int index = 0; index < 1000; index++)
			{
				var caster = new NTRIPServer();
				if (!caster.LoadSettings(folder, index))
					break;
				NtripCasters.Add(caster);
			}

			// Display warning if not casters found
			if (NtripCasters.Count == 0)
			{
				Log.Ln("WARNING : No NTRIP casters found. No RTK data will be sent");
				Log.Ln($" - Checked in {AppDomain.CurrentDomain.BaseDirectory}. Only found");
				foreach (var file in System.IO.Directory.GetFiles(folder))
					Log.Ln($"   - {file.Substring(folder.Length)}");
				return;
			}
		}

		/// <summary>
		/// We have a complete packet. Parse out the fields and forward on to interested parties
		/// </summary>
		/// <returns>True if packet processed OK. False indicates we have bad data and need to dump this packet</returns>
		internal bool ProcessRtkPacket(byte[] _byteArray, int _binaryLength)
		{
			// Send to NTRIP casters (Actually just queue)
			_data = new byte[_binaryLength];
			Array.Copy(_byteArray, _data, _binaryLength);

			uint parity = GetUInt((_binaryLength - 3) * 8, 24);
			uint calculated = Crc24.RtkCrc24(_byteArray, _binaryLength);
			uint type = GetUInt(24, 12);
			if (parity != calculated)
			{
				Log.Ln($"Checksum {type} ({parity:X6} != {calculated:X6}) [{_binaryLength}] {_byteArray.HexAsciDump(_binaryLength)}");
				return false;
			}

			// Update the totals counts
			lock (_msgTypeTotals)
			{
				if (_msgTypeTotals.ContainsKey((int)type))
					_msgTypeTotals[(int)type]++;
				else
					_msgTypeTotals.Add((int)type, 1);
			}

			Log.Ln($"Rtk {type}[{_binaryLength}]");
			//Console.Write($"\r{type}[{_binaryIndex}]    \r");

			// Post to the queues
			foreach (var _caster in NtripCasters)
				_caster.Send(_data);


			//new RtcmParser(type, sendData);

			return true;
		}

		/// <summary>
		/// Show the message counts
		/// </summary>
		internal string GetMessageTypeCounts()
		{
			string counts = "";
			lock (_msgTypeTotals)
			{
				foreach (var item in _msgTypeTotals)
					counts += $"\t{item.Key} : {item.Value:N0}{GpsParser.NL}";
			}
			return counts;
		}

		/// <summary>
		/// Log the current system status
		/// </summary>
		internal void LogStatus()
		{
			foreach (var s in NtripCasters)
				Log.Note(s.ToString());
		}

		/// <summary>
		/// Kill off all the servers
		/// </summary>
		internal void Shutdown()
		{
			foreach (var _caster in NtripCasters)
				_caster.Shutdown();
		}

		internal RtcmParser(uint type, byte[] sendData)
		{
			_data = sendData;
			switch (type)
			{
				case 1005: Decode1005(); break;
			}
		}

		/*
 Contents of the Type 1005 Message – Stationary Antenna Reference Point, No Height Information
DATA FIELD DF
NUMBER
DATA
TYPE
NO. OF
BITS
Message Number (“1005”= 0011 1110 1101) DF002 uint12 12
Reference Station ID DF003 uint12 12
Reserved for ITRF Realization Year DF021 uint6 6
GPS Indicator DF022 bit(1) 1
GLONASS Indicator DF023 bit(1) 1
Reserved for Galileo Indicator DF024 bit(1) 1
Reference-Station Indicator DF141 bit(1) 1
Antenna Reference Point ECEF-X DF025 int38 38
Single Receiver Oscillator Indicator DF142 bit(1) 1
Reserved DF001 bit(1) 1
Antenna Reference Point ECEF-Y DF026 int38 38
Quarter Cycle Indicator DF364 bit(2) 2
Antenna Reference Point ECEF-Z DF027 int38 38
TOTAL 152
 */
		void Decode1005()
		{
			var index = 24;
			var msgNo = GetUInt(ref index, 12);
			var stationId = GetUInt(ref index, 12);
			var year = GetUInt(ref index, 6);

			var gpsIndicator = GetUInt(ref index, 1); // GPS Indicator
			var glonassIndicator = GetUInt(ref index, 1); // GLONASS Indicator
			var reservedGalileo = GetUInt(ref index, 1); // Reserved for Galileo Indicator
			var referenceStationIndicator = GetUInt(ref index, 1); // Reference-Station Indicator
			var ecefX = GetInt64(ref index, 38); // Antenna Reference Point ECEF-X
			var singleReceiverOscillatorIndicator = GetUInt(ref index, 1); // Single Receiver Oscillator Indicator
			var reserved = GetUInt(ref index, 1); // Reserved
			var ecefY = GetInt64(ref index, 38); // Antenna Reference Point ECEF-Y
												 //var ecefY = (Int64)GetUInt64(ref index, 38) * -1; // Antenna Reference Point ECEF-Y
			var quarterCycleIndicator = GetUInt(ref index, 2); // Quarter Cycle Indicator
			var ecefZ = GetInt64(ref index, 38); // Antenna Reference Point ECEF-Z

			// Process the decoded values as needed


			(double Latitude, double Longitude, double Altitude) = EcefToWgs84Converter.Convert(ecefX* 0.0001, ecefY* 0.0001, ecefZ * 0.0001);

			Log.Ln($"Message Number: {msgNo}");
			Log.Ln($"Reference Station ID: {stationId}");
			Log.Ln($"ITRF Realization Year: {year}");
			Log.Ln($"GPS Indicator: {gpsIndicator}");
			Log.Ln($"GLONASS Indicator: {glonassIndicator}");
			Log.Ln($"Reserved for Galileo Indicator: {reservedGalileo}");
			Log.Ln($"Reference-Station Indicator: {referenceStationIndicator}");
			Log.Ln($"Antenna Reference Point ECEF-X: {ecefX} {Longitude}");
			Log.Ln($"Single Receiver Oscillator Indicator: {singleReceiverOscillatorIndicator}");
			Log.Ln($"Reserved: {reserved}");
			Log.Ln($"Antenna Reference Point ECEF-Y: {ecefY} {Latitude}");
			Log.Ln($"Quarter Cycle Indicator: {quarterCycleIndicator}");
			Log.Ln($"Antenna Reference Point ECEF-Z: {ecefZ} {Altitude}");

		}

		UInt64 GetUInt64(ref int pos, int len)
		{
			if (((pos + len) / 8) >= _data.Length)
			{
				Console.WriteLine("Overflow!!!!!!!");
				return 0;
			}

			UInt64 bits = 0;
			for (int i = pos; i < pos + len; i++)
				bits = (UInt64)((bits << 1) + (UInt64)(_data[i / 8] >> 7 - i % 8 & 1u));
			pos += len;
			return bits;
		}

		UInt32 GetUInt(int pos, int len) => GetUInt(ref pos, len);
		UInt32 GetUInt(ref int pos, int len)
		{
			if (((pos + len) / 8) > _data.Length)
			{
				Console.WriteLine("Overflow!!!!!!!");
				return 0;
			}

			UInt32 bits = 0;
			for (int i = pos; i < pos + len; i++)
				bits = (UInt32)((bits << 1) + (UInt64)(_data[i / 8] >> 7 - i % 8 & 1u));
			pos += len;
			return bits;
		}

		/// <summary>
		/// Get a signed integer value from the byte array starting at the specified bit index.
		/// </summary>
		/// <param name="index">The starting bit index.</param>
		/// <param name="length">The number of bits to read.</param>
		/// <returns>The signed integer value.</returns>
		Int64 GetInt64(ref int index, int length)
		{
			Int64 value = 0;
			bool isNegative = (_data[index / 8] & (1 << (7 - (index % 8)))) != 0;
			if (isNegative)
			{
				value = -1;
			}
			for (int i = 0; i < length; i++)
			{
				value = (Int64)(value << 1) | (Int64)((_data[index / 8] >> (7 - (index % 8))) & 1);
				index++;
			}
			return value;
		}

	}
}
