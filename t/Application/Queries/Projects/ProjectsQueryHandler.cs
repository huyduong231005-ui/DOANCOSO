using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.ViewModels;

namespace t.Application.Queries.Projects;

public sealed class ProjectsQueryHandler
{
    private readonly AppDbContext _db;

    public ProjectsQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ProjectListPageViewModel> GetProjectsAsync(string? regionSlug, int page = 1, int pageSize = 12, CancellationToken cancellationToken = default)
    {
        page = page <= 0 ? 1 : page;

        var query = _db.Projects
            .AsNoTracking()
            .Include(p => p.Region)
            .Include(p => p.Apartments)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(regionSlug))
            query = query.Where(p => p.Region.Slug == regionSlug);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProjectListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                RegionName = p.Region.Name,
                Address = p.Address,
                ThumbnailUrl = p.ThumbnailUrl,
                PriceFrom = p.PriceFrom,
                Status = p.Status,
                ShortDescription = p.ShortDescription,
                ApartmentCount = p.Apartments.Count
            })
            .ToListAsync(cancellationToken);

        return new ProjectListPageViewModel
        {
            Projects = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            RegionSlug = regionSlug
        };
    }

    public async Task<ProjectDetailViewModel?> GetProjectDetailAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _db.Projects
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Region)
            .Include(p => p.Images)
            .Include(p => p.Apartments).ThenInclude(a => a.Images)
            .Include(p => p.Apartments).ThenInclude(a => a.Category)
            .Include(p => p.Apartments).ThenInclude(a => a.ApartmentAmenities).ThenInclude(aa => aa.Amenity)
            .Where(p => p.Slug == slug)
            .Select(p => new ProjectDetailViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                RegionName = p.Region.Name,
                Address = p.Address,
                ThumbnailUrl = p.ThumbnailUrl,
                PriceFrom = p.PriceFrom,
                Status = p.Status,
                ShortDescription = p.ShortDescription,
                FullDescription = p.FullDescription,
                GalleryUrls = p.Images
                    .OrderBy(i => i.SortOrder)
                    .Select(i => i.Url)
                    .ToList(),
                Apartments = p.Apartments
                    .OrderByDescending(a => a.IsFeatured)
                    .Select(a => new ApartmentListViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Address = a.Address,
                        Price = a.Price,
                        Area = a.Area,
                        Bedrooms = a.Bedrooms,
                        CategoryName = a.Category.Name,
                        CoverImageUrl = a.Images.Where(i => i.IsCover).OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault()
                                        ?? a.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                        AmenityIcons = a.ApartmentAmenities.OrderBy(aa => aa.AmenityId).Select(aa => aa.Amenity.Icon).Take(3).ToList(),
                        AmenityNames = a.ApartmentAmenities.OrderBy(aa => aa.AmenityId).Select(aa => aa.Amenity.Name).Take(3).ToList()
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
