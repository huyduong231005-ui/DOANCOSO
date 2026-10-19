using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Localization;
using t.Infrastructure.Maintenance;
using t.Models.Entities;

namespace t.Areas.Staff.Controllers;

/// <summary>
/// Cổng kỹ thuật viên: chỉ thấy và thao tác trên việc được giao cho chính mình (BR-SC-04).
/// Mọi đổi trạng thái đi qua MaintenanceWorkflow với vai trò Technician.
/// </summary>
[Area("Staff")]
[Authorize(Roles = StaffRoles)]
public class JobsController : Controller
{
    public const string StaffRoles = "Electrician,Plumber,AcTechnician,Carpenter,Glazier";

    private readonly AppDbContext _db;
    private readonly MaintenanceWorkflow _workflow;

    public JobsController(AppDbContext db, MaintenanceWorkflow workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    private string Me => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private IQueryable<MaintenanceRequest> MyJobs() => _db.MaintenanceRequests.Where(r => r.AssignedToId == Me);

    public async Task<IActionResult> Index(bool done = false)
    {
        ViewData["ActiveNav"] = done ? "done" : "jobs";
        var query = MyJobs().AsNoTracking();
        query = done
            ? query.Where(r => !MaintenanceRequest.ActiveStatuses.Contains(r.Status))
            : query.Where(r => MaintenanceRequest.ActiveStatuses.Contains(r.Status));
        var jobs = await query
            .Include(r => r.Apartment).ThenInclude(a => a!.Building)
            .Include(r => r.CommonBuilding)
            .OrderByDescending(r => r.Priority).ThenBy(r => r.DueAt)
            .Take(done ? 50 : 200)
            .ToListAsync();
        ViewBag.Done = done;
        return View(jobs);
    }

    public async Task<IActionResult> Details(int id)
    {
        ViewData["ActiveNav"] = "jobs";
        var r = await MyJobs()
            .Include(x => x.Apartment).ThenInclude(a => a!.Building)
            .Include(x => x.Apartment).ThenInclude(a => a!.Residents)
            .Include(x => x.CommonBuilding)
            .Include(x => x.Reporter)
            .Include(x => x.Photos)
            .Include(x => x.History)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        ViewBag.NextStatuses = MaintenanceWorkflow.NextStatuses(r, WorkflowActor.Technician, Me);
        return View(r);
    }

    /// <summary>Bắt đầu, tạm dừng (kèm lý do), tiếp tục.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(int id, MaintenanceStatus status, string? note)
    {
        var r = await MyJobs().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        var error = await _workflow.TransitionAsync(r, status, WorkflowActor.Technician, Me, note);
        if (error == null) TempData["Success"] = $"Đã chuyển sang: {status.Vi()}.";
        else TempData["Danger"] = error;
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Ghi nguyên nhân, cách xử lý và ảnh nghiệm thu rồi chuyển sang Hoàn tất (BR-SC-06).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Resolve(int id, string? resolutionNote, string? photoUrls)
    {
        var r = await MyJobs().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();
        r.Photos.AddRange(MaintenanceWorkflow.PhotosFrom(photoUrls, MaintenancePhotoStage.Result, Me));
        var error = await _workflow.TransitionAsync(r, MaintenanceStatus.Resolved, WorkflowActor.Technician, Me,
            resolutionNote: resolutionNote);
        if (error == null) TempData["Success"] = "Đã báo hoàn tất. Chờ cư dân / quản lý xác nhận.";
        else TempData["Danger"] = error;
        return RedirectToAction(nameof(Details), new { id });
    }
}
