using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class ViewingsController : AdminBaseController
{
    public ViewingsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(ViewingStatus? status, string? q, int page = 1)
    {
        SetActiveNav("viewings");
        SetBreadcrumb(("Lịch xem phòng", null));

        const int pageSize = 25;

        var query = Db.ViewingAppointments
            .AsNoTracking()
            .Include(v => v.Apartment)
            .Include(v => v.User)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(v => v.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var key = q.Trim();
            query = query.Where(v =>
                v.ContactName.Contains(key) ||
                v.ContactPhone.Contains(key) ||
                (v.ContactEmail != null && v.ContactEmail.Contains(key)) ||
                v.Apartment.Title.Contains(key));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(v => v.Status == ViewingStatus.Pending ? 0 : 1)
            .ThenBy(v => v.ScheduledDate)
            .ThenBy(v => v.SlotHour)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Status = status;
        ViewBag.Query = q;
        ViewBag.Pager = new t.Areas.Admin.Models.PageInfo
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        var counts = await Db.ViewingAppointments
            .AsNoTracking()
            .GroupBy(v => v.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync();
        ViewBag.PendingCount = counts.FirstOrDefault(c => c.Key == ViewingStatus.Pending)?.Count ?? 0;
        ViewBag.ConfirmedCount = counts.FirstOrDefault(c => c.Key == ViewingStatus.Confirmed)?.Count ?? 0;
        ViewBag.CompletedCount = counts.FirstOrDefault(c => c.Key == ViewingStatus.Completed)?.Count ?? 0;
        ViewBag.CancelledCount = counts.FirstOrDefault(c => c.Key == ViewingStatus.Cancelled)?.Count ?? 0;

        return View(items);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var rows = await Db.ViewingAppointments
            .AsNoTracking()
            .Include(v => v.Apartment)
            .Where(v => v.ContactName.Contains(q) ||
                        v.ContactPhone.Contains(q) ||
                        (v.ContactEmail != null && v.ContactEmail.Contains(q)) ||
                        v.Apartment.Title.Contains(q))
            .OrderByDescending(v => v.ScheduledDate)
            .Take(limit)
            .Select(v => new
            {
                title = v.ContactName + " · " + v.ContactPhone,
                subtitle = v.Apartment.Title + " — " + v.ScheduledDate.ToString("dd/MM/yyyy") + " " + v.SlotHour + "h",
                url = Url.Action("Details", "Apartments", new { id = v.ApartmentId }) ?? "#",
                icon = "event"
            })
            .ToListAsync();
        return Json(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        var item = await Db.ViewingAppointments.FirstOrDefaultAsync(v => v.Id == id);
        if (item is null) return NotFound();

        item.Status = ViewingStatus.Confirmed;
        item.ConfirmedAt = DateTime.UtcNow;
        item.ConfirmedBy = User?.Identity?.Name;
        await Db.SaveChangesAsync();

        TempData["Success"] = "Đã xác nhận lịch xem phòng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var item = await Db.ViewingAppointments.FirstOrDefaultAsync(v => v.Id == id);
        if (item is null) return NotFound();

        item.Status = ViewingStatus.Cancelled;
        item.CancelledAt = DateTime.UtcNow;
        item.CancelledBy = User?.Identity?.Name;
        item.CancellationReason = reason?.Trim();
        await Db.SaveChangesAsync();

        TempData["Success"] = "Đã huỷ lịch xem phòng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        var item = await Db.ViewingAppointments.FirstOrDefaultAsync(v => v.Id == id);
        if (item is null) return NotFound();

        item.Status = ViewingStatus.Completed;
        await Db.SaveChangesAsync();

        TempData["Success"] = "Đã đánh dấu lịch hẹn là đã xem.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await Db.ViewingAppointments.FirstOrDefaultAsync(v => v.Id == id);
        if (item is null) return NotFound();

        if (item.Status is ViewingStatus.Pending or ViewingStatus.Confirmed)
        {
            TempData["Danger"] = "Chỉ có thể xoá lịch đã huỷ / đã xem / không đến.";
            return RedirectToAction(nameof(Index));
        }

        Db.ViewingAppointments.Remove(item);
        await Db.SaveChangesAsync();

        TempData["Success"] = "Đã xoá lịch hẹn.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurgeCancelled()
    {
        var items = await Db.ViewingAppointments
            .Where(v => v.Status == ViewingStatus.Cancelled)
            .ToListAsync();

        if (items.Count == 0)
        {
            TempData["Danger"] = "Không có lịch huỷ nào để xoá.";
            return RedirectToAction(nameof(Index));
        }

        Db.ViewingAppointments.RemoveRange(items);
        await Db.SaveChangesAsync();

        TempData["Success"] = $"Đã xoá {items.Count} lịch hẹn đã huỷ.";
        return RedirectToAction(nameof(Index));
    }
}
