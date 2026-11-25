namespace t.Models.Entities;

/// <summary>
/// Một dòng của hoá đơn: một loại dịch vụ trên một hoá đơn (khoá chính HoaDonId + LoaiDichVuId).
/// Số lượng và đơn giá chốt lúc lập để hoá đơn cũ không đổi khi bảng giá hay diện tích đổi.
/// </summary>
public class InvoiceItem
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int UtilityTypeId { get; set; }
    public UtilityType UtilityType { get; set; } = null!;

    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Tên dòng hiển thị = tên loại dịch vụ (cần nạp UtilityType).</summary>
    public string Description => UtilityType?.Name ?? string.Empty;
}
