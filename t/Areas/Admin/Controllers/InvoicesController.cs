using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Localization;
using t.Infrastructure.Pdf;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class InvoicesController : AdminBaseController
{
    private readonly InvoicePdfGenerator _pdfGen;
    private readonly InvoiceGenerator _invoiceGen;
    public InvoicesController(AppDbContext db, InvoicePdfGenerator pdfGen, InvoiceGenerator invoiceGen) : base(db)
    {
        _pdfGen = pdfGen;
        _invoiceGen = invoiceGen;
    }

    public async Task<IActionResult> Pdf(int id)
    {
        var inv = await Db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Include(i => i.Lease).ThenInclude(l => l.Apartment)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv == null) return NotFound();
        var bytes = _pdfGen.Generate(inv);
        return File(bytes, "application/pdf", $"{inv.InvoiceNumber}.pdf");
    }

    public async Task<IActionResult> Index(string? q, InvoiceStatus? status, InvoiceKind? kind, int? month, int page = 1)
    {
        SetActiveNav("invoices");
        SetBreadcrumb(("Hoá đơn", null));

        const int pageSize = 20;
        var query = Db.Invoices.Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
                                .Include(i => i.Lease).ThenInclude(l => l.Apartment).AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.InvoiceNumber.Contains(q) ||
                                     i.Lease.LeaseNumber.Contains(q) ||
                                     i.Lease.PrimaryTenant.FullName.Contains(q));
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (kind.HasValue) query = query.Where(i => i.Kind == kind.Value);
        if (month.HasValue) query = query.Where(i => i.BillingMonth == month.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new InvoiceListVm
            {
                Id = i.Id, InvoiceNumber = i.InvoiceNumber,
                LeaseNumber = i.Lease.LeaseNumber,
                TenantName = i.Lease.PrimaryTenant.FullName,
                UnitTitle = i.Lease.Apartment.Title,
                Kind = i.Kind, BillingMonth = i.BillingMonth,
                IssueDate = i.IssueDate, DueDate = i.DueDate,
                Total = i.Total, AmountPaid = i.AmountPaid, Balance = i.Balance,
                Status = i.Status
            }).ToListAsync();

        // Pending payment count for badge
        ViewBag.PendingPaymentsCount = await Db.Payments.CountAsync(p => p.Status == PaymentStatus.Pending);

        // KPIs (across all filtered query, not just page)
        var kpis = await query.GroupBy(i => 1).Select(g => new {
            Total = g.Sum(x => x.Total),
            Paid = g.Sum(x => x.AmountPaid),
            Balance = g.Sum(x => x.Balance)
        }).FirstOrDefaultAsync();
        ViewBag.SumTotal = kpis?.Total ?? 0m;
        ViewBag.SumPaid = kpis?.Paid ?? 0m;
        ViewBag.SumBalance = kpis?.Balance ?? 0m;

        ViewBag.Q = q; ViewBag.Status = status; ViewBag.Kind = kind; ViewBag.Month = month;
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, kind, month, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var raw = await Db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Where(i => i.InvoiceNumber.Contains(q) ||
                        i.Lease.LeaseNumber.Contains(q) ||
                        i.Lease.PrimaryTenant.FullName.Contains(q))
            .OrderByDescending(i => i.IssueDate)
            .Take(limit)
            .Select(i => new { i.Id, i.InvoiceNumber, TenantName = i.Lease.PrimaryTenant.FullName, i.Total, i.Balance, i.Status })
            .ToListAsync();
        return Json(raw.Select(i => new
        {
            title = i.InvoiceNumber + " · " + i.TenantName,
            subtitle = "Tổng " + i.Total.ToString("N0") + " · Còn " + i.Balance.ToString("N0") + " · " + i.Status.Vi(),
            url = Url.Action(nameof(Details), new { id = i.Id }) ?? "#",
            icon = "receipt_long"
        }));
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("invoices");
        var inv = await Db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Include(i => i.Lease).ThenInclude(l => l.Apartment).ThenInclude(a => a.Building)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inv == null) return NotFound();
        SetBreadcrumb(("Hoá đơn", Url.Action(nameof(Index))), (inv.InvoiceNumber, null));
        return View(inv);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Issue(int id)
    {
        var inv = await Db.Invoices.FindAsync(id);
        if (inv == null) return NotFound();
        if (inv.Status != InvoiceStatus.Draft)
        {
            TempData["Danger"] = $"Chỉ phát hành được hoá đơn ở trạng thái Nháp (hiện tại: {inv.Status.Vi()}).";
            return RedirectToAction(nameof(Details), new { id });
        }
        inv.Status = InvoiceStatus.Issued;
        if (inv.IssueDate == default) inv.IssueDate = VnTime.Now;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã phát hành hoá đơn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var inv = await Db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == id);
        if (inv == null) return NotFound();

        if (inv.Status is InvoiceStatus.Cancelled or InvoiceStatus.Paid or InvoiceStatus.Refunded)
        {
            TempData["Danger"] = $"Không thể huỷ hoá đơn ở trạng thái {inv.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (inv.Payments.Any(p => p.Status == PaymentStatus.Succeeded && p.RefundedAmount < p.Amount))
        {
            TempData["Danger"] = "Hoá đơn đã có thanh toán thành công — vui lòng hoàn tiền trước khi huỷ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Reject any Pending payments tied to this invoice
        foreach (var p in inv.Payments.Where(p => p.Status == PaymentStatus.Pending))
        {
            p.Status = PaymentStatus.Cancelled;
            p.Note = string.IsNullOrWhiteSpace(p.Note)
                ? "Huỷ do hoá đơn bị huỷ"
                : p.Note + " · Huỷ do hoá đơn bị huỷ";
        }

        inv.Status = InvoiceStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            inv.Note = string.IsNullOrEmpty(inv.Note) ? reason : inv.Note + "\nLý do huỷ: " + reason;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã huỷ hoá đơn.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Admin records a direct payment (cash, manual bank). Creates a Succeeded payment immediately.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordPayment(RecordPaymentVm input)
    {
        var inv = await Db.Invoices.Include(i => i.Payments).FirstOrDefaultAsync(i => i.Id == input.InvoiceId);
        if (inv == null) return NotFound();
        if (inv.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled or InvoiceStatus.Paid or InvoiceStatus.Refunded)
        {
            TempData["Danger"] = $"Không thể ghi nhận thanh toán cho hoá đơn ở trạng thái {inv.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }
        if (input.Amount <= 0)
        {
            TempData["Danger"] = "Số tiền phải lớn hơn 0.";
            return RedirectToAction(nameof(Details), new { id = input.InvoiceId });
        }
        if (input.Amount > inv.Balance)
        {
            TempData["Danger"] = $"Số tiền ({input.Amount:N0}) vượt quá còn lại ({inv.Balance:N0}).";
            return RedirectToAction(nameof(Details), new { id = input.InvoiceId });
        }

        var payment = new Payment
        {
            PaymentNumber = await BillingNumberGenerator.NextPaymentNumberAsync(Db),
            InvoiceId = inv.Id,
            Amount = input.Amount,
            Method = input.Method,
            Status = PaymentStatus.Succeeded,
            TransactionRef = input.TransactionRef,
            Note = input.Note,
            PaidAt = DateTime.UtcNow,
            Currency = inv.Currency
        };
        Db.Payments.Add(payment);

        ApplyPaymentSuccess(inv, input.Amount);
        await DepositLedger.OnPaymentAppliedAsync(Db, inv, input.Amount, User.FindFirstValue(ClaimTypes.NameIdentifier));

        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            payment.PaymentNumber = await BillingNumberGenerator.NextPaymentNumberAsync(Db);
            await Db.SaveChangesAsync();
        }
        TempData["Success"] = "Đã ghi nhận thanh toán.";
        return RedirectToAction(nameof(Details), new { id = inv.Id });
    }

    /// <summary>Approve a Pending payment submitted by tenant.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApprovePayment(int paymentId, string? returnInvoiceId = null)
    {
        var payment = await Db.Payments.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null) return NotFound();
        var inv = payment.Invoice;

        if (payment.Status != PaymentStatus.Pending)
        {
            TempData["Danger"] = $"Chỉ duyệt được thanh toán đang chờ (hiện tại: {payment.Status.Vi()}).";
            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }
        if (inv.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled or InvoiceStatus.Refunded)
        {
            TempData["Danger"] = $"Hoá đơn ở trạng thái {inv.Status.Vi()} — không duyệt được.";
            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }
        if (payment.Amount > inv.Balance)
        {
            TempData["Danger"] = $"Số tiền ({payment.Amount:N0}) vượt quá còn lại ({inv.Balance:N0}). Vui lòng từ chối và yêu cầu khách gửi đúng số.";
            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }

        payment.Status = PaymentStatus.Succeeded;
        payment.PaidAt = DateTime.UtcNow;
        ApplyPaymentSuccess(inv, payment.Amount);
        await DepositLedger.OnPaymentAppliedAsync(Db, inv, payment.Amount, User.FindFirstValue(ClaimTypes.NameIdentifier));

        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã duyệt thanh toán {payment.PaymentNumber}.";
        return RedirectToAction(nameof(Details), new { id = inv.Id });
    }

    /// <summary>Reject a Pending payment.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectPayment(int paymentId, string? reason)
    {
        var payment = await Db.Payments.Include(p => p.Invoice).FirstOrDefaultAsync(p => p.Id == paymentId);
        if (payment == null) return NotFound();
        if (payment.Status != PaymentStatus.Pending)
        {
            TempData["Danger"] = $"Chỉ từ chối được thanh toán đang chờ (hiện tại: {payment.Status.Vi()}).";
            return RedirectToAction(nameof(Details), new { id = payment.InvoiceId });
        }

        payment.Status = PaymentStatus.Failed;
        if (!string.IsNullOrWhiteSpace(reason))
            payment.Note = string.IsNullOrEmpty(payment.Note) ? "Từ chối: " + reason : payment.Note + "\nTừ chối: " + reason;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã từ chối thanh toán {payment.PaymentNumber}.";
        return RedirectToAction(nameof(Details), new { id = payment.InvoiceId });
    }

    /// <summary>Inbox view for Pending payments awaiting approval.</summary>
    public async Task<IActionResult> PendingPayments()
    {
        SetActiveNav("invoices");
        SetBreadcrumb(("Hoá đơn", Url.Action(nameof(Index))), ("Chờ duyệt", null));

        var rows = await Db.Payments
            .Include(p => p.Invoice).ThenInclude(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .Select(p => new PaymentListVm
            {
                Id = p.Id, PaymentNumber = p.PaymentNumber,
                InvoiceNumber = p.Invoice.InvoiceNumber,
                TenantName = p.Invoice.Lease.PrimaryTenant.FullName,
                Amount = p.Amount, Method = p.Method, Status = p.Status,
                PaidAt = p.CreatedAt,
                TransactionRef = p.TransactionRef
            })
            .ToListAsync();
        return View(rows);
    }

    // ── OneOff invoice ──
    public async Task<IActionResult> CreateOneOff(int? leaseId)
    {
        SetActiveNav("invoices");
        SetBreadcrumb(("Hoá đơn", Url.Action(nameof(Index))), ("Tạo bổ sung", null));

        var vm = new OneOffInvoiceVm
        {
            LeaseId = leaseId ?? 0,
            DueDate = VnTime.Today.AddDays(7),
            Lines = new List<OneOffInvoiceLine> { new(), new(), new() }
        };
        await PopulateOneOffLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOneOff(OneOffInvoiceVm input)
    {
        if (input.LeaseId <= 0) ModelState.AddModelError(nameof(input.LeaseId), "Chọn hợp đồng.");
        var lines = (input.Lines ?? new())
            .Where(l => !string.IsNullOrWhiteSpace(l.Description) && l.Quantity > 0 && l.UnitPrice > 0)
            .Select(l => (l.Description!.Trim(), l.Quantity, l.UnitPrice))
            .ToList();
        if (lines.Count == 0) ModelState.AddModelError("", "Cần ít nhất một dòng có mô tả và số tiền.");
        if (!ModelState.IsValid)
        {
            await PopulateOneOffLookupsAsync(input);
            return View(input);
        }

        var result = await _invoiceGen.GenerateOneOffInvoiceAsync(input.LeaseId, input.Title ?? "Hoá đơn bổ sung", lines, input.DueDate, input.Note);
        if (!result.Success)
        {
            TempData["Danger"] = result.Message;
            await PopulateOneOffLookupsAsync(input);
            return View(input);
        }
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Details), new { id = result.InvoiceId });
    }

    // ── Edit items (manual adjustment) ──
    public async Task<IActionResult> EditItems(int id)
    {
        SetActiveNav("invoices");
        var inv = await Db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == id);
        if (inv == null) return NotFound();
        if (inv.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled or InvoiceStatus.Refunded)
        {
            TempData["Danger"] = $"Không thể sửa hoá đơn ở trạng thái {inv.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        SetBreadcrumb(("Hoá đơn", Url.Action(nameof(Index))), (inv.InvoiceNumber, Url.Action(nameof(Details), new { id })), ("Sửa dòng", null));

        var vm = new EditInvoiceItemsVm
        {
            InvoiceId = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            Discount = inv.Discount,
            Tax = inv.Tax,
            Note = inv.Note,
            Lines = inv.Items.OrderBy(x => x.SortOrder)
                .Select(x => new OneOffInvoiceLine
                {
                    Id = x.Id, Description = x.Description, Quantity = x.Quantity, UnitPrice = x.UnitPrice
                }).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditItems(EditInvoiceItemsVm input)
    {
        var inv = await Db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == input.InvoiceId);
        if (inv == null) return NotFound();
        if (inv.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled or InvoiceStatus.Refunded)
        {
            TempData["Danger"] = $"Không thể sửa hoá đơn ở trạng thái {inv.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id = inv.Id });
        }

        var keepIds = input.Lines.Where(l => l.Id > 0).Select(l => l.Id).ToHashSet();
        foreach (var existing in inv.Items.Where(x => !keepIds.Contains(x.Id)).ToList())
            Db.InvoiceItems.Remove(existing);

        var sort = 0;
        foreach (var line in input.Lines.Where(l => !string.IsNullOrWhiteSpace(l.Description) && l.Quantity > 0))
        {
            var lineTotal = Math.Round(line.Quantity * line.UnitPrice, 0);
            if (line.Id > 0)
            {
                var item = inv.Items.FirstOrDefault(x => x.Id == line.Id);
                if (item != null)
                {
                    item.Description = line.Description!;
                    item.Quantity = line.Quantity;
                    item.UnitPrice = line.UnitPrice;
                    item.LineTotal = lineTotal;
                    item.SortOrder = sort++;
                }
            }
            else
            {
                inv.Items.Add(new InvoiceItem
                {
                    Description = line.Description!, Quantity = line.Quantity,
                    UnitPrice = line.UnitPrice, LineTotal = lineTotal, SortOrder = sort++
                });
            }
        }

        inv.Discount = Math.Max(0, input.Discount);
        inv.Tax = Math.Max(0, input.Tax);
        inv.Note = input.Note;
        RecomputeTotals(inv);

        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã cập nhật dòng và tính lại tổng.";
        return RedirectToAction(nameof(Details), new { id = inv.Id });
    }

    // ── Bulk action: mark Overdue manually for selected ──
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkMarkOverdue(int[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            TempData["Danger"] = "Chưa chọn hoá đơn.";
            return RedirectToAction(nameof(Index));
        }
        var rows = await Db.Invoices.Where(i => ids.Contains(i.Id) &&
            (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)).ToListAsync();
        foreach (var inv in rows) inv.Status = InvoiceStatus.Overdue;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã đánh dấu quá hạn {rows.Count} hoá đơn.";
        return RedirectToAction(nameof(Index));
    }

    // ── Helpers ──

    private static void ApplyPaymentSuccess(Invoice inv, decimal amount)
    {
        inv.AmountPaid += amount;
        inv.Balance = Math.Max(0, inv.Total - inv.AmountPaid);
        inv.Status = inv.Balance == 0 ? InvoiceStatus.Paid
                    : inv.AmountPaid > 0 ? InvoiceStatus.PartiallyPaid
                    : inv.Status;
    }

    private static void RecomputeTotals(Invoice inv)
    {
        inv.SubTotal = inv.Items.Sum(x => x.LineTotal);
        inv.Total = Math.Max(0, inv.SubTotal - inv.Discount + inv.Tax + inv.LateFee);
        inv.Balance = Math.Max(0, inv.Total - inv.AmountPaid);
        if (inv.Balance == 0 && inv.AmountPaid > 0) inv.Status = InvoiceStatus.Paid;
        else if (inv.AmountPaid > 0) inv.Status = InvoiceStatus.PartiallyPaid;
        else if (inv.Status == InvoiceStatus.Paid) inv.Status = InvoiceStatus.Issued;
    }

    private async Task PopulateOneOffLookupsAsync(OneOffInvoiceVm vm)
    {
        vm.Leases = await Db.Leases
            .Include(l => l.PrimaryTenant)
            .Include(l => l.Apartment)
            .Where(l => l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Pending || l.Status == LeaseStatus.Renewing)
            .OrderBy(l => l.LeaseNumber)
            .Select(l => new LeaseOption
            {
                Id = l.Id,
                Label = l.LeaseNumber + " · " + l.PrimaryTenant.FullName + " · " + l.Apartment.Title
            })
            .ToListAsync();
    }
}
