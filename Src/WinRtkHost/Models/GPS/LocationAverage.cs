using System;
using System.Collections.Generic;
using System.Globalization;

namespace WinRtkHost.Models.GPS
{
	/// <summary>
	/// Average the location over time. 
	/// Note : This may suffer from the law of large numbers so after a certain time the average may not be accurate
	/// </summary>
	public class LocationAverage
	{
		/// <summary>
		/// Convert standard deviation to millimeters
		/// </summary>
		const double MM_PER_DEGREE = 111_320_000.0;

		/// <summary>
		/// 2 days to stablise the location
		/// </summary>
		const int MAX_POINTS = 48 * 60 * 60;

		// Set totals
		readonly List<GeoPoint> _points = new List<GeoPoint>();

		/// <summary>
		/// Extract location for summing totals
		/// </summary>
		/// <param name="line">GGA line</param>
		public void ProcessGGALocation(string line)
		{
			try
			{
				var parts = line.Split(',');
				if (parts.Length < 14)
				{
					Log.Ln($"\tPacket length too short {parts.Length} {line}");
					return;
				}

				// Read time
				//string time = parts[1];
				//if (time.Length > 7)
				//{
				//	time = time.Insert(4, ":").Insert(2, ":").Substring(0, 8);
				//}

				// Read GPS Quality
				string quality = parts[6];
				int nQuality = 0;
				if (quality.Length > 0)
					nQuality = int.Parse(quality);

				// Location
				double lat = ParseLatLong(parts[2], 2, parts[3] == "S");
				double lng = ParseLatLong(parts[4], 3, parts[5] == "E");

				// Height
				if (!double.TryParse(parts[9], NumberStyles.Any, CultureInfo.InvariantCulture, out double height))
					height = -1;

				// Satellite count
				//string satellites = parts[7];
				//int sat = 0;
				//if (satellites.Length > 0)
				//{
				//	sat = int.Parse(satellites);
				//}

				// Skip if we have no data
				if (lng == 0.0 || lat == 0.0 || nQuality < 1)
				{
					Log.Ln($"No location data in {line}");
					return;
				}

				// We have a good result
				Log.Note(line);

				//_gpsConnected = true;

				//Log.Note($"H:{height} #{satellites} Q:{quality}");

				// Truncate the list
				while (_points.Count > MAX_POINTS)
					_points.RemoveAt(0);

				// Don't average fixed locations
				if (7 != nQuality)
					_points.Add(new GeoPoint { Latitude = lat, Longitude = lng, Height = height });
			}
			catch (Exception ex)
			{
				Log.Ln($"E408:'{line}'" + ex.ToString());
			}
		}

		/// <summary>
		/// Log the mean and standard deviations
		/// </summary>
		internal string LogMeanAndStandardDeviations()
		{
			var count = _points.Count;
			if (count < 1)
				return "No data";
			double dLngMean = 0;
			double dLatMean = 0;
			double dZMean= 0;

			// Calculate the mean
			foreach (var p in _points)
			{
				dLngMean += p.Longitude;
				dLatMean += p.Latitude;
				dZMean += p.Height;
			}
			dLngMean /= count;
			dLatMean /= count;
			dZMean /= count;

			// Calculate the standard deviation
			double dLngDev = 0;
			double dLatDev = 0;
			double dZDev = 0;
			foreach (var p in _points)
			{
				dLngDev += (p.Longitude - dLngMean) * (p.Longitude - dLngMean);
				dLatDev += (p.Latitude - dLatMean) * (p.Latitude - dLatMean);
				dZDev += (p.Height - dZMean) * (p.Height - dZMean);
			}
			dLngDev = Math.Sqrt(dLngDev / count);
			dLatDev = Math.Sqrt(dLatDev / count);
			dZDev = Math.Sqrt(dZDev / count);

			return ($"Pnts:{count} Lat:{dLatMean}° Lng:{dLngMean}° Z:{dZMean:F4}m SD : {dLatDev * MM_PER_DEGREE:N0}mm {dLngDev * MM_PER_DEGREE:N0}mm {dZDev*1000:N0}mm");
		}

		/// <summary>
		/// Parse the longitude or latitude
		/// </summary>
		double ParseLatLong(string text, int degreeDigits, bool isNegative)
		{
			if (text.Length < degreeDigits + 2)
				return 0.0;
			string degree = text.Substring(0, degreeDigits);
			string minutes = text.Substring(degreeDigits);
			double value = double.Parse(degree, CultureInfo.InvariantCulture) + double.Parse(minutes, CultureInfo.InvariantCulture) / 60.0;
			return isNegative ? value * -1 : value;
		}
	}
}
