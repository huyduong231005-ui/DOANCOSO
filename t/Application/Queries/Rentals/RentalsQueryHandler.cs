using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.ViewModels;

namespace t.Application.Queries.Rentals;

public sealed class RentalsQueryHandler
{
    private readonly AppDbContext _db;

    public RentalsQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApartmentListPageViewModel> SearchAsync(
        string? region, decimal? minPrice, decimal? maxPrice,
        double? minArea, double? maxArea,
        List<int>? categoryIds, List<int>? amenityIds,
        string? sort, int page, int pageSize,
        string? categorySlug = null,
        CancellationToken cancellationToken = default)
    {
        page = page <= 0 ? 1 : page;

        var effectiveCategoryIds = categoryIds?.ToList() ?? new List<int>();
        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            var slugId = await _db.Categories
                .Where(c => c.Slug == categorySlug)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);
            effectiveCategoryIds.Add(slugId ?? -1);
        }

        var query = _db.Apartments
            .AsNoTracking()
            .AsSplitQuery()
            .Include(a => a.Images)
            .Include(a => a.Category)
            .Include(a => a.Region)
            .Include(a => a.ApartmentAmenities).ThenInclude(aa => aa.Amenity)
            .Where(a => a.Status == Models.Entities.ListingStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(a => a.Region.Slug == region);
        if (minPrice.HasValue)
            query = query.Where(a => a.Price >= minPrice.Value);
        if (maxPrice.HasValue)
            query = query.Where(a => a.Price <= maxPrice.Value);
        if (minArea.HasValue)
            query = query.Where(a => a.Area >= minArea.Value);
        if (maxArea.HasValue)
            query = query.Where(a => a.Area <= maxArea.Value);
        if (effectiveCategoryIds.Count > 0)
            query = query.Where(a => effectiveCategoryIds.Contains(a.CategoryId));
        if (amenityIds?.Count > 0)
            query = query.Where(a => a.ApartmentAmenities.Any(aa => amenityIds.Contains(aa.AmenityId)));

        query = sort switch
        {
            "price_asc" => query.OrderBy(a => a.Price),
            "price_desc" => query.OrderByDescending(a => a.Price),
            "area_desc" => query.OrderByDescending(a => a.Area),
            _ => query.OrderByDescending(a => a.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var apartments = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ApartmentListViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Address = a.Address,
                Price = a.Price,
                Area = a.Area,
                Bedrooms = a.Bedrooms,
                CoverImageUrl = a.Images.Where(i => i.IsCover).OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
                                ?? a.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                Badge = a.IsFeatured ? "Nổi bật" : (a.Category.Slug == "nha-tro" || a.Category.Slug == "chung-cu-mini" ? "Giá tốt" : null),
                CategoryName = a.Category.Name,
                AmenityIcons = a.ApartmentAmenities.OrderBy(aa => aa.AmenityId).Select(aa => aa.Amenity.Icon).Take(3).ToList(),
                AmenityNames = a.ApartmentAmenities.OrderBy(aa => aa.AmenityId).Select(aa => aa.Amenity.Name).Take(3).ToList()
            })
            .ToListAsync(cancellationToken);

        return new ApartmentListPageViewModel
        {
            Apartments = apartments,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            RegionSlug = region,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinArea = minArea,
            MaxArea = maxArea,
            CategoryIds = categoryIds,
            AmenityIds = amenityIds,
            SortBy = sort,
            CategorySlug = categorySlug
        };
    }
}
