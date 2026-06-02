using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Tests.Integration;

public sealed class RentalPreferenceSchemaTests
{
    [Theory]
    [InlineData(FurnishingLevel.None, "Chưa có nội thất")]
    [InlineData(FurnishingLevel.Basic, "Nội thất cơ bản")]
    [InlineData(FurnishingLevel.FullyFurnished, "Đầy đủ nội thất")]
    public void FurnishingLevel_ShouldRenderVietnameseLabel(
        FurnishingLevel value,
        string expected)
    {
        Assert.Equal(expected, value.Vi());
    }

    [Theory]
    [InlineData(ParkingType.None, "Không có")]
    [InlineData(ParkingType.Motorbike, "Xe máy")]
    [InlineData(ParkingType.Car, "Ô tô")]
    public void ParkingType_ShouldRenderVietnameseLabel(
        ParkingType value,
        string expected)
    {
        Assert.Equal(expected, value.Vi());
    }

    [Fact]
    public void HouseDirection_ShouldRenderEightVietnameseDirections()
    {
        Assert.Equal(8, Enum.GetValues<HouseDirection>().Length);
        Assert.Equal("Đông Bắc", HouseDirection.NorthEast.Vi());
        Assert.Equal("Tây Nam", HouseDirection.SouthWest.Vi());
    }

    [Fact]
    public void RentalPreferenceProfile_ShouldBeUniquePerUser_AndOwnJoinTables()
    {
        using var db = CreateDbContext();

        var profile = db.Model.FindEntityType(typeof(RentalPreferenceProfile));
        Assert.NotNull(profile);
        Assert.Contains(profile!.GetIndexes(),
            index => index.IsUnique &&
                     index.Properties.Single().Name == nameof(RentalPreferenceProfile.UserId));

        var amenity = db.Model.FindEntityType(typeof(RentalPreferenceAmenity));
        Assert.NotNull(amenity);
        Assert.Equal(
            [nameof(RentalPreferenceAmenity.ProfileId), nameof(RentalPreferenceAmenity.AmenityId)],
            amenity!.FindPrimaryKey()!.Properties.Select(property => property.Name));
    }

    [Fact]
    public void Apartment_ShouldExposeNormalizedRentalConditions()
    {
        var properties = typeof(Apartment).GetProperties().Select(property => property.Name).ToHashSet();

        Assert.Contains(nameof(Apartment.FurnishingLevel), properties);
        Assert.Contains(nameof(Apartment.AllowsPets), properties);
        Assert.Contains(nameof(Apartment.ParkingType), properties);
        Assert.Contains(nameof(Apartment.AvailableFrom), properties);
        Assert.Contains(nameof(Apartment.MinLeaseMonths), properties);
        Assert.Contains(nameof(Apartment.MaxLeaseMonths), properties);
        Assert.Contains(nameof(Apartment.HouseDirection), properties);
        Assert.Contains(nameof(Apartment.FloorNumber), properties);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=rental-preference-schema-tests")
            .Options;
        return new AppDbContext(options);
    }
}
