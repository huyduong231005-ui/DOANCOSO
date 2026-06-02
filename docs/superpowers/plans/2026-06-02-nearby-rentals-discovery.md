# Nearby Rentals Discovery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add nearest-first apartment recommendations and an explicit `Gần bạn` rentals-list action that sorts matching listings by browser location without adding radius controls.

**Architecture:** Introduce a small `GeoDistance` utility for coordinate-pair validation, Haversine calculation, and Vietnamese display labels. Keep normal rentals searches on the current database paging path; use a provider-independent two-phase path only for nearby mode: filter in the database, load lightweight coordinate metadata, calculate and order distances in memory before paging, then load the selected page cards. Move detail-page recommendation selection into a focused query handler so fallback behavior can be tested without growing `HomeController`.

**Tech Stack:** ASP.NET Core MVC on .NET 10, Entity Framework Core, PostgreSQL/Npgsql, EF Core InMemory integration tests, Razor views, vanilla JavaScript, existing toast API.

---

## Workspace Note

The current workspace already contains uncommitted map work in files that this
feature must extend, especially:

- `t/Models/ViewModels/ApartmentViewModels.cs`
- `t/Views/Home/ApartmentDetail.cshtml`
- `t/Views/Shared/_Layout.cshtml`
- `t/wwwroot/css/apartment-detail.css`
- `t.Tests/Integration/PlannedFlowTests.cs`

Do not revert those edits. Before execution, either commit the existing map
baseline or carry the dirty state forward deliberately. Keep each feature commit
scoped to the files listed in its task.

## File Structure

### Create

- `t/Infrastructure/Geo/GeoDistance.cs`
  - Own coordinate-pair validation, Haversine calculation, and user-facing
    distance formatting.
- `t/Application/Queries/Rentals/NearbyApartmentRecommendationsQueryHandler.cs`
  - Own nearest-first detail recommendations and same-region fallback.
- `t/Infrastructure/Formatting/RentalPriceFormatter.cs`
  - Move the existing compact `tr` / `k` detail-card price formatting out of
    `HomeController` so the new handler can reuse it.
- `t/wwwroot/js/rentals-nearby.js`
  - Own the click-triggered browser geolocation workflow.
- `t.Tests/Infrastructure/GeoDistanceTests.cs`
  - Cover validation, calculation, and formatting.
- `t.Tests/Integration/NearbyRentalsQueryHandlerTests.cs`
  - Cover nearby query sorting, filters, unlocated fallback, and paging.
- `t.Tests/Integration/NearbyApartmentRecommendationsQueryHandlerTests.cs`
  - Cover detail recommendation ordering and fallback.
- `t.Tests/Integration/NearbyDiscoveryFlowTests.cs`
  - Cover HTTP validation, Razor hooks, pagination preservation, and static
    JavaScript contract checks.

### Modify

- `t/Models/ViewModels/ApartmentViewModels.cs`
  - Expose optional card distance and nearby-mode page state.
- `t/Models/ApartmentDetailViewModels.cs`
  - Expose optional recommendation distance.
- `t/Application/Queries/Rentals/RentalsQueryHandler.cs`
  - Add optional nearby-mode query path while preserving existing search paths.
- `t/Controllers/Api/RentalsApiController.cs`
  - Validate coordinate pairs strictly and pass nearby parameters.
- `t/Controllers/HomeController.cs`
  - Handle resilient MVC nearby input and delegate recommendation selection.
- `t/Program.cs`
  - Register the new detail recommendation query handler.
- `t/Views/Home/Rentals.cshtml`
  - Add the `Gần bạn` action, hidden nearby state, distance labels, and pagination
    preservation.
- `t/Views/Home/ApartmentDetail.cshtml`
  - Rename the recommendation section and render optional distances.
- `t/Views/Shared/_Layout.cshtml`
  - Load `rentals-nearby.js`.
- `t/wwwroot/css/rentals-page.css`
  - Style the nearby action and distance labels.
- `t/wwwroot/css/apartment-detail.css`
  - Style detail recommendation distance labels.

## Task 1: Add the Shared Geospatial Utility

**Files:**
- Create: `t/Infrastructure/Geo/GeoDistance.cs`
- Create: `t.Tests/Infrastructure/GeoDistanceTests.cs`

- [ ] **Step 1: Write failing utility tests**

Add tests covering:

```csharp
[Theory]
[InlineData(null, null, true, false)]
[InlineData(10.0, null, false, false)]
[InlineData(null, 106.0, false, false)]
[InlineData(91.0, 106.0, false, false)]
[InlineData(10.0, 181.0, false, false)]
[InlineData(10.0, 106.0, true, true)]
public void ValidatePair_ShouldReportValidityAndActivation(
    double? latitude, double? longitude, bool expectedValid, bool expectedActive)
{
    var result = GeoDistance.ValidatePair(latitude, longitude);

    Assert.Equal(expectedValid, result.IsValid);
    Assert.Equal(expectedActive, result.IsActive);
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
```

Add explicit non-finite assertions with `double.NaN`,
`double.PositiveInfinity`, and `double.NegativeInfinity`.

- [ ] **Step 2: Run the tests to verify they fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~GeoDistanceTests
```

Expected: FAIL because `GeoDistance` does not exist.

- [ ] **Step 3: Implement the minimal utility**

Create a static utility with:

```csharp
public readonly record struct GeoCoordinatePairValidation(
    bool IsValid,
    bool IsActive,
    string? Error);

public static class GeoDistance
{
    private const double EarthRadiusKm = 6371.0088;

    public static GeoCoordinatePairValidation ValidatePair(double? latitude, double? longitude);
    public static bool IsValidCoordinate(double? latitude, double? longitude);
    public static double CalculateKm(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude);
    public static string FormatKm(double distanceKm);
}
```

Rules:

- no coordinates means valid but inactive;
- exactly one coordinate means invalid;
- non-finite or out-of-range values mean invalid;
- latitude range is `-90..90`;
- longitude range is `-180..180`;
- formatting uses `CultureInfo.GetCultureInfo("vi-VN")`;
- values below `1 km` render rounded meters;
- values at or above `1 km` render one decimal kilometer.

- [ ] **Step 4: Run the tests to verify they pass**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~GeoDistanceTests
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add -- t/Infrastructure/Geo/GeoDistance.cs t.Tests/Infrastructure/GeoDistanceTests.cs
git commit -m "feat: add shared geo distance utility"
```

## Task 2: Add Nearby Ordering to Rentals Queries and API Validation

**Files:**
- Create: `t.Tests/Integration/NearbyRentalsQueryHandlerTests.cs`
- Modify: `t/Models/ViewModels/ApartmentViewModels.cs`
- Modify: `t/Application/Queries/Rentals/RentalsQueryHandler.cs`
- Modify: `t/Controllers/Api/RentalsApiController.cs`

- [ ] **Step 1: Write failing query-handler tests**

Use an isolated EF Core InMemory `AppDbContext`. Seed active apartments with:

- a near located listing;
- a far located listing;
- an unlocated listing;
- a closer listing excluded by an existing region or category filter.

Add tests proving:

```csharp
var result = await handler.SearchAsync(
    region: selectedRegionSlug,
    minPrice: null,
    maxPrice: null,
    minArea: null,
    maxArea: null,
    categoryIds: null,
    amenityIds: null,
    sort: "distance_asc",
    page: 1,
    pageSize: 10,
    latitude: 10.7942,
    longitude: 106.7219);

Assert.Collection(
    result.Apartments,
    item => Assert.Equal(nearId, item.Id),
    item => Assert.Equal(farId, item.Id),
    item => Assert.Equal(unlocatedId, item.Id));
Assert.NotNull(result.Apartments[0].DistanceKm);
Assert.Null(result.Apartments[^1].DistanceKm);
```

Add a second test with `pageSize: 1` and `page: 2` proving paging occurs after
distance ordering.

- [ ] **Step 2: Run the query tests to verify they fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~NearbyRentalsQueryHandlerTests
```

Expected: FAIL because nearby parameters and `DistanceKm` do not exist.

- [ ] **Step 3: Extend the rentals view models**

Add:

```csharp
public double? DistanceKm { get; set; }
```

to `ApartmentListViewModel`.

Add to `ApartmentListPageViewModel`:

```csharp
public double? Latitude { get; set; }
public double? Longitude { get; set; }
public bool IsNearbySort =>
    SortBy == "distance_asc" &&
    Latitude.HasValue &&
    Longitude.HasValue;
```

- [ ] **Step 4: Implement the nearby rentals query path**

Extend `SearchAsync` with optional trailing parameters before the cancellation
token:

```csharp
double? latitude = null,
double? longitude = null,
CancellationToken cancellationToken = default
```

Keep the current database ordering and paging code unchanged for non-nearby
searches.

For active nearby mode:

1. Keep all existing database filters.
2. Calculate `totalCount` from the filtered query.
3. Project only `Id`, `Latitude`, `Longitude`, and `CreatedAt`.
4. Materialize that lightweight candidate list.
5. Use `GeoDistance.IsValidCoordinate` and `GeoDistance.CalculateKm`.
6. Sort located candidates by distance ascending, then `CreatedAt` descending,
   then `Id` descending.
7. Sort unlocated candidates afterward by `CreatedAt` descending, then `Id`
   descending.
8. Apply `Skip` and `Take` to the ordered candidate IDs.
9. Load card projections only for selected IDs.
10. Reorder loaded cards to the selected-ID order and attach `DistanceKm`.
11. Set page-model `Latitude` and `Longitude` only while nearby mode is active;
    do not preserve unused coordinates for unrelated sort modes.

This two-phase branch is intentional: filtering stays in the database, while
Haversine ordering behaves identically under Npgsql and EF Core InMemory. Do not
page before distance ordering and do not impose an arbitrary radius or candidate
cap.

- [ ] **Step 5: Run the query tests to verify they pass**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~NearbyRentalsQueryHandlerTests
```

Expected: PASS.

- [ ] **Step 6: Write failing HTTP API validation tests**

In `t.Tests/Integration/NearbyDiscoveryFlowTests.cs`, add:

```csharp
[Theory]
[InlineData("/api/rentals/search?latitude=10.7")]
[InlineData("/api/rentals/search?longitude=106.7")]
[InlineData("/api/rentals/search?latitude=91&longitude=106.7")]
[InlineData("/api/rentals/search?latitude=10.7&longitude=181")]
[InlineData("/api/rentals/search?latitude=NaN&longitude=106.7")]
[InlineData("/api/rentals/search?latitude=Infinity&longitude=106.7")]
[InlineData("/api/rentals/search?latitude=abc&longitude=106.7")]
public async Task RentalsApi_ShouldRejectInvalidCoordinatePairs(string url)
{
    var response = await _client.GetAsync(url);

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

Add an HTTP test for
`/api/rentals/search?sort=distance_asc&latitude=10.7942&longitude=106.7219`
that asserts located cards have ascending `DistanceKm`.

- [ ] **Step 7: Run the HTTP tests to verify they fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~NearbyDiscoveryFlowTests
```

Expected: FAIL because the API does not validate or pass coordinates yet.

- [ ] **Step 8: Add strict API validation**

Extend `RentalsApiController.Search` with `double? latitude` and
`double? longitude`.

Validate with:

```csharp
var coordinates = GeoDistance.ValidatePair(latitude, longitude);
if (!coordinates.IsValid)
    return BadRequest(new { error = coordinates.Error });
```

Pass the coordinate pair to `RentalsQueryHandler.SearchAsync`. Both coordinates
may be omitted for existing non-nearby searches. A valid pair only changes order
when `sort=distance_asc`.

- [ ] **Step 9: Run the HTTP tests and existing rentals tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~NearbyDiscoveryFlowTests|FullyQualifiedName~RentalsApi_ShouldApplyFilterSortAndPaging"
```

Expected: PASS.

- [ ] **Step 10: Commit**

```powershell
git add -- t/Models/ViewModels/ApartmentViewModels.cs t/Application/Queries/Rentals/RentalsQueryHandler.cs t/Controllers/Api/RentalsApiController.cs t.Tests/Integration/NearbyRentalsQueryHandlerTests.cs t.Tests/Integration/NearbyDiscoveryFlowTests.cs
git commit -m "feat: sort rentals by nearby location"
```

## Task 3: Add Nearest-First Detail Recommendations

**Files:**
- Create: `t/Application/Queries/Rentals/NearbyApartmentRecommendationsQueryHandler.cs`
- Create: `t/Infrastructure/Formatting/RentalPriceFormatter.cs`
- Create: `t.Tests/Integration/NearbyApartmentRecommendationsQueryHandlerTests.cs`
- Modify: `t/Models/ApartmentDetailViewModels.cs`
- Modify: `t/Controllers/HomeController.cs`
- Modify: `t/Program.cs`

- [ ] **Step 1: Write failing recommendation-handler tests**

Use an isolated EF Core InMemory `AppDbContext`. Add:

```csharp
[Fact]
public async Task GetAsync_ShouldReturnNearestActiveApartments_ThenUnlocatedFallback()
{
    // Seed current, near, far, unlocated, and hidden apartments.
    var result = await handler.GetAsync(currentApartment, take: 3);

    Assert.Collection(
        result,
        item => Assert.Equal(nearId, item.Id),
        item => Assert.Equal(farId, item.Id),
        item => Assert.Equal(unlocatedId, item.Id));
    Assert.NotNull(result[0].DistanceKm);
    Assert.Null(result[^1].DistanceKm);
}

[Fact]
public async Task GetAsync_ShouldUseSameRegionNewestFirst_WhenCurrentApartmentIsUnlocated()
{
    var result = await handler.GetAsync(unlocatedCurrentApartment, take: 3);

    Assert.Equal(expectedSameRegionNewestIds, result.Select(item => item.Id));
    Assert.All(result, item => Assert.Null(item.DistanceKm));
}
```

- [ ] **Step 2: Run the recommendation tests to verify they fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~NearbyApartmentRecommendationsQueryHandlerTests
```

Expected: FAIL because the handler and `DistanceKm` do not exist.

- [ ] **Step 3: Extend the detail recommendation view model**

Add to `SimilarApartmentViewModel`:

```csharp
public double? DistanceKm { get; init; }
```

- [ ] **Step 4: Implement the focused recommendation handler**

Create a scoped query handler:

```csharp
public sealed class NearbyApartmentRecommendationsQueryHandler
{
    public NearbyApartmentRecommendationsQueryHandler(AppDbContext db);

    public Task<IReadOnlyList<SimilarApartmentViewModel>> GetAsync(
        Apartment currentApartment,
        int take = 3,
        CancellationToken cancellationToken = default);
}
```

Behavior:

- exclude the current apartment;
- include only active apartments;
- if the current apartment is unlocated, keep the current same-region,
  newest-first fallback;
- if the current apartment is located, load lightweight card candidates,
  calculate optional distances in memory, sort located first by distance, then
  creation time and ID, put unlocated cards last, and take three;
- map image, title, category, area, bedrooms, formatted price, address, and
  optional `DistanceKm`.

Reuse `GeoDistance`; do not duplicate coordinate or Haversine logic.

Move the existing private `HomeController.FormatPrice` logic into:

```csharp
public static class RentalPriceFormatter
{
    public static string Format(decimal price);
}
```

Use the shared formatter in both the recommendation handler and the current
apartment-detail view-model mapping. Remove the now-unused private controller
method.

- [ ] **Step 5: Register and use the handler**

In `Program.cs`:

```csharp
builder.Services.AddScoped<NearbyApartmentRecommendationsQueryHandler>();
```

Inject the handler into `HomeController`, then replace the inline query in
`ApartmentDetail` with:

```csharp
var similar = await _nearbyApartmentRecommendationsQueryHandler.GetAsync(apartment);
```

- [ ] **Step 6: Run recommendation tests and existing detail tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter "FullyQualifiedName~NearbyApartmentRecommendationsQueryHandlerTests|FullyQualifiedName~ApartmentDetail"
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add -- t/Application/Queries/Rentals/NearbyApartmentRecommendationsQueryHandler.cs t/Infrastructure/Formatting/RentalPriceFormatter.cs t.Tests/Integration/NearbyApartmentRecommendationsQueryHandlerTests.cs t/Models/ApartmentDetailViewModels.cs t/Controllers/HomeController.cs t/Program.cs
git commit -m "feat: recommend nearby apartments on detail pages"
```

## Task 4: Add the Rentals-Page Nearby UX

**Files:**
- Modify: `t/Controllers/HomeController.cs`
- Modify: `t/Views/Home/Rentals.cshtml`
- Modify: `t/Views/Home/ApartmentDetail.cshtml`
- Modify: `t/Views/Shared/_Layout.cshtml`
- Create: `t/wwwroot/js/rentals-nearby.js`
- Modify: `t/wwwroot/css/rentals-page.css`
- Modify: `t/wwwroot/css/apartment-detail.css`
- Modify: `t.Tests/Integration/NearbyDiscoveryFlowTests.cs`

- [ ] **Step 1: Write failing MVC and static JavaScript contract tests**

Add tests proving:

```csharp
[Fact]
public async Task RentalsPage_ShouldRenderNearbyAction_AndPreserveNearbyState()
{
    var html = await _client.GetStringAsync(
        "/Home/Rentals?sort=distance_asc&latitude=10.7942&longitude=106.7219");

    Assert.Contains("data-nearby-rentals", html, StringComparison.Ordinal);
    Assert.Contains("name=\"latitude\"", html, StringComparison.Ordinal);
    Assert.Contains("name=\"longitude\"", html, StringComparison.Ordinal);
    Assert.Contains("sort=distance_asc", html, StringComparison.Ordinal);
}

[Fact]
public async Task RentalsPage_ShouldIgnoreInvalidNearbyUrl_WithoutServerError()
{
    var response = await _client.GetAsync(
        "/Home/Rentals?sort=distance_asc&latitude=91&longitude=106.7");
    var html = await response.Content.ReadAsStringAsync();

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Contains("data-toast=\"warning\"", html, StringComparison.Ordinal);
}

[Fact]
public void RentalsNearbyScript_ShouldRequestLocationOnlyInsideClickHandler()
{
    var script = File.ReadAllText(GetWebRootFile("rentals-nearby.js"));

    Assert.Contains("data-nearby-rentals", script, StringComparison.Ordinal);
    Assert.Contains("addEventListener('click'", script, StringComparison.Ordinal);
    Assert.Contains("navigator.geolocation.getCurrentPosition", script, StringComparison.Ordinal);
    Assert.Contains("searchParams.delete('page')", script, StringComparison.Ordinal);
    Assert.Contains("luxe:page-loaded", script, StringComparison.Ordinal);
}
```

Make the pagination assertion target a result set with more than one page so it
verifies a real page link.

- [ ] **Step 2: Run the MVC tests to verify they fail**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~NearbyDiscoveryFlowTests
```

Expected: FAIL because MVC nearby state and the JavaScript file do not exist.

- [ ] **Step 3: Add resilient MVC coordinate handling**

Extend `HomeController.Rentals` with optional `latitude` and `longitude`.

Validate the pair with `GeoDistance.ValidatePair`. If invalid:

```csharp
TempData["Warning"] = "Vị trí không hợp lệ. Danh sách đang hiển thị theo thứ tự mặc định.";
latitude = null;
longitude = null;
if (sort == "distance_asc")
    sort = null;
```

Pass valid values to `RentalsQueryHandler.SearchAsync`.

Also inspect `ModelState` for binding failures on `latitude` and `longitude`.
A manually edited value such as `latitude=abc` must follow the same warning and
fallback branch even though model binding cannot pass it into `GeoDistance`.
Extend the invalid-MVC-URL test with that malformed-string case.

- [ ] **Step 4: Render nearby state and readable distances**

In `Rentals.cshtml`:

- add a `type="button"` secondary action with `data-nearby-rentals`;
- add a short status target with `data-nearby-rentals-status`;
- render a selected `distance_asc` option labeled `Gần bạn` in the sort dropdown
  only while `Model.IsNearbySort` is true; users enter nearby mode through the
  explicit action rather than selecting a distance sort without coordinates;
- while `Model.IsNearbySort`, render hidden `latitude` and `longitude` form
  inputs with invariant values;
- pass coordinates into pagination `Url.Action`;
- render `GeoDistance.FormatKm(apartment.DistanceKm.Value)` only when distance
  exists.

In `ApartmentDetail.cshtml`:

- rename `Căn hộ tương tự` to `Căn hộ gần vị trí này`;
- render a compact distance label only when `item.DistanceKm` exists.

Extend the MVC flow tests to assert that nearby rentals HTML and a geocoded
detail page contain a rendered `Cách ...` label, while the detail page contains
the renamed `Căn hộ gần vị trí này` heading.

- [ ] **Step 5: Implement click-triggered geolocation**

Create `rentals-nearby.js` as an idempotent initializer:

```javascript
(function () {
    'use strict';

    function initNearbyRentals() {
        document.querySelectorAll('[data-nearby-rentals]').forEach((button) => {
            if (button.dataset.nearbyBound === 'true') return;
            button.dataset.nearbyBound = 'true';

            button.addEventListener('click', () => {
                // Check support, disable while resolving, and request position.
                // On success preserve filters with URLSearchParams, reset page,
                // set latitude, longitude, sort=distance_asc, then navigate.
                // On error keep current results and call window.toast.warning.
            });
        });
    }

    document.addEventListener('DOMContentLoaded', initNearbyRentals);
    document.addEventListener('luxe:page-loaded', initNearbyRentals);
})();
```

Use:

```javascript
navigator.geolocation.getCurrentPosition(success, failure, {
    enableHighAccuracy: false,
    timeout: 10000,
    maximumAge: 300000
});
```

Do not call geolocation outside the click callback. On success use
`new URL(window.location.href)`, delete `page`, set invariant coordinate strings,
set `sort=distance_asc`, and navigate. On error restore the button and show a
short Vietnamese warning through `window.toast?.warning`.

- [ ] **Step 6: Load the JavaScript and add focused CSS**

Load `~/js/rentals-nearby.js` in `_Layout.cshtml` after `toast.js` or ensure the
script safely handles the toast API not being initialized yet.

Add small, named CSS classes for:

- nearby secondary action;
- nearby loading state;
- rentals card distance label;
- detail recommendation distance label.

Avoid broad selectors that affect favorite buttons or existing map styling.

- [ ] **Step 7: Run the UX tests**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj --filter FullyQualifiedName~NearbyDiscoveryFlowTests
```

Expected: PASS.

- [ ] **Step 8: Commit**

```powershell
git add -- t/Controllers/HomeController.cs t/Views/Home/Rentals.cshtml t/Views/Home/ApartmentDetail.cshtml t/Views/Shared/_Layout.cshtml t/wwwroot/js/rentals-nearby.js t/wwwroot/css/rentals-page.css t/wwwroot/css/apartment-detail.css t.Tests/Integration/NearbyDiscoveryFlowTests.cs
git commit -m "feat: add nearby rentals discovery ux"
```

## Task 5: Run Regression Verification and Update Documentation

**Files:**
- Modify: `docs/maplibre-openfreemap-setup.md`

- [ ] **Step 1: Update map documentation**

Add a short section explaining:

- `Gần bạn` asks browser permission only after a click;
- the rentals list sorts all matching listings nearest first;
- no radius cutoff is applied;
- detail recommendations prefer nearby apartments;
- distances are straight-line estimates;
- coordinates are passed in the URL for the active request and are not saved by
  the application.

- [ ] **Step 2: Run whitespace verification**

Run:

```powershell
git diff --check
```

Expected: no output.

- [ ] **Step 3: Run the main test suite**

Run:

```powershell
dotnet test .\t.Tests\t.Tests.csproj
```

Expected: PASS.

- [ ] **Step 4: Run PostgreSQL regression coverage**

Run:

```powershell
dotnet test .\t.PostgresTests\t.PostgresTests.csproj
```

Expected: PASS, or report that Docker / the configured external PostgreSQL test
database is unavailable. The nearby query branch does not depend on provider
translation of trigonometric functions, but this run still checks PostgreSQL
regressions.

- [ ] **Step 5: Build the web project**

Run:

```powershell
dotnet build .\t\t.csproj
```

Expected: PASS with no compilation errors.

- [ ] **Step 6: Commit documentation**

```powershell
git add -- docs/maplibre-openfreemap-setup.md
git commit -m "docs: describe nearby rentals discovery"
```

- [ ] **Step 7: Inspect final scope**

Run:

```powershell
git status --short
git log --oneline -n 8
```

Expected: feature commits are present. Any pre-existing map baseline changes are
accounted for explicitly; no unrelated edits were reverted or staged
accidentally.
