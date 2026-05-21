namespace t.Infrastructure.Storage;

public class FileStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf"
    };
    private const long MaxBytes = 10 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileStorageService> _log;

    public FileStorageService(IWebHostEnvironment env, ILogger<FileStorageService> log)
    {
        _env = env;
        _log = log;
    }

    public async Task<UploadResult> SaveAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0) return UploadResult.Fail("File rỗng.");
        if (file.Length > MaxBytes) return UploadResult.Fail($"File vượt quá {MaxBytes / 1024 / 1024} MB.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
            return UploadResult.Fail($"Định dạng không hỗ trợ: {ext}");

        var safeFolder = string.Concat((folder ?? "misc").Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrEmpty(safeFolder)) safeFolder = "misc";

        var monthBucket = DateTime.UtcNow.ToString("yyyyMM");
        var relDir = Path.Combine("uploads", safeFolder, monthBucket).Replace('\\', '/');
        var absDir = Path.Combine(_env.WebRootPath, relDir);
        Directory.CreateDirectory(absDir);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absPath = Path.Combine(absDir, fileName);
        await using (var stream = File.Create(absPath))
            await file.CopyToAsync(stream, ct);

        var url = "/" + Path.Combine(relDir, fileName).Replace('\\', '/');
        _log.LogInformation("Uploaded {File} ({Size} bytes) → {Url}", file.FileName, file.Length, url);
        return UploadResult.Ok(url, file.Length);
    }
}

public record UploadResult(bool Success, string? Url, long Size, string? Error)
{
    public static UploadResult Ok(string url, long size) => new(true, url, size, null);
    public static UploadResult Fail(string error) => new(false, null, 0, error);
}
