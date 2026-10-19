using t.Infrastructure.Maintenance;
using t.Models.Entities;

namespace t.Tests.Infrastructure;

/// <summary>Bảng chuyển trạng thái sự cố (BR-SC-03…06): ai được làm gì, ở trạng thái nào.</summary>
public class MaintenanceWorkflowTests
{
    private const string Tech = "tech-1";

    private static MaintenanceRequest Req(MaintenanceStatus status, string? assignee = Tech) =>
        new() { Status = status, AssignedToId = assignee };

    [Fact]
    public void Technician_StartsOwnAssignedJob()
    {
        Assert.Null(MaintenanceWorkflow.Check(Req(MaintenanceStatus.Acknowledged), MaintenanceStatus.InProgress,
            WorkflowActor.Technician, Tech, null, null));
    }

    [Fact]
    public void Technician_CannotTouchJobAssignedToSomeoneElse()
    {
        Assert.NotNull(MaintenanceWorkflow.Check(Req(MaintenanceStatus.Acknowledged, "tech-2"), MaintenanceStatus.InProgress,
            WorkflowActor.Technician, Tech, null, null));
    }

    [Fact]
    public void Technician_CannotRejectOrClose()
    {
        Assert.NotNull(MaintenanceWorkflow.Check(Req(MaintenanceStatus.Acknowledged), MaintenanceStatus.Rejected,
            WorkflowActor.Technician, Tech, "lý do", null));
        Assert.NotNull(MaintenanceWorkflow.Check(Req(MaintenanceStatus.Resolved), MaintenanceStatus.Closed,
            WorkflowActor.Technician, Tech, null, null));
    }

    [Fact]
    public void Resolve_RequiresResolutionNote()
    {
        var r = Req(MaintenanceStatus.InProgress);
        Assert.NotNull(MaintenanceWorkflow.Check(r, MaintenanceStatus.Resolved, WorkflowActor.Technician, Tech, null, "  "));
        Assert.Null(MaintenanceWorkflow.Check(r, MaintenanceStatus.Resolved, WorkflowActor.Technician, Tech, null, "Đã thay vòi"));
    }

    [Theory]
    [InlineData(MaintenanceStatus.Open, true)]
    [InlineData(MaintenanceStatus.Acknowledged, true)]
    [InlineData(MaintenanceStatus.InProgress, false)]
    [InlineData(MaintenanceStatus.OnHold, false)]
    [InlineData(MaintenanceStatus.Resolved, false)]
    public void Resident_CancelsOnlyBeforeWorkStarts(MaintenanceStatus status, bool allowed)
    {
        var error = MaintenanceWorkflow.Check(Req(status), MaintenanceStatus.Cancelled, WorkflowActor.Resident, "resident", null, null);
        Assert.Equal(allowed, error == null);
    }

    [Fact]
    public void StartWork_RequiresAssignee()
    {
        Assert.NotNull(MaintenanceWorkflow.Check(Req(MaintenanceStatus.Acknowledged, assignee: null), MaintenanceStatus.InProgress,
            WorkflowActor.Manager, "manager", null, null));
    }

    [Fact]
    public void Reject_And_Hold_RequireReason()
    {
        Assert.NotNull(MaintenanceWorkflow.Check(Req(MaintenanceStatus.Open), MaintenanceStatus.Rejected, WorkflowActor.Manager, "m", "", null));
        Assert.NotNull(MaintenanceWorkflow.Check(Req(MaintenanceStatus.InProgress), MaintenanceStatus.OnHold, WorkflowActor.Technician, Tech, null, null));
        Assert.Null(MaintenanceWorkflow.Check(Req(MaintenanceStatus.InProgress), MaintenanceStatus.OnHold, WorkflowActor.Technician, Tech, "Chờ mua van", null));
    }

    [Theory]
    [InlineData(MaintenanceStatus.Closed)]
    [InlineData(MaintenanceStatus.Rejected)]
    [InlineData(MaintenanceStatus.Cancelled)]
    public void FinalStates_HaveNoWayOut(MaintenanceStatus final)
    {
        Assert.Empty(MaintenanceWorkflow.NextStatuses(Req(final), WorkflowActor.Manager, "m"));
    }

    [Fact]
    public void Resident_ConfirmsOrReopensResolvedJob()
    {
        var next = MaintenanceWorkflow.NextStatuses(Req(MaintenanceStatus.Resolved), WorkflowActor.Resident, "resident");
        Assert.Equal(new[] { MaintenanceStatus.Closed, MaintenanceStatus.InProgress }.OrderBy(s => s), next.OrderBy(s => s));
    }

    [Fact]
    public void PhotosFrom_SplitsCommaListIntoRows()
    {
        var photos = MaintenanceWorkflow.PhotosFrom(" /a.jpg, /b.jpg ,,/a.jpg", MaintenancePhotoStage.Report, "u").ToList();
        Assert.Equal(new[] { "/a.jpg", "/b.jpg" }, photos.Select(p => p.Url));
        Assert.All(photos, p => Assert.Equal(MaintenancePhotoStage.Report, p.Stage));
    }
}
