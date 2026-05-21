using t.Infrastructure.Time;

namespace t.Infrastructure.Billing;

public class InvoiceGenerationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InvoiceGenerationService> _log;
    private static readonly TimeSpan TickInterval = TimeSpan.FromHours(6);

    public InvoiceGenerationService(IServiceProvider services, ILogger<InvoiceGenerationService> log)
    {
        _services = services;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("Invoice generation service started.");
        var lastRunDay = DateTime.MinValue.Date;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var today = VnTime.Today;
                if (today != lastRunDay)
                {
                    using var scope = _services.CreateScope();
                    var gen = scope.ServiceProvider.GetRequiredService<InvoiceGenerator>();
                    await gen.GenerateForBillingDayAsync(today, stoppingToken);
                    await gen.ApplyLateFeesAsync(today, stoppingToken);
                    lastRunDay = today;
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Invoice generation tick failed");
            }

            try { await Task.Delay(TickInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        _log.LogInformation("Invoice generation service stopped.");
    }
}
