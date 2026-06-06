using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using t.Infrastructure.Storage;

namespace t.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
[Route("admin/uploads")]
public class UploadsController : Controller
{
    // Self-service roles (Host/Tenant) may only upload to these folders; everything else is staff-only.
    private static readonly HashSet<string> SelfServiceFolders =
        new(StringComparer.OrdinalIgnoreCase) { "maintenance" };

    private readonly FileStorageService _storage;

    public UploadsController(FileStorageService storage) => _storage = storage;

    private bool IsAuthorizedForFolder(string folder)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Manager"))
            return true;
        return SelfServiceFolders.Contains(folder) &&
               (User.IsInRole("Tenant") || User.IsInRole("Host"));
    }

    [HttpPost("{folder}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Upload(string folder, IFormFile file, CancellationToken ct)
    {
        if (!IsAuthorizedForFolder(folder)) return Forbid();
        var r = await _storage.SaveAsync(file, folder, ct);
        if (!r.Success) return BadRequest(new { error = r.Error });
        return Ok(new { url = r.Url, size = r.Size });
    }

    [HttpPost("multi/{folder}")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> UploadMany(string folder, IFormFileCollection files, CancellationToken ct)
    {
        if (!IsAuthorizedForFolder(folder)) return Forbid();
        var results = new List<object>();
        foreach (var f in files)
        {
            var r = await _storage.SaveAsync(f, folder, ct);
            results.Add(new { name = f.FileName, success = r.Success, url = r.Url, error = r.Error });
        }
        return Ok(new { items = results });
    }
}
