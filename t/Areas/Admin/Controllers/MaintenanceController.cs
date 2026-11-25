using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Localization;
using t.Infrastructure.Maintenance;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

[t.Infrastructure.Security.Permission("maintenance.manage")]
public class MaintenanceController : AdminBaseController
{
    private static readonly string[] TechnicianRoles =
    {
        SeedData.RoleElectrician, SeedData.RolePlumber, SeedData.RoleAcTechnician, SeedData.RoleCarpenter, SeedData.RoleGlazier
    };

    private readonly UserManager<AppUser> _userManager;
    private readonly InvoiceGenerator _invoiceGen;
    private readonly MaintenanceWorkflow _workflow;

    public MaintenanceController(AppDbContext db, UserManager<AppUser> userManager, InvoiceGenerator invoiceGen,
        MaintenanceWorkflow workflow) : base(db)
    {
        _userManager = userManager;
        _invoiceGen = invoiceGen;
        _workflow = workflow;
    }

    private string? Me => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Index(string? q, MaintenanceStatus? status, MaintenancePriority? priority,
        MaintenanceCategory? category, int? buildingId, bool overdue = false, int page = 1)
    {
        SetActiveNav("maintenance");
        SetBreadcrumb(("Bảo trì / Sự cố", null));

        const int pageSize = 25;
        var now = DateTime.UtcNow;
        var query = Db.MaintenanceRequests.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.RequestNumber.Contains(q) || r.Title.Contains(q) || r.Description.Contains(q)
                || (r.Apartment != null && r.Apartment.UnitCode.Contains(q)) || (r.Location != null && r.Location.Contains(q)));
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (priority.HasValue) query = query.Where(r => r.Priority == priority.Value);
        if (category.HasValue) query = query.Where(r => r.Category == category.Value);
        if (buildingId.HasValue)
            query = query.Where(r => (r.Apartment != null && r.Apartment.Floor.BuildingId == buildingId.Value) || r.CommonBuildingId == buildingId.Value);
        if (overdue) query = query.Where(MaintenanceWorkflow.IsOverdue(now));

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.Priority).ThenByDescending(r => r.ReportedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new MaintenanceListVm
            {
                Id = r.Id, RequestNumber = r.RequestNumber, Title = r.Title,
                ApartmentId = r.ApartmentId,
                UnitCode = r.Apartment != null ? r.Apartment.UnitCode : null,
                Location = r.Location,
                BuildingName = r.Apartment != null ? "Toà " + r.Apartment.Floor.Building.Code : r.CommonBuilding != null ? "Toà " + r.CommonBuilding.Code : null,
                ReporterName = r.Reporter != null ? r.Reporter.FullName : null,
                AssignedToName = r.AssignedTo != null ? r.AssignedTo.FullName : null,
                Category = r.Category, Priority = r.Priority, Status = r.Status,
                ReportedAt = r.ReportedAt, ResolvedAt = (r.Status == MaintenanceStatus.Resolved || r.Status == MaintenanceStatus.Closed ? r.History.Where(h => h.ToStatus == MaintenanceStatus.Resolved).Max(h => (DateTime?)h.ChangedAt) : null), DueAt = r.DueAt
            }).ToListAsync();

        ViewBag.Q = q; ViewBag.Status = status; ViewBag.Priority = priority;
        ViewBag.Category = category; ViewBag.BuildingId = buildingId; ViewBag.Overdue = overdue;
        ViewBag.Buildings = await Db.Buildings.AsNoTracking().OrderBy(b => b.Code).ToListAsync();
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, priority, category, buildingId, overdue, page = pp }) ?? "#"
        };

        var counts = await Db.MaintenanceRequests
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
        ViewBag.StatusCounts = counts;
        ViewBag.OverdueCount = await Db.MaintenanceRequests.CountAsync(MaintenanceWorkflow.IsOverdue(now));
        ViewBag.UrgentCount = await Db.MaintenanceRequests
            .CountAsync(r => r.Priority == MaintenancePriority.Urgent && MaintenanceRequest.ActiveStatuses.Contains(r.Status));

        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var raw = await Db.MaintenanceRequests
            .Where(r => r.RequestNumber.Contains(q) || r.Title.Contains(q) || r.Description.Contains(q)
                || (r.Apartment != null && r.Apartment.UnitCode.Contains(q)))
            .OrderByDescending(r => r.ReportedAt)
            .Take(limit)
            .Select(r => new { r.Id, r.RequestNumber, r.Title, Place = r.Apartment != null ? r.Apartment.UnitCode : r.Location, r.Priority, r.Status })
            .ToListAsync();
        return Json(raw.Select(r => new
        {
            title = r.RequestNumber + " · " + r.Title,
            subtitle = r.Place + " · " + r.Priority.Vi() + " · " + r.Status.Vi(),
            url = Url.Action(nameof(Details), new { id = r.Id }) ?? "#",
            icon = "build"
        }));
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("maintenance");
        var r = await Db.MaintenanceRequests
            .Include(x => x.Apartment).ThenInclude(a => a!.Floor.Building)
            .Include(x => x.Apartment).ThenInclude(a => a!.Residents)
            .Include(x => x.CommonBuilding)
            .Include(x => x.Reporter).Include(x => x.AssignedTo)
            .Include(x => x.Photos)
            .Include(x => x.History).ThenInclude(h => h.ChangedBy)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        SetBreadcrumb(("Bảo trì", Url.Action(nameof(Index))), (r.RequestNumber, null));

        ViewBag.Staff = await StaffOptionsAsync();
        ViewBag.NextStatuses = MaintenanceWorkflow.NextStatuses(r, WorkflowActor.Manager, Me);
        return View(r);
    }

    public async Task<IActionResult> Create(int? apartmentId)
    {
        SetActiveNav("maintenance");
        SetBreadcrumb(("Bảo trì", Url.Action(nameof(Index))), ("Tạo yêu cầu", null));
        var vm = new MaintenanceEditVm
        {
            RequestNumber = await BillingNumberGenerator.NextMaintenanceNumberAsync(Db),
            ApartmentId = apartmentId
        };
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(MaintenanceEditVm input)
    {
        // Đúng một nơi xảy ra: căn hộ, hoặc toà + vị trí khu vực chung (khớp CK_YeuCauBaoTri_NoiXayRa).
        var isCommon = input.ApartmentId is null or 0;
        if (isCommon && (input.CommonBuildingId is null or 0 || string.IsNullOrWhiteSpace(input.Location)))
            ModelState.AddModelError(nameof(input.ApartmentId), "Chọn căn hộ, hoặc chọn toà và ghi vị trí khu vực chung.");
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(input);
            return View(input);
        }

        var req = new MaintenanceRequest
        {
            RequestNumber = string.IsNullOrWhiteSpace(input.RequestNumber)
                ? await BillingNumberGenerator.NextMaintenanceNumberAsync(Db)
                : input.RequestNumber.Trim(),
            ReportedAt = DateTime.UtcNow,
            Status = MaintenanceStatus.Open,
            ApartmentId = isCommon ? null : input.ApartmentId,
            CommonBuildingId = isCommon ? input.CommonBuildingId : null,
            Location = isCommon ? input.Location!.Trim() : null,
            ReporterId = input.ReporterId ?? Me,
            Title = input.Title,
            Description = input.Description,
            Category = input.Category,
            Priority = input.Priority,
        };
        req.Photos.AddRange(MaintenanceWorkflow.PhotosFrom(input.PhotoUrls, MaintenancePhotoStage.Report, Me));
        await _workflow.InitializeNewAsync(req, Me);
        Db.MaintenanceRequests.Add(req);

        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            req.RequestNumber = await BillingNumberGenerator.NextMaintenanceNumberAsync(Db);
            await Db.SaveChangesAsync();
        }
        TempData["Success"] = "Đã tạo yêu cầu.";
        return RedirectToAction(nameof(Details), new { id = req.Id });
    }

    /// <summary>Quản lý đổi mức ưu tiên / loại: hạn SLA tính lại từ lúc báo.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reclassify(int id, MaintenanceCategory category, MaintenancePriority priority)
    {
        var r = await Db.MaintenanceRequests.FindAsync(id);
        if (r == null) return NotFound();
        if (!MaintenanceRequest.ActiveStatuses.Contains(r.Status))
        {
            TempData["Danger"] = $"Yêu cầu đã {r.Status.Vi()}, không phân loại lại được.";
            return RedirectToAction(nameof(Details), new { id });
        }
        var changed = r.Priority != priority;
        r.Category = category;
        r.Priority = priority;
        if (changed) await _workflow.RecalculateDueAsync(r);
        await Db.SaveChangesAsync();
        TempData["Success"] = changed ? "Đã phân loại lại, hạn xử lý được tính lại theo mức ưu tiên mới." : "Đã cập nhật loại sự cố.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignMaintenanceVm input)
    {
        var r = await Db.MaintenanceRequests.FindAsync(input.Id);
        if (r == null) return NotFound();
        var assignee = await Db.Users.FindAsync(input.AssignedToId);
        if (assignee == null || !(await StaffOptionsAsync()).Any(s => s.Id == assignee.Id))
        {
            TempData["Danger"] = "Chỉ giao được cho kỹ thuật viên hoặc quản lý.";
            return RedirectToAction(nameof(Details), new { id = input.Id });
        }
        var error = await _workflow.AssignAsync(r, assignee, WorkflowActor.Manager, Me, input.Note);
        Flash(error, $"Đã giao cho {assignee.FullName}.");
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(ResolveMaintenanceVm input)
    {
        var r = await Db.MaintenanceRequests.FirstOrDefaultAsync(x => x.Id == input.Id);
        if (r == null) return NotFound();

        r.ActualCost = input.ActualCost;
        r.ChargeToTenant = input.ChargeToTenant && r.ApartmentId != null;
        r.Photos.AddRange(MaintenanceWorkflow.PhotosFrom(input.PhotoUrls, MaintenancePhotoStage.Result, Me));
        var error = await _workflow.TransitionAsync(r, MaintenanceStatus.Resolved, WorkflowActor.Manager, Me,
            resolutionNote: input.ResolutionNote);
        if (error != null)
        {
            TempData["Danger"] = error;
            return RedirectToAction(nameof(Details), new { id = input.Id });
        }

        // Lỗi trong căn do cư dân → tạo hoá đơn phát sinh cho căn (sự cố khu vực chung không tính cho ai).
        if (r.ChargeToTenant && input.ActualCost > 0)
        {
            var repairId = await Db.UtilityTypes.Where(x => x.Code == UtilityType.RepairCode).Select(x => x.Id).FirstOrDefaultAsync();
            var result = repairId == 0
                ? new t.Infrastructure.Billing.InvoiceResult(false, "chưa có loại dịch vụ sửa chữa trong bảng giá")
                : await _invoiceGen.GenerateOneOffInvoiceAsync(
                    r.ApartmentId!.Value, $"Bảo trì {r.RequestNumber}", new[] { (repairId, 1m, input.ActualCost) },
                    t.Infrastructure.Time.VnTime.Today.AddDays(7),
                    note: $"Sửa chữa {r.RequestNumber} — {r.Title}" + (string.IsNullOrWhiteSpace(input.ResolutionNote) ? "" : $". {input.ResolutionNote}"));
            TempData["Success"] = result.Success
                ? $"Đã hoàn tất. Sinh hoá đơn truy đòi {result.InvoiceId}."
                : "Đã hoàn tất (không sinh được hoá đơn: " + result.Message + ").";
        }
        else
        {
            TempData["Success"] = "Đã hoàn tất.";
        }
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    /// <summary>Mọi chuyển trạng thái còn lại (tiếp nhận, bắt đầu, tạm dừng, đóng, từ chối, huỷ) đi qua workflow.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, MaintenanceStatus status, string? note, string? returnUrl)
    {
        var r = await Db.MaintenanceRequests.FindAsync(id);
        if (r == null) return NotFound();
        var error = await _workflow.TransitionAsync(r, status, WorkflowActor.Manager, Me, note);
        Flash(error, $"Đã chuyển sang: {status.Vi()}.");
        return RedirectToLocalOrDefault(returnUrl, Url.Action(nameof(Details), new { id })!);
    }

    private void Flash(string? error, string success)
    {
        if (error == null) TempData["Success"] = success;
        else TempData["Danger"] = error;
    }

    /// <summary>Người nhận việc: kỹ thuật viên các chuyên môn và quản lý.</summary>
    private Task<List<HostOption>> StaffOptionsAsync()
    {
        var roles = TechnicianRoles.Append(SeedData.RoleManager).ToArray();
        return (from u in Db.Users
                join ur in Db.UserRoles on u.Id equals ur.UserId
                join ro in Db.Roles on ur.RoleId equals ro.Id
                where roles.Contains(ro.Name!)
                orderby u.FullName
                select new HostOption { Id = u.Id, Label = u.FullName + " · " + ro.Name })
            .ToListAsync();
    }

    private async Task PopulateLookupsAsync(MaintenanceEditVm vm)
    {
        vm.Apartments = await Db.Apartments
            .OrderBy(a => a.Floor.Building.Code).ThenBy(a => a.UnitCode)
            .Select(a => new ApartmentOption { Id = a.Id, Label = a.UnitCode + " · " + "Toà " + a.Floor.Building.Code })
            .ToListAsync();
        vm.Buildings = await Db.Buildings.OrderBy(b => b.Code)
            .Select(b => new ApartmentOption { Id = b.Id, Label = "Toà " + b.Code })
            .ToListAsync();
        vm.Reporters = await _userManager.Users.OrderBy(u => u.FullName)
            .Select(u => new HostOption { Id = u.Id, Label = u.FullName + " · " + u.Email })
            .ToListAsync();
    }
}
