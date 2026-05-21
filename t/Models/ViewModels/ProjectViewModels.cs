using t.Models.Entities;

namespace t.Models.ViewModels;

public class ProjectListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal PriceFrom { get; set; }
    public ProjectStatus Status { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public int ApartmentCount { get; set; }
}

public class ProjectListPageViewModel
{
    public List<ProjectListItemViewModel> Projects { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public string? RegionSlug { get; set; }
}

public class ProjectDetailViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal PriceFrom { get; set; }
    public ProjectStatus Status { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public List<string> GalleryUrls { get; set; } = new();
    public List<ApartmentListViewModel> Apartments { get; set; } = new();
}
