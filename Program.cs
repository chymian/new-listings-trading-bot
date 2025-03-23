using Microsoft.EntityFrameworkCore;
using new_listing_bot_cs;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Register Config Stuff
        var connectionString = hostContext.Configuration.GetConnectionString("Database");
        var apiKey = hostContext.Configuration.GetSection("ApiConfig:ApiKey").Value;
        var apiSecret = hostContext.Configuration.GetSection("ApiConfig:ApiSecret").Value;

        var botConfig = hostContext.Configuration.GetSection("BotConfig").Get<BotConfig>();
        if (botConfig != null)
        {
            services.AddSingleton(botConfig);
        }
        else
        {
            Console.WriteLine("BotConfig is null. Please check appsettings.Development.json.");
        }

        // Register Services
        services.AddScoped<Exchange>(provider => new Exchange(ExchangeNameEnum.Poloniex, apiKey, apiSecret));
        services.AddScoped<ListingsGetter>(provider => new ListingsGetter());

        // Register Db
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Register Workers
        services.AddHostedService<ExitStrategyWorker>();
        services.AddHostedService<BuyListingWorker>();
    })
    .Build();

// Apply migrations before running the host
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // This will apply pending migrations
}

await host.RunAsync();
