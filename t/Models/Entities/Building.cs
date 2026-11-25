using t.Models.Entities.Common;

namespace t.Models.Entities;

public enum BuildingStatus { Active = 0, UnderMaintenance = 1, Closed = 2 }

public class Building : BaseEntity
{
    // Tên toà không lưu: luôn là "Toà " + mã toà.
    public string Name => "Toà " + Code;
    public string Code { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Địa chỉ của toà là địa chỉ của dự án (Project.Address), không lưu lại ở toà.
    // Số tầng không lưu ở toà: là số dòng Tang của toà (Floors.Count).

    public string? ManagerId { get; set; }
    public AppUser? Manager { get; set; }

    public BuildingStatus Status { get; set; } = BuildingStatus.Active;

    // Chính sách thu phí của toà (áp cho mọi căn trong toà, không có chỗ ghi đè).
    public int DefaultBillingDay { get; set; } = 1;
    public int DefaultLateFeeAfterDays { get; set; } = 7;
    public int DefaultLateFeePercent { get; set; } = 5;
    public decimal ManagementFeePerM2 { get; set; }

    // Căn của toà lấy qua tầng: Floors → Apartments.
    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
