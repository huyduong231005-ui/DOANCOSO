using t.Infrastructure.Localization;
using t.Models.Entities;

namespace t.Infrastructure.Maintenance;

/// <summary>Một yêu cầu sự cố đã rút gọn cho việc tính KPI (đọc từ DB một lần, tính trong bộ nhớ).</summary>
public class MaintenanceKpiRow
{
    public int Id { get; init; }
    public MaintenanceStatus Status { get; init; }
    public MaintenancePriority Priority { get; init; }
    public MaintenanceCategory Category { get; init; }
    public string? BuildingName { get; init; }
    public string? TechnicianName { get; init; }
    public DateTime ReportedAt { get; init; }
    public DateTime? AcknowledgedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime? DueAt { get; init; }
    public int ResponseSlaMinutes { get; init; }
    public int? Rating { get; init; }
    /// <summary>Hệ thống (AI điều phối) tự giao việc.</summary>
    public bool AutoAssigned { get; init; }
    /// <summary>Quản lý đã giao lại cho người khác.</summary>
    public bool Reassigned { get; init; }

    public bool IsOpen => MaintenanceRequest.ActiveStatuses.Contains(Status);
    public bool OnTime => ResolvedAt != null && DueAt != null && ResolvedAt <= DueAt;
    public double? HoursToResolve => ResolvedAt == null ? null : (ResolvedAt.Value - ReportedAt).TotalHours;
}

public record MaintenanceKpiGroup(string? Name, int Count, int Resolved, double? OnTimeRate, double? MttrHours, double? CsatAvg, int Open, int Overdue);

public record MaintenanceKpiSummary(
    int Total, int Open, int OverdueNow, int Resolved,
    double? OnTimeRate, double? MttrHours,
    double? ResponseAvgMinutes, double? ResponseOnTimeRate,
    int AiAssigned, double? AiKeptRate,
    double? CsatAvg, int CsatCount,
    IReadOnlyList<MaintenanceKpiGroup> ByPriority, IReadOnlyList<MaintenanceKpiGroup> ByCategory,
    IReadOnlyList<MaintenanceKpiGroup> ByBuilding, IReadOnlyList<MaintenanceKpiGroup> ByTechnician);

/// <summary>
/// KPI của quy trình sự cố (FR-09), cùng công thức với các câu SQL ở tài liệu thiết kế dữ liệu, mục 10.
/// Tỉ lệ trả về null khi không có mẫu (tránh chia cho 0 và tránh hiện 0% gây hiểu nhầm).
/// </summary>
public static class MaintenanceKpi
{
    public static MaintenanceKpiSummary Compute(IReadOnlyCollection<MaintenanceKpiRow> rows, DateTime nowUtc)
    {
        var resolved = rows.Where(r => r.ResolvedAt != null).ToList();
        var acknowledged = rows.Where(r => r.AcknowledgedAt != null).ToList();
        var auto = rows.Where(r => r.AutoAssigned).ToList();
        var rated = rows.Where(r => r.Rating != null).ToList();

        return new MaintenanceKpiSummary(
            Total: rows.Count,
            Open: rows.Count(r => r.IsOpen),
            OverdueNow: rows.Count(r => IsOverdue(r, nowUtc)),
            Resolved: resolved.Count,
            OnTimeRate: Rate(resolved.Count(r => r.DueAt != null && r.OnTime), resolved.Count(r => r.DueAt != null)),
            MttrHours: Avg(resolved.Select(r => r.HoursToResolve!.Value)),
            ResponseAvgMinutes: Avg(acknowledged.Select(r => (r.AcknowledgedAt!.Value - r.ReportedAt).TotalMinutes)),
            ResponseOnTimeRate: Rate(acknowledged.Count(r => (r.AcknowledgedAt!.Value - r.ReportedAt).TotalMinutes <= r.ResponseSlaMinutes), acknowledged.Count),
            AiAssigned: auto.Count,
            AiKeptRate: Rate(auto.Count(r => !r.Reassigned), auto.Count),
            CsatAvg: Avg(rated.Select(r => (double)r.Rating!.Value)),
            CsatCount: rated.Count,
            ByPriority: Group(rows.OrderByDescending(r => r.Priority), r => r.Priority.Vi(), nowUtc),
            ByCategory: Group(rows, r => r.Category.Vi(), nowUtc).OrderByDescending(g => g.Count).ToList(),
            ByBuilding: Group(rows, r => r.BuildingName, nowUtc).OrderBy(g => g.Name).ToList(),
            ByTechnician: Group(rows.Where(r => r.TechnicianName != null), r => r.TechnicianName, nowUtc).OrderByDescending(g => g.Count).ToList());
    }

    private static bool IsOverdue(MaintenanceKpiRow r, DateTime now) => r.IsOpen && r.DueAt != null && r.DueAt < now;

    private static List<MaintenanceKpiGroup> Group(IEnumerable<MaintenanceKpiRow> rows, Func<MaintenanceKpiRow, string?> key, DateTime now) =>
        rows.GroupBy(key).Select(g =>
        {
            var resolved = g.Where(r => r.ResolvedAt != null).ToList();
            return new MaintenanceKpiGroup(
                g.Key, g.Count(), resolved.Count,
                Rate(resolved.Count(r => r.DueAt != null && r.OnTime), resolved.Count(r => r.DueAt != null)),
                Avg(resolved.Select(r => r.HoursToResolve!.Value)),
                Avg(g.Where(r => r.Rating != null).Select(r => (double)r.Rating!.Value)),
                g.Count(r => r.IsOpen), g.Count(r => IsOverdue(r, now)));
        }).ToList();

    private static double? Rate(int part, int whole) => whole == 0 ? null : (double)part / whole;

    private static double? Avg(IEnumerable<double> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? null : list.Average();
    }
}
