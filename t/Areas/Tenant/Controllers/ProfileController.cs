using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using t.Areas.Tenant.Models;
using t.Data;
using t.Models.Entities;

namespace t.Areas.Tenant.Controllers;

public class ProfileController : TenantBaseController
{
    private readonly UserManager<AppUser> _userManager;

    public ProfileController(AppDbContext db, UserManager<AppUser> userManager) : base(db)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        SetActiveNav("profile");
        SetBreadcrumb(("Hồ sơ", null));

        var u = await _userManager.GetUserAsync(User);
        if (u == null) return Unauthorized();
        var vm = new TenantProfileVm
        {
            FullName = u.FullName, Email = u.Email!,
            Phone = u.Phone, AvatarUrl = u.AvatarUrl
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(TenantProfileVm input)
    {
        if (!ModelState.IsValid) return View(input);
        var u = await _userManager.GetUserAsync(User);
        if (u == null) return Unauthorized();

        u.FullName = input.FullName;
        u.Phone = input.Phone;
        u.PhoneNumber = input.Phone;
        u.AvatarUrl = input.AvatarUrl;
        await _userManager.UpdateAsync(u);

        TempData["Success"] = "Đã cập nhật hồ sơ.";
        return RedirectToAction(nameof(Index));
    }
}
