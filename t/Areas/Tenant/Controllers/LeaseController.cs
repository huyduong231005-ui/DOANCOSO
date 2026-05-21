using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using t.Areas.Admin.Models;
using t.Areas.Tenant.Models;
using t.Data;
using t.Infrastructure.Pdf;
using t.Models.Entities;

namespace t.Areas.Tenant.Controllers;

public class LeaseController : TenantBaseController
{
    private readonly LeaseContractPdfGenerator _pdfGen;

    public LeaseController(AppDbContext db, LeaseContractPdfGenerator pdfGen) : base(db)
    {
        _pdfGen = pdfGen;
    }

    public async Task<IActionResult> Index(int? id)
    {
        SetActiveNav("lease");

        var baseQuery = MyLeasesQuery()
            .Include(l => l.Apartment).ThenInclude(a => a.Building)
            .Include(l => l.Apartment).ThenInclude(a => a.Floor)
            .Include(l => l.Apartment).ThenInclude(a => a.ApartmentAmenities).ThenInclude(aa => aa.Amenity)
            .Include(l => l.PrimaryTenant)
            .Include(l => l.AdditionalTenants).ThenInclude(t => t.Tenant)
            .Include(l => l.Inspections)
            .Include(l => l.DepositTransactions)
            .AsSplitQuery();

        Lease? lease;
        if (id.HasValue)
        {
            lease = await baseQuery.FirstOrDefaultAsync(l => l.Id == id.Value);
            if (lease == null) return NotFound();
        }
        else
        {
            lease = await baseQuery
                .OrderByDescending(l => l.Status == LeaseStatus.Active ? 1 : 0)
                .ThenByDescending(l => l.StartDate)
                .FirstOrDefaultAsync();
        }

        SetBreadcrumb(("Hợp đồng", null));
        ViewBag.History = await MyLeasesQuery()
            .OrderByDescending(l => l.Status == LeaseStatus.Active ? 1 : 0)
            .ThenByDescending(l => l.StartDate)
            .Select(l => new { l.Id, l.LeaseNumber, l.StartDate, l.EndDate, l.Status })
            .ToListAsync();
        ViewBag.CurrentId = lease?.Id;

        return View(lease);
    }

    public async Task<IActionResult> History(string? q, LeaseStatus? status, int page = 1)
    {
        SetActiveNav("lease");
        SetBreadcrumb(("Hợp đồng", Url.Action(nameof(Index))), ("Lịch sử", null));

        const int pageSize = 10;
        var uid = CurrentUserId;
        var query = MyLeasesQuery()
            .Include(l => l.Apartment).ThenInclude(a => a.Building)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var keyword = q.Trim();
            query = query.Where(l =>
                l.LeaseNumber.Contains(keyword) ||
                l.Apartment.Title.Contains(keyword) ||
                (l.Apartment.Building != null && l.Apartment.Building.Name.Contains(keyword)));
        }
        if (status.HasValue) query = query.Where(l => l.Status == status.Value);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(l => l.StartDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(l => new TenantLeaseHistoryRow
            {
                Id = l.Id,
                LeaseNumber = l.LeaseNumber,
                ApartmentTitle = l.Apartment.Title,
                BuildingName = l.Apartment.Building != null ? l.Apartment.Building.Name : null,
                StartDate = l.StartDate,
                EndDate = l.EndDate,
                MonthlyRent = l.MonthlyRent,
                Status = l.Status,
                IsCoTenant = l.PrimaryTenantId != uid
            })
            .ToListAsync();

        ViewBag.Q = q;
        ViewBag.Status = status;
        ViewBag.Pager = new PageInfo
        {
            Page = page, PageSize = pageSize, TotalCount = total,
            Url = pp => Url.Action(nameof(History), new { q, status, page = pp }) ?? "#"
        };
        return View(rows);
    }

    public async Task<IActionResult> ContractPdf(int id)
    {
        var lease = await MyLeasesQuery()
            .Include(l => l.PrimaryTenant)
            .Include(l => l.Apartment).ThenInclude(a => a.Building)
            .Include(l => l.AdditionalTenants).ThenInclude(t => t.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lease == null) return NotFound();
        var bytes = _pdfGen.Generate(lease);
        return File(bytes, "application/pdf", $"{lease.LeaseNumber}.pdf");
    }
}
