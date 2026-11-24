using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Infrastructure.Billing;

public record InvoiceResult(bool Success, string Message, int? InvoiceId = null);

public class InvoiceGenerator
{
    /// <summary>Số ngày cư dân có để thanh toán kể từ ngày lập hoá đơn.</summary>
    public const int PaymentTermDays = 7;

    private readonly AppDbContext _db;
    private readonly ILogger<InvoiceGenerator> _log;

    public InvoiceGenerator(AppDbContext db, ILogger<InvoiceGenerator> log)
    {
        _db = db;
        _log = log;
    }

    public static int ToBillingMonth(DateTime d) => d.Year * 100 + d.Month;

    /// <summary>
    /// Hoá đơn phí tháng của một căn: phí quản lý (diện tích × đơn giá/m² của toà),
    /// điện/nước theo chỉ số chưa xuất và các khoản phí cố định đang áp dụng (gửi xe...).
    /// </summary>
    public async Task<InvoiceResult> GenerateMonthlyInvoiceAsync(int apartmentId, DateTime forDate, CancellationToken ct = default)
    {
        var apt = await _db.Apartments
            .Include(a => a.Floor.Building)
            .Include(a => a.UtilityReadings).ThenInclude(r => r.UtilityType)
            .Include(a => a.UtilityReadings).ThenInclude(r => r.InvoiceItems).ThenInclude(i => i.Invoice)
            .Include(a => a.RecurringCharges)
            .FirstOrDefaultAsync(a => a.Id == apartmentId, ct);
        if (apt == null) return new(false, "Không tìm thấy căn hộ.");
        if (apt.Condition == ApartmentCondition.NotHandedOver) return new(false, $"Căn {apt.UnitCode} chưa bàn giao, chưa thu phí.");

        var billingMonth = ToBillingMonth(forDate);

        // ponytail: kiểm tra-rồi-ghi, hai lần phát hành đồng thời có thể ra 2 hoá đơn cùng kỳ;
        // thêm partial unique index (CanHoId, ThangTinhPhi) WHERE Monthly & chưa huỷ khi dữ liệu cũ đã sạch trùng.
        var existing = await _db.Invoices.AnyAsync(i =>
            i.ApartmentId == apartmentId && i.BillingMonth == billingMonth && i.Kind == InvoiceKind.Monthly
            && i.Status != InvoiceStatus.Cancelled, ct);
        if (existing) return new(false, $"Căn {apt.UnitCode} đã có hoá đơn tháng {billingMonth % 100:00}/{billingMonth / 100}.");

        // Hạn = ngày lập + 7: lập đúng ngày chốt thì bằng ngày chốt + 7; lập muộn (bù kỳ, lập tay)
        // vẫn đủ 7 ngày, không sinh ra đã quá hạn rồi bị tính phí trễ.
        var dueDate = forDate.Date.AddDays(PaymentTermDays);

        var items = new List<InvoiceItem>();
        var sort = 0;
        var area = (decimal)apt.Area;
        if (apt.Floor.Building.ManagementFeePerM2 > 0 && area > 0)
        {
            items.Add(new InvoiceItem
            {
                Kind = InvoiceItemKind.ManagementFee,
                Description = $"Phí quản lý {apt.UnitCode} – {forDate:MM/yyyy} ({area:0.##} m²)",
                Quantity = area, UnitPrice = apt.Floor.Building.ManagementFeePerM2,
                LineTotal = Math.Round(area * apt.Floor.Building.ManagementFeePerM2, 0), SortOrder = sort++
            });
        }

        foreach (var r in apt.UtilityReadings.Where(r => !r.Billed && r.BillingMonth == billingMonth).OrderBy(r => r.UtilityTypeId))
        {
            // Dòng trỏ về chỉ số: từ nay chỉ số được coi là đã xuất hoá đơn (không cần cờ riêng).
            items.Add(new InvoiceItem
            {
                Kind = InvoiceItemKind.Utility, UtilityReading = r,
                Description = $"{r.UtilityType.Name} ({r.UtilityType.Unit}) – {r.Consumption:0.##}",
                Quantity = r.Consumption, UnitPrice = r.UtilityType.DefaultRate,
                LineTotal = Math.Round(r.Consumption * r.UtilityType.DefaultRate, 0), SortOrder = sort++
            });
        }

        foreach (var c in apt.RecurringCharges.Where(c => c.IsActive).OrderBy(c => c.SortOrder))
        {
            items.Add(new InvoiceItem
            {
                Kind = InvoiceItemKind.Recurring, RecurringCharge = c,
                Description = c.Description,
                Quantity = 1m, UnitPrice = c.Amount, LineTotal = c.Amount, SortOrder = sort++
            });
        }

        if (items.Count == 0) return new(false, $"Căn {apt.UnitCode} không có khoản phí nào trong kỳ.");

        var subTotal = items.Sum(x => x.LineTotal);
        var invoice = new Invoice
        {
            InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, billingMonth, ct),
            ApartmentId = apt.Id,
            Kind = InvoiceKind.Monthly,
            BillingMonth = billingMonth,
            IssueDate = forDate,
            DueDate = dueDate,
            SubTotal = subTotal,
            Total = subTotal,
            Balance = subTotal,
            Status = InvoiceStatus.Issued,
            Items = items
        };
        _db.Invoices.Add(invoice);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            invoice.InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, billingMonth, ct);
            await _db.SaveChangesAsync(ct);
        }

        _log.LogInformation("Generated invoice {Number} for apartment {ApartmentId} ({Total} VND)", invoice.InvoiceNumber, apt.Id, subTotal);
        return new(true, $"Đã tạo hoá đơn {invoice.InvoiceNumber} ({subTotal:N0} đ).", invoice.Id);
    }

    /// <summary>Phát hành phí tháng cho mọi căn đã bàn giao của một toà. Trả về số hoá đơn tạo được.</summary>
    public async Task<int> GenerateForBuildingAsync(int buildingId, DateTime forDate, CancellationToken ct = default)
    {
        var ids = await _db.Apartments
            .Where(a => a.Floor.BuildingId == buildingId && a.Condition != ApartmentCondition.NotHandedOver)
            .Select(a => a.Id)
            .ToListAsync(ct);
        var generated = 0;
        foreach (var id in ids)
        {
            // Một căn lỗi không được chặn cả toà; bỏ entity lỗi khỏi context rồi làm căn tiếp theo.
            try
            {
                if ((await GenerateMonthlyInvoiceAsync(id, forDate, ct)).Success) generated++;
            }
            catch (DbUpdateException ex)
            {
                _log.LogError(ex, "Monthly invoice failed for apartment {ApartmentId}", id);
                _db.ChangeTracker.Clear();
            }
        }
        return generated;
    }

    /// <summary>
    /// Create an ad-hoc OneOff invoice with arbitrary items (e.g. cleaning fee, damage).
    /// </summary>
    public async Task<InvoiceResult> GenerateOneOffInvoiceAsync(int apartmentId, string title, IReadOnlyList<(string description, decimal qty, decimal unitPrice)> lines, DateTime dueDate, string? note = null, CancellationToken ct = default)
    {
        if (!await _db.Apartments.AnyAsync(a => a.Id == apartmentId, ct)) return new(false, "Không tìm thấy căn hộ.");
        if (lines == null || lines.Count == 0) return new(false, "Hoá đơn phải có ít nhất một dòng.");

        var now = VnTime.Now;
        var items = new List<InvoiceItem>();
        var sort = 0;
        foreach (var (desc, qty, price) in lines)
        {
            var lineTotal = Math.Round(qty * price, 0);
            items.Add(new InvoiceItem
            {
                Description = desc, Quantity = qty, UnitPrice = price, LineTotal = lineTotal, SortOrder = sort++
            });
        }
        var subTotal = items.Sum(x => x.LineTotal);
        var invoice = new Invoice
        {
            InvoiceNumber = await BillingNumberGenerator.NextOneOffInvoiceNumberAsync(_db, ct),
            ApartmentId = apartmentId,
            Kind = InvoiceKind.OneOff,
            BillingMonth = ToBillingMonth(now),
            IssueDate = now,
            DueDate = dueDate,
            SubTotal = subTotal,
            Total = subTotal,
            Balance = subTotal,
            Status = InvoiceStatus.Issued,
            Note = note,
            Items = items
        };
        _db.Invoices.Add(invoice);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            invoice.InvoiceNumber = await BillingNumberGenerator.NextOneOffInvoiceNumberAsync(_db, ct);
            await _db.SaveChangesAsync(ct);
        }
        _log.LogInformation("Generated OneOff invoice {Number} for apartment {ApartmentId} ({Total} VND, '{Title}')", invoice.InvoiceNumber, apartmentId, subTotal, title);
        return new(true, $"Đã tạo hoá đơn {invoice.InvoiceNumber} ({subTotal:N0} đ).", invoice.Id);
    }

    public async Task<int> GenerateForBillingDayAsync(DateTime asOf, CancellationToken ct = default)
    {
        // "<=" chứ không "==": máy chủ tắt đúng ngày chốt thì lần chạy sau trong tháng vẫn bù được kỳ đó.
        // Chạy lại không sinh trùng vì GenerateMonthlyInvoiceAsync bỏ qua căn đã có hoá đơn kỳ này.
        // ponytail: tắt suốt phần còn lại của tháng thì kỳ đó vẫn lỡ; cần thì bù thêm tháng trước khi sang tháng mới.
        var buildingIds = await _db.Buildings
            .Where(b => b.DefaultBillingDay <= asOf.Day && b.Status == BuildingStatus.Active)
            .Select(b => b.Id)
            .ToListAsync(ct);

        var generated = 0;
        foreach (var id in buildingIds)
            generated += await GenerateForBuildingAsync(id, asOf, ct);
        _log.LogInformation("Auto-generated {Count} invoices for billing day {Day}", generated, asOf.Day);
        return generated;
    }

    public async Task<int> ApplyLateFeesAsync(DateTime asOf, CancellationToken ct = default)
    {
        var due = await _db.Invoices
            .Include(i => i.Apartment).ThenInclude(a => a.Floor).ThenInclude(f => f.Building)
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                        && i.Balance > 0
                        && i.DueDate < asOf
                        && i.LateFee == 0)
            .ToListAsync(ct);

        var applied = 0;
        foreach (var inv in due)
        {
            var building = inv.Apartment.Floor.Building;
            if ((asOf - inv.DueDate).TotalDays < building.DefaultLateFeeAfterDays) continue;

            var fee = Math.Round(inv.Balance * building.DefaultLateFeePercent / 100m, 0);
            // LateFee lives in its own column, not as an InvoiceItem — otherwise EditItems
            // would double-count it (SubTotal already includes the line + Total adds LateFee again).
            inv.LateFee = fee;
            inv.Total += fee;
            inv.Balance += fee;
            inv.Status = InvoiceStatus.Overdue;
            applied++;
        }
        if (applied > 0) await _db.SaveChangesAsync(ct);
        _log.LogInformation("Applied late fee to {Count} invoices", applied);
        return applied;
    }

    /// <summary>Hoá đơn còn nợ đã qua hạn thanh toán chuyển sang Quá hạn.</summary>
    public async Task<int> MarkOverdueInvoicesAsync(DateTime asOf, CancellationToken ct = default)
    {
        var toMark = await _db.Invoices
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                        && i.Balance > 0
                        && i.DueDate < asOf)
            .ToListAsync(ct);
        foreach (var inv in toMark) inv.Status = InvoiceStatus.Overdue;
        if (toMark.Count > 0) await _db.SaveChangesAsync(ct);
        return toMark.Count;
    }
}
