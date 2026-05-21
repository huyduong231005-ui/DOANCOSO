using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using t.Infrastructure.Storage;

namespace t.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin,Manager,Host,Tenant")]
[Route("admin/uploads")]
public class UploadsController : Controller
{
    private readonly FileStorageService _storage;

    public UploadsController(FileStorageService storage) => _storage = storage;

    [HttpPost("{folder}")]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Upload(string folder, IFormFile file, CancellationToken ct)
    {
        var r = await _storage.SaveAsync(file, folder, ct);
        if (!r.Success) return BadRequest(new { error = r.Error });
        return Ok(new { url = r.Url, size = r.Size });
    }

    [HttpPost("multi/{folder}")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadMany(string folder, IFormFileCollection files, CancellationToken ct)
    {
        var results = new List<object>();
        foreach (var f in files)
        {
            var r = await _storage.SaveAsync(f, folder, ct);
            results.Add(new { name = f.FileName, success = r.Success, url = r.Url, error = r.Error });
        }
        return Ok(new { items = results });
    }
}
