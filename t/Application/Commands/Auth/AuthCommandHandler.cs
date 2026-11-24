using Microsoft.AspNetCore.Identity;
using t.Models.Entities;
using t.Models.ViewModels;

namespace t.Application.Commands.Auth;

public sealed class ForgotPasswordCommandResult
{
    public bool UserFound { get; init; }
    public string? Token { get; init; }
}

public sealed class AuthCommandHandler
{
    private readonly UserManager<AppUser> _userManager;

    public AuthCommandHandler(UserManager<AppUser> userManager) => _userManager = userManager;

    public async Task<ForgotPasswordCommandResult> ForgotPasswordAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return new ForgotPasswordCommandResult { UserFound = false };

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        return new ForgotPasswordCommandResult
        {
            UserFound = true,
            Token = token
        };
    }

    public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordViewModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
            return IdentityResult.Failed(new IdentityError { Description = "Tài khoản không tồn tại." });

        return await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
    }
}
