using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using t.Application.Commands.RentalPreferences;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Controllers;

[Authorize]
public sealed class RentalPreferencesController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SaveRentalPreferenceCommandHandler _handler;

    public RentalPreferencesController(
        UserManager<AppUser> userManager,
        SaveRentalPreferenceCommandHandler handler)
    {
        _userManager = userManager;
        _handler = handler;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        RentalSearchRequest request,
        string? returnUrl = null,
        CancellationToken cancellationToken = default)
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
            return Challenge();

        var normalization = RentalPreferenceNormalizer.Normalize(request, strict: false);
        var result = normalization.IsValid
            ? await _handler.HandleAsync(
                new SaveRentalPreferenceCommand(userId, normalization.Draft),
                cancellationToken)
            : new SaveRentalPreferenceResult(false, normalization.Errors);

        if (result.Success)
            TempData["Success"] = "Đã lưu hồ sơ nhu cầu thuê.";
        else
            TempData["Danger"] = result.Errors.FirstOrDefault() ?? "Không thể lưu hồ sơ nhu cầu thuê.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return RedirectToAction("Rentals", "Home");
    }
}
