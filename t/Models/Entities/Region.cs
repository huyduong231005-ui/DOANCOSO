using t.Models.Entities.Common;

namespace t.Models.Entities;

public class Region : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
