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
    private readonly MaintenanceWorkflow _workflow;

    public MaintenanceController(AppDbContext db, MaintenanceTriageService triage, MaintenanceDispatcher dispatcher,
        MaintenanceWorkflow workflow) : base(db)
    {
        _triage = triage;
        _dispatcher = dispatcher;
        _workflow = workflow;
    }

    public async Task<IActionResult> Index(MaintenanceStatus? status, int page = 1)
    {
        SetActiveNav("maintenance");
        SetBreadcrumb(("Báo sự cố", null));

        const int pageSize = 20;
        var query = MyRequests();
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
        var r = await MyRequests()
            .Include(x => x.Apartment).ThenInclude(a => a!.Floor.Building)
            .Include(x => x.CommonBuilding)
            .Include(x => x.AssignedTo)
            .Include(x => x.Photos)
            .Include(x => x.History)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        SetBreadcrumb(("Báo sự cố", Url.Action(nameof(Index))), (r.RequestNumber, null));
        ViewBag.NextStatuses = MaintenanceWorkflow.NextStatuses(r, WorkflowActor.Resident, CurrentUserId);
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
        // BR-SC-01: chỉ cư dân đang ở mới báo được, cho căn mình hoặc khu vực chung của toà mình.
        var apartment = await GetMyApartmentAsync();
        if (apartment == null)
        {
            TempData["Danger"] = "Tài khoản chưa được Ban quản lý gắn với căn hộ nào.";
            return RedirectToAction(nameof(Index));
        }
        if (input.CommonArea && string.IsNullOrWhiteSpace(input.Location))
            ModelState.AddModelError(nameof(input.Location), "Ghi rõ vị trí khu vực chung (ví dụ: hành lang tầng 12).");
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
            ApartmentId = input.CommonArea ? null : apartment.Id,
            CommonBuildingId = input.CommonArea ? apartment.Floor.BuildingId : null,
            Location = input.CommonArea ? input.Location!.Trim() : null,
            ReporterId = CurrentUserId,
            Title = input.Title,
            Description = input.Description,
            Category = category,
            Priority = priority,
            AiTriageJson = triage?.RawJson,
            Status = MaintenanceStatus.Open,
            ReportedAt = DateTime.UtcNow
        };
        req.Photos.AddRange(MaintenanceWorkflow.PhotosFrom(input.PhotoUrls, MaintenancePhotoStage.Report, CurrentUserId));
        await _workflow.InitializeNewAsync(req, CurrentUserId);
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

    /// <summary>Huỷ (trước khi kỹ thuật bắt đầu), xác nhận đã xong (kèm đánh giá) hoặc báo chưa khắc phục.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, MaintenanceStatus status, string? note, int? rating)
    {
        var r = await MyRequests().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        if (status == MaintenanceStatus.Closed && rating is >= 1 and <= 5)
        {
            r.Rating = rating;
            r.RatingComment = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        }
        var error = await _workflow.TransitionAsync(r, status, WorkflowActor.Resident, CurrentUserId, note);
        if (error == null) TempData["Success"] = status switch
        {
            MaintenanceStatus.Cancelled => "Đã huỷ yêu cầu.",
            MaintenanceStatus.Closed => "Cảm ơn bạn đã xác nhận.",
            _ => "Đã báo Ban quản lý: sự cố chưa được khắc phục."
        };
        else TempData["Danger"] = error;
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Yêu cầu cư dân được xem: tự gửi, hoặc của căn mình đang ở (các thành viên cùng hộ thấy của nhau).</summary>
    private IQueryable<MaintenanceRequest> MyRequests()
    {
        var uid = CurrentUserId;
        var aptIds = MyApartmentIds();
        return Db.MaintenanceRequests.Where(m =>
            m.ReporterId == uid || (m.ApartmentId != null && aptIds.Contains(m.ApartmentId.Value)));
    }
}
