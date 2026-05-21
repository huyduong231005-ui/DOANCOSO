using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;

namespace t.Controllers;

[Authorize]
public class ReviewsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public ReviewsController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateReviewVm input)
    {
        var uid = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(uid)) return Challenge();

        var redirectBack = Url.Action("ApartmentDetail", "Home", new { id = input.ApartmentId }) ?? "/";

        if (input.Rating < 1 || input.Rating > 5)
        {
            TempData["Danger"] = "Điểm đánh giá phải từ 1 đến 5 sao.";
            return Redirect(redirectBack);
        }
        if (string.IsNullOrWhiteSpace(input.Content) || input.Content.Trim().Length < 10)
        {
            TempData["Danger"] = "Nội dung đánh giá phải từ 10 ký tự.";
            return Redirect(redirectBack);
        }

        // Eligibility: user must have rented this apartment (any non-Pending lease).
        var hasLease = await _db.Leases.AnyAsync(l =>
            l.ApartmentId == input.ApartmentId &&
            (l.PrimaryTenantId == uid || l.AdditionalTenants.Any(t => t.TenantId == uid)) &&
            l.Status != LeaseStatus.Pending);
        if (!hasLease)
        {
            TempData["Danger"] = "Chỉ khách đã từng thuê căn hộ này mới có thể đánh giá.";
            return Redirect(redirectBack);
        }

        var existing = await _db.Reviews.FirstOrDefaultAsync(r => r.ApartmentId == input.ApartmentId && r.UserId == uid);
        if (existing != null)
        {
            if (existing.Status == ReviewStatus.Approved)
            {
                TempData["Danger"] = "Đánh giá đã được duyệt — không thể sửa.";
                return Redirect(redirectBack);
            }
            existing.Rating = input.Rating;
            existing.Content = input.Content.Trim();
            existing.Status = ReviewStatus.Pending; // re-queue for moderation
            existing.ApprovedAt = null;
            existing.ApprovedBy = null;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật đánh giá. Đang chờ ban quản lý duyệt.";
            return Redirect(redirectBack);
        }

        _db.Reviews.Add(new Review
        {
            ApartmentId = input.ApartmentId,
            UserId = uid,
            Rating = input.Rating,
            Content = input.Content.Trim(),
            Status = ReviewStatus.Pending
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Cảm ơn bạn đã đánh giá. Đang chờ ban quản lý duyệt.";
        return Redirect(redirectBack);
    }

    public async Task<IActionResult> Mine()
    {
        var uid = _userManager.GetUserId(User);
        var rows = await _db.Reviews
            .Include(r => r.Apartment)
            .Where(r => r.UserId == uid)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return View(rows);
    }

    public class CreateReviewVm
    {
        [Required] public int ApartmentId { get; set; }
        [Range(1, 5)] public int Rating { get; set; }
        [Required, StringLength(2000, MinimumLength = 10)] public string Content { get; set; } = string.Empty;
    }
}
