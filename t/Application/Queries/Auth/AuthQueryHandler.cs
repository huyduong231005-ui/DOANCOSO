using Microsoft.AspNetCore.Identity;
using t.Models.Entities;

namespace t.Application.Queries.Auth;

public sealed class AuthQueryHandler
{
    private readonly UserManager<AppUser> _userManager;

    public AuthQueryHandler(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var user = await _userManager.FindByEmailAsync(email);
        return user is not null;
    }
}
