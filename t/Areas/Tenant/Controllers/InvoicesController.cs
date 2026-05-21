using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Tenant.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Localization;
using t.Infrastructure.Pdf;
using t.Models.Entities;

namespace t.Areas.Tenant.Controllers;

public class InvoicesController : TenantBaseController
{
    private readonly InvoicePdfGenerator _pdfGen;
    public InvoicesController(AppDbContext db, InvoicePdfGenerator pdfGen) : base(db) { _pdfGen = pdfGen; }

    public async Task<IActionResult> Pdf(int id)
    {
        var uid = CurrentUserId;
        var inv = await Db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Include(i => i.Lease).ThenInclude(l => l.Apartment)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id &&
                (i.Lease.PrimaryTenantId == uid || i.Lease.AdditionalTenants.Any(t => t.TenantId == uid)));
        if (inv == null) return NotFound();
        if (inv.Status == InvoiceStatus.Draft) return BadRequest("Hoá đơn chưa phát hành.");
        var bytes = _pdfGen.Generate(inv);
        return File(bytes, "application/pdf", $"{inv.InvoiceNumber}.pdf");
    }

    public async Task<IActionResult> Index(string? q, InvoiceStatus? status, int page = 1)
    {
        SetActiveNav("invoices");
        SetBreadcrumb(("Hoá đơn", null));

        const int pageSize = 20;
        var uid = CurrentUserId;
        var query = Db.Invoices.Where(i =>
            i.Lease.PrimaryTenantId == uid ||
            i.Lease.AdditionalTenants.Any(t => t.TenantId == uid));

        if (status.HasValue) query = query.Where(i => i.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(i => i.InvoiceNumber.Contains(keyword) ||
                                      i.Lease.LeaseNumber.Contains(keyword) ||
                                      i.Lease.Apartment.Title.Contains(keyword));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(i => i.IssueDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new TenantInvoiceRow
            {
                Id = i.Id, InvoiceNumber = i.InvoiceNumber,
                Kind = i.Kind, BillingMonth = i.BillingMonth,
                DueDate = i.DueDate, Total = i.Total, Balance = i.Balance, Status = i.Status
            }).ToListAsync();

        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.SumDue = await query
            .Where(i => i.Status == InvoiceStatus.Issued ||
                        i.Status == InvoiceStatus.PartiallyPaid ||
                        i.Status == InvoiceStatus.Overdue)
            .SumAsync(i => i.Balance);
        ViewBag.Pager = new t.Areas.Admin.Models.PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("invoices");
        var uid = CurrentUserId;
        var inv = await Db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.Apartment)
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id &&
                (i.Lease.PrimaryTenantId == uid || i.Lease.AdditionalTenants.Any(t => t.TenantId == uid)));
        if (inv == null) return NotFound();
        SetBreadcrumb(("Hoá đơn", Url.Action(nameof(Index))), (inv.InvoiceNumber, null));
        return View(inv);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPayment(SubmitPaymentVm input)
    {
        var uid = CurrentUserId;
        var inv = await Db.Invoices
            .Include(i => i.Payments)
            .Include(i => i.Lease)
            .FirstOrDefaultAsync(i => i.Id == input.InvoiceId &&
                (i.Lease.PrimaryTenantId == uid || i.Lease.AdditionalTenants.Any(t => t.TenantId == uid)));
        if (inv == null) return NotFound();

        if (inv.Status is InvoiceStatus.Cancelled or InvoiceStatus.Paid or InvoiceStatus.Refunded or InvoiceStatus.Draft)
        {
            TempData["Danger"] = $"Hoá đơn ở trạng thái {inv.Status.Vi()} — không nhận thêm thanh toán.";
            return RedirectToAction(nameof(Details), new { id = input.InvoiceId });
        }
        if (input.Amount <= 0)
        {
            TempData["Danger"] = "Số tiền phải lớn hơn 0.";
            return RedirectToAction(nameof(Details), new { id = input.InvoiceId });
        }

        // A tenant may submit multiple Pending payments only up to the remaining balance.
        var pendingTotal = inv.Payments.Where(p => p.Status == PaymentStatus.Pending).Sum(p => p.Amount);
        if (pendingTotal + input.Amount > inv.Balance)
        {
            TempData["Danger"] = $"Tổng các xác nhận đang chờ duyệt ({pendingTotal:N0}) cộng với số này vượt quá còn lại ({inv.Balance:N0}).";
            return RedirectToAction(nameof(Details), new { id = input.InvoiceId });
        }

        var payment = new Payment
        {
            PaymentNumber = await BillingNumberGenerator.NextPaymentNumberAsync(Db),
            InvoiceId = inv.Id,
            Amount = input.Amount,
            Method = input.Method,
            Status = PaymentStatus.Pending,
            TransactionRef = input.TransactionRef,
            Note = input.Note,
            Currency = inv.Currency
        };
        Db.Payments.Add(payment);
        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            payment.PaymentNumber = await BillingNumberGenerator.NextPaymentNumberAsync(Db);
            await Db.SaveChangesAsync();
        }
        TempData["Success"] = "Đã gửi thông tin thanh toán. Ban quản lý sẽ xác nhận.";
        return RedirectToAction(nameof(Details), new { id = input.InvoiceId });
    }
}
