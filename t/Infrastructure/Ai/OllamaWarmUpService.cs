namespace t.Infrastructure.Ai;

/// <summary>
/// Giữ mô hình phân loại luôn nạp sẵn trong Ollama để không ai phải mồi tay trước khi dùng.
/// Gọi lại định kỳ vì Ollama có thể được bật sau web hoặc khởi động lại giữa chừng; khi mô hình
/// đã nạp, mỗi lần gọi chỉ mất vài mili giây.
/// </summary>
public class OllamaWarmUpService : BackgroundService
{
    // ponytail: Ollama khởi động lại thì tối đa 2 phút đầu yêu cầu còn bị nạp chậm; muốn bằng 0 thì
    // phải theo dõi tiến trình Ollama, chưa cần.
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    private readonly IServiceProvider _services;
    private readonly ILogger<OllamaWarmUpService> _log;

    public OllamaWarmUpService(IServiceProvider services, ILogger<OllamaWarmUpService> log)
    {
        _services = services;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var wasReady = false;
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _services.CreateScope())
            {
                var triage = scope.ServiceProvider.GetRequiredService<MaintenanceTriageService>();
                if (!triage.IsEnabled) return;
                var ready = await triage.WarmUpAsync(stoppingToken);
                // Chỉ ghi log khi đổi trạng thái để log không bị lặp mỗi 2 phút.
                if (ready && !wasReady) _log.LogInformation("Ollama model is loaded and ready for triage.");
                if (!ready && wasReady) _log.LogWarning("Ollama is not reachable; next triage will fall back or load slowly.");
                if (!ready && !wasReady) _log.LogInformation("Ollama not reachable yet; will retry warm-up.");
                wasReady = ready;
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
