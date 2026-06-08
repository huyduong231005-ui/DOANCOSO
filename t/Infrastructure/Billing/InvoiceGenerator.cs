using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Infrastructure.Billing;

public record InvoiceResult(bool Success, string Message, int? InvoiceId = null);

public class InvoiceGenerator
{
    private readonly AppDbContext _db;
    private readonly ILogger<InvoiceGenerator> _log;

    public InvoiceGenerator(AppDbContext db, ILogger<InvoiceGenerator> log)
    {
        _db = db;
        _log = log;
    }

    public static int ToBillingMonth(DateTime d) => d.Year * 100 + d.Month;

    public async Task<InvoiceResult> GenerateMonthlyInvoiceAsync(int leaseId, DateTime forDate, CancellationToken ct = default)
    {
        var lease = await _db.Leases
            .Include(l => l.Apartment)
            .Include(l => l.UtilityReadings).ThenInclude(r => r.UtilityType)
            .Include(l => l.RecurringCharges)
            .FirstOrDefaultAsync(l => l.Id == leaseId, ct);
        if (lease == null) return new(false, "Không tìm thấy hợp đồng.");
        if (lease.Status != LeaseStatus.Active) return new(false, "Hợp đồng không ở trạng thái Active.");

        var billingMonth = ToBillingMonth(forDate);

        var existing = await _db.Invoices.AnyAsync(i =>
            i.LeaseId == leaseId && i.BillingMonth == billingMonth && i.Kind == InvoiceKind.MonthlyRent, ct);
        if (existing) return new(false, $"Đã có hoá đơn tháng {billingMonth/100}/{billingMonth%100:00}.");

        var dueDay = Math.Min(lease.BillingDay + 7, 28);
        var dueDate = new DateTime(forDate.Year, forDate.Month, dueDay);

        var items = new List<InvoiceItem>
        {
            new()
            {
                Description = $"Tiền thuê {lease.Apartment.UnitCode ?? lease.Apartment.Title} – {forDate:MM/yyyy}",
                Quantity = 1m, UnitPrice = lease.MonthlyRent, LineTotal = lease.MonthlyRent, SortOrder = 0
            }
        };

        // Tự ước chỉ số điện/nước cho kỳ này nếu chưa có (theo mức tiêu thụ kỳ gần nhất),
        // để hoá đơn luôn đủ chi phí. Admin nhập chỉ số thực sau thì sửa/tính lại.
        var estimatedReadings = new List<UtilityReading>();
        var typeIdsWithHistory = lease.UtilityReadings.Select(r => r.UtilityTypeId).Distinct().ToList();
        foreach (var typeId in typeIdsWithHistory)
        {
            if (lease.UtilityReadings.Any(r => r.UtilityTypeId == typeId && r.BillingMonth == billingMonth))
                continue;

            var last = lease.UtilityReadings
                .Where(r => r.UtilityTypeId == typeId && r.BillingMonth < billingMonth)
                .OrderByDescending(r => r.BillingMonth)
                .FirstOrDefault();
            if (last == null) continue;

            estimatedReadings.Add(new UtilityReading
            {
                LeaseId = lease.Id,
                UtilityTypeId = typeId,
                UtilityType = last.UtilityType,
                BillingMonth = billingMonth,
                PreviousReading = last.CurrentReading,
                Consumption = last.Consumption,
                CurrentReading = last.CurrentReading + last.Consumption,
                Rate = last.Rate,
                Amount = Math.Round(last.Consumption * last.Rate, 0),
                Billed = false,
                ReadAt = forDate,
                Note = "Ước theo kỳ trước (chưa chốt chỉ số thực)"
            });
        }

        // Gộp chỉ số chưa xuất sẵn có + chỉ số vừa ước, tránh đếm trùng do EF fixup.
        var unbilledReadings = lease.UtilityReadings
            .Where(r => !r.Billed && r.BillingMonth == billingMonth)
            .Concat(estimatedReadings)
            .OrderBy(r => r.UtilityTypeId)
            .ToList();
        if (estimatedReadings.Count > 0) _db.UtilityReadings.AddRange(estimatedReadings);
        var sort = 1;
        foreach (var r in unbilledReadings)
        {
            items.Add(new InvoiceItem
            {
                Description = $"{r.UtilityType.Name} ({r.UtilityType.Unit}) – {r.Consumption}",
                Quantity = r.Consumption, UnitPrice = r.Rate, LineTotal = r.Amount, SortOrder = sort++
            });
            r.Billed = true;
        }

        // Phí cố định lặp lại hằng tháng (Internet, phí dịch vụ...) đang áp dụng cho hợp đồng.
        foreach (var c in lease.RecurringCharges.Where(c => c.IsActive).OrderBy(c => c.SortOrder))
        {
            items.Add(new InvoiceItem
            {
                Description = c.Description,
                Quantity = 1m, UnitPrice = c.Amount, LineTotal = c.Amount, SortOrder = sort++
            });
        }

        var subTotal = items.Sum(x => x.LineTotal);
        var invoice = new Invoice
        {
            InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, billingMonth, ct),
            LeaseId = lease.Id,
            Kind = InvoiceKind.MonthlyRent,
            BillingMonth = billingMonth,
            IsRecurring = true,
            IssueDate = forDate,
            DueDate = dueDate,
            SubTotal = subTotal,
            Total = subTotal,
            Balance = subTotal,
            Status = InvoiceStatus.Issued,
            Currency = "VND",
            Items = items
        };
        _db.Invoices.Add(invoice);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            invoice.InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, billingMonth, ct);
            await _db.SaveChangesAsync(ct);
        }

        _log.LogInformation("Generated invoice {Number} for lease {LeaseId} ({Total} VND)", invoice.InvoiceNumber, lease.Id, subTotal);
        return new(true, $"Đã tạo hoá đơn {invoice.InvoiceNumber} ({subTotal:N0} đ).", invoice.Id);
    }

    /// <summary>
    /// Create a Deposit invoice for a Pending lease, if one doesn't already exist.
    /// Should be called when the lease is created or first activated.
    /// </summary>
    public async Task<InvoiceResult> GenerateDepositInvoiceAsync(int leaseId, CancellationToken ct = default)
    {
        var lease = await _db.Leases
            .Include(l => l.Apartment)
            .FirstOrDefaultAsync(l => l.Id == leaseId, ct);
        if (lease == null) return new(false, "Không tìm thấy hợp đồng.");
        if (lease.Deposit <= 0) return new(false, "Hợp đồng không có khoản cọc.");

        var exists = await _db.Invoices.AnyAsync(i =>
            i.LeaseId == leaseId && i.Kind == InvoiceKind.Deposit && i.Status != InvoiceStatus.Cancelled, ct);
        if (exists) return new(false, "Đã có hoá đơn đặt cọc.");

        var now = VnTime.Now;
        var billingMonth = ToBillingMonth(now);
        var invoice = new Invoice
        {
            InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, billingMonth, ct),
            LeaseId = lease.Id,
            Kind = InvoiceKind.Deposit,
            BillingMonth = billingMonth,
            IsRecurring = false,
            IssueDate = now,
            DueDate = now.AddDays(7),
            SubTotal = lease.Deposit,
            Total = lease.Deposit,
            Balance = lease.Deposit,
            Status = InvoiceStatus.Issued,
            Currency = "VND",
            Items = new List<InvoiceItem>
            {
                new() { Description = $"Tiền đặt cọc — {lease.Apartment.UnitCode ?? lease.Apartment.Title}",
                        Quantity = 1m, UnitPrice = lease.Deposit, LineTotal = lease.Deposit, SortOrder = 0 }
            }
        };
        _db.Invoices.Add(invoice);
        try { await _db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            invoice.InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, billingMonth, ct);
            await _db.SaveChangesAsync(ct);
        }

        _log.LogInformation("Generated Deposit invoice {Number} for lease {LeaseId}", invoice.InvoiceNumber, lease.Id);
        return new(true, $"Đã tạo hoá đơn đặt cọc {invoice.InvoiceNumber} ({lease.Deposit:N0} đ).", invoice.Id);
    }

    /// <summary>
    /// Create an ad-hoc OneOff invoice with arbitrary items (e.g. cleaning fee, damage).
    /// </summary>
    public async Task<InvoiceResult> GenerateOneOffInvoiceAsync(int leaseId, string title, IReadOnlyList<(string description, decimal qty, decimal unitPrice)> lines, DateTime dueDate, string? note = null, CancellationToken ct = default)
    {
        var lease = await _db.Leases.FirstOrDefaultAsync(l => l.Id == leaseId, ct);
        if (lease == null) return new(false, "Không tìm thấy hợp đồng.");
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
            LeaseId = lease.Id,
            Kind = InvoiceKind.OneOff,
            BillingMonth = ToBillingMonth(now),
            IsRecurring = false,
            IssueDate = now,
            DueDate = dueDate,
            SubTotal = subTotal,
            Total = subTotal,
            Balance = subTotal,
            Status = InvoiceStatus.Issued,
            Currency = "VND",
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
        _log.LogInformation("Generated OneOff invoice {Number} for lease {LeaseId} ({Total} VND, '{Title}')", invoice.InvoiceNumber, lease.Id, subTotal, title);
        return new(true, $"Đã tạo hoá đơn {invoice.InvoiceNumber} ({subTotal:N0} đ).", invoice.Id);
    }

    public async Task<int> GenerateForBillingDayAsync(DateTime asOf, CancellationToken ct = default)
    {
        var billingDay = asOf.Day;
        var billingMonth = ToBillingMonth(asOf);

        var leaseIds = await _db.Leases
            .Where(l => l.Status == LeaseStatus.Active && l.BillingDay == billingDay
                        && l.StartDate <= asOf && l.EndDate >= asOf
                        && !l.Invoices.Any(i => i.BillingMonth == billingMonth && i.Kind == InvoiceKind.MonthlyRent))
            .Select(l => l.Id)
            .ToListAsync(ct);

        var generated = 0;
        foreach (var id in leaseIds)
        {
            var r = await GenerateMonthlyInvoiceAsync(id, asOf, ct);
            if (r.Success) generated++;
        }
        _log.LogInformation("Auto-generated {Count} invoices for billing day {Day}", generated, billingDay);
        return generated;
    }

    public async Task<int> ApplyLateFeesAsync(DateTime asOf, CancellationToken ct = default)
    {
        var dueBefore = asOf.AddDays(-1);
        var due = await _db.Invoices
            .Include(i => i.Lease)
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                        && i.DueDate < dueBefore
                        && i.LateFee == 0)
            .ToListAsync(ct);

        var applied = 0;
        foreach (var inv in due)
        {
            var graceDays = inv.Lease.LateFeeAfterDays;
            if ((asOf - inv.DueDate).TotalDays < graceDays) continue;

            var fee = Math.Round(inv.Balance * inv.Lease.LateFeePercent / 100m, 0);
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
}
