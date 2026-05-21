using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum ViewingStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Completed = 3,
    NoShow = 4
}

public class ViewingAppointment : BaseEntity
{
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }

    public DateTime ScheduledDate { get; set; }
    public int SlotHour { get; set; }

    public string? Note { get; set; }

    public ViewingStatus Status { get; set; } = ViewingStatus.Pending;

    public DateTime? ConfirmedAt { get; set; }
    public string? ConfirmedBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
}
