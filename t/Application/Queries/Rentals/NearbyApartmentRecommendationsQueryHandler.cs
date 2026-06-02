using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Formatting;
using t.Infrastructure.Geo;
using t.Models;
using t.Models.Entities;

namespace t.Application.Queries.Rentals;

public sealed class NearbyApartmentRecommendationsQueryHandler
{
    private readonly AppDbContext _db;

    public NearbyApartmentRecommendationsQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SimilarApartmentViewModel>> GetAsync(
        Apartment currentApartment,
        int take = 3,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Apartments
            .AsNoTracking()
            .Where(apartment =>
                apartment.Id != currentApartment.Id &&
                apartment.Status == ListingStatus.Active);

        if (!GeoDistance.IsValidCoordinate(currentApartment.Latitude, currentApartment.Longitude))
        {
            return await query
                .Where(apartment => apartment.RegionId == currentApartment.RegionId)
                .OrderByDescending(apartment => apartment.CreatedAt)
                .ThenByDescending(apartment => apartment.Id)
                .Take(take)
                .Select(apartment => new SimilarApartmentViewModel
                {
                    Id = apartment.Id,
                    Tag = apartment.Category.Name,
                    Title = apartment.Title,
                    Meta = $"{apartment.Area} m2 • {apartment.Bedrooms} PN",
                    Price = RentalPriceFormatter.Format(apartment.Price),
                    Location = apartment.Address,
                    Image = apartment.Images
                                .Where(image => image.IsCover)
                                .Select(image => image.Url)
                                .FirstOrDefault()
                            ?? apartment.Images
                                .OrderBy(image => image.SortOrder)
                                .Select(image => image.Url)
                                .FirstOrDefault()
                            ?? string.Empty
                })
                .ToListAsync(cancellationToken);
        }

        var candidates = await query
            .Select(apartment => new RecommendationCandidate(
                apartment.Id,
                apartment.Category.Name,
                apartment.Title,
                apartment.Area,
                apartment.Bedrooms,
                apartment.Price,
                apartment.Address,
                apartment.Images
                    .Where(image => image.IsCover)
                    .Select(image => image.Url)
                    .FirstOrDefault()
                ?? apartment.Images
                    .OrderBy(image => image.SortOrder)
                    .Select(image => image.Url)
                    .FirstOrDefault()
                ?? string.Empty,
                apartment.Latitude,
                apartment.Longitude,
                apartment.CreatedAt))
            .ToListAsync(cancellationToken);

        return candidates
            .Select(candidate => new RecommendationCandidateDistance(
                candidate,
                GeoDistance.IsValidCoordinate(candidate.Latitude, candidate.Longitude)
                    ? GeoDistance.CalculateKm(
                        currentApartment.Latitude!.Value,
                        currentApartment.Longitude!.Value,
                        candidate.Latitude!.Value,
                        candidate.Longitude!.Value)
                    : null))
            .OrderBy(candidate => !candidate.DistanceKm.HasValue)
            .ThenBy(candidate => candidate.DistanceKm)
            .ThenByDescending(candidate => candidate.Candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.Candidate.Id)
            .Take(take)
            .Select(candidate => new SimilarApartmentViewModel
            {
                Id = candidate.Candidate.Id,
                Tag = candidate.Candidate.Tag,
                Title = candidate.Candidate.Title,
                Meta = $"{candidate.Candidate.Area} m2 • {candidate.Candidate.Bedrooms} PN",
                Price = RentalPriceFormatter.Format(candidate.Candidate.Price),
                Location = candidate.Candidate.Location,
                Image = candidate.Candidate.Image,
                DistanceKm = candidate.DistanceKm
            })
            .ToList();
    }

    private sealed record RecommendationCandidate(
        int Id,
        string Tag,
        string Title,
        double Area,
        int Bedrooms,
        decimal Price,
        string Location,
        string Image,
        double? Latitude,
        double? Longitude,
        DateTime CreatedAt);

    private sealed record RecommendationCandidateDistance(
        RecommendationCandidate Candidate,
        double? DistanceKm);
}
