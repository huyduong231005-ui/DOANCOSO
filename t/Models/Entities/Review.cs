using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum ReviewStatus { Pending, Approved, Rejected }

public class Review : BaseEntity
{
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? RenterNote { get; set; }
    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }

    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
}
