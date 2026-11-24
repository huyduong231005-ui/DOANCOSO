using t.Models.Entities.Common;

namespace t.Models.Entities;

/// <summary>Dịch vụ tính theo chỉ số đồng hồ (điện, nước). Phí cố định theo căn nằm ở RecurringCharge.</summary>
public class UtilityType : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal DefaultRate { get; set; }

    public ICollection<UtilityReading> Readings { get; set; } = new List<UtilityReading>();
}
