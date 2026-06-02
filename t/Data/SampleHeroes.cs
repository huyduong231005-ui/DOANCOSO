using t.Models.Entities;

namespace t.Data;

/// <summary>
/// 3 listing nổi bật xuất hiện trên hero của trang chủ (Landmark 81, Vinhomes Ocean Park, Sun Group).
/// Slug cố định để view có thể lookup theo slug; xuất hiện cả ở /Home/Rentals và /admin/Apartments.
/// </summary>
internal static class SampleHeroes
{
    private static readonly DateOnly SeedAvailableFrom = new(2026, 6, 1);

    public const string LandmarkSlug = "landmark-81-sky-villa";
    public const string OceanParkSlug = "vinhomes-ocean-park-residence";
    public const string SunGroupSlug = "sun-group-residence";

    public static void Seed(
        AppDbContext ctx,
        string hostId,
        Region hcm, Region hn, Region dn,
        Dictionary<string, Category> cats,
        Dictionary<string, Amenity> amens)
    {
        var landmarkProject = new Project
        {
            Name = "Landmark 81 Sky Collection",
            Slug = "landmark-81-sky-collection",
            RegionId = hcm.Id,
            Address = "Tôn Đức Thắng, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
            ThumbnailUrl = "/img/hero-2.jpg",
            PriceFrom = 35_000_000m,
            Status = ProjectStatus.OpenForRent,
            ShortDescription = "Bộ sưu tập sky villa tại toà nhà cao nhất Việt Nam.",
            FullDescription = "Tổ hợp căn hộ - sky villa nằm trong toà tháp Landmark 81, view toàn cảnh trung tâm và sông Sài Gòn. Tiện ích nội khu hồ bơi vô cực, observatory deck."
        };

        var oceanParkProject = new Project
        {
            Name = "Vinhomes Ocean Park",
            Slug = "vinhomes-ocean-park",
            RegionId = hn.Id,
            Address = "Đa Tốn, Gia Lâm, Hà Nội",
            ThumbnailUrl = "/img/ocean-park.jpg",
            PriceFrom = 8_500_000m,
            Status = ProjectStatus.OpenForRent,
            ShortDescription = "Đại đô thị resort với hồ nước mặn và biển hồ nội khu.",
            FullDescription = "Khu đô thị Vinhomes Ocean Park 420 ha với biển hồ nước mặn 6.1 ha, công viên 32 ha, đầy đủ tiện ích trường học - bệnh viện - TTTM trong nội khu."
        };

        var sunGroupProject = new Project
        {
            Name = "Sun Group Boutique Residence",
            Slug = "sun-group-residence",
            RegionId = dn.Id,
            Address = "Bạch Đằng, Phường Hải Châu 1, Hải Châu, Đà Nẵng",
            ThumbnailUrl = "/img/hero-3.jpg",
            PriceFrom = 12_000_000m,
            Status = ProjectStatus.OpenForRent,
            ShortDescription = "Chuỗi villa boutique view sông Hàn ngay trung tâm Đà Nẵng.",
            FullDescription = "Tổ hợp villa boutique do Sun Group phát triển, kiến trúc hiện đại kết hợp truyền thống, sân vườn và hồ bơi riêng cho từng căn."
        };

        ctx.Projects.AddRange(landmarkProject, oceanParkProject, sunGroupProject);
        ctx.SaveChanges();

        var apt1 = new Apartment
        {
            Title = "Landmark 81 Sky Villa - 3PN view trung tâm",
            Slug = LandmarkSlug,
            UnitCode = "LM81-SV01",
            Description = "Sky villa 3 phòng ngủ tại Landmark 81, sàn gỗ nhập khẩu, ban công kính trải dài, view trực diện trung tâm Sài Gòn và sông.",
            DescriptionExtra = "Bàn giao đầy đủ nội thất Châu Âu, có phòng karaoke nhỏ, sân vườn trên không.",
            Price = 45_000_000m,
            DefaultDeposit = 90_000_000m,
            FeeNote = "Đã bao gồm phí quản lý và gym - bể bơi nội khu",
            Area = 120, Bedrooms = 3, Bathrooms = 3,
            Address = "Tôn Đức Thắng, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
            Latitude = 10.7794, Longitude = 106.7050,
            FurnishingLevel = FurnishingLevel.FullyFurnished,
            AvailableFrom = SeedAvailableFrom,
            MinLeaseMonths = 6,
            MaxLeaseMonths = 24,
            Status = ListingStatus.Active,
            Occupancy = ApartmentOccupancy.Available,
            IsFeatured = true,
            HostId = hostId,
            Region = hcm,
            Category = cats["luxury"],
            ProjectId = landmarkProject.Id
        };

        var apt2 = new Apartment
        {
            Title = "Vinhomes Ocean Park - Studio view biển hồ",
            Slug = OceanParkSlug,
            UnitCode = "VHOP-S01",
            Description = "Căn studio Vinhomes Ocean Park, view trực diện biển hồ nước mặn, full nội thất, ban công riêng.",
            DescriptionExtra = "Khu compound kín, miễn phí xe điện đưa đón nội khu, tiện ích trường học quốc tế và TTTM Vincom.",
            Price = 8_500_000m,
            DefaultDeposit = 17_000_000m,
            FeeNote = "Phí quản lý 8.500đ/m² đã tính riêng",
            Area = 35, Bedrooms = 1, Bathrooms = 1,
            Address = "Đa Tốn, Gia Lâm, Hà Nội",
            Latitude = 20.9890, Longitude = 105.9320,
            FurnishingLevel = FurnishingLevel.FullyFurnished,
            AvailableFrom = SeedAvailableFrom,
            MinLeaseMonths = 6,
            MaxLeaseMonths = 24,
            Status = ListingStatus.Active,
            Occupancy = ApartmentOccupancy.Available,
            IsFeatured = true,
            HostId = hostId,
            Region = hn,
            Category = cats["luxury"],
            ProjectId = oceanParkProject.Id
        };

        var apt3 = new Apartment
        {
            Title = "Sun Group Residence - Boutique villa view sông Hàn",
            Slug = SunGroupSlug,
            UnitCode = "SGR-V01",
            Description = "Villa boutique 2 tầng view trực diện sông Hàn và cầu Rồng, kiến trúc hiện đại + truyền thống Á Đông.",
            DescriptionExtra = "Hồ bơi riêng, sân vườn, gần phố cổ và bãi biển Mỹ Khê 5 phút lái xe.",
            Price = 12_000_000m,
            DefaultDeposit = 24_000_000m,
            FeeNote = "Có thợ làm vườn theo tuần",
            Area = 95, Bedrooms = 2, Bathrooms = 2,
            Address = "Bạch Đằng, Phường Hải Châu 1, Hải Châu, Đà Nẵng",
            Latitude = 16.0742, Longitude = 108.2240,
            FurnishingLevel = FurnishingLevel.FullyFurnished,
            AllowsPets = true,
            AvailableFrom = SeedAvailableFrom,
            MinLeaseMonths = 12,
            MaxLeaseMonths = 36,
            Status = ListingStatus.Active,
            Occupancy = ApartmentOccupancy.Available,
            IsFeatured = true,
            HostId = hostId,
            Region = dn,
            Category = cats["villa"],
            ProjectId = sunGroupProject.Id
        };

        ctx.Apartments.AddRange(apt1, apt2, apt3);
        ctx.SaveChanges();

        ctx.ApartmentImages.AddRange(
            new ApartmentImage { ApartmentId = apt1.Id, Url = "/img/hero-2.jpg",     IsCover = true,  SortOrder = 0 },
            new ApartmentImage { ApartmentId = apt1.Id, Url = "/img/hero-4.jpg",     IsCover = false, SortOrder = 1 },
            new ApartmentImage { ApartmentId = apt2.Id, Url = "/img/ocean-park.jpg", IsCover = true,  SortOrder = 0 },
            new ApartmentImage { ApartmentId = apt2.Id, Url = "/img/hero-1.jpg",     IsCover = false, SortOrder = 1 },
            new ApartmentImage { ApartmentId = apt3.Id, Url = "/img/hero-3.jpg",     IsCover = true,  SortOrder = 0 }
        );

        var heroAmenities = new[] { "wifi", "ac", "pool", "gym", "security", "furniture" };
        foreach (var apt in new[] { apt1, apt2, apt3 })
        {
            foreach (var key in heroAmenities)
            {
                if (amens.TryGetValue(key, out var a))
                    ctx.ApartmentAmenities.Add(new ApartmentAmenity { ApartmentId = apt.Id, AmenityId = a.Id });
            }
        }

        ctx.ProjectImages.AddRange(
            new ProjectImage { ProjectId = landmarkProject.Id,  Url = "/img/hero-2.jpg",     IsCover = true,  SortOrder = 0 },
            new ProjectImage { ProjectId = oceanParkProject.Id, Url = "/img/ocean-park.jpg", IsCover = true,  SortOrder = 0 },
            new ProjectImage { ProjectId = sunGroupProject.Id,  Url = "/img/hero-3.jpg",     IsCover = true,  SortOrder = 0 }
        );

        ctx.SaveChanges();
    }
}
