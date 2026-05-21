using t.Models.Entities.Common;

namespace t.Models.Entities;

public class ProjectImage : BaseEntity
{
    public int ProjectId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
    public bool IsCover { get; set; }

    public Project Project { get; set; } = null!;
}
