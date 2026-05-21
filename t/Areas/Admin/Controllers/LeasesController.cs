using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Data;
using t.Infrastructure.Billing;
using t.Infrastructure.Localization;
using t.Infrastructure.Pdf;
using t.Infrastructure.Time;
using t.Models.Entities;

namespace t.Areas.Admin.Controllers;

public class LeasesController : AdminBaseController
{
    private readonly UserManager<AppUser> _userManager;
    private readonly InvoiceGenerator _invoiceGenerator;
    private readonly LeaseContractPdfGenerator _pdfGen;

    private static readonly LeaseStatus[] BlockingStatuses =
        { LeaseStatus.Pending, LeaseStatus.Active, LeaseStatus.Renewing };

    public LeasesController(AppDbContext db, UserManager<AppUser> userManager, InvoiceGenerator invoiceGenerator, LeaseContractPdfGenerator pdfGen) : base(db)
    {
        _userManager = userManager;
        _invoiceGenerator = invoiceGenerator;
        _pdfGen = pdfGen;
    }

    public async Task<IActionResult> ContractPdf(int id)
    {
        var lease = await Db.Leases
            .Include(l => l.PrimaryTenant)
            .Include(l => l.Apartment).ThenInclude(a => a.Building)
            .Include(l => l.AdditionalTenants).ThenInclude(t => t.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lease == null) return NotFound();
        var bytes = _pdfGen.Generate(lease);
        return File(bytes, "application/pdf", $"{lease.LeaseNumber}.pdf");
    }

    public async Task<IActionResult> Index(string? q, LeaseStatus? status, int? buildingId, int page = 1)
    {
        SetActiveNav("leases");
        SetBreadcrumb(("Hợp đồng", null));

        const int pageSize = 20;
        var query = Db.Leases
            .Include(l => l.PrimaryTenant)
            .Include(l => l.Apartment).ThenInclude(a => a.Building)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(l => l.LeaseNumber.Contains(q) ||
                                     l.PrimaryTenant.FullName.Contains(q) ||
                                     l.PrimaryTenant.Email!.Contains(q) ||
                                     l.Apartment.Title.Contains(q));
        if (status.HasValue) query = query.Where(l => l.Status == status.Value);
        if (buildingId.HasValue) query = query.Where(l => l.Apartment.BuildingId == buildingId.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new LeaseListVm
            {
                Id = l.Id, LeaseNumber = l.LeaseNumber,
                TenantName = l.PrimaryTenant.FullName,
                TenantEmail = l.PrimaryTenant.Email!,
                UnitTitle = l.Apartment.Title,
                ApartmentId = l.ApartmentId,
                BuildingName = l.Apartment.Building != null ? l.Apartment.Building.Name : null,
                StartDate = l.StartDate, EndDate = l.EndDate,
                MonthlyRent = l.MonthlyRent, Deposit = l.Deposit,
                Status = l.Status
            }).ToListAsync();

        ViewBag.Q = q; ViewBag.Status = status; ViewBag.BuildingId = buildingId;
        ViewBag.Buildings = await Db.Buildings.OrderBy(b => b.Name).ToListAsync();
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(Index), new { q, status, buildingId, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        SetActiveNav("leases");
        var lease = await Db.Leases
            .Include(l => l.PrimaryTenant)
            .Include(l => l.Apartment).ThenInclude(a => a.Building)
            .Include(l => l.Apartment).ThenInclude(a => a.Floor)
            .Include(l => l.AdditionalTenants).ThenInclude(t => t.Tenant)
            .Include(l => l.Invoices).ThenInclude(i => i.Items)
            .Include(l => l.UtilityReadings).ThenInclude(r => r.UtilityType)
            .Include(l => l.Inspections)
            .Include(l => l.DepositTransactions)
            .Include(l => l.MaintenanceRequests)
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lease == null) return NotFound();
        SetBreadcrumb(("Hợp đồng", Url.Action(nameof(Index))), (lease.LeaseNumber, null));
        return View(lease);
    }

    // ── Inspections ──
    public async Task<IActionResult> CreateInspection(int id, InspectionType? type)
    {
        SetActiveNav("leases");
        var lease = await Db.Leases.FindAsync(id);
        if (lease == null) return NotFound();
        SetBreadcrumb(("Hợp đồng", Url.Action(nameof(Index))), (lease.LeaseNumber, Url.Action(nameof(Details), new { id })), ("Inspection", null));

        ViewBag.Lease = lease;
        return View("EditInspection", new InspectionEditVm
        {
            LeaseId = id,
            Type = type ?? InspectionType.MoveIn,
            InspectedAt = VnTime.Today
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditInspection(InspectionEditVm input)
    {
        var lease = await Db.Leases.Include(l => l.Inspections).FirstOrDefaultAsync(l => l.Id == input.LeaseId);
        if (lease == null) return NotFound();

        LeaseInspection insp;
        bool isNew = input.Id == 0;
        if (isNew)
        {
            insp = new LeaseInspection { LeaseId = lease.Id };
            Db.LeaseInspections.Add(insp);
        }
        else
        {
            insp = await Db.LeaseInspections.FirstOrDefaultAsync(x => x.Id == input.Id) ?? throw new InvalidOperationException();
        }

        insp.Type = input.Type;
        insp.InspectedAt = input.InspectedAt;
        insp.InspectorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        insp.OverallCondition = input.OverallCondition;
        insp.Summary = input.Summary;
        insp.DamageNotes = input.DamageNotes;
        insp.PhotoUrls = input.PhotoUrls;
        insp.DepositDeduction = input.DepositDeduction;
        insp.TenantSigned = input.TenantSigned;

        await Db.SaveChangesAsync();

        if (isNew && input.Type == InspectionType.MoveOut && input.DepositDeduction > 0)
        {
            var deduct = Math.Min(input.DepositDeduction, lease.DepositHeld);
            if (deduct > 0)
            {
                Db.DepositTransactions.Add(new DepositTransaction
                {
                    LeaseId = lease.Id,
                    Type = DepositTransactionType.Deduction,
                    Amount = deduct,
                    Reason = $"Trừ cọc theo biên bản move-out: {input.Summary ?? input.DamageNotes ?? string.Empty}",
                    RelatedInspectionId = insp.Id,
                    RecordedAt = DateTime.UtcNow,
                    RecordedBy = insp.InspectorId
                });
                lease.DepositHeld -= deduct;
                await Db.SaveChangesAsync();
            }
        }

        TempData["Success"] = isNew ? "Đã tạo biên bản." : "Đã cập nhật biên bản.";
        return RedirectToAction(nameof(Details), new { id = lease.Id });
    }

    // ── Deposit transaction (manual) ──
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordDepositTransaction(int id, DepositTransactionType type, decimal amount, string? reason)
    {
        var lease = await Db.Leases.FirstOrDefaultAsync(l => l.Id == id);
        if (lease == null) return NotFound();
        if (amount <= 0) { TempData["Danger"] = "Số tiền phải > 0."; return RedirectToAction(nameof(Details), new { id }); }

        switch (type)
        {
            case DepositTransactionType.Hold:
                lease.DepositHeld += amount;
                break;
            case DepositTransactionType.Refund:
                if (amount > lease.DepositHeld) { TempData["Danger"] = "Vượt quá cọc đang giữ."; return RedirectToAction(nameof(Details), new { id }); }
                lease.DepositHeld -= amount;
                lease.DepositRefunded += amount;
                break;
            case DepositTransactionType.Deduction:
            case DepositTransactionType.Forfeit:
                if (amount > lease.DepositHeld) { TempData["Danger"] = "Vượt quá cọc đang giữ."; return RedirectToAction(nameof(Details), new { id }); }
                lease.DepositHeld -= amount;
                break;
            case DepositTransactionType.Adjustment:
                // signed via reason; for simplicity treat as positive add
                lease.DepositHeld += amount;
                break;
        }

        Db.DepositTransactions.Add(new DepositTransaction
        {
            LeaseId = id, Type = type, Amount = amount,
            Reason = reason ?? type.ToString(),
            RecordedAt = DateTime.UtcNow,
            RecordedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
        });
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã ghi sổ cọc.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Create(int? apartmentId)
    {
        SetActiveNav("leases");
        SetBreadcrumb(("Hợp đồng", Url.Action(nameof(Index))), ("Tạo mới", null));

        var vm = new LeaseEditVm
        {
            ApartmentId = apartmentId ?? 0,
            BillingDay = 1,
            StartDate = VnTime.Today,
            EndDate = VnTime.Today.AddYears(1)
        };
        if (apartmentId.HasValue)
        {
            var apt = await Db.Apartments.FindAsync(apartmentId.Value);
            if (apt != null)
            {
                vm.MonthlyRent = apt.Price;
                vm.Deposit = apt.DefaultDeposit ?? apt.Price * 2;
            }
        }
        await PopulateLookupsAsync(vm);
        return View("Edit", vm);
    }

    public async Task<IActionResult> Edit(int id)
    {
        SetActiveNav("leases");
        var lease = await Db.Leases.Include(l => l.AdditionalTenants).FirstOrDefaultAsync(l => l.Id == id);
        if (lease == null) return NotFound();
        SetBreadcrumb(("Hợp đồng", Url.Action(nameof(Index))), (lease.LeaseNumber, null));

        var vm = new LeaseEditVm
        {
            Id = lease.Id, LeaseNumber = lease.LeaseNumber,
            ApartmentId = lease.ApartmentId, PrimaryTenantId = lease.PrimaryTenantId,
            StartDate = lease.StartDate, EndDate = lease.EndDate,
            MonthlyRent = lease.MonthlyRent, Deposit = lease.Deposit,
            BillingDay = lease.BillingDay, LateFeePercent = lease.LateFeePercent, LateFeeAfterDays = lease.LateFeeAfterDays,
            ContractUrl = lease.ContractUrl, Notes = lease.Notes,
            Status = lease.Status,
            CoTenantIds = lease.AdditionalTenants.Select(t => t.TenantId).ToList()
        };
        await PopulateLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LeaseEditVm input)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLookupsAsync(input);
            return View(input);
        }
        if (input.EndDate <= input.StartDate)
        {
            ModelState.AddModelError(nameof(input.EndDate), "Ngày kết thúc phải sau ngày bắt đầu.");
            await PopulateLookupsAsync(input);
            return View(input);
        }
        if (input.ApartmentId == 0)
        {
            ModelState.AddModelError(nameof(input.ApartmentId), "Chọn căn hộ.");
            await PopulateLookupsAsync(input);
            return View(input);
        }
        if (!await Db.Users.AnyAsync(u => u.Id == input.PrimaryTenantId))
        {
            ModelState.AddModelError(nameof(input.PrimaryTenantId), "Khách thuê chính không tồn tại.");
            await PopulateLookupsAsync(input);
            return View(input);
        }
        if (!await Db.Apartments.AnyAsync(a => a.Id == input.ApartmentId))
        {
            ModelState.AddModelError(nameof(input.ApartmentId), "Căn hộ không tồn tại.");
            await PopulateLookupsAsync(input);
            return View(input);
        }

        bool isNew = input.Id == 0;

        // Block overlapping leases on same apartment (only when this lease itself blocks the apartment)
        if (BlockingStatuses.Contains(input.Status))
        {
            var hasOverlap = await Db.Leases.AnyAsync(l =>
                l.Id != input.Id &&
                l.ApartmentId == input.ApartmentId &&
                BlockingStatuses.Contains(l.Status) &&
                l.StartDate < input.EndDate && input.StartDate < l.EndDate);
            if (hasOverlap)
            {
                ModelState.AddModelError(nameof(input.ApartmentId), "Căn hộ đã có hợp đồng khác trong khoảng thời gian này.");
                await PopulateLookupsAsync(input);
                return View(input);
            }
        }

        Lease lease;
        if (isNew)
        {
            lease = new Lease();
            Db.Leases.Add(lease);
        }
        else
        {
            lease = await Db.Leases.Include(l => l.AdditionalTenants).FirstOrDefaultAsync(l => l.Id == input.Id) ?? throw new InvalidOperationException();
        }

        lease.LeaseNumber = string.IsNullOrWhiteSpace(input.LeaseNumber)
            ? await GenerateLeaseNumberAsync()
            : input.LeaseNumber.Trim();
        lease.ApartmentId = input.ApartmentId;
        lease.PrimaryTenantId = input.PrimaryTenantId;
        lease.StartDate = input.StartDate;
        lease.EndDate = input.EndDate;
        lease.MonthlyRent = input.MonthlyRent;

        // Deposit: stored as the *agreed* amount. Actual held cash is tracked via
        // DepositTransactions, which are created only when a payment on the Deposit
        // invoice succeeds (see DepositLedger). After activation Deposit is locked.
        if (isNew || lease.ActivatedAt == null)
        {
            lease.Deposit = input.Deposit;
        }
        else if (input.Deposit != lease.Deposit)
        {
            TempData["Warning"] = $"Tiền cọc gốc đã khoá sau khi kích hoạt — dùng \"Ghi nhận thủ công\" để điều chỉnh sổ cọc (giá trị {input.Deposit:N0} bị bỏ qua).";
        }

        lease.BillingDay = input.BillingDay;
        lease.LateFeePercent = input.LateFeePercent;
        lease.LateFeeAfterDays = input.LateFeeAfterDays;
        lease.ContractUrl = input.ContractUrl;
        lease.Notes = input.Notes;

        // Status transitions: not all moves are allowed via form edit.
        ApplyStatusTransition(lease, input.Status);

        if (lease.Status == LeaseStatus.Active && lease.ActivatedAt == null)
            lease.ActivatedAt = DateTime.UtcNow;

        var existing = lease.AdditionalTenants?.ToList() ?? new();
        foreach (var rm in existing.Where(e => !input.CoTenantIds.Contains(e.TenantId))) Db.LeaseTenants.Remove(rm);
        foreach (var addId in input.CoTenantIds.Where(id => id != input.PrimaryTenantId && !existing.Any(e => e.TenantId == id)))
            Db.LeaseTenants.Add(new LeaseTenant { Lease = lease, TenantId = addId });

        await SyncApartmentOccupancyAsync(input.ApartmentId, lease);

        try
        {
            await Db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintError(ex))
        {
            // LeaseNumber collided (race). Retry once with a fresh number.
            lease.LeaseNumber = await GenerateLeaseNumberAsync();
            await Db.SaveChangesAsync();
        }

        TempData["Success"] = isNew ? "Đã tạo hợp đồng." : "Đã cập nhật hợp đồng.";
        return RedirectToAction(nameof(Details), new { id = lease.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(int id)
    {
        var l = await Db.Leases.Include(x => x.Apartment).FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound();
        if (l.Status != LeaseStatus.Pending)
        {
            TempData["Danger"] = $"Chỉ kích hoạt được hợp đồng đang Chờ duyệt (hiện tại: {l.Status.Vi()}).";
            return RedirectToAction(nameof(Details), new { id });
        }

        var hasActiveOnUnit = await Db.Leases.AnyAsync(x =>
            x.Id != l.Id && x.ApartmentId == l.ApartmentId && x.Status == LeaseStatus.Active);
        if (hasActiveOnUnit)
        {
            TempData["Danger"] = "Căn hộ đang có hợp đồng Active khác — không thể kích hoạt thêm.";
            return RedirectToAction(nameof(Details), new { id });
        }

        l.Status = LeaseStatus.Active;
        l.ActivatedAt = DateTime.UtcNow;
        l.Apartment.Occupancy = ApartmentOccupancy.Occupied;
        await Db.SaveChangesAsync();

        // Auto-create a Deposit invoice so the tenant can track + pay their deposit officially.
        if (l.Deposit > 0)
            await _invoiceGenerator.GenerateDepositInvoiceAsync(l.Id);

        TempData["Success"] = "Đã kích hoạt hợp đồng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(int id, DateTime terminatedAt, string? reason, decimal refundAmount, bool forceWithUnpaid = false, bool applyDepositToUnpaid = false)
    {
        var l = await Db.Leases
            .Include(x => x.Apartment)
            .Include(x => x.Invoices)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound();

        if (l.Status is not (LeaseStatus.Active or LeaseStatus.Pending or LeaseStatus.Renewing or LeaseStatus.Expired))
        {
            TempData["Danger"] = $"Không thể chấm dứt hợp đồng ở trạng thái {l.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Exclude Deposit invoices from "cấn cọc" target — applying deposit on a deposit
        // invoice would loop through DepositLedger (held += amount when payment applied).
        var unpaidInvoices = l.Invoices
            .Where(i => i.Kind != InvoiceKind.Deposit &&
                        i.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid or InvoiceStatus.Overdue)
            .OrderBy(i => i.DueDate)
            .ToList();

        if (unpaidInvoices.Count > 0 && !forceWithUnpaid && !applyDepositToUnpaid)
        {
            TempData["Danger"] = $"Còn {unpaidInvoices.Count} hoá đơn chưa thanh toán. Chọn \"Cấn cọc\" để trừ vào cọc, hoặc \"Chấm dứt dù còn nợ\".";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (refundAmount < 0)
        {
            TempData["Danger"] = "Số tiền hoàn cọc không được âm.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Compute available deposit budget if applying to unpaid.
        var totalUnpaid = unpaidInvoices.Sum(i => i.Balance);
        if (applyDepositToUnpaid && totalUnpaid > 0)
        {
            var availableAfterRefund = l.DepositHeld - refundAmount;
            if (availableAfterRefund < 0)
            {
                TempData["Danger"] = "Hoàn cọc vượt quá cọc đang giữ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var remainingBudget = availableAfterRefund;
            foreach (var inv in unpaidInvoices)
            {
                if (remainingBudget <= 0) break;
                var applyAmount = Math.Min(inv.Balance, remainingBudget);

                var payment = new Payment
                {
                    PaymentNumber = await BillingNumberGenerator.NextPaymentNumberAsync(Db),
                    InvoiceId = inv.Id,
                    Amount = applyAmount,
                    Method = PaymentMethod.Cash,
                    Status = PaymentStatus.Succeeded,
                    Note = "Cấn cọc khi chấm dứt hợp đồng",
                    PaidAt = DateTime.UtcNow,
                    Currency = inv.Currency
                };
                Db.Payments.Add(payment);

                inv.AmountPaid += applyAmount;
                inv.Balance = Math.Max(0, inv.Total - inv.AmountPaid);
                inv.Status = inv.Balance == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

                Db.DepositTransactions.Add(new DepositTransaction
                {
                    LeaseId = l.Id,
                    Type = DepositTransactionType.Deduction,
                    Amount = applyAmount,
                    Reason = $"Cấn cọc vào hoá đơn {inv.InvoiceNumber}",
                    RecordedAt = DateTime.UtcNow,
                    RecordedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
                });
                l.DepositHeld -= applyAmount;
                remainingBudget -= applyAmount;
            }
        }

        if (refundAmount > l.DepositHeld)
        {
            TempData["Danger"] = $"Hoàn cọc {refundAmount:N0} vượt quá cọc còn lại sau khi cấn ({l.DepositHeld:N0}).";
            return RedirectToAction(nameof(Details), new { id });
        }

        l.Status = LeaseStatus.Terminated;
        l.TerminatedAt = terminatedAt;
        l.TerminationReason = reason;
        if (refundAmount > 0)
        {
            l.DepositRefunded += refundAmount;
            l.DepositHeld -= refundAmount;
            Db.DepositTransactions.Add(new DepositTransaction
            {
                LeaseId = l.Id, Type = DepositTransactionType.Refund,
                Amount = refundAmount,
                Reason = "Hoàn cọc khi chấm dứt hợp đồng" + (string.IsNullOrEmpty(reason) ? "" : ": " + reason),
                RecordedAt = DateTime.UtcNow,
                RecordedBy = User.FindFirstValue(ClaimTypes.NameIdentifier)
            });
        }

        // Cancel still-Draft invoices (Issued/Paid/Overdue are kept for accounting trail).
        foreach (var inv in l.Invoices.Where(i => i.Status == InvoiceStatus.Draft))
            inv.Status = InvoiceStatus.Cancelled;

        await SyncApartmentOccupancyAsync(l.ApartmentId, l);
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã chấm dứt hợp đồng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Renew(int id, DateTime newEndDate, decimal? newMonthlyRent)
    {
        var l = await Db.Leases.FirstOrDefaultAsync(x => x.Id == id);
        if (l == null) return NotFound();
        if (l.Status is not (LeaseStatus.Active or LeaseStatus.Renewing or LeaseStatus.Expired))
        {
            TempData["Danger"] = $"Không gia hạn được hợp đồng ở trạng thái {l.Status.Vi()}.";
            return RedirectToAction(nameof(Details), new { id });
        }
        if (newEndDate <= l.EndDate)
        {
            TempData["Danger"] = "Ngày kết thúc mới phải sau ngày kết thúc hiện tại.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Block if another lease already booked the apartment after current EndDate
        var conflict = await Db.Leases.AnyAsync(x =>
            x.Id != l.Id && x.ApartmentId == l.ApartmentId &&
            BlockingStatuses.Contains(x.Status) &&
            x.StartDate < newEndDate && l.EndDate < x.EndDate);
        if (conflict)
        {
            TempData["Danger"] = "Căn hộ đã có hợp đồng khác trong khoảng gia hạn.";
            return RedirectToAction(nameof(Details), new { id });
        }

        l.EndDate = newEndDate;
        if (newMonthlyRent.HasValue && newMonthlyRent.Value > 0) l.MonthlyRent = newMonthlyRent.Value;
        l.Status = LeaseStatus.Active;
        await Db.SaveChangesAsync();
        TempData["Success"] = "Đã gia hạn hợp đồng.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateInvoice(int id)
    {
        var result = await _invoiceGenerator.GenerateMonthlyInvoiceAsync(id, VnTime.Now);
        TempData[result.Success ? "Success" : "Danger"] = result.Message;
        return RedirectToAction(nameof(Details), new { id });
    }

    // ── Helpers ──

    /// <summary>
    /// Generates a unique lease number that doesn't collide with existing rows.
    /// Pattern: LEASE-yyyyMM-#### where #### is the next sequence within the month.
    /// </summary>
    private async Task<string> GenerateLeaseNumberAsync()
    {
        var prefix = $"LEASE-{VnTime.Now:yyyyMM}-";
        var lastSeq = await Db.Leases
            .Where(l => l.LeaseNumber.StartsWith(prefix))
            .Select(l => l.LeaseNumber)
            .ToListAsync();
        var maxSeq = lastSeq
            .Select(s => int.TryParse(s.AsSpan(prefix.Length), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return prefix + (maxSeq + 1).ToString("0000");
    }

    /// <summary>
    /// Only the transitions allowed via form edit are applied. Stronger transitions
    /// (Activate, Terminate, Renew) must go through dedicated actions for auditability.
    /// </summary>
    private static void ApplyStatusTransition(Lease lease, LeaseStatus desired)
    {
        // New lease: any status that doesn't require a dedicated workflow is fine.
        if (lease.Id == 0)
        {
            lease.Status = desired switch
            {
                LeaseStatus.Pending or LeaseStatus.Active => desired,
                _ => LeaseStatus.Pending
            };
            return;
        }

        var current = lease.Status;
        if (current == desired) return;

        var allowed = current switch
        {
            LeaseStatus.Pending => new[] { LeaseStatus.Active },
            LeaseStatus.Active => new[] { LeaseStatus.Renewing, LeaseStatus.Expired },
            LeaseStatus.Renewing => new[] { LeaseStatus.Active, LeaseStatus.Expired },
            LeaseStatus.Expired => new[] { LeaseStatus.Renewing },
            _ => Array.Empty<LeaseStatus>()
        };
        if (allowed.Contains(desired)) lease.Status = desired;
        // else: silently keep current — UI should warn before submit
    }

    /// <summary>
    /// Recomputes apartment occupancy based on ALL leases on that unit, not just the
    /// one being edited. Prevents a freshly-created Pending lease from overwriting
    /// the Occupied state of an active lease.
    /// </summary>
    private async Task SyncApartmentOccupancyAsync(int apartmentId, Lease changed)
    {
        var apt = await Db.Apartments.FindAsync(apartmentId);
        if (apt == null) return;

        // Consider all leases on this unit, including the in-memory changed one
        // (which may not be persisted yet).
        var others = await Db.Leases
            .Where(l => l.ApartmentId == apartmentId && l.Id != changed.Id)
            .Select(l => new { l.Status })
            .ToListAsync();

        var statuses = others.Select(o => o.Status).Append(changed.Status).ToList();
        apt.Occupancy = statuses.Any(s => s == LeaseStatus.Active) ? ApartmentOccupancy.Occupied
                       : statuses.Any(s => s == LeaseStatus.Pending || s == LeaseStatus.Renewing) ? ApartmentOccupancy.Reserved
                       : ApartmentOccupancy.Available;
    }

    private static bool IsUniqueConstraintError(DbUpdateException ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);
    }

    private async Task PopulateLookupsAsync(LeaseEditVm vm)
    {
        vm.Apartments = await Db.Apartments.Include(a => a.Building).OrderBy(a => a.Title).ToListAsync();
        var tenants = await _userManager.GetUsersInRoleAsync("Tenant");
        vm.Tenants = tenants
            .OrderBy(u => u.FullName)
            .Select(u => new HostOption { Id = u.Id, Label = u.FullName + " · " + u.Email })
            .ToList();
    }
}
