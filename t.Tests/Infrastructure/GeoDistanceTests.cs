using t.Infrastructure.Geo;

namespace t.Tests.Infrastructure;

public class GeoDistanceTests
{
    [Theory]
    [InlineData(null, null, true, false)]
    [InlineData(10.0, null, false, false)]
    [InlineData(null, 106.0, false, false)]
    [InlineData(91.0, 106.0, false, false)]
    [InlineData(10.0, 181.0, false, false)]
    [InlineData(10.0, 106.0, true, true)]
    public void ValidatePair_ShouldReportValidityAndActivation(
        double? latitude,
        double? longitude,
        bool expectedValid,
        bool expectedActive)
    {
        var result = GeoDistance.ValidatePair(latitude, longitude);

        Assert.Equal(expectedValid, result.IsValid);
        Assert.Equal(expectedActive, result.IsActive);
    }

    [Theory]
    [InlineData(double.NaN, 106.0)]
    [InlineData(double.PositiveInfinity, 106.0)]
    [InlineData(double.NegativeInfinity, 106.0)]
    public void ValidatePair_ShouldRejectNonFiniteCoordinates(double latitude, double longitude)
    {
        var result = GeoDistance.ValidatePair(latitude, longitude);

        Assert.False(result.IsValid);
        Assert.False(result.IsActive);
    }

    [Fact]
    public void CalculateKm_ShouldReturnKnownApproximateDistance()
    {
        var distance = GeoDistance.CalculateKm(10.7942, 106.7219, 10.7952, 106.7193);

        Assert.InRange(distance, 0.29, 0.32);
    }

    [Theory]
    [InlineData(0.85, "Cách 850 m")]
    [InlineData(2.44, "Cách 2,4 km")]
    public void FormatKm_ShouldUseCompactVietnameseLabel(double distance, string expected)
    {
        Assert.Equal(expected, GeoDistance.FormatKm(distance));
    }
}
