using Microsoft.AspNetCore.Mvc;
using t.Infrastructure.Security;
using t.Models.ViewModels;

namespace t.Controllers.Api;

[ApiController]
[Route("api/dev/auth")]
public class DevAuthApiController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IDevResetTokenStore _tokenStore;

    public DevAuthApiController(IWebHostEnvironment env, IDevResetTokenStore tokenStore)
    {
        _env = env;
        _tokenStore = tokenStore;
    }

    [HttpGet("reset-token")]
    public ActionResult<DevResetTokenResponse> GetResetToken([FromQuery] string email)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        return Ok(new DevResetTokenResponse
        {
            Email = email,
            Token = _tokenStore.Get(email)
        });
    }
}
