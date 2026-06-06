using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using t.Data;

namespace t.Areas.Admin.Controllers;

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

    protected static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var s = input.Trim().ToLowerInvariant();
        var map = new Dictionary<char, char>
        {
            {'à','a'},{'á','a'},{'ạ','a'},{'ả','a'},{'ã','a'},
            {'â','a'},{'ầ','a'},{'ấ','a'},{'ậ','a'},{'ẩ','a'},{'ẫ','a'},
            {'ă','a'},{'ằ','a'},{'ắ','a'},{'ặ','a'},{'ẳ','a'},{'ẵ','a'},
            {'è','e'},{'é','e'},{'ẹ','e'},{'ẻ','e'},{'ẽ','e'},
            {'ê','e'},{'ề','e'},{'ế','e'},{'ệ','e'},{'ể','e'},{'ễ','e'},
            {'ì','i'},{'í','i'},{'ị','i'},{'ỉ','i'},{'ĩ','i'},
            {'ò','o'},{'ó','o'},{'ọ','o'},{'ỏ','o'},{'õ','o'},
            {'ô','o'},{'ồ','o'},{'ố','o'},{'ộ','o'},{'ổ','o'},{'ỗ','o'},
            {'ơ','o'},{'ờ','o'},{'ớ','o'},{'ợ','o'},{'ở','o'},{'ỡ','o'},
            {'ù','u'},{'ú','u'},{'ụ','u'},{'ủ','u'},{'ũ','u'},
            {'ư','u'},{'ừ','u'},{'ứ','u'},{'ự','u'},{'ử','u'},{'ữ','u'},
            {'ỳ','y'},{'ý','y'},{'ỵ','y'},{'ỷ','y'},{'ỹ','y'},
            {'đ','d'}
        };
        var sb = new System.Text.StringBuilder();
        foreach (var ch in s)
            sb.Append(map.TryGetValue(ch, out var v) ? v : ch);
        var raw = sb.ToString();
        var clean = System.Text.RegularExpressions.Regex.Replace(raw, "[^a-z0-9]+", "-");
        return clean.Trim('-');
    }
}
