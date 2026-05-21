using t.Models.Entities.Common;

namespace t.Models.Entities;

public class UtilityReading : BaseEntity
{
    public int LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public int UtilityTypeId { get; set; }
    public UtilityType UtilityType { get; set; } = null!;

    public int BillingMonth { get; set; }

    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
    public decimal Consumption { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }

    public bool Billed { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? Note { get; set; }
}
