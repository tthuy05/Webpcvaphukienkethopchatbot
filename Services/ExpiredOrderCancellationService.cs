using Microsoft.Extensions.Options;

namespace Webpcvaphukienkethopchatbot.Services;

public sealed class ExpiredOrderCancellationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<CommerceOptions> _options;
    private readonly ILogger<ExpiredOrderCancellationService> _logger;

    public ExpiredOrderCancellationService(
        IServiceProvider serviceProvider,
        IOptions<CommerceOptions> options,
        ILogger<ExpiredOrderCancellationService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var cutoff = DateTime.UtcNow.AddHours(-_options.Value.PendingOrderExpirationHours);
            var count = await orderService.CancelExpiredOrdersAsync(cutoff, cancellationToken);
            if (count > 0)
            {
                _logger.LogInformation("Automatically cancelled {Count} expired pending orders.", count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to cancel expired orders.");
        }
    }
}
