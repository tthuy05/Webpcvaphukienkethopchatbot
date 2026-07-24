using Microsoft.EntityFrameworkCore;

namespace Webpcvaphukienkethopchatbot.Data;

public class DatabaseSeedHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeedHostedService> _logger;

    public DatabaseSeedHostedService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DatabaseSeedHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
        await DataSeeder.EnsureRolesAsync(scope.ServiceProvider);

        if (_configuration.GetValue("SeedData:Enabled", false))
        {
            await DataSeeder.SeedAsync(scope.ServiceProvider, cancellationToken);
        }

        _logger.LogInformation("Database migration completed. Demo seed enabled: {SeedEnabled}.",
            _configuration.GetValue("SeedData:Enabled", false));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
