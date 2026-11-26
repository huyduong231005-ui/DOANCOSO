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
    private readonly t.Infrastructure.Email.IEmailSender _email;
    private readonly ILogger<HomeController> _log;

    public HomeController(
        AppDbContext db,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        AuthCommandHandler authCommandHandler,
        ResidentInvites invites,
        t.Infrastructure.Email.IEmailSender email,
        ILogger<HomeController> log)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _authCommandHandler = authCommandHandler;
        _invites = invites;
        _email = email;
        _log = log;
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
                BuildingCount = _db.Buildings.Count(b => b.ProjectId == p.Id),
                ApartmentCount = _db.Apartments.Count(a => a.Building != null && a.Building.ProjectId == p.Id)
            })
            .ToListAsync();
        return View(projects);
    }

    public IActionResult Privacy() => View();

    /// <summary>Đã đăng nhập nhưng vai trò không được vào trang: báo rõ (403) và giữ nguyên phiên.</summary>
    [HttpGet]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        ViewData["PortalUrl"] = PortalUrl(User.IsInRole);
        return View();
    }

    /// <summary>Trang chính theo vai trò, dùng sau khi đăng nhập và trên trang không có quyền.</summary>
    private static string PortalUrl(Func<string, bool> isInRole)
    {
        if (isInRole("Admin") || isInRole("Manager")) return "/admin";
        if (Areas.Staff.Controllers.JobsController.StaffRoles.Split(',').Any(isInRole)) return "/staff/jobs";
        if (isInRole("Tenant")) return "/tenant";
        return "/";
    }

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
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true); // sai 5 lần khoá 5 phút (mặc định Identity)

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return LocalRedirect("/");
            var roles = await _userManager.GetRolesAsync(user);
            return LocalRedirect(PortalUrl(roles.Contains));
        }

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Tài khoản đang bị khoá do đăng nhập sai nhiều lần hoặc theo yêu cầu của Ban quản lý. Thử lại sau ít phút hoặc liên hệ Ban quản lý."
            : "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    // Liên kết trong thư mời của Ban quản lý. Tài khoản của người cư trú đã có sẵn (BQL tạo khi ghi nhận cư trú):
    // chưa đặt mật khẩu thì sang trang kích hoạt, đã kích hoạt thì chỉ cần đăng nhập.
    [HttpGet]
    public async Task<IActionResult> Invite(string? token)
    {
        var resident = await _invites.FindAsync(token);
        if (resident == null)
        {
            TempData["AuthError"] = "Lời mời không hợp lệ hoặc đã hết hạn. Liên hệ Ban quản lý để được gửi lại.";
            return RedirectToAction(nameof(Login));
        }
        if (!resident.User.IsActivated) return RedirectToAction(nameof(Register), new { invite = token });

        // Tài khoản đã có: bấm thư là xác nhận nhận căn.
        resident.ConfirmedAt ??= DateTime.UtcNow;
        await _userManager.UpdateAsync(resident.User); // lưu cả thay đổi của bản ghi cư trú (cùng DbContext)
        if (!await _userManager.IsInRoleAsync(resident.User, "Tenant"))
            await _userManager.AddToRoleAsync(resident.User, "Tenant");
        TempData["AuthInfo"] = $"Căn {resident.Apartment.UnitCode} đã gắn với tài khoản {resident.User.Email}. Đăng nhập để xem.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? invite)
    {
        var resident = await _invites.FindAsync(invite);
        if (resident == null || resident.User.IsActivated) return RegisterNeedsInvite();
        ViewData["Invite"] = invite;
        ViewData["InviteUnit"] = resident.Apartment.UnitCode;
        return View(new RegisterViewModel { FullName = resident.User.FullName, Email = resident.User.Email!, Phone = resident.User.PhoneNumber });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? invite)
    {
        // Chỉ kích hoạt qua thư mời (BR-CD-02): email là email được mời (ô email bị khoá trên form,
        // kiểm tra lại ở đây). Bấm được liên kết trong thư là đã chứng minh sở hữu hộp thư.
        var invited = await _invites.FindAsync(invite);
        if (invited == null || invited.User.IsActivated) return RegisterNeedsInvite();
        ViewData["Invite"] = invite;
        ViewData["InviteUnit"] = invited.Apartment.UnitCode;
        model.Email = invited.User.Email!;
        ModelState.Remove(nameof(model.Email));

        if (!ModelState.IsValid)
            return View(model);

        var user = invited.User;
        user.FullName = model.FullName;
        user.PhoneNumber = model.Phone;
        user.EmailConfirmed = true;
        invited.ConfirmedAt = DateTime.UtcNow;
        var result = await _userManager.AddPasswordAsync(user, model.Password); // lưu luôn bản ghi cư trú (cùng DbContext)

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "Tenant");
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect("/tenant");
        }

        foreach (var error in result.Errors)
        {
            var message = ToVietnameseIdentityError(error.Code);
            if (error.Code.StartsWith("Password", StringComparison.Ordinal))
            {
                ModelState.AddModelError(nameof(model.Password), message);
                continue;
            }

            ModelState.AddModelError(string.Empty, message);
        }

        return View(model);
    }

    private IActionResult RegisterNeedsInvite()
    {
        TempData["AuthInfo"] = "Tài khoản cư dân được tạo qua thư mời của Ban quản lý. Liên hệ Ban quản lý nếu bạn chưa nhận được thư.";
        return RedirectToAction(nameof(Login));
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

        var result = await _authCommandHandler.ForgotPasswordAsync(model.Email);
        if (result.UserFound)
        {
            var link = Url.Action(nameof(ResetPassword), "Home", new { email = model.Email, token = result.Token }, Request.Scheme)!;
            var html = "<p>Bạn (hoặc ai đó) vừa yêu cầu đặt lại mật khẩu cổng cư dân.</p>" +
                       $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">Đặt lại mật khẩu</a></p>" +
                       "<p>Nếu không phải bạn yêu cầu, hãy bỏ qua thư này; mật khẩu hiện tại vẫn giữ nguyên.</p>";
            try { await _email.SendAsync(model.Email, "Đặt lại mật khẩu cổng cư dân", html); }
            catch (Exception ex) { _log.LogWarning(ex, "Could not send password reset email"); }
        }
        // Cùng một thông báo dù email có tồn tại hay không, để không lộ email nào có tài khoản.
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
