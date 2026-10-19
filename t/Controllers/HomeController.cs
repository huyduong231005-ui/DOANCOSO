using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Application.Commands.Auth;
using t.Data;
using t.Infrastructure.Residents;
using t.Models;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly AuthCommandHandler _authCommandHandler;
    private readonly ResidentInvites _invites;

    public HomeController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        AuthCommandHandler authCommandHandler,
        ResidentInvites invites)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _authCommandHandler = authCommandHandler;
        _invites = invites;
    }

    // Trang giới thiệu Ban quản lý: các khu đang vận hành, lối vào cổng cư dân / quản trị.
    public async Task<IActionResult> Index()
    {
        var projects = await _db.Projects
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new LandingProjectVm
            {
                Name = p.Name,
                Address = p.Address,
                ThumbnailUrl = p.ThumbnailUrl,
                ShortDescription = p.ShortDescription,
                BuildingCount = _db.Buildings.Count(b => b.ProjectId == p.Id),
                ApartmentCount = _db.Apartments.Count(a => a.Building != null && a.Building.ProjectId == p.Id)
            })
            .ToListAsync();
        return View(projects);
    }

    public IActionResult Privacy() => View();

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User?.Identity?.IsAuthenticated == true)
            await _signInManager.SignOutAsync();

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None, Duration = 0)]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
            return View(model);

        if (User?.Identity?.IsAuthenticated == true)
            await _signInManager.SignOutAsync();

        var result = await _signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Admin") || roles.Contains("Manager"))
                    return LocalRedirect("/admin");
                if (roles.Any(r => Areas.Staff.Controllers.JobsController.StaffRoles.Split(',').Contains(r)))
                    return LocalRedirect("/staff/jobs");
                if (roles.Contains("Tenant"))
                    return LocalRedirect("/tenant");
            }
            return LocalRedirect("/");
        }

        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    // Liên kết trong thư mời của Ban quản lý: đã có tài khoản đúng email thì gắn căn luôn rồi mời đăng nhập,
    // chưa có thì sang trang đăng ký với email được mời.
    [HttpGet]
    public async Task<IActionResult> Invite(string? token)
    {
        var resident = await _invites.FindAsync(token);
        if (resident == null)
        {
            TempData["AuthError"] = "Lời mời không hợp lệ hoặc đã hết hạn. Liên hệ Ban quản lý để được gửi lại.";
            return RedirectToAction(nameof(Login));
        }

        var existing = await _userManager.FindByEmailAsync(resident.Email!);
        if (existing == null) return RedirectToAction(nameof(Register), new { invite = token });

        if (await _invites.AcceptAsync(token, existing) == null)
        {
            TempData["AuthError"] = "Cư dân này đã được gắn với một tài khoản khác. Liên hệ Ban quản lý.";
            return RedirectToAction(nameof(Login));
        }
        if (!await _userManager.IsInRoleAsync(existing, "Tenant"))
            await _userManager.AddToRoleAsync(existing, "Tenant");
        TempData["AuthInfo"] = $"Đã gắn căn {resident.Apartment.UnitCode} vào tài khoản {resident.Email}. Đăng nhập để xem.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? invite)
    {
        var resident = await _invites.FindAsync(invite);
        if (resident == null) return View();
        ViewData["Invite"] = invite;
        ViewData["InviteUnit"] = resident.Apartment.UnitCode;
        return View(new RegisterViewModel { FullName = resident.FullName, Email = resident.Email!, Phone = resident.Phone });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? invite)
    {
        // Đăng ký qua thư mời: email phải đúng email được mời (ô email bị khoá trên form, kiểm tra lại ở đây).
        var invited = await _invites.FindAsync(invite);
        if (invited != null)
        {
            ViewData["Invite"] = invite;
            ViewData["InviteUnit"] = invited.Apartment.UnitCode;
            model.Email = invited.Email!;
            ModelState.Remove(nameof(model.Email));
        }

        if (!ModelState.IsValid)
            return View(model);

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            Phone = model.Phone,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // Public sign-up always lands in the Tenant role. Without this the user
            // cannot access /tenant area and admin lease dropdowns won't list them.
            await _userManager.AddToRoleAsync(user, "Tenant");
            await _signInManager.SignInAsync(user, isPersistent: false);
            if (invited != null && await _invites.AcceptAsync(invite, user) != null)
                return LocalRedirect("/tenant");
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            var message = ToVietnameseIdentityError(error.Code);
            if (error.Code is "DuplicateEmail" or "InvalidEmail")
            {
                ModelState.AddModelError(nameof(model.Email), message);
                continue;
            }

            if (error.Code.StartsWith("Password", StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.Password), message);
                continue;
            }

            ModelState.AddModelError(string.Empty, message);
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _authCommandHandler.ForgotPasswordAsync(model.Email);
        TempData["ForgotSuccess"] = true;
        return View(model);
    }

    [HttpGet]
    public IActionResult ResetPassword(string email, string token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
            return BadRequest();

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authCommandHandler.ResetPasswordAsync(model);
        if (result.Succeeded)
        {
            TempData["ResetSuccess"] = true;
            return RedirectToAction(nameof(Login));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        Response.Cookies.Delete(".AspNetCore.Identity.Application");
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string ToVietnameseIdentityError(string code)
    {
        return code switch
        {
            "DuplicateEmail" => "Email này đã được đăng ký. Vui lòng dùng email khác.",
            "DuplicateUserName" => "Tài khoản đã tồn tại. Vui lòng kiểm tra lại email.",
            "InvalidEmail" => "Email không đúng định dạng.",
            "PasswordTooShort" => "Mật khẩu phải có ít nhất 6 ký tự.",
            "PasswordRequiresDigit" => "Mật khẩu phải chứa ít nhất 1 chữ số.",
            "PasswordRequiresUpper" => "Mật khẩu phải chứa ít nhất 1 chữ cái in hoa.",
            "PasswordRequiresLower" => "Mật khẩu phải chứa ít nhất 1 chữ cái thường.",
            "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt.",
            _ => "Không thể tạo tài khoản. Vui lòng kiểm tra lại thông tin đã nhập."
        };
    }
}
