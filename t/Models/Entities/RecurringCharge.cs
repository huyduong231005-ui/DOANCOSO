using t.Models.Entities.Common;

namespace t.Models.Entities;

/// <summary>
/// Khoản phí cố định lặp lại hằng tháng cho một hợp đồng (vd: Internet, Phí dịch vụ,
/// Phí quản lý...). Mỗi đầu kỳ, InvoiceGenerator sẽ thêm các khoản đang áp dụng
/// (IsActive = true) vào hoá đơn tiền thuê tháng dưới dạng InvoiceItem.
/// </summary>
public class RecurringCharge : BaseEntity
{
    public int LeaseId { get; set; }
    public Lease Lease { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
