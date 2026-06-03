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
        RentalSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 12 : request.PageSize;

        if (request.Sort == "match_desc")
            return await SearchByMatchAsync(request, cancellationToken);

        var result = await SearchAsync(
            request.Region,
            request.MinPrice,
            request.MaxPrice,
            request.MinArea,
            request.MaxArea,
            request.CategoryIds,
            request.AmenityIds,
            request.Sort,
            request.Page,
            request.PageSize,
            request.Category,
            request.Latitude,
            request.Longitude,
            request.PreferredLatitude,
            request.PreferredLongitude,
            request.MaxDistanceKm,
            cancellationToken);
        result.Search = request;
        return result;
    }

    public async Task<ApartmentListPageViewModel> SearchAsync(
        string? region, decimal? minPrice, decimal? maxPrice,
        double? minArea, double? maxArea,
        List<int>? categoryIds, List<int>? amenityIds,
        string? sort, int page, int pageSize,
        string? categorySlug = null,
        double? latitude = null,
        double? longitude = null,
        double? preferredLatitude = null,
        double? preferredLongitude = null,
        double? maxDistanceKm = null,
        CancellationToken cancellationToken = default)
    {
        page = page <= 0 ? 1 : page;
        minPrice = minPrice > 0 ? minPrice : null;
        maxPrice = maxPrice > 0 ? maxPrice : null;

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
        var preferredCoordinates = GeoDistance.ValidatePair(preferredLatitude, preferredLongitude);
        Dictionary<int, double>? preferredDistanceById = null;
        if (preferredCoordinates.IsActive && maxDistanceKm > 0)
        {
            var distanceCandidates = await query
                .Select(a => new NearbyCandidate(
                    a.Id,
                    a.Latitude,
                    a.Longitude,
                    a.CreatedAt))
                .ToListAsync(cancellationToken);

            var matchingCandidates = distanceCandidates
                .Select(candidate => new NearbyCandidateDistance(
                    candidate,
                    GeoDistance.IsValidCoordinate(candidate.Latitude, candidate.Longitude)
                        ? GeoDistance.CalculateKm(
                            preferredLatitude!.Value,
                            preferredLongitude!.Value,
                            candidate.Latitude!.Value,
                            candidate.Longitude!.Value)
                        : null))
                .Where(candidate => candidate.DistanceKm.HasValue &&
                                    candidate.DistanceKm.Value <= maxDistanceKm.Value)
                .ToList();

            var matchingIds = matchingCandidates.Select(candidate => candidate.Candidate.Id).ToList();
            preferredDistanceById = matchingCandidates.ToDictionary(
                candidate => candidate.Candidate.Id,
                candidate => candidate.DistanceKm!.Value);
            query = query.Where(a => matchingIds.Contains(a.Id));
        }

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
            if (preferredDistanceById is not null)
            {
                foreach (var apartment in apartments)
                {
                    if (preferredDistanceById.TryGetValue(apartment.Id, out var distanceKm))
                        apartment.DistanceKm = distanceKm;
                }
            }
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
            Longitude = isNearbySort ? longitude : null,
            Search = new RentalSearchRequest
            {
                Region = region,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinArea = minArea,
                MaxArea = maxArea,
                CategoryIds = categoryIds?.ToList() ?? new List<int>(),
                AmenityIds = amenityIds?.ToList() ?? new List<int>(),
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                Category = categorySlug,
                Latitude = latitude,
                Longitude = longitude,
                PreferredLatitude = preferredLatitude,
                PreferredLongitude = preferredLongitude,
                MaxDistanceKm = maxDistanceKm
            }
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

    private async Task<ApartmentListPageViewModel> SearchByMatchAsync(
        RentalSearchRequest request,
        CancellationToken cancellationToken)
    {
        var normalization = RentalPreferenceNormalizer.Normalize(request, strict: false);
        var draft = normalization.Draft;
        if (!draft.RegionId.HasValue && !string.IsNullOrWhiteSpace(request.Region))
        {
            draft.RegionId = await _db.Regions
                .Where(region => region.Slug == request.Region)
                .Select(region => (int?)region.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
        var effectiveCategoryIds = draft.CategoryIds.ToHashSet();
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var slugId = await _db.Categories
                .Where(category => category.Slug == request.Category)
                .Select(category => (int?)category.Id)
                .FirstOrDefaultAsync(cancellationToken);
            effectiveCategoryIds.Add(slugId ?? -1);
        }
        draft.CategoryIds = effectiveCategoryIds;

        var query = _db.Apartments
            .AsNoTracking()
            .Where(apartment => apartment.Status == ListingStatus.Active);

        query = ApplyRequiredSqlFilters(query, draft);
        var projectedCandidates = await query
            .Select(apartment => new
            {
                apartment.Id,
                apartment.CreatedAt,
                apartment.RegionId,
                apartment.Price,
                apartment.Area,
                apartment.Bedrooms,
                apartment.CategoryId,
                AmenityIds = apartment.ApartmentAmenities
                    .Select(link => link.AmenityId)
                    .ToList(),
                apartment.FurnishingLevel,
                apartment.AllowsPets,
                apartment.ParkingType,
                apartment.AvailableFrom,
                apartment.MinLeaseMonths,
                apartment.MaxLeaseMonths,
                FloorNumber = apartment.Floor != null
                    ? apartment.Floor.Number
                    : apartment.FloorNumber,
                apartment.HouseDirection,
                apartment.Latitude,
                apartment.Longitude
            })
            .ToListAsync(cancellationToken);

        var matches = projectedCandidates
            .Select(candidate => new
            {
                candidate.Id,
                candidate.CreatedAt,
                Result = RentalMatchScorer.Score(
                    new RentalMatchCandidate(
                        candidate.Id,
                        candidate.CreatedAt,
                        candidate.RegionId,
                        candidate.Price,
                        candidate.Area,
                        candidate.Bedrooms,
                        candidate.CategoryId,
                        candidate.AmenityIds.ToHashSet(),
                        candidate.FurnishingLevel,
                        candidate.AllowsPets,
                        candidate.ParkingType,
                        candidate.AvailableFrom,
                        candidate.MinLeaseMonths,
                        candidate.MaxLeaseMonths,
                        candidate.FloorNumber,
                        candidate.HouseDirection,
                        candidate.Latitude,
                        candidate.Longitude),
                    draft)
            })
            .Where(match => match.Result.IsEligible)
            .OrderByDescending(match => match.Result.ScorePercent)
            .ThenByDescending(match => match.CreatedAt)
            .ThenByDescending(match => match.Id)
            .ToList();
        var selected = matches
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        var selectedIds = selected.Select(match => match.Id).ToList();
        var matchById = selected.ToDictionary(match => match.Id, match => match.Result);
        var cardsById = await ProjectCards(
                _db.Apartments
                    .AsNoTracking()
                    .Where(apartment => selectedIds.Contains(apartment.Id)))
            .ToDictionaryAsync(card => card.Id, cancellationToken);
        var apartments = selectedIds
            .Select(id =>
            {
                var card = cardsById[id];
                card.MatchPercent = matchById[id].ScorePercent;
                card.MatchReasons = matchById[id].Reasons.ToList();
                return card;
            })
            .ToList();

        return new ApartmentListPageViewModel
        {
            Apartments = apartments,
            TotalCount = matches.Count,
            Page = request.Page,
            PageSize = request.PageSize,
            RegionSlug = request.Region,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            MinArea = request.MinArea,
            MaxArea = request.MaxArea,
            CategoryIds = request.CategoryIds,
            AmenityIds = request.AmenityIds,
            SortBy = request.Sort,
            CategorySlug = request.Category,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Search = request,
            HasUsableMatchCriteria = HasUsableMatchCriteria(draft)
        };
    }

    private static IQueryable<Apartment> ApplyRequiredSqlFilters(
        IQueryable<Apartment> query,
        RentalPreferenceDraft draft)
    {
        if (draft.RequiredCriteria.Contains("region") && draft.RegionId.HasValue)
            query = query.Where(apartment => apartment.RegionId == draft.RegionId.Value);
        if (draft.RequiredCriteria.Contains("priceRange"))
        {
            if (draft.MinPrice.HasValue)
                query = query.Where(apartment => apartment.Price >= draft.MinPrice.Value);
            if (draft.MaxPrice.HasValue)
                query = query.Where(apartment => apartment.Price <= draft.MaxPrice.Value);
        }
        if (draft.RequiredCriteria.Contains("areaRange"))
        {
            if (draft.MinArea.HasValue)
                query = query.Where(apartment => apartment.Area >= draft.MinArea.Value);
            if (draft.MaxArea.HasValue)
                query = query.Where(apartment => apartment.Area <= draft.MaxArea.Value);
        }
        if (draft.RequiredCriteria.Contains("bedrooms") && draft.MinBedrooms.HasValue)
            query = query.Where(apartment => apartment.Bedrooms >= draft.MinBedrooms.Value);
        if (draft.RequiredCriteria.Contains("category") && draft.CategoryIds.Count > 0)
            query = query.Where(apartment => draft.CategoryIds.Contains(apartment.CategoryId));
        foreach (var requiredAmenityId in draft.RequiredAmenityIds)
        {
            query = query.Where(apartment =>
                apartment.ApartmentAmenities.Any(link => link.AmenityId == requiredAmenityId));
        }

        return query;
    }

    private static bool HasUsableMatchCriteria(RentalPreferenceDraft draft)
    {
        return draft.RegionId.HasValue ||
               draft.MinPrice.HasValue ||
               draft.MaxPrice.HasValue ||
               draft.MinArea.HasValue ||
               draft.MaxArea.HasValue ||
               draft.MinBedrooms.HasValue ||
               draft.CategoryIds.Count > 0 ||
               draft.AmenityIds.Count > 0 ||
               draft.FurnishingLevel.HasValue ||
               draft.AllowsPets == true ||
               draft.ParkingType.HasValue ||
               draft.AvailableBy.HasValue ||
               draft.MaxDistanceKm.HasValue ||
               draft.MinFloor.HasValue ||
               draft.MaxFloor.HasValue ||
               draft.HouseDirection.HasValue ||
               draft.MinLeaseMonths.HasValue ||
               draft.MaxLeaseMonths.HasValue;
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
