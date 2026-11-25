using t.Models.Entities.Common;

namespace t.Models.Entities;

/// <summary>Loại khoản trên hoá đơn, cho biết dòng lấy từ đâu.</summary>
public enum InvoiceItemKind
{
    ManagementFee = 0,  // phí quản lý = diện tích × đơn giá của toà
    Utility = 1,        // điện, nước theo một dòng chỉ số (UtilityReadingId)
    Recurring = 2,      // khoản phí định kỳ của căn (RecurringChargeId)
    Repair = 3,         // chi phí sửa chữa tính cho cư dân (MaintenanceRequestId)
    Other = 4
}

/// <summary>
/// Một dòng của hoá đơn. Nội dung, số lượng và đơn giá được chốt lúc lập để hoá đơn cũ không đổi khi bảng giá đổi;
/// khoá ngoại nguồn (chỉ số, phí định kỳ, sự cố) cho biết dòng được tạo từ dữ liệu nào.
/// </summary>
public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public InvoiceItemKind Kind { get; set; } = InvoiceItemKind.Other;
    public int? UtilityReadingId { get; set; }
    public UtilityReading? UtilityReading { get; set; }
    public int? RecurringChargeId { get; set; }
    public RecurringCharge? RecurringCharge { get; set; }
    public int? MaintenanceRequestId { get; set; }
    public MaintenanceRequest? MaintenanceRequest { get; set; }

    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
