namespace t.Infrastructure.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _log;
    private readonly IWebHostEnvironment _env;

    public LoggingEmailSender(ILogger<LoggingEmailSender> log, IWebHostEnvironment env)
    {
        _log = log;
        _env = env;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _log.LogInformation("[EMAIL → {To}] {Subject}", to, subject);

        // In development, also persist to wwwroot/dev-emails for inspection.
        try
        {
            var dir = Path.Combine(_env.WebRootPath, "dev-emails");
            Directory.CreateDirectory(dir);
            var safe = string.Concat(subject.Where(c => char.IsLetterOrDigit(c) || c == ' ')).Trim().Replace(' ', '_');
            if (safe.Length > 60) safe = safe.Substring(0, 60);
            var fileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{safe}_{Guid.NewGuid():N}.html";
            var html = $"<!doctype html><meta charset=utf-8><title>{subject}</title>" +
                       $"<div style='font-family:sans-serif;padding:20px'><p><strong>To:</strong> {to}</p><p><strong>Subject:</strong> {subject}</p><hr/>{htmlBody}</div>";
            await File.WriteAllTextAsync(Path.Combine(dir, fileName), html, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Failed to persist dev email");
        }
    }
}
