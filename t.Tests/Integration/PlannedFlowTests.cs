using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Application.Commands.Listings;
using t.Data;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Tests.Integration;

public class PlannedFlowTests : IClassFixture<TestWebApplicationFactory>, IClassFixture<DevelopmentWebApplicationFactory>
{
    private static readonly Regex RequestTokenRegex =
        new("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly TestWebApplicationFactory _factory;
    private readonly DevelopmentWebApplicationFactory _devFactory;

    public PlannedFlowTests(TestWebApplicationFactory factory, DevelopmentWebApplicationFactory devFactory)
    {
        _factory = factory;
        _devFactory = devFactory;
    }

    [Fact]
    public void ApartmentToProject_ForeignKey_ShouldUseNoActionDeleteBehavior()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var apartmentEntity = db.Model.FindEntityType(typeof(Apartment));
        Assert.NotNull(apartmentEntity);

        var projectForeignKey = apartmentEntity!.GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(Apartment.ProjectId)));

        Assert.Equal(DeleteBehavior.NoAction, projectForeignKey.DeleteBehavior);
    }

    [Fact]
    public async Task Register_ShouldSucceed_AndDuplicateEmailShouldFail()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var email = $"new-user-{Guid.NewGuid():N}@example.com";
        var firstToken = await GetRequestVerificationTokenAsync(client, "/Home/Register");

        var firstPost = await client.PostAsync("/Home/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FullName"] = "Nguoi Dung Moi",
            ["Email"] = email,
            ["Phone"] = "0901234567",
            ["Password"] = "Abc12345",
            ["ConfirmPassword"] = "Abc12345",
            ["__RequestVerificationToken"] = firstToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, firstPost.StatusCode);
        Assert.Equal("/", firstPost.Headers.Location?.OriginalString);

        var secondToken = await GetRequestVerificationTokenAsync(client, "/Home/Register");
        var secondPost = await client.PostAsync("/Home/Register", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FullName"] = "Nguoi Dung Moi",
            ["Email"] = email,
            ["Phone"] = "0901234567",
            ["Password"] = "Abc12345",
            ["ConfirmPassword"] = "Abc12345",
            ["__RequestVerificationToken"] = secondToken
        }));

        Assert.Equal(HttpStatusCode.OK, secondPost.StatusCode);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usersWithEmail = await db.Users.CountAsync(u => u.Email == email);
        Assert.Equal(1, usersWithEmail);
    }

    [Fact]
    public async Task ForgotAndResetPassword_ShouldWorkWithValidToken_AndFailWithInvalidToken()
    {
        using var client = _devFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        const string email = "host@luxehaven.vn";

        var forgotToken = await GetRequestVerificationTokenAsync(client, "/Home/ForgotPassword");
        var forgotPost = await client.PostAsync("/Home/ForgotPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["__RequestVerificationToken"] = forgotToken
        }));

        Assert.Equal(HttpStatusCode.OK, forgotPost.StatusCode);

        var devTokenResponse = await client.GetFromJsonAsync<DevResetTokenResponse>($"/api/dev/auth/reset-token?email={Uri.EscapeDataString(email)}");
        Assert.NotNull(devTokenResponse);
        Assert.False(string.IsNullOrWhiteSpace(devTokenResponse!.Token));

        var invalidResetPage = await client.GetAsync($"/Home/ResetPassword?email={Uri.EscapeDataString(email)}&token=invalid-token");
        Assert.Equal(HttpStatusCode.OK, invalidResetPage.StatusCode);
        var invalidResetHtml = await invalidResetPage.Content.ReadAsStringAsync();
        var invalidResetRequestToken = ExtractRequestVerificationToken(invalidResetHtml);

        var invalidResetPost = await client.PostAsync("/Home/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Token"] = "invalid-token",
            ["NewPassword"] = "Host@4567",
            ["ConfirmPassword"] = "Host@4567",
            ["__RequestVerificationToken"] = invalidResetRequestToken
        }));

        Assert.Equal(HttpStatusCode.OK, invalidResetPost.StatusCode);

        var validToken = devTokenResponse.Token!;
        var validResetPage = await client.GetAsync($"/Home/ResetPassword?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(validToken)}");
        Assert.Equal(HttpStatusCode.OK, validResetPage.StatusCode);
        var validResetHtml = await validResetPage.Content.ReadAsStringAsync();
        var validResetRequestToken = ExtractRequestVerificationToken(validResetHtml);

        var validResetPost = await client.PostAsync("/Home/ResetPassword", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Token"] = validToken,
            ["NewPassword"] = "Host@4567",
            ["ConfirmPassword"] = "Host@4567",
            ["__RequestVerificationToken"] = validResetRequestToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, validResetPost.StatusCode);
        Assert.Equal("/Home/Login", validResetPost.Headers.Location?.OriginalString);

        var loginToken = await GetRequestVerificationTokenAsync(client, "/Home/Login");
        var loginPost = await client.PostAsync("/Home/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = email,
            ["Password"] = "Host@4567",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = loginToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginPost.StatusCode);
        Assert.Equal("/", loginPost.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task CreateListingCommand_ShouldPersistApartmentImagesAmenitiesAndProject()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var handler = services.GetRequiredService<CreateListingCommandHandler>();

        var hostId = await db.Users.Select(u => u.Id).FirstAsync();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
        var regionId = await db.Regions.Select(r => r.Id).FirstAsync();
        var projectId = await db.Projects.Select(p => p.Id).FirstAsync();
        var amenityIds = await db.Amenities.OrderBy(a => a.Id).Select(a => a.Id).Take(2).ToListAsync();

        var image1 = CreateImageFile("listing-1.jpg", 1024);
        var image2 = CreateImageFile("listing-2.jpg", 1024);
        var image3 = CreateImageFile("listing-3.jpg", 1024);
        var image4 = CreateImageFile("listing-4.jpg", 1024);
        var image5 = CreateImageFile("listing-5.jpg", 1024);

        var model = new CreateApartmentViewModel
        {
            Title = "Integration Listing",
            Description = "Test listing",
            CategoryId = categoryId,
            Area = 62,
            Bedrooms = 2,
            Bathrooms = 2,
            Price = 15_000_000,
            Address = "Test Address",
            RegionId = regionId,
            ProjectId = projectId,
            AmenityIds = amenityIds,
            CoverImageIndex = 2,
            Images = new List<IFormFile> { image1, image2, image3, image4, image5 }
        };

        var result = await handler.HandleAsync(new CreateListingCommand
        {
            HostId = hostId,
            Model = model
        });

        Assert.True(result.Success);
        Assert.True(result.ApartmentId > 0);

        var apartmentId = result.ApartmentId;
        var apartment = await db.Apartments
            .Include(a => a.Images)
            .Include(a => a.ApartmentAmenities)
            .SingleAsync(a => a.Id == apartmentId);

        Assert.Equal(projectId, apartment.ProjectId);
        Assert.Equal(5, apartment.Images.Count);
        var coverImage = Assert.Single(apartment.Images, i => i.IsCover);
        Assert.Equal(2, coverImage.SortOrder);
        Assert.Equal(amenityIds.Count, apartment.ApartmentAmenities.Count);
    }

    [Fact]
    public async Task CreateListingCommand_ShouldFail_WhenImageCountIsBelowMinimum()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var handler = services.GetRequiredService<CreateListingCommandHandler>();

        var hostId = await db.Users.Select(u => u.Id).FirstAsync();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
        var regionId = await db.Regions.Select(r => r.Id).FirstAsync();

        var model = new CreateApartmentViewModel
        {
            Title = "Invalid Listing",
            CategoryId = categoryId,
            Area = 40,
            Bedrooms = 1,
            Bathrooms = 1,
            Price = 5_000_000,
            Address = "Test Address",
            RegionId = regionId,
            CoverImageIndex = 0,
            Images = new List<IFormFile>()
        };

        var result = await handler.HandleAsync(new CreateListingCommand
        {
            HostId = hostId,
            Model = model
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Key == nameof(model.Images) && e.Message.Contains("tối thiểu 1 ảnh", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreateListingCommand_ShouldFail_WhenImageFormatOrSizeInvalid()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<AppDbContext>();
        var handler = services.GetRequiredService<CreateListingCommandHandler>();

        var hostId = await db.Users.Select(u => u.Id).FirstAsync();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
        var regionId = await db.Regions.Select(r => r.Id).FirstAsync();

        var image1 = CreateImageFile("listing-1.jpg", 1024);
        var image2 = CreateImageFile("listing-2.jpg", 1024);
        var image3 = CreateImageFile("listing-3.jpg", 1024);
        var image4 = CreateImageFile("listing-4.jpg", 1024);
        var invalidExtension = CreateImageFile("listing-5.gif", 1024);
        var tooLarge = CreateImageFile("listing-6.jpg", 5 * 1024 * 1024 + 1);

        var model = new CreateApartmentViewModel
        {
            Title = "Invalid Listing 2",
            CategoryId = categoryId,
            Area = 40,
            Bedrooms = 1,
            Bathrooms = 1,
            Price = 5_000_000,
            Address = "Test Address",
            RegionId = regionId,
            CoverImageIndex = 0,
            Images = new List<IFormFile> { image1, image2, image3, image4, invalidExtension, tooLarge }
        };

        var result = await handler.HandleAsync(new CreateListingCommand
        {
            HostId = hostId,
            Model = model
        });

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Key == nameof(model.Images) && e.Message.Contains("định dạng không hợp lệ", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Key == nameof(model.Images) && e.Message.Contains("vượt quá 5MB", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProjectEndpoints_ShouldReturnDetail_And404ForUnknownSlug()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var listResponse = await client.GetFromJsonAsync<ProjectListPageViewModel>("/api/projects");
        Assert.NotNull(listResponse);
        Assert.NotEmpty(listResponse!.Projects);

        var slug = listResponse.Projects[0].Slug;
        var detailResponse = await client.GetFromJsonAsync<ProjectDetailViewModel>($"/api/projects/{slug}");
        Assert.NotNull(detailResponse);
        Assert.Equal(slug, detailResponse!.Slug);
        Assert.NotNull(detailResponse.Apartments);

        var missing = await client.GetAsync("/api/projects/slug-khong-ton-tai");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);

        var publicList = await client.GetAsync("/du-an");
        Assert.Equal(HttpStatusCode.OK, publicList.StatusCode);
    }

    [Fact]
    public async Task RentalsApi_ShouldApplyFilterSortAndPaging()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var regionSlug = await db.Regions.Select(r => r.Slug).FirstAsync();
        var categoryId = await db.Categories.Where(c => c.Slug == "can-ho-cao-cap").Select(c => c.Id).FirstAsync();
        var amenityId = await db.Amenities.Where(a => a.Icon == "wifi").Select(a => a.Id).FirstAsync();

        var url =
            $"/api/rentals/search?region={Uri.EscapeDataString(regionSlug)}&minPrice=3000000&maxPrice=20000000&minArea=20&maxArea=80" +
            $"&categoryIds={categoryId}&amenityIds={amenityId}&sort=price_desc&page=1&pageSize=2";

        var result = await client.GetFromJsonAsync<RentalsSearchResultViewModel>(url);
        Assert.NotNull(result);
        Assert.NotEmpty(result!.Apartments);
        Assert.True(result.Apartments.Count <= 2);

        var prices = result.Apartments.Select(a => a.Price).ToList();
        var sortedPrices = prices.OrderByDescending(p => p).ToList();
        Assert.Equal(sortedPrices, prices);

        var ids = result.Apartments.Select(a => a.Id).ToList();
        var mapped = await db.Apartments
            .Include(a => a.Region)
            .Include(a => a.ApartmentAmenities)
            .Where(a => ids.Contains(a.Id))
            .ToListAsync();

        Assert.All(mapped, apartment =>
        {
            Assert.Equal(regionSlug, apartment.Region.Slug);
            Assert.InRange(apartment.Price, 3_000_000, 20_000_000);
            Assert.InRange(apartment.Area, 20, 80);
            Assert.Equal(categoryId, apartment.CategoryId);
            Assert.Contains(apartment.ApartmentAmenities, aa => aa.AmenityId == amenityId);
        });
    }

    [Fact]
    public async Task EmailExistsApi_ShouldReturnExpectedResults()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var exists = await client.GetFromJsonAsync<EmailExistsResponse>("/api/auth/email-exists?email=host@luxehaven.vn");
        var missing = await client.GetFromJsonAsync<EmailExistsResponse>("/api/auth/email-exists?email=not-found@example.com");

        Assert.NotNull(exists);
        Assert.NotNull(missing);
        Assert.True(exists!.Exists);
        Assert.False(missing!.Exists);
    }

    [Fact]
    public async Task RentalsPage_ShouldUseVietnameseCurrencySuffix_WithAccent()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Home/Rentals");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("VND/tháng", html, StringComparison.Ordinal);
    }

    [Fact]
    public void SiteCss_ShouldDefineSharedFormFontFamily()
    {
        var cssPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "t", "wwwroot", "css", "site.css"));

        var css = File.ReadAllText(cssPath);
        Assert.Contains("--form-font-family", css, StringComparison.Ordinal);
        Assert.Contains("form input", css, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostListing_FormSubmit_ShouldRedirectToApartmentDetail_WhenPayloadValid()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginAsHostAsync(client);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var categoryId = await db.Categories.Select(c => c.Id).FirstAsync();
        var regionId = await db.Regions.Select(r => r.Id).FirstAsync();
        var projectId = await db.Projects.Select(p => p.Id).FirstAsync();
        var amenityIds = await db.Amenities.OrderBy(a => a.Id).Select(a => a.Id).Take(2).ToListAsync();

        var token = await GetRequestVerificationTokenAsync(client, "/Home/PostListing");
        using var content = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("Integration HTTP Listing"), "Title" },
            { new StringContent("Mô tả test đầy đủ cho căn hộ này, ít nhất 20 ký tự."), "Description" },
            { new StringContent(categoryId.ToString()), "CategoryId" },
            { new StringContent("65"), "Area" },
            { new StringContent("2"), "Bedrooms" },
            { new StringContent("2"), "Bathrooms" },
            { new StringContent("12345678"), "Price" },
            { new StringContent("Test fee note"), "FeeNote" },
            { new StringContent("123 Test St"), "Address" },
            { new StringContent("10.794200"), "Latitude" },
            { new StringContent("106.721900"), "Longitude" },
            { new StringContent(regionId.ToString()), "RegionId" },
            { new StringContent(projectId.ToString()), "ProjectId" },
            { new StringContent("0"), "CoverImageIndex" }
        };

        foreach (var amenityId in amenityIds)
            content.Add(new StringContent(amenityId.ToString()), "AmenityIds");

        content.Add(CreateImageContent("listing-1.jpg", 1024), "Images", "listing-1.jpg");
        content.Add(CreateImageContent("listing-2.jpg", 1024), "Images", "listing-2.jpg");
        content.Add(CreateImageContent("listing-3.jpg", 1024), "Images", "listing-3.jpg");
        content.Add(CreateImageContent("listing-4.jpg", 1024), "Images", "listing-4.jpg");
        content.Add(CreateImageContent("listing-5.jpg", 1024), "Images", "listing-5.jpg");

        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        HttpResponseMessage response;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("vi-VN");
            response = await client.PostAsync("/Home/PostListing", content);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            var html = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException(
                $"Expected redirect but got {(int)response.StatusCode}. Response excerpt: {html[..Math.Min(400, html.Length)]}");
        }

        Assert.Contains("/Home/ApartmentDetail", response.Headers.Location?.OriginalString ?? string.Empty);

        var apartmentId = int.Parse(
            response.Headers.Location!.OriginalString.Split('/').Last(),
            CultureInfo.InvariantCulture);
        var apartment = await db.Apartments.SingleAsync(a => a.Id == apartmentId);

        Assert.Equal(10.7942, apartment.Latitude);
        Assert.Equal(106.7219, apartment.Longitude);
    }

    [Fact]
    public async Task PostListing_ShouldRenderMapLibreOpenFreeMapHooks_WithoutCredentials()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        await LoginAsHostAsync(client);

        var response = await client.GetAsync("/Home/PostListing");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("data-mapbox-access-token", html, StringComparison.Ordinal);
        Assert.DoesNotContain("mapbox", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/js/maplibre-loader.js", html, StringComparison.Ordinal);
        Assert.DoesNotContain("leaflet", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-address-autocomplete", html, StringComparison.Ordinal);
        Assert.Contains("id=\"address-suggestions\"", html, StringComparison.Ordinal);

        var scriptPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "t", "wwwroot", "js", "post-listing.js"));
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("window.loadMapLibre", script, StringComparison.Ordinal);
        Assert.Contains("https://tiles.openfreemap.org/styles/liberty", script, StringComparison.Ordinal);
        Assert.Contains("https://photon.komoot.io/api/", script, StringComparison.Ordinal);
        Assert.Contains("AUTOCOMPLETE_DELAY_MS = 500", script, StringComparison.Ordinal);
        Assert.Contains("AUTOCOMPLETE_MIN_CHARS = 3", script, StringComparison.Ordinal);
        Assert.Contains("AUTOCOMPLETE_LIMIT = 5", script, StringComparison.Ordinal);
        Assert.Contains("new AbortController()", script, StringComparison.Ordinal);
        Assert.DoesNotContain("mapbox", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nominatim", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("google.maps", script, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("search/searchbox", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SoftNavigation_ShouldReinitializeMapLibrePages()
    {
        var webRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "t", "wwwroot", "js"));
        var layoutPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "t", "Views", "Shared", "_Layout.cshtml"));

        var layout = File.ReadAllText(layoutPath);
        var siteScript = File.ReadAllText(Path.Combine(webRoot, "site.js"));
        var postListingScript = File.ReadAllText(Path.Combine(webRoot, "post-listing.js"));
        var detailMapScript = File.ReadAllText(Path.Combine(webRoot, "apartment-detail-map.js"));

        Assert.Contains("~/js/maplibre-loader.js", layout, StringComparison.Ordinal);
        Assert.Contains("~/js/post-listing.js", layout, StringComparison.Ordinal);
        Assert.Contains("~/js/apartment-detail-map.js", layout, StringComparison.Ordinal);
        Assert.Contains("luxe:page-loaded", siteScript, StringComparison.Ordinal);
        Assert.Contains("luxe:page-loaded", postListingScript, StringComparison.Ordinal);
        Assert.Contains("luxe:page-loaded", detailMapScript, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApartmentDetail_ShouldRenderInteractiveGalleryHooks()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Home/ApartmentDetail?id=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-gallery-images=", html, StringComparison.Ordinal);
        Assert.Contains("data-gallery-open", html, StringComparison.Ordinal);
        Assert.Contains("data-ap-lightbox", html, StringComparison.Ordinal);
    }

    private static async Task<string> GetRequestVerificationTokenAsync(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        return ExtractRequestVerificationToken(html);
    }

    private static string ExtractRequestVerificationToken(string html)
    {
        var match = RequestTokenRegex.Match(html);
        Assert.True(match.Success, "Anti-forgery token was not found in HTML response.");
        return match.Groups[1].Value;
    }

    private static FormFile CreateImageFile(string fileName, int length)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "Images", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }

    private static ByteArrayContent CreateImageContent(string fileName, int length)
    {
        var bytes = new byte[length];
        Random.Shared.NextBytes(bytes);
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
        {
            Name = "\"Images\"",
            FileName = $"\"{fileName}\""
        };
        return fileContent;
    }

    private static async Task LoginAsHostAsync(HttpClient client)
    {
        var loginToken = await GetRequestVerificationTokenAsync(client, "/Home/Login");
        var loginResponse = await client.PostAsync("/Home/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Email"] = "host@luxehaven.vn",
            ["Password"] = "Host@123",
            ["RememberMe"] = "false",
            ["__RequestVerificationToken"] = loginToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
    }
}
