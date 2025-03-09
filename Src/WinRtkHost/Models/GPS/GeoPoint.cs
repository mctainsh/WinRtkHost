namespace WinRtkHost.Models.GPS
{
	/// <summary>
	/// Represents a single point in space
	/// </summary>
	internal class GeoPoint
	{
		internal double Latitude { get; set; }
		internal double Longitude { get; set; }
		internal double Height { get; set; }
	}
}
