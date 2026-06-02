using t.Models.Entities.Common;

namespace t.Models.Entities;

public sealed class RentalPreferenceCategory : IAuditable
{
    public int ProfileId { get; set; }
    public RentalPreferenceProfile Profile { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
