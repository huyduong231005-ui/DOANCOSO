using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class ReviewsController : AdminBaseController
{
    public ReviewsController(AppDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, ReviewStatus? status, int? rating, int page = 1)
    {
        SetActiveNav("reviews");
        SetBreadcrumb(("Đánh giá", null));

        const int pageSize = 25;
        var query = Db.Reviews.Include(r => r.Apartment).Include(r => r.User).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.Content.Contains(q) || r.Apartment.Title.Contains(q) || r.User.FullName.Contains(q));
        if (status.HasValue) query = query.Where(r => r.Status == status.Value);
        if (rating.HasValue) query = query.Where(r => r.Rating == rating.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new ReviewListVm
            {
                Id = r.Id, ApartmentId = r.ApartmentId, ApartmentTitle = r.Apartment.Title,
                UserName = r.User.FullName, Rating = r.Rating,
                Content = r.Content, Status = r.Status, CreatedAt = r.CreatedAt
            }).ToListAsync();

        ViewBag.Q = q; ViewBag.Status = status; ViewBag.Rating = rating;
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, rating, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Suggest(string? q, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(q)) return Json(Array.Empty<object>());
        q = q.Trim();
        var rows = await Db.Reviews
            .Include(r => r.Apartment)
            .Include(r => r.User)
            .Where(r => r.Content.Contains(q) || r.Apartment.Title.Contains(q) || r.User.FullName.Contains(q))
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new
            {
                title = r.User.FullName + " · " + r.Rating + "★",
                subtitle = r.Apartment.Title + " — " + (r.Content.Length > 60 ? r.Content.Substring(0, 60) + "..." : r.Content),
                url = Url.Action("Details", "Apartments", new { id = r.ApartmentId }) ?? "#",
                icon = "reviews"
            })
            .ToListAsync();
        return Json(rows);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var r = await Db.Reviews.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = ReviewStatus.Approved;
        r.ApprovedAt = DateTime.UtcNow;
        r.ApprovedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã duyệt đánh giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        var r = await Db.Reviews.FindAsync(id);
        if (r == null) return NotFound();
        r.Status = ReviewStatus.Rejected;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã từ chối đánh giá.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var r = await Db.Reviews.FindAsync(id);
        if (r == null) return NotFound();
        Db.Reviews.Remove(r);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã xoá đánh giá.";
        return RedirectToAction(nameof(Index));
    }
}
