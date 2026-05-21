using Microsoft.AspNetCore.Mvc;
using t.Application.Queries.Auth;
using t.Models.ViewModels;

namespace t.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AuthQueryHandler _authQueryHandler;

    public AuthApiController(AuthQueryHandler authQueryHandler)
    {
        _authQueryHandler = authQueryHandler;
    }

    [HttpGet("email-exists")]
    public async Task<ActionResult<EmailExistsResponse>> EmailExists([FromQuery] string email)
    {
        var exists = await _authQueryHandler.EmailExistsAsync(email);
        return Ok(new EmailExistsResponse
        {
            Email = email,
            Exists = exists
        });
    }
}
