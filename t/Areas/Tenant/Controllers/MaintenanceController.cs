using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Tenant.Models;
using t.Data;
using t.Infrastructure.Ai;
using t.Infrastructure.Billing;
using t.Infrastructure.Maintenance;
using t.Models.Entities;

namespace t.Areas.Tenant.Controllers;

public class MaintenanceController : TenantBaseController
{
    private readonly MaintenanceTriageService _triage;

    private readonly MaintenanceDispatcher _dispatcher;

    public MaintenanceController(AppDbContext db, MaintenanceTriageService triage, MaintenanceDispatcher dispatcher) : base(db)
    {
        _triage = triage;
        _dispatcher = dispatcher;
    }

    public async Task<IActionResult> Index(MaintenanceStatus? status, int page = 1)
    {
        SetActiveNav("maintenance");
        SetBreadcrumb(("Báo sự cố", null));

        const int pageSize = 20;
        var uid = CurrentUserId;
        var aptIds = await MyApartmentIds().ToListAsync();

        // Cư dân thấy: yêu cầu tự gửi HOẶC mọi yêu cầu của căn mình đang ở
        // (các thành viên cùng hộ thấy báo cáo của nhau).
        var query = Db.MaintenanceRequests.Where(m =>
            m.ReporterId == uid || aptIds.Contains(m.ApartmentId));
        if (status.HasValue) query = query.Where(m => m.Status == status.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(m => m.ReportedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new TenantMaintenanceRow
            {
                Id = m.Id, RequestNumber = m.RequestNumber, Title = m.Title,
                Category = m.Category, Priority = m.Priority, Status = m.Status,
                ReportedAt = m.ReportedAt
            }).ToListAsync();

        ViewBag.Status = status;
        ViewBag.Pager = new t.Areas.Admin.Models.PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { status, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("maintenance");
        var uid = CurrentUserId;
        var aptIds = await MyApartmentIds().ToListAsync();
        var r = await Db.MaintenanceRequests
            .Include(x => x.Apartment).ThenInclude(a => a.Building)
            .Include(x => x.AssignedTo)
            .FirstOrDefaultAsync(x => x.Id == id &&
                (x.ReporterId == uid || aptIds.Contains(x.ApartmentId)));
        if (r == null) return NotFound();
        SetBreadcrumb(("Báo sự cố", Url.Action(nameof(Index))), (r.RequestNumber, null));
        return View(r);
    }

    public async Task<IActionResult> Create()
    {
        SetActiveNav("maintenance");
        SetBreadcrumb(("Báo sự cố", Url.Action(nameof(Index))), ("Tạo yêu cầu", null));

        var apartment = await GetMyApartmentAsync();
        if (apartment == null)
        {
            TempData["Danger"] = "Tài khoản chưa được Ban quản lý gắn với căn hộ nào nên chưa báo sự cố được.";
            return RedirectToAction(nameof(Index));
        }
        ViewBag.Apartment = apartment;
        return View(new CreateMaintenanceVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMaintenanceVm input)
    {
        var apartment = await GetMyApartmentAsync();
        if (apartment == null)
        {
            TempData["Danger"] = "Tài khoản chưa được Ban quản lý gắn với căn hộ nào.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Apartment = apartment;
            return View(input);
        }

        var triage = await _triage.TriageAsync(input.Description, HttpContext.RequestAborted);
        var (category, priority) = MaintenanceTriageService.Apply(input.Category, input.Priority, triage);

        var req = new MaintenanceRequest
        {
            RequestNumber = await BillingNumberGenerator.NextMaintenanceNumberAsync(Db),
            ApartmentId = apartment.Id,
            ReporterId = CurrentUserId,
            Title = input.Title,
            Description = input.Description,
            Category = category,
            Priority = priority,
            AiTriageJson = triage?.RawJson,
            PhotoUrls = input.PhotoUrls,
            Status = MaintenanceStatus.Open,
            ReportedAt = DateTime.UtcNow
        };
        Db.MaintenanceRequests.Add(req);
        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException ex) when (BillingNumberGenerator.IsUniqueConstraintError(ex))
        {
            req.RequestNumber = await BillingNumberGenerator.NextMaintenanceNumberAsync(Db);
            await Db.SaveChangesAsync();
        }

        // Không truyền RequestAborted: yêu cầu đã lưu thì vẫn phải báo cho thợ dù cư dân đóng trang.
        await _dispatcher.DispatchAsync(req, triage, aiUnavailable: triage == null && _triage.IsEnabled);

        TempData["Success"] = "Đã gửi yêu cầu. Ban quản lý sẽ phản hồi sớm.";
        if (triage?.FollowUpQuestion != null)
            TempData["Info"] = "Ban quản lý cần thêm thông tin: " + triage.FollowUpQuestion;
        return RedirectToAction(nameof(Details), new { id = req.Id });
    }
}
