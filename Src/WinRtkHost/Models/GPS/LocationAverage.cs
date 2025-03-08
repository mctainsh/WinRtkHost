using System;

namespace WinRtkHost.Models.GPS
{
	public class LocationAverage
	{
		const int LOCATION_SET_SIZE = 60;

		// Large totals
		double _dLngOrg;
		double _dLatOrg;
		double _dZOrg;
		double _dLngTotal = 0;
		double _dLatTotal = 0;
		double _dZTotal = 0;
		uint _count = 0;

		// Set totals
		readonly double[] _dLats = new double[LOCATION_SET_SIZE];
		readonly double[] _dLngs = new double[LOCATION_SET_SIZE];
		readonly double[] _dZs = new double[LOCATION_SET_SIZE];
		uint _setIndex = 0;

		// True if all well
		bool _gpsConnected = false;

		/// <summary>
		/// Extract location for summing totals
		/// </summary>
		/// <param name="line">GGA line</param>
		/// <returns>Result string</returns>
		public string ProcessGGALocation(string line)
		{
			string result;
			var parts = line.Split(',');

			if (parts.Length < 14)
			{
				Log.Ln($"\tPacket length too short {parts.Length} {line}");
				return "Packet too short";
			}

			// Read time
			string time = parts[1];
			if (time.Length > 7)
			{
				time = time.Insert(4, ":").Insert(2, ":").Substring(0, 8);
			}

			// Read GPS Quality
			string quality = parts[6];
			int nQuality = 0;
			if (quality.Length > 0)
			{
				nQuality = int.Parse(quality);
			}

			// Location
			double lat = ParseLatLong(parts[2], 2, parts[3] == "S");
			double lng = ParseLatLong(parts[4], 3, parts[5] == "E");

			// Height
			if (!double.TryParse(parts[9], out double height))
				height = -1;

			// Satellite count
			string satellites = parts[7];
			//int sat = 0;
			//if (satellites.Length > 0)
			//{
			//	sat = int.Parse(satellites);
			//}

			// Skip if we have no data
			if (lng == 0.0 || lat == 0.0 || nQuality < 1)
			{
				Log.Ln($"No location data in {line}");
				return "No location";
			}

			_gpsConnected = true;

			result = $"H:{height} #{satellites} Q:{quality}";

			// Build the set totals
			if (_setIndex < LOCATION_SET_SIZE)
			{
				_dLngs[_setIndex] = lng;
				_dLats[_setIndex] = lat;
				_dZs[_setIndex] = height;
				_setIndex++;
				return result;
			}

			// Make the mean and standard deviations
			double dLngMean = 0;
			double dLatMean = 0;
			double dZMean = 0;
			LogMeanAndStandardDeviations(ref dLngMean, ref dLatMean, ref dZMean);
			LogMeanLocations();

			// Start the process
			if (_count == 0)
			{
				_dLngOrg = dLngMean;
				_dLatOrg = dLatMean;
				_dZOrg = dZMean;
			}
			else
			{
				_dLngTotal += dLngMean - _dLngOrg;
				_dLatTotal += dLatMean - _dLatOrg;
				_dZTotal += dZMean - _dZOrg;
			}
			_count++;

			return result;
		}

		/// <summary>
		/// Log the mean and standard deviations
		/// </summary>
		void LogMeanAndStandardDeviations(ref double dLngMean, ref double dLatMean, ref double dZMean)
		{
			for (int i = 0; i < LOCATION_SET_SIZE; i++)
			{
				dLngMean += _dLngs[i];
				dLatMean += _dLats[i];
				dZMean += _dZs[i];
			}
			dLngMean /= LOCATION_SET_SIZE;
			dLatMean /= LOCATION_SET_SIZE;
			dZMean /= LOCATION_SET_SIZE;

			// Calculate the standard deviation
			double dLngDev = 0;
			double dLatDev = 0;
			double dZDev = 0;
			for (int i = 0; i < LOCATION_SET_SIZE; i++)
			{
				dLngDev += (_dLngs[i] - dLngMean) * (_dLngs[i] - dLngMean);
				dLatDev += (_dLats[i] - dLatMean) * (_dLats[i] - dLatMean);
				dZDev += (_dZs[i] - dZMean) * (_dZs[i] - dZMean);
			}
			dLngDev = Math.Sqrt(dLngDev / LOCATION_SET_SIZE);
			dLatDev = Math.Sqrt(dLatDev / LOCATION_SET_SIZE);
			dZDev = Math.Sqrt(dZDev / LOCATION_SET_SIZE);

			Log.Note($"Location {_count} : {dLatMean} {dLngMean} {dZMean:F4} : {dLatDev * 1000.0} {dLngDev * 1000.0} {dZDev}");
			_setIndex = 0;
		}

		/// <summary>
		/// Log the mean locations
		/// Called after the program finishes
		/// </summary>
		internal void LogMeanLocations()
		{
			if (_count == 0)
				return;
			Log.Note($"Location Mean {_count}");
			Log.Note($"\tLatitude  {_dLatOrg + _dLatTotal / _count} ");
			Log.Note($"\tLongitude {_dLngOrg + _dLngTotal / _count} ");
			Log.Note($"\tHeight    {_dZOrg + _dZTotal / _count}m ");
		}

		/// <summary>
		/// Parse the longitude or latitude
		/// </summary>
		double ParseLatLong(string text, int degreeDigits, bool isNegative)
		{
			if (text.Length < degreeDigits)
				return 0.0;
			string degree = text.Substring(0, degreeDigits);
			string minutes = text.Substring(degreeDigits);
			double value = double.Parse(degree) + double.Parse(minutes) / 60.0;
			return isNegative ? value * -1 : value;
		}
	}
}
