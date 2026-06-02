using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure;
using t.Models.Entities;

namespace t.Application.Commands.Listings;

public sealed class CreateListingCommandHandler
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    private const long MaxFileSize = 5 * 1024 * 1024;
    private const int MinImages = 1;
    private const int MaxImages = 15;

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public CreateListingCommandHandler(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<CreateListingResult> HandleAsync(CreateListingCommand command, CancellationToken cancellationToken = default)
    {
        var model = command.Model;
        var errors = new List<(string Key, string Message)>();

        model.Title = (model.Title ?? string.Empty).Trim();
        model.Address = (model.Address ?? string.Empty).Trim();
        model.Description = model.Description?.Trim();
        model.FeeNote = model.FeeNote?.Trim();

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == model.CategoryId, cancellationToken);
        if (!categoryExists)
            errors.Add((nameof(model.CategoryId), "Loại hình đã chọn không tồn tại."));

        var regionExists = await _db.Regions.AnyAsync(r => r.Id == model.RegionId, cancellationToken);
        if (!regionExists)
            errors.Add((nameof(model.RegionId), "Khu vực đã chọn không tồn tại."));

        if (model.ProjectId.HasValue)
        {
            var projectExists = await _db.Projects.AnyAsync(p => p.Id == model.ProjectId.Value, cancellationToken);
            if (!projectExists)
                errors.Add((nameof(model.ProjectId), "Dự án đã chọn không tồn tại."));
        }

        if (model.Images is null || model.Images.Count < MinImages)
            errors.Add((nameof(model.Images), $"Cần tải tối thiểu {MinImages} ảnh."));

        if (model.Images is not null && model.Images.Count > MaxImages)
            errors.Add((nameof(model.Images), $"Chỉ được tải tối đa {MaxImages} ảnh."));

        if (model.Images is not null)
        {
            for (var i = 0; i < model.Images.Count; i++)
            {
                var file = model.Images[i];
                if (file.Length == 0)
                {
                    errors.Add((nameof(model.Images), $"Ảnh thứ {i + 1} rỗng."));
                    continue;
                }

                if (file.Length > MaxFileSize)
                    errors.Add((nameof(model.Images), $"Ảnh thứ {i + 1} vượt quá 5MB."));

                var ext = Path.GetExtension(file.FileName);
                if (!AllowedExtensions.Contains(ext))
                    errors.Add((nameof(model.Images), $"Ảnh thứ {i + 1} có định dạng không hợp lệ."));
            }
        }

        if (model.CoverImageIndex < 0 || model.CoverImageIndex >= (model.Images?.Count ?? 0))
            errors.Add((nameof(model.CoverImageIndex), "CoverImageIndex không hợp lệ."));

        if (!string.IsNullOrWhiteSpace(command.HostId))
        {
            var hostExists = await _db.Users.AnyAsync(u => u.Id == command.HostId, cancellationToken);
            if (!hostExists)
                errors.Add((nameof(command.HostId), "Không tìm thấy chủ tin đăng."));
        }
        else
        {
            errors.Add((nameof(command.HostId), "Không xác định được người đăng tin."));
        }

        if (errors.Count > 0)
            return CreateListingResult.Fail(errors);

        var images = model.Images;
        if (images is null)
            return CreateListingResult.Fail((nameof(model.Images), "Danh sách ảnh không hợp lệ."));

        var slug = await GenerateUniqueSlugAsync(model.Title, cancellationToken);

        var apartment = new Apartment
        {
            Title = model.Title,
            Slug = slug,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? "Một không gian sống tiện nghi và hiện đại."
                : model.Description,
            CategoryId = model.CategoryId,
            Area = model.Area,
            Bedrooms = model.Bedrooms,
            Bathrooms = model.Bathrooms <= 0 ? 1 : model.Bathrooms,
            Price = model.Price,
            DefaultDeposit = model.DefaultDeposit,
            FeeNote = string.IsNullOrWhiteSpace(model.FeeNote)
                ? "Chưa bao gồm phí quản lý"
                : model.FeeNote,
            Address = model.Address,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            RegionId = model.RegionId,
            HostId = command.HostId,
            ProjectId = model.ProjectId,
            // New listings start as Draft and require admin moderation before going public.
            // Admin approves via /Admin/Apartments/Approve. Host sees status badge in MyListings.
            Status = ListingStatus.Draft,
            Occupancy = ApartmentOccupancy.Available,
            IsFeatured = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Apartments.Add(apartment);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniqueSlugError(ex))
        {
            // Two concurrent posts picked the same slug. Generate a new one with a
            // numeric suffix and retry once.
            apartment.Slug = await GenerateUniqueSlugAsync(model.Title, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }

        if (model.AmenityIds.Count > 0)
        {
            var amenities = model.AmenityIds
                .Distinct()
                .Select(amenityId => new ApartmentAmenity
                {
                    ApartmentId = apartment.Id,
                    AmenityId = amenityId
                });
            await _db.ApartmentAmenities.AddRangeAsync(amenities, cancellationToken);
        }

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        var now = DateTime.UtcNow;
        var relativeFolder = Path.Combine("uploads", "listings", now.ToString("yyyy"), now.ToString("MM"));
        var absoluteFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        for (var i = 0; i < images.Count; i++)
        {
            var file = images[i];
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var absolutePath = Path.Combine(absoluteFolder, fileName);
            await using (var fs = new FileStream(absolutePath, FileMode.Create))
            {
                await file.CopyToAsync(fs, cancellationToken);
            }

            var relativePath = "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
            _db.ApartmentImages.Add(new ApartmentImage
            {
                ApartmentId = apartment.Id,
                Url = relativePath,
                IsCover = i == model.CoverImageIndex,
                SortOrder = i
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return CreateListingResult.Ok(apartment.Id);
    }

    private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken cancellationToken)
    {
        var baseSlug = Slugify.Make(title);
        if (string.IsNullOrEmpty(baseSlug)) baseSlug = "tin-dang";

        var slug = baseSlug;
        var i = 2;
        while (await _db.Apartments.AnyAsync(a => a.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{i++}";
            if (i > 200)
            {
                // Safe fallback: short suffix of a Guid keeps slug under DB length cap.
                slug = baseSlug + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                break;
            }
        }
        return slug;
    }

    private static bool IsUniqueSlugError(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
