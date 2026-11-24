using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using t.Data;

namespace t.Areas.Admin.Controllers;

// Vai trò mở cửa khu quản trị; từng phân hệ còn kiểm tra mã quyền trong VaiTro_Quyen bằng [Permission].
// Bảng điều khiển, báo cáo, điện nước, tải ảnh chưa có mã quyền riêng nên chỉ cần vai trò.
[Area("Admin")]
[Authorize(Roles = "Admin,Manager")]
public abstract class AdminBaseController : Controller
{
    protected readonly AppDbContext Db;

    protected AdminBaseController(AppDbContext db) => Db = db;

    protected void SetBreadcrumb(params (string Text, string? Url)[] crumbs)
    {
        ViewData["Breadcrumbs"] = crumbs.ToList();
    }

    protected void SetActiveNav(string key) => ViewData["ActiveNav"] = key;

    protected IActionResult RedirectToLocalOrDefault(string? returnUrl, string fallbackUrl)
    {
        return !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? Redirect(returnUrl)
            : Redirect(fallbackUrl);
    }

}
