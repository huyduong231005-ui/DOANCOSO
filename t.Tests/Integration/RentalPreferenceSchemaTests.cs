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
}
