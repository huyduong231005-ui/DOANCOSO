using t.Application.Queries.Rentals;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Tests.Application;

public sealed class RentalMatchScorerTests
{
    [Fact]
    public void Score_ShouldRejectCandidate_WhenRequiredAmenityIsMissing()
    {
        var draft = Draft(amenityIds: new HashSet<int> { 3, 7 }, requiredAmenityIds: new HashSet<int> { 7 });
        var result = RentalMatchScorer.Score(Candidate(amenityIds: new HashSet<int> { 3, 4 }), draft);

        Assert.False(result.IsEligible);
    }

    [Fact]
    public void Score_ShouldReturnPercentageAndAtMostThreeVietnameseReasons()
    {
        var draft = Draft(
            minPrice: 8_000_000,
            maxPrice: 20_000_000,
            minBedrooms: 2,
            furnishingLevel: FurnishingLevel.FullyFurnished,
            preferredLatitude: 10.7942,
            preferredLongitude: 106.7219,
            maxDistanceKm: 5);

        var result = RentalMatchScorer.Score(Candidate(), draft);

        Assert.True(result.IsEligible);
        Assert.InRange(result.ScorePercent, 1, 100);
        Assert.InRange(result.Reasons.Count, 1, 3);
        Assert.All(result.Reasons, reason => Assert.False(string.IsNullOrWhiteSpace(reason)));
    }

    [Fact]
    public void Score_ShouldReturnOneHundred_WhenOnlyRequiredCriteriaMatch()
    {
        var draft = Draft(minBedrooms: 2, requiredCriteria: new HashSet<string> { "bedrooms" });
        var result = RentalMatchScorer.Score(Candidate(bedrooms: 2), draft);

        Assert.True(result.IsEligible);
        Assert.Equal(100, result.ScorePercent);
    }

    [Theory]
    [InlineData("region")]
    [InlineData("priceRange")]
    [InlineData("areaRange")]
    [InlineData("bedrooms")]
    [InlineData("category")]
    [InlineData("maxDistance")]
    [InlineData("furnishing")]
    [InlineData("pets")]
    [InlineData("parking")]
    [InlineData("moveInDate")]
    [InlineData("floorRange")]
    [InlineData("direction")]
    [InlineData("leaseRange")]
    public void Score_ShouldRejectCandidate_WhenRequiredScalarDoesNotMatch(string requiredCriterion)
    {
        var draft = Draft(
            regionId: 2,
            minPrice: 8_000_000,
            maxPrice: 20_000_000,
            minArea: 40,
            maxArea: 90,
            minBedrooms: 2,
            categoryIds: new HashSet<int> { 2 },
            furnishingLevel: FurnishingLevel.FullyFurnished,
            allowsPets: true,
            parkingType: ParkingType.Car,
            availableBy: new DateOnly(2026, 6, 2),
            preferredLatitude: 10.7942,
            preferredLongitude: 106.7219,
            maxDistanceKm: 1,
            minFloor: 5,
            maxFloor: 10,
            houseDirection: HouseDirection.East,
            minLeaseMonths: 12,
            maxLeaseMonths: 24,
            requiredCriteria: new HashSet<string> { requiredCriterion });

        var result = RentalMatchScorer.Score(
            Candidate(
                regionId: 1,
                price: 30_000_000,
                area: 20,
                bedrooms: 1,
                categoryId: 1,
                furnishingLevel: FurnishingLevel.Basic,
                allowsPets: false,
                parkingType: ParkingType.Motorbike,
                availableFrom: new DateOnly(2026, 7, 1),
                floorNumber: 2,
                houseDirection: HouseDirection.West,
                minLeaseMonths: 1,
                maxLeaseMonths: 6,
                latitude: 10.9,
                longitude: 106.9),
            draft);

        Assert.False(result.IsEligible);
    }

    [Fact]
    public void Score_ShouldCompareFurnishingAndParkingAsOrderedLevels()
    {
        var draft = Draft(
            furnishingLevel: FurnishingLevel.Basic,
            parkingType: ParkingType.Motorbike,
            requiredCriteria: new HashSet<string> { "furnishing", "parking" });

        var result = RentalMatchScorer.Score(
            Candidate(
                furnishingLevel: FurnishingLevel.FullyFurnished,
                parkingType: ParkingType.Car),
            draft);

        Assert.True(result.IsEligible);
        Assert.Equal(100, result.ScorePercent);
    }

    private static RentalPreferenceDraft Draft(
        int? regionId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        double? minArea = null,
        double? maxArea = null,
        int? minBedrooms = null,
        IReadOnlySet<int>? categoryIds = null,
        IReadOnlySet<int>? amenityIds = null,
        IReadOnlySet<int>? requiredAmenityIds = null,
        FurnishingLevel? furnishingLevel = null,
        bool? allowsPets = null,
        ParkingType? parkingType = null,
        DateOnly? availableBy = null,
        double? preferredLatitude = null,
        double? preferredLongitude = null,
        double? maxDistanceKm = null,
        int? minFloor = null,
        int? maxFloor = null,
        HouseDirection? houseDirection = null,
        int? minLeaseMonths = null,
        int? maxLeaseMonths = null,
        IReadOnlySet<string>? requiredCriteria = null)
    {
        return new RentalPreferenceDraft
        {
            RegionId = regionId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinArea = minArea,
            MaxArea = maxArea,
            MinBedrooms = minBedrooms,
            CategoryIds = categoryIds ?? new HashSet<int>(),
            AmenityIds = amenityIds ?? new HashSet<int>(),
            RequiredAmenityIds = requiredAmenityIds ?? new HashSet<int>(),
            FurnishingLevel = furnishingLevel,
            AllowsPets = allowsPets,
            ParkingType = parkingType,
            AvailableBy = availableBy,
            PreferredLatitude = preferredLatitude,
            PreferredLongitude = preferredLongitude,
            MaxDistanceKm = maxDistanceKm,
            MinFloor = minFloor,
            MaxFloor = maxFloor,
            HouseDirection = houseDirection,
            MinLeaseMonths = minLeaseMonths,
            MaxLeaseMonths = maxLeaseMonths,
            RequiredCriteria = requiredCriteria ?? new HashSet<string>()
        };
    }

    private static RentalMatchCandidate Candidate(
        int regionId = 1,
        decimal price = 12_000_000,
        double area = 65,
        int bedrooms = 2,
        int categoryId = 2,
        IReadOnlySet<int>? amenityIds = null,
        FurnishingLevel furnishingLevel = FurnishingLevel.FullyFurnished,
        bool allowsPets = true,
        ParkingType parkingType = ParkingType.Car,
        DateOnly? availableFrom = null,
        int minLeaseMonths = 6,
        int maxLeaseMonths = 24,
        int? floorNumber = 8,
        HouseDirection? houseDirection = HouseDirection.East,
        double? latitude = 10.7952,
        double? longitude = 106.7193)
    {
        return new RentalMatchCandidate(
            1,
            DateTime.UtcNow,
            regionId,
            price,
            area,
            bedrooms,
            categoryId,
            amenityIds ?? new HashSet<int> { 3, 7 },
            furnishingLevel,
            allowsPets,
            parkingType,
            availableFrom ?? new DateOnly(2026, 6, 1),
            minLeaseMonths,
            maxLeaseMonths,
            floorNumber,
            houseDirection,
            latitude,
            longitude);
    }
}

public sealed class RentalPreferenceNormalizerTests
{
    [Fact]
    public void Normalize_ShouldRejectUnknownRequiredCriterion_InStrictMode()
    {
        var result = RentalPreferenceNormalizer.Normalize(
            new RentalSearchRequest { RequiredCriteria = ["unknown"] },
            strict: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("unknown"));
    }

    [Fact]
    public void Normalize_ShouldDropUnknownAndValuelessRequiredCriteria_InMvcMode()
    {
        var result = RentalPreferenceNormalizer.Normalize(
            new RentalSearchRequest { RequiredCriteria = ["unknown", "parking"] },
            strict: false);

        Assert.True(result.IsValid);
        Assert.Empty(result.Draft.RequiredCriteria);
        Assert.Equal(2, result.Warnings.Count);
    }

    [Fact]
    public void Normalize_ShouldRejectRequiredAmenitiesOutsideDesiredAmenities()
    {
        var result = RentalPreferenceNormalizer.Normalize(
            new RentalSearchRequest
            {
                AmenityIds = [3],
                RequiredAmenityIds = [7]
            },
            strict: true);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Normalize_ShouldTreatFalsePetsAsNoPreference()
    {
        var result = RentalPreferenceNormalizer.Normalize(
            new RentalSearchRequest
            {
                AllowsPets = false,
                RequiredCriteria = ["pets"]
            },
            strict: false);

        Assert.Null(result.Draft.AllowsPets);
        Assert.DoesNotContain("pets", result.Draft.RequiredCriteria);
    }
}
