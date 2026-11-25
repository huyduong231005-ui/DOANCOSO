using t.Models.Entities.Common;

namespace t.Models.Entities;

/// <summary>
/// Khoản phí cố định lặp lại hằng tháng của một căn hộ (vd: gửi xe máy, gửi ô tô,
/// Internet...). Mỗi đầu kỳ, InvoiceGenerator sẽ thêm các khoản đang áp dụng
/// (IsActive = true) vào hoá đơn tiền thuê tháng dưới dạng InvoiceItem.
/// </summary>
public class RecurringCharge : BaseEntity
{
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public bool IsActive { get; set; } = true;
}
