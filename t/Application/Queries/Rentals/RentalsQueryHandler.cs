using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Geo;
using t.Models.Entities;
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
        double? latitude = null,
        double? longitude = null,
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

        var coordinates = GeoDistance.ValidatePair(latitude, longitude);
        var isNearbySort = sort == "distance_asc" && coordinates.IsActive;
        var totalCount = await query.CountAsync(cancellationToken);
        List<ApartmentListViewModel> apartments;

        if (isNearbySort)
        {
            var candidates = await query
                .Select(a => new NearbyCandidate(
                    a.Id,
                    a.Latitude,
                    a.Longitude,
                    a.CreatedAt))
                .ToListAsync(cancellationToken);

            var selectedCandidates = candidates
                .Select(candidate => new NearbyCandidateDistance(
                    candidate,
                    GeoDistance.IsValidCoordinate(candidate.Latitude, candidate.Longitude)
                        ? GeoDistance.CalculateKm(
                            latitude!.Value,
                            longitude!.Value,
                            candidate.Latitude!.Value,
                            candidate.Longitude!.Value)
                        : null))
                .OrderBy(candidate => !candidate.DistanceKm.HasValue)
                .ThenBy(candidate => candidate.DistanceKm)
                .ThenByDescending(candidate => candidate.Candidate.CreatedAt)
                .ThenByDescending(candidate => candidate.Candidate.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var selectedIds = selectedCandidates.Select(candidate => candidate.Candidate.Id).ToList();
            var distanceById = selectedCandidates.ToDictionary(
                candidate => candidate.Candidate.Id,
                candidate => candidate.DistanceKm);
            var cardsById = await ProjectCards(query.Where(a => selectedIds.Contains(a.Id)))
                .ToDictionaryAsync(card => card.Id, cancellationToken);

            apartments = selectedIds
                .Select(id =>
                {
                    var card = cardsById[id];
                    card.DistanceKm = distanceById[id];
                    return card;
                })
                .ToList();
        }
        else
        {
            query = sort switch
            {
                "price_asc" => query.OrderBy(a => a.Price),
                "price_desc" => query.OrderByDescending(a => a.Price),
                "area_desc" => query.OrderByDescending(a => a.Area),
                _ => query.OrderByDescending(a => a.CreatedAt)
            };

            apartments = await ProjectCards(query)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

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
            CategorySlug = categorySlug,
            Latitude = isNearbySort ? latitude : null,
            Longitude = isNearbySort ? longitude : null
        };
    }

    private static IQueryable<ApartmentListViewModel> ProjectCards(IQueryable<Apartment> query)
    {
        return query.Select(a => new ApartmentListViewModel
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
        });
    }

    private sealed record NearbyCandidate(
        int Id,
        double? Latitude,
        double? Longitude,
        DateTime CreatedAt);

    private sealed record NearbyCandidateDistance(
        NearbyCandidate Candidate,
        double? DistanceKm);
}
