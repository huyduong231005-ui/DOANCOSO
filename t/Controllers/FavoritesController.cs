using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;

namespace t.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public FavoritesController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var uid = _userManager.GetUserId(User);
        var items = await _db.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == uid)
            .Include(f => f.Apartment).ThenInclude(a => a.Images)
            .Include(f => f.Apartment).ThenInclude(a => a.Category)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Apartment)
            .ToListAsync();
        return View(items);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int apartmentId, string? returnUrl)
    {
        var uid = _userManager.GetUserId(User);
        if (uid == null) return Challenge();

        var apartmentExists = await _db.Apartments.AnyAsync(a => a.Id == apartmentId);
        if (!apartmentExists) return NotFound();

        var existing = await _db.Favorites.FirstOrDefaultAsync(f => f.UserId == uid && f.ApartmentId == apartmentId);
        bool nowFavorite;
        if (existing != null)
        {
            _db.Favorites.Remove(existing);
            nowFavorite = false;
        }
        else
        {
            _db.Favorites.Add(new Favorite { UserId = uid, ApartmentId = apartmentId });
            nowFavorite = true;
        }
        await _db.SaveChangesAsync();

        // AJAX caller: return JSON. Otherwise redirect back.
        if (Request.Headers["X-Requested-With"] == "fetch")
            return Json(new { ok = true, favorite = nowFavorite });

        TempData["Success"] = nowFavorite ? "Đã thêm vào yêu thích." : "Đã bỏ khỏi yêu thích.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}
