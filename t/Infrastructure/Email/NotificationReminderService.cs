using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Infrastructure.Email;

/// <summary>Sends reminders to the apartment representative: invoice due in 3 days, overdue invoices.</summary>
public class NotificationReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<NotificationReminderService> _log;
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(12);

    public NotificationReminderService(IServiceProvider services, ILogger<NotificationReminderService> log)
    {
        _services = services;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Notification reminder service started.");
        var lastDay = DateTime.MinValue.Date;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (VnTime.Today != lastDay)
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    await RunOnceAsync(db, sender, stoppingToken);
                    lastDay = VnTime.Today;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Reminder tick failed.");
            }

            try { await Task.Delay(TickInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    public static async Task RunOnceAsync(AppDbContext db, IEmailSender sender, CancellationToken ct)
    {
        var now = VnTime.Today;
        var dueWithin = now.AddDays(3);

        // Invoices due in next 3 days, not yet paid
        var dueSoon = await db.Invoices
            .Include(i => i.Apartment).ThenInclude(a => a.Residents)
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                        && i.DueDate >= now && i.DueDate <= dueWithin)
            .ToListAsync(ct);
        foreach (var i in dueSoon)
        {
            var to = i.Apartment.PrimaryResident;
            if (string.IsNullOrEmpty(to?.Email)) continue;
            await sender.SendAsync(
                to.Email,
                $"[Luxe Haven] Hoá đơn {i.InvoiceNumber} sắp đến hạn ({i.DueDate:dd/MM})",
                BuildDueSoonHtml(i, to.FullName),
                ct);
        }

        // Overdue invoices
        var overdue = await db.Invoices
            .Include(i => i.Apartment).ThenInclude(a => a.Residents)
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                        && i.DueDate < now)
            .ToListAsync(ct);
        foreach (var i in overdue)
        {
            var to = i.Apartment.PrimaryResident;
            if (string.IsNullOrEmpty(to?.Email)) continue;
            var daysLate = (int)(now - i.DueDate).TotalDays;
            await sender.SendAsync(
                to.Email,
                $"[Luxe Haven] Hoá đơn {i.InvoiceNumber} đã trễ {daysLate} ngày",
                BuildOverdueHtml(i, to.FullName, daysLate),
                ct);
        }
    }

    private static string BuildDueSoonHtml(Invoice i, string name) => $@"
<p>Xin chào {name},</p>
<p>Hoá đơn <strong>{i.InvoiceNumber}</strong> của căn <strong>{i.Apartment.UnitCode}</strong> đến hạn ngày <strong>{i.DueDate:dd/MM/yyyy}</strong>.</p>
<p>Số tiền cần thanh toán: <strong>{i.Balance:N0} {i.Currency}</strong>.</p>
<p>Vui lòng đăng nhập cổng cư dân để xem chi tiết và thanh toán.</p>
<p>— Ban quản lý Luxe Haven</p>";

    private static string BuildOverdueHtml(Invoice i, string name, int daysLate) => $@"
<p>Xin chào {name},</p>
<p>Hoá đơn <strong>{i.InvoiceNumber}</strong> của căn <strong>{i.Apartment.UnitCode}</strong> đã quá hạn <strong>{daysLate} ngày</strong>.</p>
<p>Số tiền chưa thanh toán: <strong>{i.Balance:N0} {i.Currency}</strong>.</p>
<p>Đề nghị quý cư dân thanh toán sớm để tránh phát sinh phí trễ hạn.</p>
<p>— Ban quản lý Luxe Haven</p>";
}
