using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Models.Entities;

namespace t.Infrastructure.Billing;

/// <summary>
/// Keeps Lease.DepositHeld in sync with Invoice/Payment activity for Deposit-kind invoices.
/// Single source of truth: deposit is only "held" once a payment on a Deposit invoice succeeds.
/// </summary>
public static class DepositLedger
{
    /// <summary>
    /// Call when a payment on an invoice transitions to Succeeded. If the invoice is a Deposit
    /// invoice, record a Hold transaction and increase Lease.DepositHeld by the payment amount.
    /// No-op for non-Deposit invoices.
    /// </summary>
    public static async Task OnPaymentAppliedAsync(AppDbContext db, Invoice invoice, decimal amount, string? recordedBy, CancellationToken ct = default)
    {
        if (invoice.Kind != InvoiceKind.Deposit || amount <= 0) return;

        var lease = await db.Leases.FirstOrDefaultAsync(l => l.Id == invoice.LeaseId, ct);
        if (lease == null) return;

        lease.DepositHeld += amount;

        db.DepositTransactions.Add(new DepositTransaction
        {
            LeaseId = lease.Id,
            Type = DepositTransactionType.Hold,
            Amount = amount,
            Reason = $"Nhận cọc theo hoá đơn {invoice.InvoiceNumber}",
            RecordedAt = DateTime.UtcNow,
            RecordedBy = recordedBy
        });
    }

    /// <summary>
    /// Call when a payment on a Deposit invoice is refunded (fully or partially).
    /// Decreases Lease.DepositHeld and records an Adjustment transaction.
    /// </summary>
    public static async Task OnPaymentReversedAsync(AppDbContext db, Invoice invoice, decimal amount, string? recordedBy, CancellationToken ct = default)
    {
        if (invoice.Kind != InvoiceKind.Deposit || amount <= 0) return;

        var lease = await db.Leases.FirstOrDefaultAsync(l => l.Id == invoice.LeaseId, ct);
        if (lease == null) return;

        var reverseAmount = Math.Min(amount, lease.DepositHeld);
        if (reverseAmount <= 0) return;

        lease.DepositHeld -= reverseAmount;

        db.DepositTransactions.Add(new DepositTransaction
        {
            LeaseId = lease.Id,
            Type = DepositTransactionType.Adjustment,
            Amount = reverseAmount,
            Reason = $"Hoàn cọc theo hoá đơn {invoice.InvoiceNumber} (refund)",
            RecordedAt = DateTime.UtcNow,
            RecordedBy = recordedBy
        });
    }
}
