using System.Globalization;

namespace t.Infrastructure.Geo;

public readonly record struct GeoCoordinatePairValidation(
    bool IsValid,
    bool IsActive,
    string? Error);

public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0088;

    public static GeoCoordinatePairValidation ValidatePair(double? latitude, double? longitude)
    {
        if (!latitude.HasValue && !longitude.HasValue)
            return new GeoCoordinatePairValidation(true, false, null);

        if (!latitude.HasValue || !longitude.HasValue)
            return new GeoCoordinatePairValidation(false, false, "Vui lòng cung cấp đầy đủ vĩ độ và kinh độ.");

        if (!IsValidCoordinate(latitude, longitude))
            return new GeoCoordinatePairValidation(false, false, "Vị trí không hợp lệ.");

        return new GeoCoordinatePairValidation(true, true, null);
    }

    public static bool IsValidCoordinate(double? latitude, double? longitude)
    {
        return latitude.HasValue &&
               longitude.HasValue &&
               double.IsFinite(latitude.Value) &&
               double.IsFinite(longitude.Value) &&
               latitude.Value is >= -90 and <= 90 &&
               longitude.Value is >= -180 and <= 180;
    }

    public static double CalculateKm(
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude)
    {
        var fromLatitudeRadians = DegreesToRadians(fromLatitude);
        var toLatitudeRadians = DegreesToRadians(toLatitude);
        var latitudeDelta = DegreesToRadians(toLatitude - fromLatitude);
        var longitudeDelta = DegreesToRadians(toLongitude - fromLongitude);

        var haversine =
            Math.Pow(Math.Sin(latitudeDelta / 2), 2) +
            Math.Cos(fromLatitudeRadians) *
            Math.Cos(toLatitudeRadians) *
            Math.Pow(Math.Sin(longitudeDelta / 2), 2);

        return EarthRadiusKm * 2 * Math.Atan2(Math.Sqrt(haversine), Math.Sqrt(1 - haversine));
    }

    public static string FormatKm(double distanceKm)
    {
        if (distanceKm < 1)
            return $"Cách {Math.Round(distanceKm * 1000):0} m";

        return $"Cách {distanceKm.ToString("0.0", CultureInfo.GetCultureInfo("vi-VN"))} km";
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
}
