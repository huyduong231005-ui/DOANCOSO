using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using t.Models.Entities;

namespace t.Data;

public static class SeedData
{
    private static readonly DateOnly SeedAvailableFrom = new(2026, 6, 1);

    public const string RoleAdmin = "Admin";
    public const string RoleManager = "Manager";
    public const string RoleHost = "Host";
    public const string RoleTenant = "Tenant";
    public const string RoleCustomer = "Customer";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            await ctx.Database.MigrateAsync();
        }
        catch (InvalidOperationException)
        {
            await ctx.Database.EnsureCreatedAsync();
        }

        await SeedRolesAndPermissionsAsync(ctx, roleMgr);

        var admin = await SeedAdminAsync(userMgr);
        if (admin != null && !await userMgr.IsInRoleAsync(admin, RoleAdmin))
            await userMgr.AddToRoleAsync(admin, RoleAdmin);

        await HideDeprecatedSampleListingsAsync(ctx);

        if (await ctx.Regions.AnyAsync()) return;

        var (hcm, hn, dn) = SeedRegions(ctx);
        var cats = SeedCategories(ctx);
        var amens = SeedAmenities(ctx);
        await ctx.SaveChangesAsync();

        var (projLandmark, projThuDuc, projQuan7) = SeedProjects(ctx, hcm.Id);
        await ctx.SaveChangesAsync();

        var (host, renter, renter2, renter3, renter4) = await SeedUsersAsync(userMgr);
        await userMgr.AddToRoleAsync(host, RoleHost);
        await userMgr.AddToRoleAsync(renter, RoleTenant);
        await userMgr.AddToRoleAsync(renter2, RoleTenant);
        await userMgr.AddToRoleAsync(renter3, RoleTenant);
        await userMgr.AddToRoleAsync(renter4, RoleTenant);

        var (b1, b2) = SeedBuildings(ctx, hcm.Id, projLandmark.Id, projThuDuc.Id, host.Id);
        await ctx.SaveChangesAsync();

        var floors = SeedFloors(ctx, b1.Id, b2.Id);
        await ctx.SaveChangesAsync();

        var (apt1, apt2, apt3, apt4) = SeedApartments(ctx, host, hcm, cats, projLandmark.Id, projThuDuc.Id, projQuan7.Id, b1.Id, b2.Id, floors);
        await ctx.SaveChangesAsync();

        SeedImages(ctx, apt1.Id, apt2.Id, apt3.Id, apt4.Id, projLandmark.Id, projThuDuc.Id, projQuan7.Id);
        SeedApartmentAmenities(ctx, apt1.Id, apt2.Id, apt3.Id, apt4.Id, amens);
        SeedReviews(ctx, apt1.Id, renter.Id, renter2.Id);
        await ctx.SaveChangesAsync();

        SampleListings.Seed(ctx, host.Id, hcm, hn, dn, cats, amens);
        SampleHeroes.Seed(ctx, host.Id, hcm, hn, dn, cats, amens);

        var utilTypes = SeedUtilityTypes(ctx);
        await ctx.SaveChangesAsync();

        SeedRichLeaseFlow(ctx, renter, renter2, renter3, renter4, apt1, apt2, apt3, apt4, utilTypes);
        await ctx.SaveChangesAsync();
    }

    private static async Task SeedRolesAndPermissionsAsync(AppDbContext ctx, RoleManager<IdentityRole> roleMgr)
    {
        foreach (var role in new[] { RoleAdmin, RoleManager, RoleHost, RoleTenant, RoleCustomer })
        {
            if (!await roleMgr.RoleExistsAsync(role))
                await roleMgr.CreateAsync(new IdentityRole(role));
        }

        if (await ctx.Permissions.AnyAsync()) return;

        var permissions = new (string Module, string Code, string Name)[]
        {
            ("Apartments", "apartments.view",   "Xem danh sách căn hộ"),
            ("Apartments", "apartments.create", "Tạo căn hộ"),
            ("Apartments", "apartments.update", "Cập nhật căn hộ"),
            ("Apartments", "apartments.delete", "Xoá căn hộ"),
            ("Projects",   "projects.view",     "Xem dự án"),
            ("Projects",   "projects.manage",   "Quản lý dự án"),
            ("Bookings",   "bookings.view",     "Xem booking"),
            ("Bookings",   "bookings.confirm",  "Xác nhận booking"),
            ("Bookings",   "bookings.cancel",   "Huỷ booking"),
            ("Invoices",   "invoices.view",     "Xem hoá đơn"),
            ("Invoices",   "invoices.issue",    "Phát hành hoá đơn"),
            ("Payments",   "payments.view",     "Xem thanh toán"),
            ("Payments",   "payments.refund",   "Hoàn tiền"),
            ("Reviews",    "reviews.moderate",  "Kiểm duyệt review"),
            ("Users",      "users.view",        "Xem người dùng"),
            ("Users",      "users.manage",      "Quản lý người dùng"),
            ("Roles",      "roles.manage",      "Quản lý phân quyền"),
            ("Audit",      "audit.view",        "Xem audit log"),
        };

        foreach (var (module, code, name) in permissions)
            ctx.Permissions.Add(new Permission { Module = module, Code = code, DisplayName = name });

        await ctx.SaveChangesAsync();

        var allPerms = await ctx.Permissions.ToListAsync();
        var adminRole = await roleMgr.FindByNameAsync(RoleAdmin);
        var managerRole = await roleMgr.FindByNameAsync(RoleManager);
        var hostRole = await roleMgr.FindByNameAsync(RoleHost);

        if (adminRole != null)
            foreach (var p in allPerms)
                ctx.RolePermissions.Add(new RolePermission { RoleId = adminRole.Id, PermissionId = p.Id });

        if (managerRole != null)
            foreach (var p in allPerms.Where(p => p.Code != "roles.manage" && p.Code != "users.manage"))
                ctx.RolePermissions.Add(new RolePermission { RoleId = managerRole.Id, PermissionId = p.Id });

        if (hostRole != null)
            foreach (var p in allPerms.Where(p =>
                p.Code is "apartments.view" or "apartments.create" or "apartments.update"
                       or "bookings.view" or "invoices.view" or "payments.view"))
                ctx.RolePermissions.Add(new RolePermission { RoleId = hostRole.Id, PermissionId = p.Id });

        await ctx.SaveChangesAsync();
    }

    private static async Task HideDeprecatedSampleListingsAsync(AppDbContext ctx)
    {
        var deprecatedSlugs = new[] { "ccm-tan-binh-full-do" };
        var apartments = await ctx.Apartments
            .IgnoreQueryFilters()
            .Where(apartment => deprecatedSlugs.Contains(apartment.Slug) && !apartment.IsDeleted)
            .ToListAsync();

        if (apartments.Count == 0)
        {
            return;
        }

        foreach (var apartment in apartments)
        {
            apartment.IsDeleted = true;
        }

        await ctx.SaveChangesAsync();
    }

    private static (Region hcm, Region hn, Region dn) SeedRegions(AppDbContext ctx)
    {
        var dn = new Region { Name = "Đà Nẵng", Slug = "da-nang", ImageUrl = "/img/region-danang.jpg" };
        var hn = new Region { Name = "Hà Nội", Slug = "ha-noi", ImageUrl = "/img/region-hanoi.jpg" };
        var hcm = new Region { Name = "TP. Hồ Chí Minh", Slug = "tp-ho-chi-minh", ImageUrl = "/img/region-hcm.jpg" };
        ctx.Regions.AddRange(dn, hn, hcm);
        return (hcm, hn, dn);
    }

    private static Dictionary<string, Category> SeedCategories(AppDbContext ctx)
    {
        var dict = new Dictionary<string, Category>
        {
            ["luxury"]    = new() { Name = "Căn hộ chung cư cao cấp", Slug = "can-ho-cao-cap",   Icon = "apartment" },
            ["house"]     = new() { Name = "Nhà nguyên căn",          Slug = "nha-nguyen-can",   Icon = "home" },
            ["villa"]     = new() { Name = "Biệt thự sân vườn",       Slug = "biet-thu",         Icon = "villa" },
            ["penthouse"] = new() { Name = "Penthouse / Duplex",      Slug = "penthouse",        Icon = "domain" },
            ["room"]      = new() { Name = "Nhà trọ",                 Slug = "nha-tro",          Icon = "savings" },
            ["mini"]      = new() { Name = "Chung cư mini",           Slug = "chung-cu-mini",    Icon = "cottage" }
        };
        ctx.Categories.AddRange(dict.Values);
        return dict;
    }

    private static Dictionary<string, Amenity> SeedAmenities(AppDbContext ctx)
    {
        var dict = new Dictionary<string, Amenity>
        {
            ["wifi"]      = new() { Name = "Wi-Fi tốc độ cao", Slug = "wifi",       Icon = "wifi" },
            ["ac"]        = new() { Name = "Máy lạnh",         Slug = "ac",         Icon = "ac_unit" },
            ["washer"]    = new() { Name = "Máy giặt",         Slug = "washer",     Icon = "local_laundry_service" },
            ["fridge"]    = new() { Name = "Tủ lạnh",          Slug = "fridge",     Icon = "kitchen" },
            ["furniture"] = new() { Name = "Full nội thất",    Slug = "furniture",  Icon = "chair" },
            ["security"]  = new() { Name = "An ninh 24/7",     Slug = "security",   Icon = "security" },
            ["pool"]      = new() { Name = "Hồ bơi",           Slug = "pool",       Icon = "pool" },
            ["gym"]       = new() { Name = "Phòng gym",        Slug = "gym",        Icon = "fitness_center" },
            ["parking"]   = new() { Name = "Chỗ đậu xe",       Slug = "parking",    Icon = "garage" }
        };
        ctx.Amenities.AddRange(dict.Values);
        return dict;
    }

    private static (Project landmark, Project thuDuc, Project quan7) SeedProjects(AppDbContext ctx, int hcmId)
    {
        var landmark = new Project
        {
            Name = "Landmark Riverside Collection",
            Slug = "landmark-riverside-collection",
            RegionId = hcmId,
            Address = "Bình Thạnh, TP. Hồ Chí Minh",
            ThumbnailUrl = "/img/hero-2.jpg",
            PriceFrom = 12_500_000,
            Status = ProjectStatus.OpenForRent,
            ShortDescription = "Tổ hợp căn hộ cao cấp bên sông, tập trung khách thuê chuyên gia.",
            FullDescription = "Dự án tập trung vào trải nghiệm sống cao cấp, hệ thống tiện ích hoàn chỉnh và kết nối nhanh đến trung tâm."
        };
        var thuDuc = new Project
        {
            Name = "Thu Duc Smart Living",
            Slug = "thu-duc-smart-living",
            RegionId = hcmId,
            Address = "Thủ Đức, TP. Hồ Chí Minh",
            ThumbnailUrl = "/img/rent-2.jpg",
            PriceFrom = 3_200_000,
            Status = ProjectStatus.OpenForRent,
            ShortDescription = "Nhóm nhà ở tối ưu chi phí cho sinh viên và người đi làm.",
            FullDescription = "Mục tiêu hướng đến nhu cầu ở thực tế với giá hợp lý, đề cao an ninh, vận hành ổn định và vị trí thuận tiện."
        };
        var quan7 = new Project
        {
            Name = "South Gate Residence",
            Slug = "south-gate-residence",
            RegionId = hcmId,
            Address = "Quận 7, TP. Hồ Chí Minh",
            ThumbnailUrl = "/img/rent-4.jpg",
            PriceFrom = 4_500_000,
            Status = ProjectStatus.Upcoming,
            ShortDescription = "Cụm căn hộ mini đầy đủ nội thất khu Nam Sài Gòn.",
            FullDescription = "Dự án định hướng nhu cầu ở lâu dài, tối ưu tiện ích nội khu và kết nối giao thông đến trung tâm thành phố."
        };
        ctx.Projects.AddRange(landmark, thuDuc, quan7);
        return (landmark, thuDuc, quan7);
    }

    private static async Task<AppUser?> SeedAdminAsync(UserManager<AppUser> userMgr)
    {
        var existing = await userMgr.FindByEmailAsync("admin@luxehaven.vn");
        if (existing != null) return existing;

        var admin = new AppUser
        {
            UserName = "admin@luxehaven.vn", Email = "admin@luxehaven.vn",
            FullName = "Quản trị viên", Phone = "098 000 0001",
            EmailConfirmed = true
        };
        var result = await userMgr.CreateAsync(admin, "Admin@123");
        return result.Succeeded ? admin : null;
    }

    private static async Task<(AppUser host, AppUser renter, AppUser renter2, AppUser renter3, AppUser renter4)> SeedUsersAsync(UserManager<AppUser> userMgr)
    {
        var host = new AppUser
        {
            UserName = "host@luxehaven.vn", Email = "host@luxehaven.vn",
            FullName = "Phạm Hoàng Nam", Phone = "090 123 4567",
            IsHost = true, HostTitle = "Premium Host",
            AvatarUrl = "/img/detail/agent.jpg", EmailConfirmed = true
        };
        await userMgr.CreateAsync(host, "Host@123");

        var renter = new AppUser
        {
            UserName = "renter@luxehaven.vn", Email = "renter@luxehaven.vn",
            FullName = "Nguyễn Minh Quân", Phone = "091 234 5678",
            AvatarUrl = "/img/detail/user1.jpg", EmailConfirmed = true
        };
        await userMgr.CreateAsync(renter, "Renter@123");

        var renter2 = new AppUser
        {
            UserName = "renter2@luxehaven.vn", Email = "renter2@luxehaven.vn",
            FullName = "Lê Thu Hà", Phone = "092 345 6789",
            AvatarUrl = "/img/detail/user2.jpg", EmailConfirmed = true
        };
        await userMgr.CreateAsync(renter2, "Renter@123");

        var renter3 = new AppUser
        {
            UserName = "renter3@luxehaven.vn", Email = "renter3@luxehaven.vn",
            FullName = "Trần Văn Hùng", Phone = "093 456 7890",
            AvatarUrl = "/img/detail/user1.jpg", EmailConfirmed = true
        };
        await userMgr.CreateAsync(renter3, "Renter@123");

        var renter4 = new AppUser
        {
            UserName = "renter4@luxehaven.vn", Email = "renter4@luxehaven.vn",
            FullName = "Phạm Thị Mai", Phone = "094 567 8901",
            AvatarUrl = "/img/detail/user2.jpg", EmailConfirmed = true
        };
        await userMgr.CreateAsync(renter4, "Renter@123");

        return (host, renter, renter2, renter3, renter4);
    }

    private static (Building b1, Building b2) SeedBuildings(AppDbContext ctx, int regionId, int landmarkProjectId, int thuDucProjectId, string managerId)
    {
        var b1 = new Building
        {
            Name = "Landmark Tower A", Slug = "landmark-tower-a", Code = "LMA",
            ProjectId = landmarkProjectId, RegionId = regionId,
            Address = "Bình Thạnh, TP. Hồ Chí Minh",
            FloorCount = 5,
            ThumbnailUrl = "/img/hero-2.jpg",
            Description = "Toà tháp A của tổ hợp Landmark Riverside, view sông Sài Gòn.",
            ManagerId = managerId,
            Status = BuildingStatus.Active
        };
        var b2 = new Building
        {
            Name = "Smart Living Block 1", Slug = "smart-living-block-1", Code = "SLB1",
            ProjectId = thuDucProjectId, RegionId = regionId,
            Address = "Thủ Đức, TP. Hồ Chí Minh",
            FloorCount = 4,
            ThumbnailUrl = "/img/rent-2.jpg",
            Description = "Cụm nhà ở vận hành quản lí cho sinh viên / nhân viên văn phòng.",
            ManagerId = managerId,
            Status = BuildingStatus.Active
        };
        ctx.Buildings.AddRange(b1, b2);
        return (b1, b2);
    }

    private static Dictionary<string, Floor> SeedFloors(AppDbContext ctx, int buildingAId, int buildingBId)
    {
        var dict = new Dictionary<string, Floor>();
        for (int i = 1; i <= 5; i++)
        {
            var f = new Floor { BuildingId = buildingAId, Number = i, Label = $"Tầng {i}" };
            dict[$"A{i}"] = f; ctx.Floors.Add(f);
        }
        for (int i = 1; i <= 4; i++)
        {
            var f = new Floor { BuildingId = buildingBId, Number = i, Label = $"Tầng {i}" };
            dict[$"B{i}"] = f; ctx.Floors.Add(f);
        }
        return dict;
    }

    private static Dictionary<string, UtilityType> SeedUtilityTypes(AppDbContext ctx)
    {
        var dict = new Dictionary<string, UtilityType>
        {
            ["electricity"] = new() { Code = "ELEC", Name = "Điện", Unit = "kWh", BillingMode = UtilityBillingMode.Metered, DefaultRate = 3500m, Icon = "bolt" },
            ["water"]       = new() { Code = "WATER", Name = "Nước", Unit = "m³", BillingMode = UtilityBillingMode.Metered, DefaultRate = 25000m, Icon = "water_drop" },
            ["internet"]    = new() { Code = "NET", Name = "Internet", Unit = "tháng", BillingMode = UtilityBillingMode.Fixed, DefaultRate = 200000m, Icon = "wifi" },
            ["service"]     = new() { Code = "SERVICE", Name = "Phí dịch vụ", Unit = "tháng", BillingMode = UtilityBillingMode.Fixed, DefaultRate = 500000m, Icon = "cleaning_services" }
        };
        ctx.UtilityTypes.AddRange(dict.Values);
        return dict;
    }

    private static (Apartment a1, Apartment a2, Apartment a3, Apartment a4) SeedApartments(
        AppDbContext ctx, AppUser host, Region hcm,
        Dictionary<string, Category> cats, int landmarkId, int thuDucId, int quan7Id,
        int buildingAId, int buildingBId, Dictionary<string, Floor> floors)
    {
        var apt1 = new Apartment
        {
            Title = "Luxe Studio Landmark 81 - View sông Sài Gòn",
            Slug = "luxe-studio-landmark-81", UnitCode = "A-501",
            Description = "Studio cao cấp tại Landmark 81 với thiết kế mở, ánh sáng tự nhiên lớn và tầm nhìn trực diện sông Sài Gòn.",
            DescriptionExtra = "Nội thất hoàn thiện đồng bộ, vận hành ổn định, phù hợp khách ở lâu dài.",
            Price = 12_500_000, DefaultDeposit = 25_000_000m, FeeNote = "Phí quản lý: Miễn phí 1 năm",
            Area = 45, Bedrooms = 1, Bathrooms = 1,
            Address = "Phường 22, Bình Thạnh, TP. Hồ Chí Minh",
            Latitude = 10.7942, Longitude = 106.7219,
            FurnishingLevel = FurnishingLevel.FullyFurnished,
            AvailableFrom = SeedAvailableFrom,
            MinLeaseMonths = 6,
            MaxLeaseMonths = 24,
            FloorNumber = 5,
            Status = ListingStatus.Active, Occupancy = ApartmentOccupancy.Occupied, IsFeatured = true,
            HostId = host.Id, Region = hcm, Category = cats["luxury"], ProjectId = landmarkId,
            BuildingId = buildingAId, FloorId = floors["A5"].Id
        };
        var apt2 = new Apartment
        {
            Title = "Studio Block 1 - 102",
            Slug = "smart-living-block1-102", UnitCode = "B-102",
            Description = "Phòng trọ thiết kế gọn gàng, thoáng sáng, bố cục hợp lý cho sinh viên hoặc người đi làm.",
            DescriptionExtra = "Khu vực yên tĩnh, hàng xóm văn minh.",
            Price = 3_200_000, DefaultDeposit = 6_400_000m, FeeNote = "Điện nước tính theo đồng hồ riêng",
            Area = 20, Bedrooms = 1, Bathrooms = 1,
            Address = "Phường Linh Tây, Thủ Đức, TP. Hồ Chí Minh",
            Latitude = 10.8530, Longitude = 106.7590,
            ParkingType = ParkingType.Motorbike,
            AvailableFrom = SeedAvailableFrom,
            MinLeaseMonths = 3,
            MaxLeaseMonths = 12,
            FloorNumber = 1,
            Status = ListingStatus.Active, Occupancy = ApartmentOccupancy.Available,
            HostId = host.Id, Region = hcm, Category = cats["room"], ProjectId = thuDucId,
            BuildingId = buildingBId, FloorId = floors["B1"].Id
        };
        var apt3 = new Apartment
        {
            Title = "Penthouse Landmark - 2PN view sông",
            Slug = "penthouse-landmark-2pn", UnitCode = "A-PH1",
            Description = "Căn hộ 2 phòng ngủ diện tích lớn, bố trí phòng khách và bếp tách rõ.",
            DescriptionExtra = "Nằm trong khu dân cư cao cấp với hệ tiện ích trọn gói.",
            Price = 18_000_000, DefaultDeposit = 36_000_000m, FeeNote = "Miễn phí quản lý 6 tháng đầu",
            Area = 72, Bedrooms = 2, Bathrooms = 2,
            Address = "Nguyễn Hữu Cảnh, Bình Thạnh, TP. Hồ Chí Minh",
            Latitude = 10.7952, Longitude = 106.7193,
            FurnishingLevel = FurnishingLevel.FullyFurnished,
            AvailableFrom = SeedAvailableFrom,
            MinLeaseMonths = 6,
            MaxLeaseMonths = 24,
            FloorNumber = 5,
            Status = ListingStatus.Active, Occupancy = ApartmentOccupancy.Available,
            HostId = host.Id, Region = hcm, Category = cats["luxury"], ProjectId = landmarkId,
            BuildingId = buildingAId, FloorId = floors["A5"].Id
        };
        var apt4 = new Apartment
        {
            Title = "Studio Block 1 - 203",
            Slug = "smart-living-block1-203", UnitCode = "B-203",
            Description = "Căn hộ mini đã trang bị sẵn nội thất cơ bản, phù hợp cho người độc thân hoặc cặp đôi.",
            DescriptionExtra = "Khu vực xung quanh đầy đủ tiện ích sinh hoạt.",
            Price = 4_500_000, DefaultDeposit = 9_000_000m, FeeNote = "Có chỗ để xe, camera 24/7",
            Area = 25, Bedrooms = 1, Bathrooms = 1,
            Address = "Linh Tây, Thủ Đức, TP. Hồ Chí Minh",
            Latitude = 10.7326, Longitude = 106.7196,
            FurnishingLevel = FurnishingLevel.Basic,
            ParkingType = ParkingType.Motorbike,
            AvailableFrom = SeedAvailableFrom,
            MinLeaseMonths = 3,
            MaxLeaseMonths = 12,
            FloorNumber = 2,
            Status = ListingStatus.Active, Occupancy = ApartmentOccupancy.Available,
            HostId = host.Id, Region = hcm, Category = cats["mini"], ProjectId = quan7Id,
            BuildingId = buildingBId, FloorId = floors["B2"].Id
        };
        ctx.Apartments.AddRange(apt1, apt2, apt3, apt4);
        return (apt1, apt2, apt3, apt4);
    }

    private static void SeedImages(AppDbContext ctx, int a1, int a2, int a3, int a4, int p1, int p2, int p3)
    {
        ctx.ApartmentImages.AddRange(
            new ApartmentImage { ApartmentId = a1, Url = "/img/rent-1.jpg", IsCover = true, SortOrder = 0 },
            new ApartmentImage { ApartmentId = a1, Url = "/img/detail/bed.jpg", SortOrder = 1 },
            new ApartmentImage { ApartmentId = a1, Url = "/img/detail/bath.jpg", SortOrder = 2 },
            new ApartmentImage { ApartmentId = a1, Url = "/img/detail/kitchen.jpg", SortOrder = 3 },

            new ApartmentImage { ApartmentId = a2, Url = "/img/rent-2.jpg", IsCover = true, SortOrder = 0 },
            new ApartmentImage { ApartmentId = a2, Url = "/img/detail/sim3.jpg", SortOrder = 1 },
            new ApartmentImage { ApartmentId = a2, Url = "/img/detail/bath.jpg", SortOrder = 2 },
            new ApartmentImage { ApartmentId = a2, Url = "/img/detail/bed.jpg", SortOrder = 3 },

            new ApartmentImage { ApartmentId = a3, Url = "/img/rent-3.jpg", IsCover = true, SortOrder = 0 },
            new ApartmentImage { ApartmentId = a3, Url = "/img/detail/kitchen.jpg", SortOrder = 1 },
            new ApartmentImage { ApartmentId = a3, Url = "/img/detail/bed.jpg", SortOrder = 2 },
            new ApartmentImage { ApartmentId = a3, Url = "/img/detail/sim1.jpg", SortOrder = 3 },

            new ApartmentImage { ApartmentId = a4, Url = "/img/rent-4.jpg", IsCover = true, SortOrder = 0 },
            new ApartmentImage { ApartmentId = a4, Url = "/img/detail/sim2.jpg", SortOrder = 1 },
            new ApartmentImage { ApartmentId = a4, Url = "/img/detail/bath.jpg", SortOrder = 2 },
            new ApartmentImage { ApartmentId = a4, Url = "/img/detail/kitchen.jpg", SortOrder = 3 }
        );

        ctx.ProjectImages.AddRange(
            new ProjectImage { ProjectId = p1, Url = "/img/hero-2.jpg", SortOrder = 0, IsCover = true },
            new ProjectImage { ProjectId = p1, Url = "/img/rent-1.jpg", SortOrder = 1 },
            new ProjectImage { ProjectId = p1, Url = "/img/detail/kitchen.jpg", SortOrder = 2 },
            new ProjectImage { ProjectId = p2, Url = "/img/rent-2.jpg", SortOrder = 0, IsCover = true },
            new ProjectImage { ProjectId = p2, Url = "/img/detail/sim3.jpg", SortOrder = 1 },
            new ProjectImage { ProjectId = p3, Url = "/img/rent-4.jpg", SortOrder = 0, IsCover = true },
            new ProjectImage { ProjectId = p3, Url = "/img/detail/sim2.jpg", SortOrder = 1 }
        );
    }

    private static void SeedApartmentAmenities(AppDbContext ctx, int a1, int a2, int a3, int a4, Dictionary<string, Amenity> am)
    {
        ctx.ApartmentAmenities.AddRange(
            new ApartmentAmenity { ApartmentId = a1, AmenityId = am["wifi"].Id },
            new ApartmentAmenity { ApartmentId = a1, AmenityId = am["ac"].Id },
            new ApartmentAmenity { ApartmentId = a1, AmenityId = am["security"].Id },
            new ApartmentAmenity { ApartmentId = a1, AmenityId = am["pool"].Id },
            new ApartmentAmenity { ApartmentId = a1, AmenityId = am["gym"].Id },
            new ApartmentAmenity { ApartmentId = a1, AmenityId = am["furniture"].Id },

            new ApartmentAmenity { ApartmentId = a2, AmenityId = am["parking"].Id },
            new ApartmentAmenity { ApartmentId = a2, AmenityId = am["wifi"].Id },

            new ApartmentAmenity { ApartmentId = a3, AmenityId = am["pool"].Id },
            new ApartmentAmenity { ApartmentId = a3, AmenityId = am["gym"].Id },
            new ApartmentAmenity { ApartmentId = a3, AmenityId = am["wifi"].Id },
            new ApartmentAmenity { ApartmentId = a3, AmenityId = am["ac"].Id },
            new ApartmentAmenity { ApartmentId = a3, AmenityId = am["washer"].Id },
            new ApartmentAmenity { ApartmentId = a3, AmenityId = am["furniture"].Id },

            new ApartmentAmenity { ApartmentId = a4, AmenityId = am["fridge"].Id },
            new ApartmentAmenity { ApartmentId = a4, AmenityId = am["furniture"].Id },
            new ApartmentAmenity { ApartmentId = a4, AmenityId = am["parking"].Id },
            new ApartmentAmenity { ApartmentId = a4, AmenityId = am["security"].Id }
        );
    }

    private static void SeedReviews(AppDbContext ctx, int aptId, string renterId, string renter2Id)
    {
        ctx.Reviews.AddRange(
            new Review
            {
                ApartmentId = aptId, UserId = renterId, Rating = 5,
                Content = "Vị trí đẹp, bố trí phòng hợp lý và tiện ích đúng mô tả.",
                RenterNote = "Đã thuê 6 tháng",
                Status = ReviewStatus.Approved, ApprovedAt = DateTime.UtcNow
            },
            new Review
            {
                ApartmentId = aptId, UserId = renter2Id, Rating = 4,
                Content = "Hình ảnh đúng với căn hộ thực tế, không gian sạch và thoáng.",
                RenterNote = "Khách tham quan",
                Status = ReviewStatus.Approved, ApprovedAt = DateTime.UtcNow
            }
        );
    }

    private static void SeedSampleMaintenanceAndInspection(AppDbContext ctx, Lease lease, Apartment apt, string reporterId)
    {
        var moveIn = new LeaseInspection
        {
            Lease = lease,
            Type = InspectionType.MoveIn,
            InspectedAt = lease.StartDate,
            InspectorId = reporterId,
            OverallCondition = OverallCondition.Good,
            Summary = "Bàn giao đầy đủ nội thất, thiết bị hoạt động bình thường.",
            DamageNotes = "Tường sơn còn nguyên, sàn không trầy xước.",
            TenantSigned = true
        };
        ctx.LeaseInspections.Add(moveIn);

        var depositTx = new DepositTransaction
        {
            Lease = lease, Type = DepositTransactionType.Hold,
            Amount = lease.Deposit,
            Reason = "Nhận tiền cọc khi ký hợp đồng",
            RecordedAt = lease.StartDate,
            RecordedBy = reporterId
        };
        ctx.DepositTransactions.Add(depositTx);

        var seq = 1;
        var mr = new MaintenanceRequest
        {
            RequestNumber = $"MR-{DateTime.UtcNow:yyyyMM}-{seq:0000}",
            ApartmentId = apt.Id,
            Lease = lease,
            ReporterId = lease.PrimaryTenantId,
            Title = "Vòi nước phòng tắm bị rò rỉ",
            Description = "Vòi nước nóng ở phòng tắm chính bị rò nước, nhỏ giọt liên tục cả khi đã đóng.",
            Category = MaintenanceCategory.Plumbing,
            Priority = MaintenancePriority.Medium,
            Status = MaintenanceStatus.Open,
            EstimatedCost = 350000m,
            ReportedAt = DateTime.UtcNow.AddDays(-2)
        };
        ctx.MaintenanceRequests.Add(mr);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Demo finance flow: 4 leases × 3–6 months of invoices, mixed statuses
    // (Paid, PartiallyPaid, Overdue, Issued). Tổng AmountPaid ≈ 100 tr VND.
    // ──────────────────────────────────────────────────────────────────────
    private static void SeedRichLeaseFlow(
        AppDbContext ctx,
        AppUser renter1, AppUser renter2, AppUser renter3, AppUser renter4,
        Apartment apt1, Apartment apt2, Apartment apt3, Apartment apt4,
        Dictionary<string, UtilityType> utilTypes)
    {
        var today = DateTime.UtcNow.Date;
        int seqInvoice = 0;
        int seqPayment = 0;
        int leaseCounter = 0;

        // ── Lease 1: cao cấp, lịch sử 6 tháng, 5 paid + 1 issued (hiện tại) ──
        BuildLease(renter1, apt1, monthsAgo: 5, plan: new[]
        {
            (offset: -5, state: PayState.Paid),
            (offset: -4, state: PayState.Paid),
            (offset: -3, state: PayState.Paid),
            (offset: -2, state: PayState.Paid),
            (offset: -1, state: PayState.Paid),
            (offset:  0, state: PayState.Issued),
        }, elecKwh: 220m, waterM3: 8m);

        // ── Lease 2: phòng trọ, 5 tháng — 3 paid + 1 partial + 1 issued ──
        BuildLease(renter2, apt2, monthsAgo: 4, plan: new[]
        {
            (offset: -4, state: PayState.Paid),
            (offset: -3, state: PayState.Paid),
            (offset: -2, state: PayState.Paid),
            (offset: -1, state: PayState.PartiallyPaid),
            (offset:  0, state: PayState.Issued),
        }, elecKwh: 95m, waterM3: 5m);

        // ── Lease 3: penthouse, 3 tháng — 1 paid + 1 overdue + 1 issued ──
        BuildLease(renter3, apt3, monthsAgo: 2, plan: new[]
        {
            (offset: -2, state: PayState.Paid),
            (offset: -1, state: PayState.Overdue),
            (offset:  0, state: PayState.Issued),
        }, elecKwh: 350m, waterM3: 12m);

        // ── Lease 4: mini, 4 tháng — 2 paid + 1 overdue + 1 issued ──
        BuildLease(renter4, apt4, monthsAgo: 3, plan: new[]
        {
            (offset: -3, state: PayState.Paid),
            (offset: -2, state: PayState.Paid),
            (offset: -1, state: PayState.Overdue),
            (offset:  0, state: PayState.Issued),
        }, elecKwh: 110m, waterM3: 5m);

        // Sample maintenance/inspection chỉ cho lease 1 (apt1)
        var firstLease = ctx.Leases.Local.OrderBy(l => l.LeaseNumber).First();
        SeedSampleMaintenanceAndInspection(ctx, firstLease, apt1, renter1.Id);

        // ────────────────────────────────────────────────────────
        void BuildLease(AppUser tenant, Apartment apt, int monthsAgo,
                        (int offset, PayState state)[] plan, decimal elecKwh, decimal waterM3)
        {
            leaseCounter++;
            var startDate = new DateTime(today.Year, today.Month, 1).AddMonths(-monthsAgo);
            var endDate = startDate.AddYears(1);

            var lease = new Lease
            {
                LeaseNumber = $"LEASE-{startDate:yyyyMM}-{leaseCounter:D4}",
                ApartmentId = apt.Id,
                PrimaryTenantId = tenant.Id,
                StartDate = startDate, EndDate = endDate,
                MonthlyRent = apt.Price,
                Deposit = apt.DefaultDeposit ?? apt.Price * 2,
                DepositHeld = apt.DefaultDeposit ?? apt.Price * 2,
                BillingDay = 1,
                LateFeePercent = 5, LateFeeAfterDays = 7,
                Status = LeaseStatus.Active,
                ActivatedAt = startDate,
                Notes = $"Hợp đồng mẫu — {tenant.FullName} thuê {apt.UnitCode}."
            };
            ctx.Leases.Add(lease);

            // Phí định kỳ mẫu — để hoá đơn tự sinh các kỳ sau cũng có Internet + phí dịch vụ.
            ctx.RecurringCharges.AddRange(
                new RecurringCharge { Lease = lease, Description = "Internet", Amount = 200000m, SortOrder = 0, IsActive = true },
                new RecurringCharge { Lease = lease, Description = "Phí dịch vụ", Amount = 500000m, SortOrder = 1, IsActive = true });

            decimal prevElec = 1200m;
            decimal prevWater = 45m;

            foreach (var (offset, state) in plan)
            {
                var monthDate = new DateTime(today.Year, today.Month, 1).AddMonths(offset);
                var billingMonth = monthDate.Year * 100 + monthDate.Month;
                var issueDate = monthDate;
                // Hạn thanh toán theo đúng quy tắc thật của hệ thống: NgayChotKy + 7 (tối đa ngày 28).
                var dueDate = new DateTime(monthDate.Year, monthDate.Month, Math.Min(lease.BillingDay + 7, 28));
                // Hoá đơn demo chưa thanh toán xong (Issued/PartiallyPaid): nếu hạn đã ở quá khứ thì
                // lùi tới hôm nay + ân hạn để background service không lập tức gắn Quá hạn, giữ
                // trạng thái mẫu ổn định.
                if (state is PayState.Issued or PayState.PartiallyPaid && dueDate < today)
                    dueDate = today.AddDays(lease.LateFeeAfterDays);

                var elecAmount = elecKwh * 3500m;
                var waterAmount = waterM3 * 25000m;
                var elec = new UtilityReading
                {
                    Lease = lease, UtilityTypeId = utilTypes["electricity"].Id,
                    BillingMonth = billingMonth,
                    PreviousReading = prevElec, CurrentReading = prevElec + elecKwh,
                    Consumption = elecKwh, Rate = 3500m, Amount = elecAmount,
                    ReadAt = monthDate, Billed = true
                };
                var water = new UtilityReading
                {
                    Lease = lease, UtilityTypeId = utilTypes["water"].Id,
                    BillingMonth = billingMonth,
                    PreviousReading = prevWater, CurrentReading = prevWater + waterM3,
                    Consumption = waterM3, Rate = 25000m, Amount = waterAmount,
                    ReadAt = monthDate, Billed = true
                };
                ctx.UtilityReadings.AddRange(elec, water);
                prevElec += elecKwh;
                prevWater += waterM3;

                var subTotal = lease.MonthlyRent + elecAmount + waterAmount + 200000m + 500000m;
                seqInvoice++;
                var invoice = new Invoice
                {
                    InvoiceNumber = $"INV-{billingMonth}-{seqInvoice:D4}",
                    Lease = lease,
                    Kind = InvoiceKind.MonthlyRent, BillingMonth = billingMonth, IsRecurring = true,
                    IssueDate = issueDate, DueDate = dueDate,
                    SubTotal = subTotal,
                    Total = subTotal,
                    Currency = "VND",
                    Items =
                    {
                        new InvoiceItem { Description = $"Tiền thuê căn {apt.UnitCode ?? apt.Title} - {monthDate:MM/yyyy}", Quantity = 1m, UnitPrice = lease.MonthlyRent, LineTotal = lease.MonthlyRent, SortOrder = 0 },
                        new InvoiceItem { Description = "Điện (kWh)", Quantity = elecKwh, UnitPrice = 3500m, LineTotal = elecAmount, SortOrder = 1 },
                        new InvoiceItem { Description = "Nước (m³)", Quantity = waterM3, UnitPrice = 25000m, LineTotal = waterAmount, SortOrder = 2 },
                        new InvoiceItem { Description = "Internet", Quantity = 1m, UnitPrice = 200000m, LineTotal = 200000m, SortOrder = 3 },
                        new InvoiceItem { Description = "Phí dịch vụ", Quantity = 1m, UnitPrice = 500000m, LineTotal = 500000m, SortOrder = 4 }
                    }
                };

                switch (state)
                {
                    case PayState.Paid:
                        invoice.AmountPaid = subTotal;
                        invoice.Balance = 0;
                        invoice.Status = InvoiceStatus.Paid;
                        seqPayment++;
                        invoice.Payments.Add(new Payment
                        {
                            PaymentNumber = $"PAY-{monthDate:yyyyMM}-{seqPayment:D4}",
                            Amount = subTotal,
                            Method = PaymentMethod.BankTransfer,
                            Status = PaymentStatus.Succeeded,
                            PaidAt = dueDate.AddDays(-2),
                            Currency = "VND",
                            Note = "Khách chuyển khoản đúng hạn"
                        });
                        break;
                    case PayState.PartiallyPaid:
                        var partial = Math.Round(subTotal * 0.4m, 0);
                        invoice.AmountPaid = partial;
                        invoice.Balance = subTotal - partial;
                        invoice.Status = InvoiceStatus.PartiallyPaid;
                        seqPayment++;
                        invoice.Payments.Add(new Payment
                        {
                            PaymentNumber = $"PAY-{monthDate:yyyyMM}-{seqPayment:D4}",
                            Amount = partial,
                            Method = PaymentMethod.Cash,
                            Status = PaymentStatus.Succeeded,
                            PaidAt = dueDate.AddDays(1),
                            Currency = "VND",
                            Note = "Khách trả trước một phần, hẹn trả nốt"
                        });
                        break;
                    case PayState.Overdue:
                        invoice.AmountPaid = 0;
                        invoice.Balance = subTotal;
                        invoice.Status = InvoiceStatus.Overdue;
                        break;
                    case PayState.Issued:
                    default:
                        invoice.AmountPaid = 0;
                        invoice.Balance = subTotal;
                        invoice.Status = InvoiceStatus.Issued;
                        break;
                }

                ctx.Invoices.Add(invoice);
            }
        }
    }

    private enum PayState { Issued, PartiallyPaid, Paid, Overdue }
}
