using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Infrastructure.Billing;

/// <summary>
/// Periodically transitions Active/Renewing leases past their EndDate into Expired,
/// frees up apartment occupancy that is no longer held by any lease, and marks
/// unpaid invoices past their DueDate as Overdue.
/// </summary>
public class LeaseLifecycleService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LeaseLifecycleService> _log;
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(6);

    public LeaseLifecycleService(IServiceProvider services, ILogger<LeaseLifecycleService> log)
    {
        _services = services;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Lease lifecycle service started.");
        var lastRunDay = DateTime.MinValue.Date;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var today = VnTime.Today;
                if (today != lastRunDay)
                {
                    await ExpireOverdueLeasesAsync(stoppingToken);
                    await MarkOverdueInvoicesAsync(stoppingToken);
                    lastRunDay = today;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Lease lifecycle tick failed");
            }

            try { await Task.Delay(TickInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _log.LogInformation("Lease lifecycle service stopped.");
    }

    private async Task ExpireOverdueLeasesAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var today = VnTime.Today;

        var overdue = await db.Leases
            .Include(l => l.Apartment)
            .Where(l => (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Renewing)
                        && l.EndDate < today)
            .ToListAsync(ct);

        if (overdue.Count == 0) return;

        foreach (var l in overdue)
        {
            l.Status = LeaseStatus.Expired;

            // Free apartment occupancy only if no other Active/Pending lease holds it.
            var stillBlocked = await db.Leases.AnyAsync(o =>
                o.Id != l.Id && o.ApartmentId == l.ApartmentId &&
                (o.Status == LeaseStatus.Active ||
                 o.Status == LeaseStatus.Pending ||
                 o.Status == LeaseStatus.Renewing), ct);

            if (!stillBlocked && l.Apartment != null)
                l.Apartment.Occupancy = ApartmentOccupancy.Available;
        }

        await db.SaveChangesAsync(ct);
        _log.LogInformation("Auto-expired {Count} lease(s).", overdue.Count);
    }

    private async Task MarkOverdueInvoicesAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var today = VnTime.Today;

        var toMark = await db.Invoices
            .Where(i => (i.Status == InvoiceStatus.Issued || i.Status == InvoiceStatus.PartiallyPaid)
                        && i.Balance > 0
                        && i.DueDate < today)
            .ToListAsync(ct);

        if (toMark.Count == 0) return;

        foreach (var inv in toMark)
            inv.Status = InvoiceStatus.Overdue;

        await db.SaveChangesAsync(ct);
        _log.LogInformation("Auto-marked {Count} invoice(s) as Overdue.", toMark.Count);
    }
}
