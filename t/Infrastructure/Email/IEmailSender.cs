namespace t.Infrastructure.Email;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>
/// Gửi email thật qua SMTP (STARTTLS, cổng 587). Dùng khi <c>Smtp:Host</c> được cấu hình;
/// mật khẩu đặt qua biến môi trường <c>Smtp__Password</c>, không ghi vào appsettings.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config) => _config = config;

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var smtp = _config.GetSection("Smtp");
        var user = smtp["User"];
        using var client = new System.Net.Mail.SmtpClient(smtp["Host"], smtp.GetValue("Port", 587))
        {
            EnableSsl = true,
            Credentials = new System.Net.NetworkCredential(user, smtp["Password"]),
        };
        var from = string.IsNullOrWhiteSpace(smtp["From"]) ? user : smtp["From"];
        using var message = new System.Net.Mail.MailMessage(from!, to, subject, htmlBody) { IsBodyHtml = true };
        // SmtpClient.Timeout không áp dụng cho bản async; không giới hạn thì máy chủ SMTP treo sẽ giữ request của cư dân.
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await client.SendMailAsync(message, timeout.Token);
    }
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
