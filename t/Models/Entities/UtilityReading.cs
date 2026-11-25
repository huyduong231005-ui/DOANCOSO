using t.Models.Entities.Common;

namespace t.Models.Entities;

public class UtilityReading : BaseEntity
{
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = null!;

    public int UtilityTypeId { get; set; }
    public UtilityType UtilityType { get; set; } = null!;

    public int BillingMonth { get; set; }

    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
    // Chỉ số chỉ lưu số đồng hồ. Lượng tiêu thụ suy ra từ hai chỉ số; đơn giá và thành tiền
    // nằm ở dòng hoá đơn lập từ chỉ số này (InvoiceItem), không lưu lại ở đây.
    public decimal Consumption => CurrentReading - PreviousReading;

    /// <summary>Tạm tính theo đơn giá hiện hành (cần nạp UtilityType); số tiền chốt nằm ở dòng hoá đơn.</summary>
    public decimal EstimatedAmount => Math.Round(Consumption * UtilityType.DefaultRate, 0);

    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Các dòng hoá đơn lập từ chỉ số này. Chỉ số đã xuất hoá đơn khi có dòng thuộc hoá đơn chưa huỷ
    /// (xem <see cref="IsBilledExpr"/>), không lưu thêm cờ ở bảng chỉ số.
    /// </summary>
    public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public static readonly System.Linq.Expressions.Expression<Func<UtilityReading, bool>> IsBilledExpr =
        r => r.InvoiceItems.Any(i => i.Invoice.Status != InvoiceStatus.Cancelled);

    public static readonly System.Linq.Expressions.Expression<Func<UtilityReading, bool>> NotBilledExpr =
        r => !r.InvoiceItems.Any(i => i.Invoice.Status != InvoiceStatus.Cancelled);

    /// <summary>Dùng khi đã Include InvoiceItems.Invoice.</summary>
    public bool Billed => InvoiceItems.Any(i => i.Invoice != null && i.Invoice.Status != InvoiceStatus.Cancelled);
}
