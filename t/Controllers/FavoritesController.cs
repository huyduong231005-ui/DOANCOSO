using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Application.Commands.Favorites;
using t.Data;
using t.Models.Entities;

namespace t.Controllers;

[Authorize]
public class FavoritesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly SetFavoriteCommandHandler _setFavoriteCommandHandler;

    public FavoritesController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        SetFavoriteCommandHandler setFavoriteCommandHandler)
    {
        _db = db;
        _userManager = userManager;
        _setFavoriteCommandHandler = setFavoriteCommandHandler;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActiveNav"] = "favorites";
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
    public async Task<IActionResult> Set(
        int apartmentId,
        bool shouldBeFavorite,
        string? returnUrl,
        CancellationToken cancellationToken)
    {
        var uid = _userManager.GetUserId(User);
        if (uid == null) return Challenge();

        var result = await _setFavoriteCommandHandler.HandleAsync(
            new SetFavoriteCommand(uid, apartmentId, shouldBeFavorite),
            cancellationToken);
        if (result.Status == SetFavoriteStatus.ApartmentNotFound) return NotFound();

        // AJAX caller: return JSON. Otherwise redirect back.
        if (Request.Headers["X-Requested-With"] == "fetch")
            return Json(new { ok = true, favorite = result.IsFavorite });

        TempData["Success"] = result.IsFavorite ? "Đã thêm vào yêu thích." : "Đã bỏ khỏi yêu thích.";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }
}
