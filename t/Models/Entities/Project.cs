using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum ProjectStatus
{
    Upcoming = 0,
    OpenForRent = 1,
    Completed = 2
}

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public string Address { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public decimal PriceFrom { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.OpenForRent;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;

    public Region Region { get; set; } = null!;
    public ICollection<ProjectImage> Images { get; set; } = new List<ProjectImage>();
    public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}
