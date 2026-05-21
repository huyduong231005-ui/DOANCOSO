using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace t.Infrastructure.Audit;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    public string? UserId =>
        _accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName =>
        _accessor.HttpContext?.User.Identity?.Name;

    public string? IpAddress =>
        _accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _accessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
