using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Controllers;

// Dev-only endpoint to seed a multi-unit operational scenario for manual testing.
// Hard-gated by IHostEnvironment.IsDevelopment(); a no-op otherwise.
public class DevController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userMgr;
    private readonly InvoiceGenerator _invoiceGen;
    private readonly IWebHostEnvironment _env;

    public DevController(AppDbContext db, UserManager<AppUser> userMgr, InvoiceGenerator invoiceGen, IWebHostEnvironment env)
    {
        _db = db;
        _userMgr = userMgr;
        _invoiceGen = invoiceGen;
        _env = env;
    }

    [HttpGet("/Dev/SeedScenario")]
    public async Task<IActionResult> SeedScenario()
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var log = new List<string>();
        var now = VnTime.Today;
        var thisMonth = now.Year * 100 + now.Month;
        var lastMonthDate = now.AddMonths(-1);
        var lastMonth = lastMonthDate.Year * 100 + lastMonthDate.Month;

        // ── Tenants ──────────────────────────────────────────────────────────
        var renter = await _userMgr.FindByEmailAsync("renter@luxehaven.vn");
        var renter2 = await _userMgr.FindByEmailAsync("renter2@luxehaven.vn");
        var renter3 = await _userMgr.FindByEmailAsync("renter3@luxehaven.vn");
        if (renter3 == null)
        {
            renter3 = new AppUser
            {
                UserName = "renter3@luxehaven.vn", Email = "renter3@luxehaven.vn",
                FullName = "Trần Văn Khoa", Phone = "093 456 7890",
                EmailConfirmed = true
            };
            var rc = await _userMgr.CreateAsync(renter3, "Renter@123");
            if (rc.Succeeded)
            {
                await _userMgr.AddToRoleAsync(renter3, SeedData.RoleTenant);
                log.Add("Created tenant renter3@luxehaven.vn");
            }
        }
        if (renter == null || renter2 == null || renter3 == null)
            return Content("Missing base tenants; run app once to trigger SeedData first.", "text/plain");

        // ── Apartments (by UnitCode) ─────────────────────────────────────────
        var apts = await _db.Apartments
            .Where(a => new[] { "B-102", "A-PH1", "B-203" }.Contains(a.UnitCode))
            .ToListAsync();
        var apt2 = apts.FirstOrDefault(a => a.UnitCode == "B-102");
        var apt3 = apts.FirstOrDefault(a => a.UnitCode == "A-PH1");
        var apt4 = apts.FirstOrDefault(a => a.UnitCode == "B-203");
        if (apt2 == null || apt3 == null || apt4 == null)
            return Content("Required demo apartments not found.", "text/plain");

        // ── Lease B on apt2 (paid + partial) ─────────────────────────────────
        if (!await _db.Leases.AnyAsync(l => l.ApartmentId == apt2.Id && l.Status != LeaseStatus.Terminated))
        {
            var leaseB = new Lease
            {
                LeaseNumber = $"LEASE-{lastMonthDate:yyyyMM}-0002",
                ApartmentId = apt2.Id,
                PrimaryTenantId = renter2.Id,
                StartDate = lastMonthDate.AddMonths(-2),
                EndDate = lastMonthDate.AddMonths(-2).AddYears(1),
                MonthlyRent = apt2.Price,
                Deposit = apt2.DefaultDeposit ?? apt2.Price * 2,
                DepositHeld = apt2.DefaultDeposit ?? apt2.Price * 2,
                BillingDay = 1, LateFeePercent = 5, LateFeeAfterDays = 7,
                Status = LeaseStatus.Active,
                ActivatedAt = lastMonthDate.AddMonths(-2),
                Notes = "Demo scenario B – paid + partial"
            };
            _db.Leases.Add(leaseB);
            apt2.Occupancy = ApartmentOccupancy.Occupied;
            await _db.SaveChangesAsync();
            log.Add($"Created lease {leaseB.LeaseNumber} on {apt2.UnitCode}");

            // Last month invoice: fully paid via cash
            var invB1 = await CreateMonthlyInvoiceAsync(leaseB, lastMonth, lastMonthDate, includeUtilities: true);
            await RecordSuccessfulPaymentAsync(invB1, invB1.Total, PaymentMethod.Cash, "Tiền mặt nhận đủ");
            log.Add($"Lease B – invoice {invB1.InvoiceNumber} paid in full ({invB1.Total:N0})");

            // This month: partial payment
            var invB2 = await CreateMonthlyInvoiceAsync(leaseB, thisMonth, now, includeUtilities: true);
            await RecordSuccessfulPaymentAsync(invB2, invB2.Total / 2m, PaymentMethod.BankTransfer, "Trả trước 50%");
            log.Add($"Lease B – invoice {invB2.InvoiceNumber} partially paid 50% (balance {invB2.Balance:N0})");
        }

        // ── Lease C on apt3 (deposit + overdue + pending approval) ───────────
        if (!await _db.Leases.AnyAsync(l => l.ApartmentId == apt3.Id && l.Status != LeaseStatus.Terminated))
        {
            var startC = lastMonthDate.AddMonths(-1);
            var leaseC = new Lease
            {
                LeaseNumber = $"LEASE-{startC:yyyyMM}-0003",
                ApartmentId = apt3.Id,
                PrimaryTenantId = renter3.Id,
                StartDate = startC, EndDate = startC.AddYears(1),
                MonthlyRent = apt3.Price,
                Deposit = apt3.DefaultDeposit ?? apt3.Price * 2,
                DepositHeld = 0m, // will be set via deposit invoice payment
                BillingDay = 1, LateFeePercent = 5, LateFeeAfterDays = 7,
                Status = LeaseStatus.Active,
                ActivatedAt = startC,
                Notes = "Demo scenario C – overdue + pending"
            };
            _db.Leases.Add(leaseC);
            apt3.Occupancy = ApartmentOccupancy.Occupied;
            await _db.SaveChangesAsync();
            log.Add($"Created lease {leaseC.LeaseNumber} on {apt3.UnitCode}");

            // Deposit invoice → fully paid (drives DepositHeld via DepositLedger)
            var depBillingMonth = startC.Year * 100 + startC.Month;
            var depInv = new Invoice
            {
                InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, depBillingMonth),
                LeaseId = leaseC.Id,
                Kind = InvoiceKind.Deposit, BillingMonth = depBillingMonth, IsRecurring = false,
                IssueDate = startC, DueDate = startC.AddDays(3),
                SubTotal = leaseC.Deposit, Total = leaseC.Deposit, Balance = leaseC.Deposit,
                Status = InvoiceStatus.Issued, Currency = "VND",
                Items = { new InvoiceItem { Description = "Tiền cọc 2 tháng", Quantity = 1m, UnitPrice = leaseC.Deposit, LineTotal = leaseC.Deposit, SortOrder = 0 } }
            };
            _db.Invoices.Add(depInv);
            await _db.SaveChangesAsync();
            await RecordSuccessfulPaymentAsync(depInv, depInv.Total, PaymentMethod.BankTransfer, "Cọc qua chuyển khoản");
            log.Add($"Lease C – deposit {depInv.InvoiceNumber} paid ({depInv.Total:N0})");

            // Last month rent → overdue
            var invC1 = await CreateMonthlyInvoiceAsync(leaseC, lastMonth, lastMonthDate, includeUtilities: true, forceOverdue: true);
            log.Add($"Lease C – invoice {invC1.InvoiceNumber} marked Overdue (balance {invC1.Balance:N0})");

            // This month → has a Pending payment submitted by tenant (awaiting admin approval)
            var invC2 = await CreateMonthlyInvoiceAsync(leaseC, thisMonth, now, includeUtilities: true);
            var pendingPmt = new Payment
            {
                PaymentNumber = await BillingNumberGenerator.NextPaymentNumberAsync(_db),
                InvoiceId = invC2.Id,
                Amount = invC2.Balance,
                Method = PaymentMethod.BankTransfer,
                Status = PaymentStatus.Pending,
                TransactionRef = "VCB-20260520-998877",
                Note = "Khách thuê gửi qua VCB",
                Currency = "VND"
            };
            _db.Payments.Add(pendingPmt);
            await _db.SaveChangesAsync();
            log.Add($"Lease C – invoice {invC2.InvoiceNumber} has Pending payment {pendingPmt.PaymentNumber}");
        }

        // ── Lease D on apt4 (Pending lease, no invoices yet) ─────────────────
        if (!await _db.Leases.AnyAsync(l => l.ApartmentId == apt4.Id && l.Status != LeaseStatus.Terminated))
        {
            var leaseD = new Lease
            {
                LeaseNumber = $"LEASE-{now:yyyyMM}-0004",
                ApartmentId = apt4.Id,
                PrimaryTenantId = renter.Id,
                StartDate = now.AddDays(7), EndDate = now.AddDays(7).AddMonths(6),
                MonthlyRent = apt4.Price,
                Deposit = apt4.DefaultDeposit ?? apt4.Price * 2,
                DepositHeld = 0m,
                BillingDay = 1, LateFeePercent = 5, LateFeeAfterDays = 7,
                Status = LeaseStatus.Pending,
                Notes = "Demo scenario D – pending move-in"
            };
            _db.Leases.Add(leaseD);
            apt4.Occupancy = ApartmentOccupancy.Reserved;
            await _db.SaveChangesAsync();
            log.Add($"Created pending lease {leaseD.LeaseNumber} on {apt4.UnitCode}");
        }

        // ── Viewings (mixed statuses across available units) ─────────────────
        if (!await _db.ViewingAppointments.AnyAsync(v => v.Note != null && v.Note.StartsWith("[demo]")))
        {
            var avail = await _db.Apartments
                .Where(a => a.Occupancy == ApartmentOccupancy.Available && a.Status == ListingStatus.Active)
                .OrderBy(a => a.Id).Take(3).ToListAsync();
            if (avail.Count >= 3)
            {
                _db.ViewingAppointments.AddRange(
                    new ViewingAppointment
                    {
                        ApartmentId = avail[0].Id, UserId = renter.Id,
                        ContactName = renter.FullName, ContactPhone = renter.Phone ?? "0900000001",
                        ContactEmail = renter.Email,
                        ScheduledDate = now.AddDays(1), SlotHour = 10,
                        Status = ViewingStatus.Pending,
                        Note = "[demo] khách quan tâm view sông"
                    },
                    new ViewingAppointment
                    {
                        ApartmentId = avail[1].Id, UserId = renter2.Id,
                        ContactName = renter2.FullName, ContactPhone = renter2.Phone ?? "0900000002",
                        ContactEmail = renter2.Email,
                        ScheduledDate = now.AddDays(2), SlotHour = 14,
                        Status = ViewingStatus.Confirmed,
                        ConfirmedAt = DateTime.UtcNow, ConfirmedBy = "system",
                        Note = "[demo] confirmed slot 14h"
                    },
                    new ViewingAppointment
                    {
                        ApartmentId = avail[2].Id, UserId = renter3.Id,
                        ContactName = renter3.FullName, ContactPhone = renter3.Phone ?? "0900000003",
                        ContactEmail = renter3.Email,
                        ScheduledDate = now.AddDays(-3), SlotHour = 9,
                        Status = ViewingStatus.Completed,
                        Note = "[demo] đã xem, không ký"
                    },
                    new ViewingAppointment
                    {
                        ApartmentId = avail[0].Id,
                        ContactName = "Khách lạ", ContactPhone = "0911222333",
                        ScheduledDate = now.AddDays(-5), SlotHour = 15,
                        Status = ViewingStatus.Cancelled,
                        CancelledAt = DateTime.UtcNow, CancelledBy = "system",
                        CancellationReason = "Khách báo bận",
                        Note = "[demo] cancelled"
                    }
                );
                await _db.SaveChangesAsync();
                log.Add($"Created 4 viewing appointments");
            }
            else
            {
                log.Add("Skipped viewings — not enough available apartments");
            }
        }

        log.Add("DONE.");
        return Content(string.Join("\n", log), "text/plain");
    }

    // Same logic as InvoiceGenerator.GenerateMonthlyInvoiceAsync but lets us back-date the issue/due
    // and forcibly mark Overdue for the demo "last month unpaid" lane.
    private async Task<Invoice> CreateMonthlyInvoiceAsync(Lease lease, int billingMonth, DateTime issueDate, bool includeUtilities, bool forceOverdue = false)
    {
        var existing = await _db.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i =>
            i.LeaseId == lease.Id && i.BillingMonth == billingMonth && i.Kind == InvoiceKind.MonthlyRent);
        if (existing != null) return existing;

        var apt = await _db.Apartments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == lease.ApartmentId);
        var unitLabel = apt?.UnitCode ?? apt?.Title ?? $"Lease#{lease.Id}";

        var dueDate = new DateTime(issueDate.Year, issueDate.Month, Math.Min(lease.BillingDay + 7, 28));
        var items = new List<InvoiceItem>
        {
            new() { Description = $"Tiền thuê {unitLabel} – {issueDate:MM/yyyy}", Quantity = 1m, UnitPrice = lease.MonthlyRent, LineTotal = lease.MonthlyRent, SortOrder = 0 }
        };

        if (includeUtilities)
        {
            // Light utilities: 80 kWh + 5 m³ + internet + service
            items.Add(new InvoiceItem { Description = "Điện (kWh)", Quantity = 80m, UnitPrice = 3500m, LineTotal = 280000m, SortOrder = 1 });
            items.Add(new InvoiceItem { Description = "Nước (m³)", Quantity = 5m, UnitPrice = 25000m, LineTotal = 125000m, SortOrder = 2 });
            items.Add(new InvoiceItem { Description = "Internet", Quantity = 1m, UnitPrice = 200000m, LineTotal = 200000m, SortOrder = 3 });
            items.Add(new InvoiceItem { Description = "Phí dịch vụ", Quantity = 1m, UnitPrice = 500000m, LineTotal = 500000m, SortOrder = 4 });
        }

        var subTotal = items.Sum(x => x.LineTotal);
        var inv = new Invoice
        {
            InvoiceNumber = await BillingNumberGenerator.NextInvoiceNumberAsync(_db, billingMonth),
            LeaseId = lease.Id,
            Kind = InvoiceKind.MonthlyRent, BillingMonth = billingMonth, IsRecurring = true,
            IssueDate = issueDate, DueDate = dueDate,
            SubTotal = subTotal, Total = subTotal, Balance = subTotal,
            Status = forceOverdue ? InvoiceStatus.Overdue : InvoiceStatus.Issued,
            Currency = "VND", Items = items
        };
        _db.Invoices.Add(inv);
        await _db.SaveChangesAsync();
        return inv;
    }

    private async Task RecordSuccessfulPaymentAsync(Invoice inv, decimal amount, PaymentMethod method, string note)
    {
        var pmt = new Payment
        {
            PaymentNumber = await BillingNumberGenerator.NextPaymentNumberAsync(_db),
            InvoiceId = inv.Id, Amount = amount, Method = method,
            Status = PaymentStatus.Succeeded, PaidAt = DateTime.UtcNow,
            Note = note, Currency = inv.Currency
        };
        _db.Payments.Add(pmt);

        inv.AmountPaid += amount;
        inv.Balance = Math.Max(0, inv.Total - inv.AmountPaid);
        inv.Status = inv.Balance == 0 ? InvoiceStatus.Paid
                    : inv.AmountPaid > 0 ? InvoiceStatus.PartiallyPaid
                    : inv.Status;

        await DepositLedger.OnPaymentAppliedAsync(_db, inv, amount, null);
        await _db.SaveChangesAsync();
    }
}
