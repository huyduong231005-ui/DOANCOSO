using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using t.Data;
using t.Infrastructure.Email;
using t.Infrastructure.Maintenance;
using t.Models.Entities;

namespace t.Tests.Integration;

/// <summary>Tuần 6: cảnh báo quá hạn SLA.</summary>
public sealed class Week6Tests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public Week6Tests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task OverdueAlert_EmailsManagersOnce_AndMarksHistory_SkipsClosedAndNotYetDue()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var apt = await db.Apartments.OrderBy(a => a.Id).FirstAsync();
        var now = DateTime.UtcNow;
        var reporterId = await db.Users.Select(u => u.Id).FirstAsync(); // mỗi yêu cầu có đúng một người báo
        MaintenanceRequest New(string no, MaintenanceStatus st, DateTime due) => new()
        {
            RequestNumber = no, ApartmentId = apt.Id, ReporterId = reporterId, Title = "Thử " + no, Description = "Thử", Status = st,
            ReportedAt = now.AddDays(-2), DueAt = due
        };
        var tag = Guid.NewGuid().ToString("N")[..6];
        var overdue = New("OD-" + tag, MaintenanceStatus.InProgress, now.AddHours(-1));
        var closed = New("CL-" + tag, MaintenanceStatus.Closed, now.AddHours(-1));
        closed.ResolutionNote = "Đã xử lý"; // đã đóng phải có kết quả (CK_YeuCauBaoTri_KetQua)
        var notYet = New("NY-" + tag, MaintenanceStatus.Acknowledged, now.AddHours(5));
        db.MaintenanceRequests.AddRange(overdue, closed, notYet);
        // Các yêu cầu mẫu khác có thể cũng đã quá hạn: đánh dấu sẵn để lượt quét chỉ thấy ba yêu cầu này.
        foreach (var r in await db.MaintenanceRequests.Where(MaintenanceWorkflow.IsOverdue(now)).ToListAsync())
            db.MaintenanceStatusChanges.Add(new MaintenanceStatusChange
            {
                MaintenanceRequestId = r.Id, ToStatus = r.Status, ChangedAt = now,
                Note = MaintenanceOverdueAlertService.AlertNote
            });
        await db.SaveChangesAsync();
        db.MaintenanceStatusChanges.RemoveRange(db.MaintenanceStatusChanges.Local
            .Where(h => h.MaintenanceRequestId == overdue.Id && h.Note == MaintenanceOverdueAlertService.AlertNote));
        await db.SaveChangesAsync();

        var mail = new List<(string To, string Html)>();
        var sender = new RecordingSender(mail);
        var first = await MaintenanceOverdueAlertService.RunOnceAsync(db, sender, now);
        var second = await MaintenanceOverdueAlertService.RunOnceAsync(db, sender, now);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        var letter = Assert.Single(mail);
        Assert.Contains("quanly@luxehaven.vn", letter.To);
        Assert.Contains(overdue.RequestNumber, letter.Html);
        Assert.DoesNotContain(closed.RequestNumber, letter.Html);
        Assert.DoesNotContain(notYet.RequestNumber, letter.Html);
        Assert.True(await db.MaintenanceStatusChanges.AnyAsync(h =>
            h.MaintenanceRequestId == overdue.Id && h.Note == MaintenanceOverdueAlertService.AlertNote && h.ChangedById == null));
    }

    private sealed class RecordingSender(List<(string To, string Html)> sent) : IEmailSender
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            sent.Add((to, htmlBody));
            return Task.CompletedTask;
        }
    }
}
