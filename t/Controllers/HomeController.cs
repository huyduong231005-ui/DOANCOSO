using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Application.Commands.Auth;
using t.Data;
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

    public HomeController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        AuthCommandHandler authCommandHandler)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _authCommandHandler = authCommandHandler;
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
                if (roles.Contains("Tenant"))
                    return LocalRedirect("/tenant");
            }
            return LocalRedirect("/");
        }

        ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
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
