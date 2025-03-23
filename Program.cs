using Microsoft.EntityFrameworkCore;
using new_listing_bot_cs;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configuration setup
        var configuration = hostContext.Configuration;
        
        // Validate connection string
        var connectionString = configuration.GetConnectionString("Database") 
            ?? throw new InvalidOperationException("Database connection string is missing");
        
        // Configure database with retry logic
        services.AddDbContext<AppDbContext>(options => 
            options.UseNpgsql(connectionString, o => o
                .EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null))
                .UseSnakeCaseNamingConvention());  // Add this if using snake_case in PostgreSQL

        // Validate and register bot config
        var botConfig = configuration.GetSection("BotConfig").Get<BotConfig>()
            ?? throw new InvalidOperationException("BotConfig is missing");
        services.AddSingleton(botConfig);

        // Register exchange services with validation
        var apiKey = configuration["ApiConfig:ApiKey"]
            ?? throw new InvalidOperationException("ApiKey is missing");
        var apiSecret = configuration["ApiConfig:ApiSecret"]
            ?? throw new InvalidOperationException("ApiSecret is missing");
        
        services.AddScoped<Exchange>(_ => new Exchange(
            ExchangeNameEnum.Poloniex, 
            apiKey, 
            apiSecret
        ));

        services.AddScoped<ListingsGetter>();

        // Register workers
        services.AddHostedService<ExitStrategyWorker>();
        services.AddHostedService<BuyListingWorker>();
    })
    .UseConsoleLifetime()
    .Build();

// Database migration and health check
using (var scope = host.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    try 
    {
        logger.LogInformation("Applying database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Migrations applied successfully");
    }
    catch (NpgsqlException ex)
    {
        logger.LogCritical(ex, "Database migration failed");
        throw;
    }
}

await host.RunAsync();

