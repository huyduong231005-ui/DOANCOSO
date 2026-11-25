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
    // nằm ở dòng hoá đơn của kỳ, không lưu lại ở đây.
    public decimal Consumption => CurrentReading - PreviousReading;

    /// <summary>Tạm tính theo đơn giá hiện hành (cần nạp UtilityType); số tiền chốt nằm ở dòng hoá đơn.</summary>
    public decimal EstimatedAmount => Math.Round(Consumption * UtilityType.DefaultRate, 0);

    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Chỉ số đã tính tiền khi căn có hoá đơn phí tháng chưa huỷ cùng kỳ (sau khi lập hoá đơn kỳ đó
    /// thì không nhận thêm chỉ số), nên không cần cờ hay khoá ngoại tới dòng hoá đơn.
    /// </summary>
    public static System.Linq.Expressions.Expression<Func<Invoice, bool>> BillingInvoice(int apartmentId, int billingMonth) =>
        i => i.ApartmentId == apartmentId && i.BillingMonth == billingMonth
             && i.Kind == InvoiceKind.Monthly && i.Status != InvoiceStatus.Cancelled;

    /// <summary>Dùng khi đã nạp Apartment.Invoices.</summary>
    public bool Billed => Apartment?.Invoices.Any(i => i.BillingMonth == BillingMonth && i.Kind == InvoiceKind.Monthly
                                                   && i.Status != InvoiceStatus.Cancelled) == true;
}
