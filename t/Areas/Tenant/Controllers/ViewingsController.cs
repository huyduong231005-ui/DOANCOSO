using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Tenant.Controllers;

public class ViewingsController : TenantBaseController
{
    public ViewingsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(ViewingStatus? status)
    {
        SetActiveNav("viewings");
        SetBreadcrumb(("Lịch xem phòng", null));

        var uid = CurrentUserId;
        var query = Db.ViewingAppointments
            .AsNoTracking()
            .Include(v => v.Apartment).ThenInclude(a => a.Images)
            .Where(v => v.UserId == uid);

        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        var items = await query
            .OrderByDescending(v => v.ScheduledDate)
            .ThenBy(v => v.SlotHour)
            .ToListAsync();

        ViewBag.Status = status;
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var uid = CurrentUserId;
        var item = await Db.ViewingAppointments
            .FirstOrDefaultAsync(v => v.Id == id && v.UserId == uid);
        if (item is null) return NotFound();

        if (item.Status == ViewingStatus.Completed)
        {
            TempData["Danger"] = "Lịch đã xem, không thể huỷ.";
            return RedirectToAction(nameof(Index));
        }

        item.Status = ViewingStatus.Cancelled;
        item.CancelledAt = DateTime.UtcNow;
        item.CancelledBy = User?.Identity?.Name;
        item.CancellationReason = reason?.Trim();
        await Db.SaveChangesAsync();

        TempData["Success"] = "Đã huỷ lịch xem phòng.";
        return RedirectToAction(nameof(Index));
    }
}
