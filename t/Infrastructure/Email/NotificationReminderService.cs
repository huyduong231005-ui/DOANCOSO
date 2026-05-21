using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Infrastructure.Email;

/// <summary>Sends reminders: invoice due in 3 days, lease expiring in 30 days, overdue invoices.</summary>
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
        var expireWithin = now.AddDays(30);

        // Invoices due in next 3 days, not yet paid
        var dueSoon = await db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                        && i.DueDate >= now && i.DueDate <= dueWithin)
            .ToListAsync(ct);
        foreach (var i in dueSoon)
        {
            if (string.IsNullOrEmpty(i.Lease.PrimaryTenant.Email)) continue;
            await sender.SendAsync(
                i.Lease.PrimaryTenant.Email,
                $"[Luxe Haven] Hoá đơn {i.InvoiceNumber} sắp đến hạn ({i.DueDate:dd/MM})",
                BuildDueSoonHtml(i),
                ct);
        }

        // Overdue invoices
        var overdue = await db.Invoices
            .Include(i => i.Lease).ThenInclude(l => l.PrimaryTenant)
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                        && i.DueDate < now)
            .ToListAsync(ct);
        foreach (var i in overdue)
        {
            if (string.IsNullOrEmpty(i.Lease.PrimaryTenant.Email)) continue;
            var daysLate = (int)(now - i.DueDate).TotalDays;
            await sender.SendAsync(
                i.Lease.PrimaryTenant.Email,
                $"[Luxe Haven] Hoá đơn {i.InvoiceNumber} đã trễ {daysLate} ngày",
                BuildOverdueHtml(i, daysLate),
                ct);
        }

        // Leases expiring in next 30 days
        var expiring = await db.Leases
            .Include(l => l.PrimaryTenant).Include(l => l.Apartment)
            .Where(l => l.Status == LeaseStatus.Active && l.EndDate >= now && l.EndDate <= expireWithin)
            .ToListAsync(ct);
        foreach (var l in expiring)
        {
            if (string.IsNullOrEmpty(l.PrimaryTenant.Email)) continue;
            var daysLeft = (int)(l.EndDate - now).TotalDays;
            await sender.SendAsync(
                l.PrimaryTenant.Email,
                $"[Luxe Haven] Hợp đồng {l.LeaseNumber} sắp hết hạn ({daysLeft} ngày)",
                BuildExpiringHtml(l, daysLeft),
                ct);
        }
    }

    private static string BuildDueSoonHtml(Invoice i) => $@"
<p>Xin chào {i.Lease.PrimaryTenant.FullName},</p>
<p>Hoá đơn <strong>{i.InvoiceNumber}</strong> ({i.Kind}) đến hạn ngày <strong>{i.DueDate:dd/MM/yyyy}</strong>.</p>
<p>Số tiền cần thanh toán: <strong>{i.Balance:N0} {i.Currency}</strong>.</p>
<p>Vui lòng đăng nhập để xem chi tiết và thanh toán.</p>
<p>— Luxe Haven</p>";

    private static string BuildOverdueHtml(Invoice i, int daysLate) => $@"
<p>Xin chào {i.Lease.PrimaryTenant.FullName},</p>
<p>Hoá đơn <strong>{i.InvoiceNumber}</strong> đã quá hạn <strong>{daysLate} ngày</strong>.</p>
<p>Số tiền chưa thanh toán: <strong>{i.Balance:N0} {i.Currency}</strong>.</p>
<p>Đề nghị quý khách thanh toán sớm để tránh phát sinh phí trễ hạn.</p>
<p>— Luxe Haven</p>";

    private static string BuildExpiringHtml(Lease l, int daysLeft) => $@"
<p>Xin chào {l.PrimaryTenant.FullName},</p>
<p>Hợp đồng <strong>{l.LeaseNumber}</strong> tại căn <strong>{l.Apartment.Title}</strong> sẽ hết hạn vào ngày <strong>{l.EndDate:dd/MM/yyyy}</strong> (còn {daysLeft} ngày).</p>
<p>Vui lòng liên hệ ban quản lý nếu muốn gia hạn hợp đồng.</p>
<p>— Luxe Haven</p>";
}
