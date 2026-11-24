using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using t.Data;

namespace t.Infrastructure.Security;

/// <summary>
/// Tra bảng VaiTro_Quyen: người dùng có quyền khi ít nhất một vai trò của họ được cấp mã quyền đó (BR-PQ-01).
/// Kết quả nhớ trong một request vì một trang có thể hỏi nhiều lần (bộ lọc + nút trên giao diện).
/// </summary>
public class PermissionService
{
    private readonly AppDbContext _db;
    private readonly Dictionary<string, bool> _cache = new();

    public PermissionService(AppDbContext db) => _db = db;

    public async Task<bool> HasAsync(ClaimsPrincipal user, string code)
    {
        if (_cache.TryGetValue(code, out var ok)) return ok;
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        ok = roles.Count > 0 && await _db.RolePermissions
            .AnyAsync(rp => roles.Contains(rp.Role.Name!) && rp.Permission.Code == code);
        return _cache[code] = ok;
    }
}

/// <summary>
/// Chặn theo mã quyền: GET cần quyền xem, các phương thức khác (POST) cần quyền thao tác.
/// Đặt ở controller và ở action thì cả hai đều phải đạt (vd thêm cư dân cần apartments.update và residents.manage).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    public PermissionAttribute(string view, string? change = null)
    {
        View = view;
        Change = change ?? view;
    }

    public string View { get; }
    public string Change { get; }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (http.User.Identity?.IsAuthenticated != true) return; // [Authorize] tự chuyển về trang đăng nhập
        var code = HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method) ? View : Change;
        if (!await http.RequestServices.GetRequiredService<PermissionService>().HasAsync(http.User, code))
            context.Result = new ForbidResult();
    }
}
