using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class PaymentsController : AdminBaseController
{
    public PaymentsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, InvoiceStatus? status, int? month, int page = 1)
    {
        SetActiveNav("payments");
        SetBreadcrumb(("Công nợ phải thu", null));

        const int pageSize = 20;

        // Only invoices that still need collecting (Balance > 0, not Draft/Cancelled/Refunded/Paid).
        var query = Db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Include(i => i.Lease).ThenInclude(l => l.Apartment)
            .Include(i => i.Items)
            .Where(i => i.Balance > 0
                        && i.Status != InvoiceStatus.Draft
                        && i.Status != InvoiceStatus.Cancelled
                        && i.Status != InvoiceStatus.Refunded
                        && i.Status != InvoiceStatus.Paid)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(i => i.InvoiceNumber.Contains(q)
                                  || i.Lease.LeaseNumber.Contains(q)
                                  || i.Lease.PrimaryTenant.FullName.Contains(q));
        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (month.HasValue) query = query.Where(i => i.BillingMonth == month.Value);

        var total = await query.CountAsync();

        // KPIs across filtered query.
        var kpis = await query.GroupBy(_ => 1).Select(g => new
        {
            Outstanding = g.Sum(x => x.Balance),
            Overdue = g.Where(x => x.Status == InvoiceStatus.Overdue).Sum(x => x.Balance),
            Invoices = g.Count()
        }).FirstOrDefaultAsync();
        ViewBag.SumOutstanding = kpis?.Outstanding ?? 0m;
        ViewBag.SumOverdue = kpis?.Overdue ?? 0m;
        ViewBag.InvoiceCount = kpis?.Invoices ?? 0;
        ViewBag.PendingPaymentsCount = await Db.Payments.CountAsync(p => p.Status == PaymentStatus.Pending);

        var invoices = await query
            .OrderBy(i => i.Status == InvoiceStatus.Overdue ? 0 : 1)
            .ThenBy(i => i.DueDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        var pendingByInvoice = await Db.Payments
            .Where(p => p.Status == PaymentStatus.Pending && invoices.Select(i => i.Id).Contains(p.InvoiceId))
            .Select(p => p.InvoiceId)
            .ToListAsync();
        var pendingSet = pendingByInvoice.ToHashSet();

        var rows = invoices.Select(i =>
        {
            decimal rent = 0, util = 0, other = 0;
            if (i.Kind == InvoiceKind.MonthlyRent)
            {
                rent = i.Items.Where(x => x.SortOrder == 0).Sum(x => x.LineTotal);
                util = i.Items.Where(x => x.SortOrder > 0).Sum(x => x.LineTotal);
            }
            else
            {
                other = i.Items.Sum(x => x.LineTotal);
            }
            return new PaymentInvoiceListVm
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                LeaseNumber = i.Lease.LeaseNumber,
                TenantName = i.Lease.PrimaryTenant.FullName,
                UnitTitle = i.Lease.Apartment.Title,
                Kind = i.Kind,
                BillingMonth = i.BillingMonth,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                RentAmount = rent,
                UtilityAmount = util,
                OtherAmount = other,
                Total = i.Total,
                AmountPaid = i.AmountPaid,
                Balance = i.Balance,
                Status = i.Status,
                HasPendingPayment = pendingSet.Contains(i.Id)
            };
        }).ToList();

        ViewBag.Q = q; ViewBag.Status = status; ViewBag.Month = month;
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, month, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var raw = await Db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Where(i => i.Balance > 0
                     && i.Status != InvoiceStatus.Draft
                     && i.Status != InvoiceStatus.Cancelled
                     && i.Status != InvoiceStatus.Refunded
                     && i.Status != InvoiceStatus.Paid)
            .Where(i => i.InvoiceNumber.Contains(q) ||
                        i.Lease.LeaseNumber.Contains(q) ||
                        i.Lease.PrimaryTenant.FullName.Contains(q))
            .OrderBy(i => i.DueDate)
            .Take(limit)
            .Select(i => new { i.Id, i.InvoiceNumber, TenantName = i.Lease.PrimaryTenant.FullName, i.Balance, i.DueDate, i.Status })
            .ToListAsync();
        return Json(raw.Select(i => new
        {
            title = i.InvoiceNumber + " · " + i.TenantName,
            subtitle = "Còn " + i.Balance.ToString("N0") + " · Hạn " + i.DueDate.ToString("dd/MM/yyyy") + " · " + i.Status.Vi(),
            url = Url.Action("Details", "Invoices", new { id = i.Id }) ?? "#",
            icon = "payments"
        }));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int id, decimal? amount)
    {
        var p = await Db.Payments.Include(x => x.Invoice).FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();
        if (p.Status != PaymentStatus.Succeeded)
        {
            TempData["Danger"] = "Chỉ có thể hoàn tiền cho thanh toán đã thành công.";
            return RedirectToAction(nameof(Index));
        }

        var refundAmount = amount ?? (p.Amount - p.RefundedAmount);
        if (refundAmount <= 0 || refundAmount > p.Amount - p.RefundedAmount)
        {
            TempData["Danger"] = "Số tiền hoàn không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        p.RefundedAmount += refundAmount;
        p.RefundedAt = DateTime.UtcNow;
        p.Status = p.RefundedAmount >= p.Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;

        var inv = p.Invoice;
        inv.AmountPaid = Math.Max(0, inv.AmountPaid - refundAmount);
        inv.Balance = Math.Max(0, inv.Total - inv.AmountPaid);

        // Mirror to deposit ledger if this was a deposit invoice payment.
        await DepositLedger.OnPaymentReversedAsync(Db, inv, refundAmount, User.FindFirstValue(ClaimTypes.NameIdentifier));

        // Re-evaluate invoice status. If invoice is now fully unpaid AND every payment is fully refunded,
        // mark as Refunded; otherwise downgrade Paid → PartiallyPaid → Issued.
        await Db.Entry(inv).Collection(i => i.Payments).LoadAsync();
        var anyActive = inv.Payments.Any(x => x.Status == PaymentStatus.Succeeded || x.Status == PaymentStatus.PartiallyRefunded);
        if (inv.AmountPaid == 0)
            inv.Status = anyActive ? InvoiceStatus.Issued : InvoiceStatus.Refunded;
        else if (inv.AmountPaid < inv.Total) inv.Status = InvoiceStatus.PartiallyPaid;
        else inv.Status = InvoiceStatus.Paid;

        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã hoàn {refundAmount:N0} đ.";
        return RedirectToAction(nameof(Index));
    }
}
