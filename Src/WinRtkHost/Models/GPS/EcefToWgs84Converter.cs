using System;

namespace WinRtkHost.Models.GPS
{
	public class EcefToWgs84Converter
	{
		// WGS84 constants
		private const double SemiMajorAxis = 6378137.0; // meters
		private const double Flattening = 1.0 / 298.257223563;
		private const double SemiMinorAxis = SemiMajorAxis * (1 - Flattening);
		private const double EccentricitySquared = (SemiMajorAxis * SemiMajorAxis - SemiMinorAxis * SemiMinorAxis) / (SemiMajorAxis * SemiMajorAxis);

		public static (double Latitude, double Longitude, double Altitude) Convert(double x, double y, double z)
		{
			// Calculate longitude
			double longitude = Math.Atan2(y, x);

			// Initialize variables
			double p = Math.Sqrt(x * x + y * y);
			double theta = Math.Atan2(z * SemiMajorAxis, p * SemiMinorAxis);
			double sinTheta = Math.Sin(theta);
			double cosTheta = Math.Cos(theta);

			// Calculate latitude
			double latitude = Math.Atan2(z + EccentricitySquared * SemiMinorAxis * sinTheta * sinTheta * sinTheta,
										 p - EccentricitySquared * SemiMajorAxis * cosTheta * cosTheta * cosTheta);

			// Calculate N, the radius of curvature in the prime vertical
			double sinLatitude = Math.Sin(latitude);
			double N = SemiMajorAxis / Math.Sqrt(1 - EccentricitySquared * sinLatitude * sinLatitude);

			// Calculate altitude
			double altitude = (p / Math.Cos(latitude)) - N;

			// Convert radians to degrees
			latitude = latitude * (180.0 / Math.PI);
			longitude = longitude * (180.0 / Math.PI);

			return (latitude, longitude, altitude);
		}
	}
}
