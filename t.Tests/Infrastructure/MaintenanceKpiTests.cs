using t.Infrastructure.Maintenance;
using t.Models.Entities;

namespace t.Tests.Infrastructure;

/// <summary>Công thức KPI sự cố (FR-09) khớp với tài liệu thiết kế dữ liệu, mục 10.</summary>
public class MaintenanceKpiTests
{
    private static readonly DateTime T0 = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = T0.AddDays(5);

    private static MaintenanceKpiRow Row(int id, MaintenanceStatus status, double? ackMin = null, double? resolvedH = null,
        double dueH = 24, int? rating = null, string? tech = "Thợ A", bool auto = false, bool reassigned = false,
        MaintenancePriority priority = MaintenancePriority.Medium, MaintenanceCategory category = MaintenanceCategory.Plumbing) =>
        new()
        {
            Id = id, Status = status, Priority = priority, Category = category, BuildingName = "Toà A", TechnicianName = tech,
            ReportedAt = T0, AcknowledgedAt = ackMin == null ? null : T0.AddMinutes(ackMin.Value),
            ResolvedAt = resolvedH == null ? null : T0.AddHours(resolvedH.Value), DueAt = T0.AddHours(dueH),
            ResponseSlaMinutes = 120, Rating = rating, AutoAssigned = auto, Reassigned = reassigned,
        };

    [Fact]
    public void OnTimeRate_CountsOnlyResolvedRequests()
    {
        var kpi = MaintenanceKpi.Compute(new[]
        {
            Row(1, MaintenanceStatus.Closed, resolvedH: 10),                 // đúng hạn
            Row(2, MaintenanceStatus.Resolved, resolvedH: 30),               // trễ (hạn 24h)
            Row(3, MaintenanceStatus.InProgress),                            // chưa xong: không tính
        }, Now);

        Assert.Equal(3, kpi.Total);
        Assert.Equal(2, kpi.Resolved);
        Assert.Equal(0.5, kpi.OnTimeRate);
        Assert.Equal(20, kpi.MttrHours);                                     // (10 + 30) / 2
    }

    [Fact]
    public void OverdueNow_IsOpenAndPastDue()
    {
        var kpi = MaintenanceKpi.Compute(new[]
        {
            Row(1, MaintenanceStatus.InProgress, dueH: 24),                  // quá hạn (now = T0 + 5 ngày)
            Row(2, MaintenanceStatus.OnHold, dueH: 24 * 10),                 // chưa tới hạn
            Row(3, MaintenanceStatus.Cancelled, dueH: 1),                    // đã huỷ: không tính
        }, Now);

        Assert.Equal(2, kpi.Open);
        Assert.Equal(1, kpi.OverdueNow);
    }

    [Fact]
    public void ResponseOnTime_ComparesAcknowledgeWithResponseSla()
    {
        var kpi = MaintenanceKpi.Compute(new[]
        {
            Row(1, MaintenanceStatus.Acknowledged, ackMin: 30),              // trong 120 phút
            Row(2, MaintenanceStatus.Acknowledged, ackMin: 200),             // trễ
            Row(3, MaintenanceStatus.Open),                                  // chưa tiếp nhận: không tính
        }, Now);

        Assert.Equal(115, kpi.ResponseAvgMinutes);
        Assert.Equal(0.5, kpi.ResponseOnTimeRate);
    }

    [Fact]
    public void AutoKeptRate_IsAutoAssignedWithoutManualReassign()
    {
        var kpi = MaintenanceKpi.Compute(new[]
        {
            Row(1, MaintenanceStatus.Closed, auto: true),
            Row(2, MaintenanceStatus.Closed, auto: true),
            Row(3, MaintenanceStatus.Closed, auto: true, reassigned: true),
            Row(4, MaintenanceStatus.Closed, auto: false, reassigned: true), // quản lý tự giao: không tính
        }, Now);

        Assert.Equal(3, kpi.AutoAssignedCount);
        Assert.Equal(2.0 / 3, kpi.AutoKeptRate!.Value, 3);
    }

    [Fact]
    public void Csat_AndTechnicianBreakdown()
    {
        var kpi = MaintenanceKpi.Compute(new[]
        {
            Row(1, MaintenanceStatus.Closed, resolvedH: 2, rating: 5, tech: "Thợ A"),
            Row(2, MaintenanceStatus.Closed, resolvedH: 4, rating: 3, tech: "Thợ A"),
            Row(3, MaintenanceStatus.InProgress, tech: "Thợ B"),
            Row(4, MaintenanceStatus.Open, tech: null),
        }, Now);

        Assert.Equal(4, kpi.CsatAvg);
        Assert.Equal(2, kpi.CsatCount);
        var a = Assert.Single(kpi.ByTechnician, g => g.Name == "Thợ A");
        Assert.Equal((2, 2, 1.0, 3.0, 4.0), (a.Count, a.Resolved, a.OnTimeRate, a.MttrHours, a.CsatAvg));
        Assert.DoesNotContain(kpi.ByTechnician, g => g.Name == null);        // chưa giao không thành một dòng
    }

    [Fact]
    public void EmptyPeriod_GivesNullRatesNotDivideByZero()
    {
        var kpi = MaintenanceKpi.Compute(Array.Empty<MaintenanceKpiRow>(), Now);
        Assert.Equal(0, kpi.Total);
        Assert.Null(kpi.OnTimeRate);
        Assert.Null(kpi.MttrHours);
        Assert.Null(kpi.AutoKeptRate);
    }
}
