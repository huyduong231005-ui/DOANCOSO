# Rental Preference Matching Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Thiết kế lại trang thuê với sidebar trái, đồng bộ điều kiện thuê của tin đăng và thêm một hồ sơ nhu cầu đang hoạt động cho mỗi tài khoản để lọc bắt buộc, chấm điểm và sắp xếp căn phù hợp nhất.

**Architecture:** Mở rộng `Apartment` bằng các thuộc tính điều kiện thuê chuẩn hóa và lưu hồ sơ người dùng trong aggregate `RentalPreferenceProfile`. Dùng `RentalSearchRequest` làm contract query string thống nhất cho MVC và API; tách `RentalMatchScorer` thành lớp thuần để loại điều kiện bắt buộc và tính điểm mong muốn trước khi phân trang theo đường query hai pha. UI giữ sidebar trái gọn, đưa tiêu chí nâng cao và bản đồ vị trí ưu tiên vào drawer; autocomplete Photon và OpenFreeMap được tách thành helper JavaScript dùng chung với form đăng tin.

**Tech Stack:** ASP.NET Core MVC trên .NET 10, Entity Framework Core, PostgreSQL/Npgsql, EF Core InMemory integration tests, xUnit, Razor, vanilla JavaScript, MapLibre GL JS, OpenFreeMap, Photon autocomplete, Playwright CLI.

---

## Nguyên Tắc Thực Hiện

- Đọc spec trước khi bắt đầu:
  `docs/superpowers/specs/2026-06-02-rental-preference-matching-design.md`.
- Khi bắt đầu triển khai, tạo worktree hoặc branch riêng theo
  `superpowers:using-git-worktrees`; không triển khai trực tiếp trên nhánh đang
  chứa tài liệu nếu chưa chủ động chọn cách làm đó.
- Dùng `superpowers:test-driven-development` cho từng task: viết test fail, chạy
  để quan sát fail đúng lý do, viết code tối thiểu, chạy pass, rồi commit.
- Dùng `frontend-skill` khi thực hiện Task 8 và Task 9 vì các task đó thay đổi bố
  cục và hành vi UI.
- Dùng `playwright` và `superpowers:verification-before-completion` ở Task 12.
- Mọi nội dung mới người dùng nhìn thấy phải là tiếng Việt có dấu.
- Không sửa hoặc commit file cũ chưa track
  `docs/superpowers/plans/2026-05-31-favorites-hardening-and-ui.md`.
- Không thêm notification, nhiều hồ sơ, trọng số tùy chỉnh hoặc mô hình
  key-value; các phần này ngoài phạm vi.

## File Structure

### Create

- `t/Models/Entities/RentalPreferenceProfile.cs`
  - Aggregate một-một theo tài khoản; lưu scalar preference và cờ bắt buộc.
- `t/Models/Entities/RentalPreferenceCategory.cs`
  - Bảng nối profile với loại hình được chấp nhận.
- `t/Models/Entities/RentalPreferenceAmenity.cs`
  - Bảng nối profile với tiện ích; mỗi dòng lưu `IsRequired`.
- `t/Models/ViewModels/RentalPreferenceViewModels.cs`
  - `RentalSearchRequest`, `RentalPreferenceDraft`, kết quả validate và helper
    build route values.
- `t/Application/Queries/Rentals/RentalMatchScorer.cs`
  - Candidate gọn, kết quả match và scorer thuần không phụ thuộc EF Core.
- `t/Application/Queries/Rentals/RentalPreferenceProfileQueryHandler.cs`
  - Đọc profile hiện có và map về draft để áp dụng lại trên trang thuê.
- `t/Application/Commands/RentalPreferences/SaveRentalPreferenceCommand.cs`
  - Contract lưu profile.
- `t/Application/Commands/RentalPreferences/SaveRentalPreferenceCommandHandler.cs`
  - Validate, upsert profile và đồng bộ bảng nối.
- `t/Controllers/RentalPreferencesController.cs`
  - Endpoint POST có authorize và anti-forgery để lưu profile.
- `t/wwwroot/js/address-map-autocomplete.js`
  - Helper MapLibre + Photon dùng chung.
- `t/wwwroot/js/rentals-preferences.js`
  - Drawer, mobile filter, vị trí ưu tiên và tiếp tục lưu sau đăng nhập.
- `t.Tests/Application/RentalMatchScorerTests.cs`
  - Unit test scorer.
- `t.Tests/Integration/RentalPreferenceSchemaTests.cs`
  - Test metadata entity, mapping và migration script.
- `t.Tests/Integration/RentalPreferencePersistenceTests.cs`
  - Test upsert profile và endpoint lưu.
- `t.Tests/Integration/RentalMatchingFlowTests.cs`
  - Test query/API match mode.
- `t.Tests/Integration/RentalPreferenceUiFlowTests.cs`
  - Test Razor hooks, layout contract và JavaScript contract.
- `t/Migrations/<timestamp>_AddRentalPreferenceMatching.cs`
  - Migration do EF Core sinh và được chỉnh backfill có chủ đích.
- `t/Migrations/<timestamp>_AddRentalPreferenceMatching.Designer.cs`
  - Designer do EF Core sinh.

### Modify

- `t/Models/Entities/Apartment.cs`
- `t/Models/Entities/AppUser.cs`
- `t/Data/AppDbContext.cs`
- `t/Migrations/AppDbContextModelSnapshot.cs`
- `t/Infrastructure/Localization/VietnameseLabels.cs`
- `t/Data/SeedData.cs`
- `t/Data/SampleListings.cs`
- `t/Data/SampleHeroes.cs`
- `t/Models/ViewModels/ApartmentViewModels.cs`
- `t/Models/ViewModels/MyListingsViewModels.cs`
- `t/Areas/Admin/Models/AdminViewModels.cs`
- `t/Application/Queries/Rentals/RentalsQueryHandler.cs`
- `t/Application/Commands/Listings/CreateListingCommandHandler.cs`
- `t/Controllers/Api/RentalsApiController.cs`
- `t/Controllers/HomeController.cs`
- `t/Controllers/MyListingsController.cs`
- `t/Areas/Admin/Controllers/ApartmentsController.cs`
- `t/Program.cs`
- `t/Views/Home/Rentals.cshtml`
- `t/Views/Home/PostListing.cshtml`
- `t/Views/Home/ApartmentDetail.cshtml`
- `t/Views/MyListings/Edit.cshtml`
- `t/Areas/Admin/Views/Apartments/Edit.cshtml`
- `t/Views/Shared/_Layout.cshtml`
- `t/wwwroot/js/post-listing.js`
- `t/wwwroot/css/rentals-page.css`
- `t/wwwroot/css/posting-page.css`
- `t.Tests/Integration/PlannedFlowTests.cs`
- `t.Tests/Integration/NearbyDiscoveryFlowTests.cs`

## Task 1: Thêm Enum Điều Kiện Thuê Và Nhãn Tiếng Việt

**Files:**
- Modify: `t/Models/Entities/Apartment.cs`
- Modify: `t/Infrastructure/Localization/VietnameseLabels.cs`
- Create: `t.Tests/Integration/RentalPreferenceSchemaTests.cs`

- [ ] **Step 1: Viết test fail cho enum và nhãn tiếng Việt**

Thêm các test:

```csharp
[Theory]
[InlineData(FurnishingLevel.None, "Chưa có nội thất")]
[InlineData(FurnishingLevel.Basic, "Nội thất cơ bản")]
[InlineData(FurnishingLevel.FullyFurnished, "Đầy đủ nội thất")]
public void FurnishingLevel_ShouldRenderVietnameseLabel(
    FurnishingLevel value, string expected)
{
    Assert.Equal(expected, value.Vi());
}

[Theory]
[InlineData(ParkingType.None, "Không có")]
[InlineData(ParkingType.Motorbike, "Xe máy")]
[InlineData(ParkingType.Car, "Ô tô")]
public void ParkingType_ShouldRenderVietnameseLabel(
    ParkingType value, string expected)
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
```

- [ ] **Step 2: Chạy test để xác nhận fail đúng lý do**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferenceSchemaTests
```

Expected: FAIL vì ba enum và extension `Vi()` chưa tồn tại.

- [ ] **Step 3: Thêm enum và nhãn**

Trong `Apartment.cs`, thêm:

```csharp
public enum FurnishingLevel { None = 0, Basic = 1, FullyFurnished = 2 }
public enum ParkingType { None = 0, Motorbike = 1, Car = 2 }
public enum HouseDirection
{
    East = 0, West = 1, South = 2, North = 3,
    NorthEast = 4, SouthEast = 5, NorthWest = 6, SouthWest = 7
}
```

Trong `VietnameseLabels.cs`, thêm overload `Vi()` cho từng enum. Không trả tên
enum tiếng Anh ở nhánh hợp lệ.

- [ ] **Step 4: Chạy test enum**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferenceSchemaTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- t/Models/Entities/Apartment.cs t/Infrastructure/Localization/VietnameseLabels.cs t.Tests/Integration/RentalPreferenceSchemaTests.cs
git commit -m "feat: add rental condition enums"
```

## Task 2: Thêm Schema Tin Đăng Và Hồ Sơ Nhu Cầu

**Files:**
- Modify: `t/Models/Entities/Apartment.cs`
- Modify: `t/Models/Entities/AppUser.cs`
- Create: `t/Models/Entities/RentalPreferenceProfile.cs`
- Create: `t/Models/Entities/RentalPreferenceCategory.cs`
- Create: `t/Models/Entities/RentalPreferenceAmenity.cs`
- Modify: `t/Data/AppDbContext.cs`
- Modify: `t/Migrations/AppDbContextModelSnapshot.cs`
- Create: `t/Migrations/<timestamp>_AddRentalPreferenceMatching.cs`
- Create: `t/Migrations/<timestamp>_AddRentalPreferenceMatching.Designer.cs`
- Modify: `t.Tests/Integration/RentalPreferenceSchemaTests.cs`

- [ ] **Step 1: Viết test metadata fail**

Thêm test dùng `db.Model`:

```csharp
[Fact]
public void RentalPreferenceProfile_ShouldBeUniquePerUser_AndOwnJoinTables()
{
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var profile = db.Model.FindEntityType(typeof(RentalPreferenceProfile));
    Assert.NotNull(profile);
    Assert.Contains(profile!.GetIndexes(),
        index => index.IsUnique &&
                 index.Properties.Single().Name == nameof(RentalPreferenceProfile.UserId));

    var amenity = db.Model.FindEntityType(typeof(RentalPreferenceAmenity));
    Assert.NotNull(amenity);
    Assert.Equal(
        new[] { nameof(RentalPreferenceAmenity.ProfileId), nameof(RentalPreferenceAmenity.AmenityId) },
        amenity!.FindPrimaryKey()!.Properties.Select(p => p.Name));
}

[Fact]
public void Apartment_ShouldExposeNormalizedRentalConditions()
{
    var properties = typeof(Apartment).GetProperties().Select(p => p.Name).ToHashSet();
    Assert.Contains(nameof(Apartment.FurnishingLevel), properties);
    Assert.Contains(nameof(Apartment.AllowsPets), properties);
    Assert.Contains(nameof(Apartment.ParkingType), properties);
    Assert.Contains(nameof(Apartment.AvailableFrom), properties);
    Assert.Contains(nameof(Apartment.MinLeaseMonths), properties);
    Assert.Contains(nameof(Apartment.MaxLeaseMonths), properties);
    Assert.Contains(nameof(Apartment.HouseDirection), properties);
    Assert.Contains(nameof(Apartment.FloorNumber), properties);
}
```

- [ ] **Step 2: Chạy test metadata để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferenceSchemaTests
```

Expected: FAIL vì entity và mapping chưa tồn tại.

- [ ] **Step 3: Mở rộng `Apartment`**

Thêm property:

```csharp
public FurnishingLevel FurnishingLevel { get; set; }
public bool AllowsPets { get; set; }
public ParkingType ParkingType { get; set; }
public DateOnly AvailableFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
public int MinLeaseMonths { get; set; } = 1;
public int MaxLeaseMonths { get; set; } = 12;
public HouseDirection? HouseDirection { get; set; }
public int? FloorNumber { get; set; }
```

- [ ] **Step 4: Tạo aggregate profile**

`RentalPreferenceProfile` kế thừa `BaseEntity`, có:

```csharp
public string UserId { get; set; } = string.Empty;
public int? RegionId { get; set; }
public decimal? MinPrice { get; set; }
public decimal? MaxPrice { get; set; }
public double? MinArea { get; set; }
public double? MaxArea { get; set; }
public int? MinBedrooms { get; set; }
public string? PreferredAddress { get; set; }
public double? PreferredLatitude { get; set; }
public double? PreferredLongitude { get; set; }
public double? MaxDistanceKm { get; set; }
public FurnishingLevel? FurnishingLevel { get; set; }
public bool? AllowsPets { get; set; }
public ParkingType? ParkingType { get; set; }
public DateOnly? MoveInDate { get; set; }
public int? MinFloor { get; set; }
public int? MaxFloor { get; set; }
public HouseDirection? HouseDirection { get; set; }
public int? MinLeaseMonths { get; set; }
public int? MaxLeaseMonths { get; set; }
```

Thêm cờ bool `RequireRegion`, `RequirePriceRange`, `RequireAreaRange`,
`RequireBedrooms`, `RequireCategoryMatch`, `RequireMaxDistance`,
`RequireFurnishing`, `RequirePets`, `RequireParking`, `RequireMoveInDate`,
`RequireFloorRange`, `RequireDirection`, `RequireLeaseRange`.

Thêm navigation `User`, `Region`, `Categories`, `Amenities`. Hai bảng nối dùng
composite key; `RentalPreferenceAmenity` có thêm `bool IsRequired`.

- [ ] **Step 5: Map schema trong `AppDbContext`**

Thêm `DbSet` cho ba entity, navigation profile trên `AppUser`, mapping tên bảng
tiếng Việt nhất quán với codebase:

```text
HoSoNhuCauThue
HoSoNhuCauThue_DanhMuc
HoSoNhuCauThue_TienIch
```

Map cột `CanHo` mới, unique index profile theo `NguoiDungId`, index cho các cột
lọc phổ biến và delete behavior cascade từ profile sang junction.

- [ ] **Step 6: Sinh migration và bổ sung backfill an toàn**

Run:

```powershell
dotnet ef migrations add AddRentalPreferenceMatching --project .\t\t.csproj --startup-project .\t\t.csproj
```

Trong migration sinh ra:

- thêm cột mới cho `CanHo`;
- `AvailableFrom` dùng `defaultValueSql: "CURRENT_DATE"`;
- `MinLeaseMonths` mặc định `1`;
- `MaxLeaseMonths` mặc định `12`;
- nội thất mặc định `0`, thú cưng mặc định `false`, đậu xe mặc định `0`;
- tạo ba bảng profile;
- chạy SQL backfill nội thất từ tiện ích slug `furniture`;
- chạy SQL backfill đậu xe mức `Motorbike` từ tiện ích slug `parking`;
- không tự suy đoán hướng nhà hoặc tầng.

- [ ] **Step 7: Kiểm tra migration script và chạy test metadata**

Run:

```powershell
dotnet ef migrations script --project .\t\t.csproj --startup-project .\t\t.csproj --output .\output\rental-preference-migration.sql
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferenceSchemaTests
```

Expected: migration script sinh thành công; test PASS.

- [ ] **Step 8: Commit**

```powershell
git add -- t/Models/Entities t/Data/AppDbContext.cs t/Migrations t.Tests/Integration/RentalPreferenceSchemaTests.cs
git commit -m "feat: add rental preference schema"
```

## Task 3: Đồng Bộ Dữ Liệu Mẫu

**Files:**
- Modify: `t/Data/SeedData.cs`
- Modify: `t/Data/SampleListings.cs`
- Modify: `t/Data/SampleHeroes.cs`
- Modify: `t.Tests/Integration/RentalPreferenceSchemaTests.cs`

- [ ] **Step 1: Viết test seed fail**

Thêm:

```csharp
[Fact]
public async Task SeededApartments_ShouldHaveUsableRentalConditions()
{
    using var scope = _factory.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var apartments = await db.Apartments.AsNoTracking().ToListAsync();

    Assert.NotEmpty(apartments);
    Assert.All(apartments, apartment =>
    {
        Assert.True(apartment.AvailableFrom != default);
        Assert.InRange(apartment.MinLeaseMonths, 1, apartment.MaxLeaseMonths);
        Assert.True(Enum.IsDefined(apartment.FurnishingLevel));
        Assert.True(Enum.IsDefined(apartment.ParkingType));
    });

    var centralPark = apartments.Single(x => x.Slug == "vhcp-1pn-view-song");
    Assert.Equal(FurnishingLevel.FullyFurnished, centralPark.FurnishingLevel);
    Assert.Equal(ParkingType.Motorbike, centralPark.ParkingType);
    Assert.Equal(6, centralPark.MinLeaseMonths);
}
```

- [ ] **Step 2: Chạy test seed để xác nhận fail hoặc thiếu dữ liệu có chủ đích**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~SeededApartments_ShouldHaveUsableRentalConditions
```

Expected: FAIL vì known sample vẫn chỉ nhận default chung, chưa có dữ liệu điều
kiện thuê được khai báo rõ theo từng sản phẩm.

- [ ] **Step 3: Mở rộng record mẫu**

Thêm các trường mới vào `SampleListings.Sample`, khai báo rõ theo từng mẫu thay
vì chỉ dựa vào default. Quy ước:

- có amenity `furniture` thì thường là `FullyFurnished`, nếu mô tả chỉ có đồ cơ
  bản thì dùng `Basic`;
- có `parking` thì nhà phố/villa có thể dùng `Car`, căn nhỏ dùng `Motorbike`;
- `AllowsPets` chỉ true cho mẫu có mô tả hoặc giả định demo phù hợp;
- nhà trọ/chung cư mini mặc định khoảng thuê `3-12`, căn hộ `6-24`, nhà nguyên
  căn/villa `12-36`;
- `AvailableFrom` dùng ngày seed ổn định để test dự đoán được.

Khai báo tương tự cho `SampleHeroes` và bốn căn mẫu đầu trong `SeedData`.

- [ ] **Step 4: Chạy test seed và regression hiện có**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~RentalPreferenceSchemaTests|FullyQualifiedName~Nearby"
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- t/Data/SeedData.cs t/Data/SampleListings.cs t/Data/SampleHeroes.cs t.Tests/Integration/RentalPreferenceSchemaTests.cs
git commit -m "feat: synchronize rental condition seed data"
```

## Task 4: Tạo Contract Tìm Kiếm Và Engine Chấm Điểm Thuần

**Files:**
- Create: `t/Models/ViewModels/RentalPreferenceViewModels.cs`
- Create: `t/Application/Queries/Rentals/RentalMatchScorer.cs`
- Create: `t.Tests/Application/RentalMatchScorerTests.cs`

- [ ] **Step 1: Viết test scorer fail**

Tạo candidate factory và các test độc lập:

```csharp
[Fact]
public void Score_ShouldRejectCandidate_WhenRequiredAmenityIsMissing()
{
    var draft = Draft(requiredAmenityIds: [7]);
    var candidate = Candidate(amenityIds: [3, 4]);

    var result = RentalMatchScorer.Score(candidate, draft);

    Assert.False(result.IsEligible);
}

[Fact]
public void Score_ShouldReturnPercentageAndAtMostThreeVietnameseReasons()
{
    var draft = Draft(
        minPrice: 8_000_000,
        maxPrice: 20_000_000,
        minBedrooms: 2,
        furnishing: FurnishingLevel.FullyFurnished,
        preferredLatitude: 10.7942,
        preferredLongitude: 106.7219,
        maxDistanceKm: 5);
    var candidate = MatchingCandidate();

    var result = RentalMatchScorer.Score(candidate, draft);

    Assert.True(result.IsEligible);
    Assert.InRange(result.ScorePercent, 1, 100);
    Assert.InRange(result.Reasons.Count, 1, 3);
    Assert.All(result.Reasons, reason => Assert.False(string.IsNullOrWhiteSpace(reason)));
}

[Fact]
public void Score_ShouldReturnOneHundred_WhenOnlyRequiredCriteriaMatch()
{
    var draft = Draft(minBedrooms: 2, requiredCriteria: ["bedrooms"]);
    var result = RentalMatchScorer.Score(Candidate(bedrooms: 2), draft);
    Assert.True(result.IsEligible);
    Assert.Equal(100, result.ScorePercent);
}
```

Thêm theory cho:

- từng scalar required;
- so sánh nội thất theo cấp tăng dần;
- so sánh parking theo cấp tăng dần;
- ngày vào ở;
- giao khoảng thuê;
- giao khoảng tầng;
- hướng nhà;
- khoảng cách;
- category;
- tiện ích mong muốn tính riêng từng tiện ích;
- lý do ưu tiên vị trí, giá, loại hình, phòng ngủ, nội thất, rồi tiện ích.

- [ ] **Step 2: Chạy unit test scorer để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalMatchScorerTests
```

Expected: FAIL vì scorer và contract chưa tồn tại.

- [ ] **Step 3: Tạo request, draft và validation**

Trong `RentalPreferenceViewModels.cs`, tạo:

```csharp
public sealed class RentalSearchRequest
{
    // Existing filters: Region, MinPrice, MaxPrice, MinArea, MaxArea,
    // CategoryIds, AmenityIds, Sort, Page, PageSize, Category,
    // Latitude, Longitude.
    // Matching fields: MinBedrooms, FurnishingLevel, AllowsPets, ParkingType,
    // AvailableBy, PreferredAddress, PreferredLatitude, PreferredLongitude,
    // MaxDistanceKm, MinFloor, MaxFloor, HouseDirection,
    // MinLeaseMonths, MaxLeaseMonths, RequiredCriteria, RequiredAmenityIds,
    // PendingPreferenceSave.
}

public sealed class RentalPreferenceDraft
{
    // Nullable scalar values, category IDs, amenities with required flags,
    // and required scalar key set.
}
```

Thêm allowlist key:

```csharp
public static class RentalPreferenceCriteria
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>
    {
        "region", "priceRange", "areaRange", "bedrooms", "category",
        "maxDistance", "furnishing", "pets", "parking", "moveInDate",
        "floorRange", "direction", "leaseRange"
    };
}
```

Thêm validator/normalizer:

- MVC mode: bỏ qua key lạ, bỏ required flag không có value, trả warning;
- API mode: key lạ hoặc range sai là invalid;
- validate tọa độ bằng `GeoDistance.ValidatePair`;
- validate enum bằng `Enum.IsDefined`;
- validate range không âm và min không lớn hơn max;
- `requiredAmenityIds` phải là tập con của `amenityIds`.
- `AllowsPets` phía nhu cầu chỉ có ý nghĩa khi là `true`: `null` là không quan
  trọng; không tạo tiêu chí match cho `false`.
- `RequireMaxDistance` chỉ có hiệu lực khi có đủ vị trí ưu tiên hợp lệ và bán
  kính dương.

- [ ] **Step 4: Implement scorer thuần**

Tạo:

```csharp
public sealed record RentalMatchCandidate(
    int Id, DateTime CreatedAt, decimal Price, double Area, int Bedrooms,
    int CategoryId, IReadOnlySet<int> AmenityIds, FurnishingLevel FurnishingLevel,
    bool AllowsPets, ParkingType ParkingType, DateOnly AvailableFrom,
    int MinLeaseMonths, int MaxLeaseMonths, int? FloorNumber,
    HouseDirection? HouseDirection, double? Latitude, double? Longitude);

public sealed record RentalMatchResult(
    bool IsEligible, int ScorePercent, IReadOnlyList<string> Reasons);

public static class RentalMatchScorer
{
    public static RentalMatchResult Score(
        RentalMatchCandidate candidate,
        RentalPreferenceDraft draft);
}
```

Mỗi tiêu chí mong muốn hợp lệ đóng góp một điểm bằng nhau. Mỗi amenity mong muốn
là một điểm riêng. Required criterion chỉ loại candidate; nếu draft chỉ có
required criterion và candidate hợp lệ thì trả `100%`. Dùng tối đa ba lý do
tiếng Việt.

- [ ] **Step 5: Chạy unit test scorer**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalMatchScorerTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add -- t/Models/ViewModels/RentalPreferenceViewModels.cs t/Application/Queries/Rentals/RentalMatchScorer.cs t.Tests/Application/RentalMatchScorerTests.cs
git commit -m "feat: add rental preference matching engine"
```

## Task 5: Tích Hợp Match Mode Vào Query Và API

**Files:**
- Modify: `t/Models/ViewModels/ApartmentViewModels.cs`
- Modify: `t/Models/ViewModels/ApiViewModels.cs`
- Modify: `t/Application/Queries/Rentals/RentalsQueryHandler.cs`
- Modify: `t/Controllers/Api/RentalsApiController.cs`
- Create: `t.Tests/Integration/RentalMatchingFlowTests.cs`

- [ ] **Step 1: Viết integration test fail cho query**

Seed candidate có điểm khác nhau, một căn bị loại required, một căn thiếu dữ
liệu required, và hai căn đồng điểm khác `CreatedAt`/ID. Thêm test:

```csharp
[Fact]
public async Task SearchAsync_ShouldFilterRequiredCriteria_ThenPageByMatchScore()
{
    var result = await handler.SearchAsync(new RentalSearchRequest
    {
        Sort = "match_desc",
        Page = 1,
        PageSize = 2,
        MinBedrooms = 2,
        FurnishingLevel = FurnishingLevel.Basic,
        RequiredCriteria = ["bedrooms"]
    });

    Assert.Equal(eligibleCount, result.TotalCount);
    Assert.Equal(expectedFirstPageIds, result.Apartments.Select(x => x.Id));
    Assert.All(result.Apartments, item => Assert.NotNull(item.MatchPercent));
}
```

Thêm test:

- tie break theo `CreatedAt`, rồi ID;
- pagination sau scoring;
- `match_desc` không tiêu chí fallback mới nhất;
- sort cũ và `distance_asc` vẫn giữ hành vi;
- API query sai trả `400`;
- API `match_desc` hợp lệ trả card có điểm.

- [ ] **Step 2: Chạy integration test để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalMatchingFlowTests
```

Expected: FAIL vì query chưa nhận request và card chưa có match metadata.

- [ ] **Step 3: Mở rộng card và page model**

Thêm vào `ApartmentListViewModel`:

```csharp
public int? MatchPercent { get; set; }
public List<string> MatchReasons { get; set; } = new();
```

Thêm vào `ApartmentListPageViewModel`:

```csharp
public RentalSearchRequest Search { get; set; } = new();
public bool IsMatchSort => Search.Sort == "match_desc";
public bool HasUsableMatchCriteria { get; set; }
```

- [ ] **Step 4: Refactor query handler sang request object**

Đổi chữ ký:

```csharp
public Task<ApartmentListPageViewModel> SearchAsync(
    RentalSearchRequest request,
    CancellationToken cancellationToken = default)
```

Giữ nguyên normal query và nearby query. Thêm match query hai pha:

1. apply required filters dịch được sang SQL;
2. project candidate gọn, gồm resolved floor:
   `a.Floor != null ? a.Floor.Number : a.FloorNumber`;
3. score bằng `RentalMatchScorer`;
4. loại `!IsEligible`;
5. order điểm giảm dần, `CreatedAt` giảm dần, ID giảm dần;
6. page theo ID;
7. query card đầy đủ;
8. gắn điểm và lý do theo dictionary;
9. giữ đúng thứ tự đã score.

Không page trước khi score.

Phân biệt rõ hai chế độ:

- khi sort khác `match_desc`, giữ hành vi cũ: filter khu vực, khoảng giá, diện
  tích, category và amenity là filter cứng;
- khi sort là `match_desc`, các giá trị đã chọn là tiêu chí mong muốn mặc định,
  không lọc cứng; chỉ scalar/category/amenity được đánh dấu required mới loại
  candidate;
- required amenity phải dùng semantics `All`, không dùng `Any`;
- filter trạng thái tin `Active` vẫn luôn áp dụng.

- [ ] **Step 5: Cập nhật API**

Bind:

```csharp
public async Task<ActionResult<RentalsSearchResultViewModel>> Search(
    [FromQuery] RentalSearchRequest request)
```

Clamp `PageSize` về `1..50`, validate strict, trả `BadRequest(new { error })`
khi sai, rồi gọi handler.

- [ ] **Step 6: Chạy test query/API và regression nearby**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~RentalMatchingFlowTests|FullyQualifiedName~NearbyRentalsQueryHandlerTests|FullyQualifiedName~NearbyDiscoveryFlowTests"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add -- t/Models/ViewModels t/Application/Queries/Rentals/RentalsQueryHandler.cs t/Controllers/Api/RentalsApiController.cs t.Tests/Integration/RentalMatchingFlowTests.cs
git commit -m "feat: add best-match rentals search"
```

## Task 6: Lưu Và Đọc Một Hồ Sơ Nhu Cầu Trên Mỗi Tài Khoản

**Files:**
- Create: `t/Application/Commands/RentalPreferences/SaveRentalPreferenceCommand.cs`
- Create: `t/Application/Commands/RentalPreferences/SaveRentalPreferenceCommandHandler.cs`
- Create: `t/Application/Queries/Rentals/RentalPreferenceProfileQueryHandler.cs`
- Create: `t/Controllers/RentalPreferencesController.cs`
- Modify: `t/Program.cs`
- Create: `t.Tests/Integration/RentalPreferencePersistenceTests.cs`

- [ ] **Step 1: Viết test upsert fail**

Thêm:

```csharp
[Fact]
public async Task SaveAsync_ShouldCreateThenUpdateSingleProfilePerUser()
{
    await handler.HandleAsync(new SaveRentalPreferenceCommand(userId, firstDraft));
    await handler.HandleAsync(new SaveRentalPreferenceCommand(userId, changedDraft));

    Assert.Equal(1, await db.RentalPreferenceProfiles.CountAsync(x => x.UserId == userId));
    var saved = await db.RentalPreferenceProfiles
        .Include(x => x.Categories)
        .Include(x => x.Amenities)
        .SingleAsync(x => x.UserId == userId);
    Assert.Equal(changedDraft.MinPrice, saved.MinPrice);
    Assert.Contains(saved.Amenities, x => x.AmenityId == wifiId && x.IsRequired);
}
```

Thêm test:

- bỏ cờ required khi scalar không có value;
- required amenity không thuộc amenity đã chọn bị từ chối;
- category/amenity ID không tồn tại bị từ chối;
- tọa độ sai bị từ chối;
- GET profile query map lại đúng draft.

- [ ] **Step 2: Chạy persistence tests để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferencePersistenceTests
```

Expected: FAIL vì handler chưa tồn tại.

- [ ] **Step 3: Implement command handler**

Handler:

- validate draft strict;
- kiểm tra category, amenity, region tồn tại;
- load profile theo `UserId` với junction;
- create hoặc update scalar;
- replace category links;
- replace amenity links với `IsRequired`;
- save một transaction logic;
- trả result có `Success`, `Errors`.

- [ ] **Step 4: Implement query handler và controller**

Controller:

```csharp
[Authorize]
public sealed class RentalPreferencesController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        RentalSearchRequest request,
        string? returnUrl = null);
}
```

Chỉ redirect local URL. Nếu save lỗi, đặt `TempData["Danger"]` bằng tiếng Việt
và redirect về rentals giữ filter. Nếu thành công, đặt `TempData["Success"]`.

Đăng ký hai handler trong `Program.cs`.

- [ ] **Step 5: Chạy persistence tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferencePersistenceTests
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add -- t/Application/Commands/RentalPreferences t/Application/Queries/Rentals/RentalPreferenceProfileQueryHandler.cs t/Controllers/RentalPreferencesController.cs t/Program.cs t.Tests/Integration/RentalPreferencePersistenceTests.cs
git commit -m "feat: persist rental preference profiles"
```

## Task 7: Bind MVC Search, Hồ Sơ Đã Lưu Và Luồng Đăng Nhập Quay Lại

**Files:**
- Modify: `t/Controllers/HomeController.cs`
- Modify: `t/Models/ViewModels/RentalPreferenceViewModels.cs`
- Modify: `t.Tests/Integration/RentalPreferencePersistenceTests.cs`
- Create: `t.Tests/Integration/RentalPreferenceUiFlowTests.cs`

- [ ] **Step 1: Viết HTTP test fail**

Thêm test:

```csharp
[Fact]
public async Task SaveEndpoint_ShouldRequireLogin()
{
    using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    var response = await client.PostAsync(
        "/RentalPreferences/Save",
        FormWithTokenAndPreference(client, minBedrooms: 2));

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Contains("/Home/Login", response.Headers.Location!.OriginalString);
    Assert.Contains("ReturnUrl=", response.Headers.Location!.OriginalString);
}

[Fact]
public async Task Login_ShouldPreservePendingRentalReturnUrl()
{
    var returnUrl =
        "/Home/Rentals?minBedrooms=2&sort=match_desc&pendingPreferenceSave=1";
    var token = await GetRequestVerificationTokenAsync(
        client, "/Home/Login?returnUrl=" + Uri.EscapeDataString(returnUrl));

    var response = await client.PostAsync(
        "/Home/Login?returnUrl=" + Uri.EscapeDataString(returnUrl),
        ValidLoginForm(token));

    Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    Assert.Equal(returnUrl, response.Headers.Location!.OriginalString);
}
```

Thêm test:

- MVC `match_desc` query sai không 500, fallback mới nhất và có warning;
- rentals GET khi đăng nhập nạp profile hiện có;
- route values của pagination giữ toàn bộ advanced filter;
- trang rentals render `pendingPreferenceSave=1` thành hook tiếp tục lưu;
- local redirect guard chặn URL ngoài host.
- link `Áp dụng hồ sơ đã lưu` build query từ saved draft và đặt
  `sort=match_desc`.

Lưu ý: direct POST anonymous vào endpoint save chỉ cần bị `[Authorize]` chặn.
Luồng UX quay lại đúng filter là trách nhiệm của nút save trong
`rentals-preferences.js`, được kiểm tra bằng static contract và Playwright ở các
task sau.

- [ ] **Step 2: Chạy HTTP tests để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~RentalPreferencePersistenceTests|FullyQualifiedName~RentalPreferenceUiFlowTests"
```

Expected: FAIL vì MVC chưa bind request mới và chưa nạp saved profile.

- [ ] **Step 3: Refactor `HomeController.Rentals`**

Đổi action sang:

```csharp
public async Task<IActionResult> Rentals([FromQuery] RentalSearchRequest request)
```

Trong action:

- validate lenient;
- dùng warning tiếng Việt nếu bỏ input sai;
- gọi query handler;
- load saved profile nếu có `UserId`;
- đưa saved draft vào page model;
- giữ lookup category, amenity, region và favorite;
- giữ category top-nav hiện có.

Trong `RentalSearchRequest`, thêm `ToRouteValues()` để pagination, return URL,
soft navigation và clear pending flag không phải tự lắp query string ở nhiều
nơi. Helper phải preserve list parameters bằng repeated keys.

- [ ] **Step 4: Cập nhật login return URL contract**

Giữ `HomeController.Login` hiện có nhưng đảm bảo:

- chỉ redirect local return URL;
- URL rentals chứa filter và `pendingPreferenceSave=1` được bảo toàn;
- GET không ghi dữ liệu.

- [ ] **Step 5: Chạy HTTP tests và regression auth**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~RentalPreferencePersistenceTests|FullyQualifiedName~RentalPreferenceUiFlowTests|FullyQualifiedName~PlannedFlowTests.Register|FullyQualifiedName~PlannedFlowTests.Forgot"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add -- t/Controllers/HomeController.cs t/Models/ViewModels/RentalPreferenceViewModels.cs t.Tests/Integration/RentalPreferencePersistenceTests.cs t.Tests/Integration/RentalPreferenceUiFlowTests.cs
git commit -m "feat: connect rental profiles to mvc search"
```

## Task 8: Thiết Kế Lại Trang Thuê Với Sidebar Và Drawer

**Files:**
- Modify: `t/Views/Home/Rentals.cshtml`
- Modify: `t/wwwroot/css/rentals-page.css`
- Create: `t/wwwroot/js/rentals-preferences.js`
- Modify: `t/Views/Shared/_Layout.cshtml`
- Modify: `t.Tests/Integration/RentalPreferenceUiFlowTests.cs`

- [ ] **Step 1: Viết Razor/static contract tests fail**

Thêm assertions:

```csharp
Assert.Contains("data-rentals-filter-sidebar", html);
Assert.Contains("data-rentals-results-grid", html);
Assert.Contains("data-rentals-advanced-drawer", html);
Assert.Contains("data-rentals-filter-open", html);
Assert.Contains("data-preference-save", html);
Assert.Contains("data-preference-apply", html);
Assert.Contains("Phù hợp nhất", html);
Assert.Contains("Bộ lọc nâng cao", html);
Assert.Contains("Lưu hồ sơ nhu cầu", html);
```

Đọc CSS/JS và assert:

- desktop grid sidebar `260-280px` + content;
- results desktop ba cột, tablet hai cột;
- mobile drawer;
- listener `luxe:page-loaded`;
- pending save POST hook.

- [ ] **Step 2: Chạy UI contract test để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferenceUiFlowTests
```

Expected: FAIL vì layout mới chưa có.

- [ ] **Step 3: Viết markup sidebar và drawer**

Dùng layout:

```html
<main class="rentals-page-shell">
  <aside class="rentals-filter-sidebar" data-rentals-filter-sidebar>...</aside>
  <section class="rentals-results">...</section>
</main>
<aside class="rentals-advanced-drawer" data-rentals-advanced-drawer hidden>...</aside>
```

Sidebar:

- region, price, category, area, bedrooms, amenity phổ biến;
- sort;
- `Gần bạn`;
- `Bộ lọc nâng cao`;
- `Áp dụng`;
- `Xóa bộ lọc`;
- `Lưu hồ sơ nhu cầu`;
- `Áp dụng hồ sơ đã lưu` nếu có saved profile.

Nút `Áp dụng hồ sơ đã lưu` là link hoặc action GET build từ saved draft, đặt
`sort=match_desc`; không tự ghi đè filter đang nhập khi người dùng chỉ mở trang.

Drawer:

- furnishing, parking, pets, available date;
- vùng nhập địa chỉ ưu tiên và container bản đồ;
- max radius;
- floor range;
- direction;
- lease range;
- toggle required per criterion.

Dùng tiếng Việt có dấu cho mọi text.

- [ ] **Step 4: Render match metadata trên card**

Chỉ khi `Model.IsMatchSort` và card có điểm:

```cshtml
<p class="rentals-match-score">@apartment.MatchPercent% phù hợp</p>
<ul class="rentals-match-reasons">
  @foreach (var reason in apartment.MatchReasons.Take(3))
  {
      <li>@reason</li>
  }
</ul>
```

- [ ] **Step 5: Implement JavaScript drawer và pending save**

`rentals-preferences.js`:

- bind một lần trên `DOMContentLoaded` và `luxe:page-loaded`;
- mở/đóng drawer desktop và mobile;
- sync `requiredCriteria` theo toggle;
- submit filter GET bình thường;
- nếu anonymous bấm save, chuyển sang login với local rentals return URL có
  `pendingPreferenceSave=1`;
- khi trở lại và đã authenticated, form lưu ẩn có anti-forgery được server
  render; JavaScript tự submit đúng một POST;
- bỏ pending flag sau save;
- có nút fallback `Hoàn tất lưu hồ sơ` nếu auto POST không chạy;
- không đụng logic `rentals-nearby.js`.

- [ ] **Step 6: Style responsive**

Trong CSS:

- desktop sidebar `position: sticky`;
- desktop results ba cột;
- tablet hai cột;
- mobile một cột và drawer toàn màn hình;
- score/reason gọn để không làm card quá cao;
- reduced motion giữ được.

- [ ] **Step 7: Chạy UI tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~RentalPreferenceUiFlowTests
```

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add -- t/Views/Home/Rentals.cshtml t/wwwroot/css/rentals-page.css t/wwwroot/js/rentals-preferences.js t/Views/Shared/_Layout.cshtml t.Tests/Integration/RentalPreferenceUiFlowTests.cs
git commit -m "feat: redesign rentals filters and preference drawer"
```

## Task 9: Tách Helper Bản Đồ Địa Chỉ Dùng Chung

**Files:**
- Create: `t/wwwroot/js/address-map-autocomplete.js`
- Modify: `t/wwwroot/js/post-listing.js`
- Modify: `t/wwwroot/js/rentals-preferences.js`
- Modify: `t/Views/Shared/_Layout.cshtml`
- Modify: `t/Views/Home/Rentals.cshtml`
- Modify: `t/Views/Home/PostListing.cshtml`
- Modify: `t/wwwroot/css/rentals-page.css`
- Modify: `t/wwwroot/css/posting-page.css`
- Modify: `t.Tests/Integration/PlannedFlowTests.cs`
- Modify: `t.Tests/Integration/RentalPreferenceUiFlowTests.cs`

- [ ] **Step 1: Viết static contract test fail**

Assert layout load helper trước hai consumer:

```csharp
Assert.Contains("~/js/address-map-autocomplete.js", layout);
Assert.True(
    layout.IndexOf("~/js/address-map-autocomplete.js", StringComparison.Ordinal) <
    layout.IndexOf("~/js/post-listing.js", StringComparison.Ordinal));
```

Assert helper chứa:

- `https://photon.komoot.io/api/`;
- `https://tiles.openfreemap.org/styles/liberty`;
- `window.createLuxeAddressMap`;
- `new AbortController()`;
- listener chọn suggestion;
- marker click và drag.

Assert post listing và rentals preference consumer gọi helper thay vì copy Photon
URL.

- [ ] **Step 2: Chạy test để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~PostListing_ShouldRenderMapLibre|FullyQualifiedName~RentalPreferenceUiFlowTests"
```

Expected: FAIL vì helper chưa tồn tại.

- [ ] **Step 3: Extract helper**

Expose:

```javascript
window.createLuxeAddressMap = async function ({
  mapElement,
  addressInput,
  latitudeInput,
  longitudeInput,
  suggestionsElement,
  statusElement,
  defaultPosition,
  suggestionClassName
}) { /* map, marker, Photon autocomplete, fallback tiếng Việt */ };
```

Helper:

- load `window.loadMapLibre()`;
- khởi tạo marker draggable;
- cập nhật hidden tọa độ sáu chữ số;
- debounce Photon `500ms`, min query `3`, limit `5`;
- abort request cũ;
- lọc feature không có tọa độ;
- fallback vẫn cho nhập địa chỉ thủ công.

- [ ] **Step 4: Refactor post listing và nối drawer**

- `post-listing.js` giữ image picker/category defaults, gọi helper cho
  `#posting-map`.
- `rentals-preferences.js` chỉ khởi tạo map drawer khi mở lần đầu, gọi helper cho
  `#preference-map`.
- Dùng ID riêng cho suggestion/status của hai form để không conflict khi soft
  navigation.

- [ ] **Step 5: Chạy static test và nearby regression**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~PostListing_ShouldRenderMapLibre|FullyQualifiedName~SoftNavigation_ShouldReinitializeMapLibrePages|FullyQualifiedName~RentalPreferenceUiFlowTests|FullyQualifiedName~NearbyDiscoveryFlowTests"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add -- t/wwwroot/js t/Views/Shared/_Layout.cshtml t/Views/Home/Rentals.cshtml t/Views/Home/PostListing.cshtml t/wwwroot/css t.Tests/Integration
git commit -m "refactor: share address map autocomplete"
```

## Task 10: Đồng Bộ Form Đăng Tin Mới

**Files:**
- Modify: `t/Models/ViewModels/ApartmentViewModels.cs`
- Modify: `t/Application/Commands/Listings/CreateListingCommandHandler.cs`
- Modify: `t/Views/Home/PostListing.cshtml`
- Modify: `t/wwwroot/css/posting-page.css`
- Modify: `t.Tests/Integration/PlannedFlowTests.cs`

- [ ] **Step 1: Mở rộng test tạo tin để fail**

Trong test command và multipart HTTP hiện có, gửi:

```csharp
FurnishingLevel = FurnishingLevel.FullyFurnished,
AllowsPets = true,
ParkingType = ParkingType.Car,
AvailableFrom = new DateOnly(2026, 6, 15),
MinLeaseMonths = 6,
MaxLeaseMonths = 24,
HouseDirection = HouseDirection.SouthEast,
FloorNumber = 12
```

Assert entity lưu đúng. Thêm theory payload sai:

- enum nội thất ngoài range;
- enum parking ngoài range;
- ngày vào ở default;
- tầng âm;
- min lease nhỏ hơn `1`;
- min lease lớn hơn max.

Cập nhật mọi fixture tạo `CreateApartmentViewModel` hiện có trong
`PlannedFlowTests` bằng một bộ điều kiện thuê hợp lệ mặc định để các test ảnh,
slug và HTTP cũ tiếp tục fail/pass đúng nguyên nhân ban đầu.

- [ ] **Step 2: Chạy listing tests để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~CreateListingCommand|FullyQualifiedName~PostListing_FormSubmit"
```

Expected: FAIL vì model/handler chưa lưu thuộc tính mới.

- [ ] **Step 3: Mở rộng view model và server validation**

Thêm property nullable vào `CreateApartmentViewModel` cho các input bắt buộc để
phân biệt rõ “chưa chọn” với giá trị enum/bool mặc định:

```csharp
[Required] public FurnishingLevel? FurnishingLevel { get; set; }
[Required] public bool? AllowsPets { get; set; }
[Required] public ParkingType? ParkingType { get; set; }
[Required] public DateOnly? AvailableFrom { get; set; }
[Required] public int? MinLeaseMonths { get; set; }
[Required] public int? MaxLeaseMonths { get; set; }
public HouseDirection? HouseDirection { get; set; }
public int? FloorNumber { get; set; }
```

Dùng DataAnnotations cho required/range cơ bản, nhưng handler vẫn validate:

```csharp
if (!model.FurnishingLevel.HasValue || !Enum.IsDefined(model.FurnishingLevel.Value)) ...
if (!model.AllowsPets.HasValue) ...
if (!model.ParkingType.HasValue || !Enum.IsDefined(model.ParkingType.Value)) ...
if (model.HouseDirection.HasValue && !Enum.IsDefined(model.HouseDirection.Value)) ...
if (!model.AvailableFrom.HasValue) ...
if (model.FloorNumber < 0) ...
if (!model.MinLeaseMonths.HasValue || model.MinLeaseMonths < 1) ...
if (!model.MaxLeaseMonths.HasValue || model.MaxLeaseMonths < model.MinLeaseMonths) ...
```

Sau validation, unwrap và lưu property vào entity.

- [ ] **Step 4: Thêm nhóm UI `Điều kiện thuê`**

Trong `PostListing.cshtml`, thêm card tiếng Việt:

- tình trạng nội thất;
- cho phép thú cưng;
- loại chỗ đậu xe;
- ngày có thể vào ở;
- thời hạn thuê tối thiểu/tối đa;
- tầng;
- hướng nhà với lựa chọn `Chưa xác định`.

Trường cốt lõi required; tầng/hướng optional.

- [ ] **Step 5: Chạy listing tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~CreateListingCommand|FullyQualifiedName~PostListing"
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add -- t/Models/ViewModels/ApartmentViewModels.cs t/Application/Commands/Listings/CreateListingCommandHandler.cs t/Views/Home/PostListing.cshtml t/wwwroot/css/posting-page.css t.Tests/Integration/PlannedFlowTests.cs
git commit -m "feat: collect rental conditions on listing creation"
```

## Task 11: Đồng Bộ Màn Sửa Chủ Nhà, Admin Và Chi Tiết

**Files:**
- Modify: `t/Models/ViewModels/MyListingsViewModels.cs`
- Modify: `t/Controllers/MyListingsController.cs`
- Modify: `t/Views/MyListings/Edit.cshtml`
- Modify: `t/Areas/Admin/Models/AdminViewModels.cs`
- Modify: `t/Areas/Admin/Controllers/ApartmentsController.cs`
- Modify: `t/Areas/Admin/Views/Apartments/Edit.cshtml`
- Modify: `t/Views/Home/ApartmentDetail.cshtml`
- Modify: `t.Tests/Integration/PlannedFlowTests.cs`

- [ ] **Step 1: Viết regression test fail**

Thêm HTTP/integration tests:

- host GET edit render nhóm `Điều kiện thuê`;
- host POST edit cập nhật property mới cho tin thuộc quyền sở hữu;
- admin GET edit render nhóm `Điều kiện thuê`;
- admin POST edit cập nhật property mới;
- detail render nhãn tiếng Việt, không lộ tên enum tiếng Anh.

- [ ] **Step 2: Chạy test để xác nhận fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~RentalConditionEdit|FullyQualifiedName~ApartmentDetail_ShouldRenderRentalConditions"
```

Expected: FAIL vì view model/controller/view chưa đồng bộ.

- [ ] **Step 3: Mở rộng host edit**

Thêm property mới vào `EditListingViewModel`, map hai chiều trong
`MyListingsController`, validate enum/range giống create handler, và thêm nhóm
UI tiếng Việt trong `Views/MyListings/Edit.cshtml`.

- [ ] **Step 4: Mở rộng admin edit**

Thêm property mới vào `ApartmentEditVm`, map hai chiều trong admin
`ApartmentsController`, validation server, và nhóm UI tiếng Việt trong view.
Trong action admin `Create()`, điền default hợp lệ cho nội thất, thú cưng, chỗ
đậu xe, ngày vào ở và khoảng thuê để form tạo mới không bắt đầu ở trạng thái
không hợp lệ.

- [ ] **Step 5: Hiển thị tóm tắt trên detail**

Thêm vùng `Điều kiện thuê` gọn:

- nội thất;
- thú cưng;
- chỗ đậu xe;
- ngày có thể vào ở;
- thời hạn thuê;
- tầng/hướng nếu có.

Luôn dùng `Vi()`.

- [ ] **Step 6: Chạy test edit/detail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~RentalConditionEdit|FullyQualifiedName~ApartmentDetail"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add -- t/Models/ViewModels/MyListingsViewModels.cs t/Controllers/MyListingsController.cs t/Views/MyListings/Edit.cshtml t/Areas/Admin t/Views/Home/ApartmentDetail.cshtml t.Tests/Integration/PlannedFlowTests.cs
git commit -m "feat: synchronize rental condition editing"
```

## Task 12: Verification Tự Động Và Playwright Trình Duyệt Thật

**Files:**
- Modify only if a verified regression requires a focused fix.

- [ ] **Step 1: Chạy formatting/build**

Run:

```powershell
dotnet build .\t\t.csproj
```

Expected: exit `0`, không warning mới.

- [ ] **Step 2: Chạy toàn bộ test**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj
```

Expected: toàn bộ test PASS.

- [ ] **Step 3: Kiểm tra migration PostgreSQL**

Run:

```powershell
dotnet ef migrations script --project .\t\t.csproj --startup-project .\t\t.csproj --output .\output\rental-preference-migration.sql
```

Expected: sinh script thành công. Nếu Docker/PostgreSQL local khả dụng, chạy
migration trên database test sạch và rollback/recreate theo quy trình local.
Nếu không khả dụng, ghi rõ giới hạn xác minh.

- [ ] **Step 4: Khởi động app local và test desktop**

Dùng app tại `http://localhost:5073` và Playwright headed. Kiểm tra:

1. `/Home/Rentals` có sidebar trái sticky.
2. Desktop đủ rộng có ba card mỗi hàng.
3. Drawer nâng cao mở/đóng được.
4. Khách chưa đăng nhập chọn filter và `Phù hợp nhất`.
5. Match card có phần trăm và tối đa ba lý do.
6. `Gần bạn` vẫn xin geolocation chỉ sau click.
7. Pagination giữ advanced filter.

- [ ] **Step 5: Test login-return-save**

Trong browser:

1. chưa đăng nhập nhập filter và vị trí ưu tiên;
2. bấm `Lưu hồ sơ nhu cầu`;
3. xác nhận redirect login;
4. đăng nhập `host@luxehaven.vn` / `Host@123`;
5. xác nhận quay lại đúng filter;
6. xác nhận profile được lưu một lần;
7. chỉnh filter, lưu lại và xác nhận không tạo profile thứ hai;
8. bấm `Áp dụng hồ sơ đã lưu`.

- [ ] **Step 6: Test map và form đăng tin**

Trong browser:

1. autocomplete Photon vị trí ưu tiên trả suggestion;
2. click suggestion cập nhật marker;
3. click map và drag marker đổi tọa độ;
4. vào `/Home/PostListing`;
5. xác nhận map đăng tin cũ vẫn hoạt động;
6. xác nhận card `Điều kiện thuê` có validation tiếng Việt;
7. submit tin hợp lệ;
8. sửa lại tin qua `Tin của tôi`;
9. kiểm tra detail hiển thị tóm tắt điều kiện thuê.

- [ ] **Step 7: Test responsive**

Playwright viewport:

```text
Desktop: 1440x1000
Tablet: 900x1000
Mobile: 390x844
```

Xác nhận desktop ba cột, tablet hai cột, mobile một cột và drawer toàn màn hình.

- [ ] **Step 8: Smoke test ít nhất mười màn hình**

Kiểm tra status, title, heading và console error:

```text
/
/Home/Rentals
/du-an
/du-an/landmark-riverside-collection
/Home/Login
/Home/Register
/Home/ForgotPassword
/Home/PostListing
/Favorites
/Home/ApartmentDetail/1
/MyListings
/Admin/Apartments
```

Guest route cần auth phải redirect đúng. Console phải không có error ứng dụng.
Ghi riêng warning MapLibre/OpenFreeMap không chặn luồng nếu vẫn còn.

- [ ] **Step 9: Kiểm tra worktree**

Run:

```powershell
git diff --check
git status --short
git log --oneline -15
```

Expected:

- không whitespace error;
- chỉ còn thay đổi thuộc phạm vi;
- file cũ chưa track
  `docs/superpowers/plans/2026-05-31-favorites-hardening-and-ui.md`
  vẫn không bị stage.

- [ ] **Step 10: Commit fix xác minh nếu có**

Chỉ commit nếu browser hoặc full suite phát hiện regression và đã sửa bằng
test fail trước. Chỉ stage đúng các file của fix đã xác minh, rồi commit:
`fix: resolve rental preference regressions`.

## Checklist Nghiệm Thu Cuối

- [ ] Trang thuê desktop có sidebar trái và ba card mỗi hàng.
- [ ] Tablet hai cột; mobile một cột và drawer toàn màn hình.
- [ ] Mọi nhãn, validation và lý do mới là tiếng Việt có dấu.
- [ ] Khách chưa đăng nhập lọc thử và dùng `Phù hợp nhất`.
- [ ] Lưu dở redirect login rồi quay lại đúng filter.
- [ ] Mỗi tài khoản chỉ có một profile được upsert.
- [ ] Required criterion loại căn không đạt trước phân trang.
- [ ] Desired criterion tính điểm và tối đa ba lý do.
- [ ] Các sort cũ, `Gần bạn`, pagination, favorite và soft navigation còn hoạt động.
- [ ] Đăng tin mới, sửa tin host, sửa admin và detail dùng cùng điều kiện thuê.
- [ ] Dữ liệu mẫu có điều kiện thuê nhất quán.
- [ ] Build, full tests, migration script và Playwright smoke có bằng chứng.
