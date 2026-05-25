using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class MaintenanceController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly InvoiceGenerator _invoiceGen;

    public MaintenanceController(AppDbContext db, UserManager<AppUser> userManager, InvoiceGenerator invoiceGen) : base(db)
    {
        _userManager = userManager;
        _invoiceGen = invoiceGen;
    }

    public async Task<IActionResult> Index(string? q, MaintenanceStatus? status, MaintenancePriority? priority, MaintenanceCategory? category, int? buildingId, int page = 1)
    {
        SetActiveNav("maintenance");
        SetBreadcrumb(("Bảo trì / Sự cố", null));

        const int pageSize = 25;
        var query = Db.MaintenanceRequests
            .Include(r => r.Apartment).ThenInclude(a => a.Building)
            .Include(r => r.Reporter)
            .Include(r => r.AssignedTo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.RequestNumber.Contains(q) || r.Title.Contains(q) || r.Description.Contains(q) || r.Apartment.Title.Contains(q));
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (priority.HasValue) query = query.Where(r => r.Priority == priority.Value);
        if (category.HasValue) query = query.Where(r => r.Category == category.Value);
        if (buildingId.HasValue) query = query.Where(r => r.Apartment.BuildingId == buildingId.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.Priority).ThenByDescending(r => r.ReportedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new MaintenanceListVm
            {
                Id = r.Id, RequestNumber = r.RequestNumber, Title = r.Title,
                ApartmentId = r.ApartmentId,
                UnitTitle = r.Apartment.Title, UnitCode = r.Apartment.UnitCode,
                BuildingName = r.Apartment.Building != null ? r.Apartment.Building.Name : null,
                ReporterName = r.Reporter != null ? r.Reporter.FullName : null,
                AssignedToName = r.AssignedTo != null ? r.AssignedTo.FullName : null,
                Category = r.Category, Priority = r.Priority, Status = r.Status,
                ReportedAt = r.ReportedAt, ResolvedAt = r.ResolvedAt
            }).ToListAsync();

        ViewBag.Q = q; ViewBag.Status = status; ViewBag.Priority = priority;
        ViewBag.Category = category; ViewBag.BuildingId = buildingId;
        ViewBag.Buildings = await Db.Buildings.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, priority, category, buildingId, page = pp }) ?? "#"
        };

        var counts = await Db.MaintenanceRequests
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        ViewBag.OpenCount = counts.FirstOrDefault(c => c.Key == MaintenanceStatus.Open)?.Count ?? 0;
        ViewBag.AckCount = counts.FirstOrDefault(c => c.Key == MaintenanceStatus.Acknowledged)?.Count ?? 0;
        ViewBag.InProgressCount = counts.FirstOrDefault(c => c.Key == MaintenanceStatus.InProgress)?.Count ?? 0;
        ViewBag.ResolvedCount = counts.FirstOrDefault(c => c.Key == MaintenanceStatus.Resolved)?.Count ?? 0;
        ViewBag.UrgentCount = await Db.MaintenanceRequests
            .CountAsync(r => r.Priority == MaintenancePriority.Urgent
                          && r.Status != MaintenanceStatus.Closed
                          && r.Status != MaintenanceStatus.Resolved
                          && r.Status != MaintenanceStatus.Rejected);

        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var raw = await Db.MaintenanceRequests
            .Include(r => r.Apartment)
            .Where(r => r.RequestNumber.Contains(q) || r.Title.Contains(q) || r.Description.Contains(q) || r.Apartment.Title.Contains(q))
            .OrderByDescending(r => r.ReportedAt)
            .Take(limit)
            .Select(r => new { r.Id, r.RequestNumber, r.Title, UnitTitle = r.Apartment.Title, r.Priority, r.Status })
            .ToListAsync();
        return Json(raw.Select(r => new
        {
            title = r.RequestNumber + " · " + r.Title,
            subtitle = r.UnitTitle + " · " + r.Priority.Vi() + " · " + r.Status.Vi(),
            url = Url.Action(nameof(Details), new { id = r.Id }) ?? "#",
            icon = "build"
        }));
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("maintenance");
        var r = await Db.MaintenanceRequests
            .Include(x => x.Apartment).ThenInclude(a => a.Building)
            .Include(x => x.Lease).ThenInclude(l => l!.PrimaryTenant)
            .Include(x => x.Reporter).Include(x => x.AssignedTo)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        SetBreadcrumb(("Bảo trì", Url.Action(nameof(Index))), (r.RequestNumber, null));

        ViewBag.Staff = await _userManager.Users
            .OrderBy(u => u.FullName)
            .Select(u => new HostOption { Id = u.Id, Label = u.FullName + " · " + u.Email })
            .ToListAsync();
        return View(r);
    }

    public async Task<IActionResult> Create(int? apartmentId)
    {
        SetActiveNav("maintenance");
        SetBreadcrumb(("Bảo trì", Url.Action(nameof(Index))), ("Tạo yêu cầu", null));
        var vm = new MaintenanceEditVm
        {
            RequestNumber = await BillingNumberGenerator.NextMaintenanceNumberAsync(Db),
            ApartmentId = apartmentId ?? 0
        };
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MaintenanceEditVm input)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(input);
            return View(input);
        }
        if (input.ApartmentId == 0)
        {
            ModelState.AddModelError(nameof(input.ApartmentId), "Chọn căn hộ.");
            await PopulateLookupsAsync(input);
            return View(input);
        }

        MaintenanceRequest req;
        bool isNew = input.Id == 0;
        if (isNew)
        {
            req = new MaintenanceRequest
            {
                RequestNumber = string.IsNullOrWhiteSpace(input.RequestNumber)
                    ? await BillingNumberGenerator.NextMaintenanceNumberAsync(Db)
                    : input.RequestNumber.Trim(),
                ReportedAt = DateTime.UtcNow,
                Status = MaintenanceStatus.Open
            };
            Db.MaintenanceRequests.Add(req);
        }
        else
        {
            req = await Db.MaintenanceRequests.FindAsync(input.Id) ?? throw new InvalidOperationException();
        }

        req.ApartmentId = input.ApartmentId;
        req.LeaseId = input.LeaseId;
        if (req.LeaseId == null)
        {
            // auto-pick active lease
            var active = await Db.Leases.Where(l => l.ApartmentId == input.ApartmentId && l.Status == LeaseStatus.Active)
                .Select(l => l.Id).FirstOrDefaultAsync();
            if (active > 0) req.LeaseId = active;
        }
        req.ReporterId = input.ReporterId ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        req.Title = input.Title;
        req.Description = input.Description;
        req.Category = input.Category;
        req.Priority = input.Priority;
        req.PhotoUrls = input.PhotoUrls;
        req.EstimatedCost = input.EstimatedCost;

        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (isNew && BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            req.RequestNumber = await BillingNumberGenerator.NextMaintenanceNumberAsync(Db);
            await Db.SaveChangesAsync();
        }
        TempData["Success"] = isNew ? "Đã tạo yêu cầu." : "Đã cập nhật.";
        return RedirectToAction(nameof(Details), new { id = req.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(int id)
    {
        var r = await Db.MaintenanceRequests.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status is MaintenanceStatus.Closed or MaintenanceStatus.Rejected or MaintenanceStatus.Resolved)
        {
            TempData["Danger"] = $"Không thể tiếp nhận yêu cầu ở trạng thái {r.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        r.Status = MaintenanceStatus.Acknowledged;
        r.AcknowledgedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã tiếp nhận.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignMaintenanceVm input)
    {
        var r = await Db.MaintenanceRequests.FindAsync(input.Id);
        if (r == null) return NotFound();
        r.AssignedToId = input.AssignedToId;
        if (r.Status == MaintenanceStatus.Open) r.Status = MaintenanceStatus.Acknowledged;
        if (r.Status != MaintenanceStatus.Resolved && r.Status != MaintenanceStatus.Closed)
            r.Status = MaintenanceStatus.InProgress;
        r.AcknowledgedAt ??= DateTime.UtcNow;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã phân công.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(ResolveMaintenanceVm input)
    {
        var r = await Db.MaintenanceRequests.Include(x => x.Lease).FirstOrDefaultAsync(x => x.Id == input.Id);
        if (r == null) return NotFound();
        if (r.Status is MaintenanceStatus.Closed or MaintenanceStatus.Rejected)
        {
            TempData["Danger"] = $"Không thể đánh dấu hoàn tất khi yêu cầu đã {r.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id = input.Id });
        }

        r.ResolutionNote = input.ResolutionNote;
        r.ActualCost = input.ActualCost;
        r.ChargeToTenant = input.ChargeToTenant;
        r.Status = MaintenanceStatus.Resolved;
        r.ResolvedAt = DateTime.UtcNow;

        if (input.DeductFromDeposit && r.LeaseId.HasValue && r.Lease != null && input.ActualCost > 0)
        {
            var deduct = Math.Min(input.ActualCost, r.Lease.DepositHeld);
            if (deduct > 0)
            {
                Db.DepositTransactions.Add(new DepositTransaction
                {
                    LeaseId = r.LeaseId.Value, Type = DepositTransactionType.Deduction,
                    Amount = deduct,
                    Reason = $"Trừ chi phí sửa chữa {r.RequestNumber}: {r.Title}",
                    RelatedRequestId = r.Id,
                    RecordedAt = DateTime.UtcNow,
                    RecordedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                });
                r.Lease.DepositHeld -= deduct;
            }
        }

        await Db.SaveChangesAsync();

        // ChargeToTenant && !DeductFromDeposit → auto-create a OneOff invoice so the
        // tenant has a payable record. Otherwise the cost falls through.
        if (input.ChargeToTenant && !input.DeductFromDeposit && r.LeaseId.HasValue && input.ActualCost > 0)
        {
            var lines = new List<(string description, decimal qty, decimal unitPrice)>
            {
                ($"Chi phí sửa chữa {r.RequestNumber} — {r.Title}", 1m, input.ActualCost)
            };
            var result = await _invoiceGen.GenerateOneOffInvoiceAsync(
                r.LeaseId.Value,
                $"Bảo trì {r.RequestNumber}",
                lines,
                t.Infrastructure.Time.VnTime.Today.AddDays(7),
                note: input.ResolutionNote);
            if (result.Success)
                TempData["Success"] = $"Đã hoàn tất. Sinh hoá đơn truy đòi {result.InvoiceId}.";
            else
                TempData["Success"] = "Đã hoàn tất (không sinh được hoá đơn: " + result.Message + ").";
        }
        else
        {
            TempData["Success"] = "Đã hoàn tất.";
        }
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(int id)
    {
        var r = await Db.MaintenanceRequests.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status is MaintenanceStatus.Closed or MaintenanceStatus.Rejected)
        {
            TempData["Danger"] = $"Yêu cầu đã ở trạng thái {r.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        r.Status = MaintenanceStatus.Closed;
        r.ClosedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã đóng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? reason)
    {
        var r = await Db.MaintenanceRequests.FindAsync(id);
        if (r == null) return NotFound();
        if (r.Status is MaintenanceStatus.Closed or MaintenanceStatus.Resolved)
        {
            TempData["Danger"] = $"Yêu cầu đã {r.Status.Vi()} — không thể từ chối.";
            return RedirectToAction(nameof(Details), new { id });
        }
        r.Status = MaintenanceStatus.Rejected;
        r.ResolutionNote = string.IsNullOrWhiteSpace(reason) ? r.ResolutionNote : "Từ chối: " + reason;
        r.ClosedAt = DateTime.UtcNow;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã từ chối.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, MaintenanceStatus status, string? returnUrl)
    {
        var r = await Db.MaintenanceRequests.FindAsync(id);
        if (r == null) return NotFound();

        r.Status = status;
        var now = DateTime.UtcNow;
        switch (status)
        {
            case MaintenanceStatus.Acknowledged:
                r.AcknowledgedAt ??= now;
                break;
            case MaintenanceStatus.InProgress:
                r.AcknowledgedAt ??= now;
                break;
            case MaintenanceStatus.Resolved:
                r.AcknowledgedAt ??= now;
                r.ResolvedAt ??= now;
                break;
            case MaintenanceStatus.Closed:
                r.AcknowledgedAt ??= now;
                r.ResolvedAt ??= now;
                r.ClosedAt ??= now;
                break;
            case MaintenanceStatus.Rejected:
                r.ClosedAt ??= now;
                break;
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật trạng thái: {status.Vi()}.";
        return Redirect(string.IsNullOrEmpty(returnUrl) ? Url.Action(nameof(Index))! : returnUrl);
    }

    private async Task PopulateLookupsAsync(MaintenanceEditVm vm)
    {
        vm.Apartments = await Db.Apartments.Include(a => a.Building).OrderBy(a => a.Title).ToListAsync();
        vm.Reporters = await _userManager.Users.OrderBy(u => u.FullName)
            .Select(u => new HostOption { Id = u.Id, Label = u.FullName + " · " + u.Email })
            .ToListAsync();
    }
}
