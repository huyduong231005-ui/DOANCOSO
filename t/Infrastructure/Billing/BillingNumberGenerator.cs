using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Time;

namespace t.Infrastructure.Billing;

/// <summary>
/// Race-safe sequence generators for InvoiceNumber / PaymentNumber.
/// Uses MAX(seq)+1 per prefix (per billing month / per VN day). Callers must
/// retry on unique-constraint violation in case two requests pick the same
/// number concurrently — the unique index on InvoiceNumber/PaymentNumber is
/// the source of truth.
/// </summary>
public static class BillingNumberGenerator
{
    public static async Task<string> NextInvoiceNumberAsync(AppDbContext db, int billingMonth, CancellationToken ct = default)
    {
        var prefix = $"INV-{billingMonth}-";
        return await NextWithPrefixAsync(prefix,
            await db.Invoices.Where(i => i.InvoiceNumber.StartsWith(prefix))
                              .Select(i => i.InvoiceNumber).ToListAsync(ct));
    }

    public static async Task<string> NextOneOffInvoiceNumberAsync(AppDbContext db, CancellationToken ct = default)
    {
        var prefix = $"INV-{VnTime.Now:yyyyMM}-";
        return await NextWithPrefixAsync(prefix,
            await db.Invoices.Where(i => i.InvoiceNumber.StartsWith(prefix))
                              .Select(i => i.InvoiceNumber).ToListAsync(ct));
    }

    public static async Task<string> NextMaintenanceNumberAsync(AppDbContext db, CancellationToken ct = default)
    {
        var prefix = $"MR-{VnTime.Now:yyyyMM}-";
        return await NextWithPrefixAsync(prefix,
            await db.MaintenanceRequests.Where(r => r.RequestNumber.StartsWith(prefix))
                                          .Select(r => r.RequestNumber).ToListAsync(ct));
    }

    public static async Task<string> NextPaymentNumberAsync(AppDbContext db, CancellationToken ct = default)
    {
        var prefix = $"PMT-{VnTime.Now:yyyyMMdd}-";
        return await NextWithPrefixAsync(prefix,
            await db.Payments.Where(p => p.PaymentNumber.StartsWith(prefix))
                              .Select(p => p.PaymentNumber).ToListAsync(ct));
    }

    private static Task<string> NextWithPrefixAsync(string prefix, List<string> existing)
    {
        var max = existing
            .Select(s => int.TryParse(s.AsSpan(prefix.Length), out var v) ? v : 0)
            .DefaultIfEmpty(0)
            .Max();
        return Task.FromResult(prefix + (max + 1).ToString("0000"));
    }

    /// <summary>True if the exception is a SQL unique-constraint violation.</summary>
    public static bool IsUniqueConstraintError(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }
}
