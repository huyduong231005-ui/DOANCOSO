using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum UtilityBillingMode
{
    Metered = 0,
    Fixed = 1
}

public class UtilityType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public UtilityBillingMode BillingMode { get; set; } = UtilityBillingMode.Metered;
    public decimal DefaultRate { get; set; }
    public string? Icon { get; set; }

    public ICollection<UtilityReading> Readings { get; set; } = new List<UtilityReading>();
}
