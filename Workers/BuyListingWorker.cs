using ExchangeSharp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using new_listing_bot_cs.DataAccess.Entities;

namespace new_listing_bot_cs;

public class BuyListingWorker : BackgroundService
{
    private readonly BotConfig _botConfig;
    private readonly ILogger<BuyListingWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public BuyListingWorker(
        ILogger<BuyListingWorker> logger, 
        IServiceProvider serviceProvider, 
        BotConfig botConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _botConfig = botConfig;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Listings Worker");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var exchangeService = scope.ServiceProvider.GetRequiredService<Exchange>();
                var listingService = scope.ServiceProvider.GetRequiredService<ListingsGetter>();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var listings = await listingService.GetListings();
                var latestAnnouncement = listings?.Data?.Catalogs?.FirstOrDefault()?.Articles?.FirstOrDefault()?.Title;

                if (string.IsNullOrEmpty(latestAnnouncement) 
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                if (!latestAnnouncement.Contains("will list", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(1000, stoppingToken);
                    continue;
                }

                _logger.LogInformation("NEW LISTING ANNOUNCEMENT: {Announcement}", latestAnnouncement);

                var symbols = ListingsGetter.ExtractSymbols(latestAnnouncement);
                foreach (var symbol in symbols)
                {
                    if (string.IsNullOrWhiteSpace(symbol))
                    {
                        continue;
                    }

                    try
                    {
                        var marketSymbol = $"{symbol.ToUpper()}_USDT";
								// Update the portfolio check
												var exists = await dbContext.Portfolio
														.AnyAsync(p => p.OrderResult.ExchangeOrderResult.MarketSymbol == marketSymbol, stoppingToken);

                        if (exists)
                        {
                            _logger.LogDebug("{Symbol} already in portfolio", symbol);
                            continue;
                        }

                        _logger.LogInformation("Buying {Amount} of {Symbol}", 
                            _botConfig.BuyAmount, symbol);

                        var orderRequest = new ExchangeOrderRequest
                        {
                            MarketSymbol = marketSymbol,
                            Amount = _botConfig.BuyAmount,
                            IsBuy = true,
                            OrderType = OrderType.Market,
                            ExtraParameters = { ["amount"] = _botConfig.BuyAmount }
                        };

                        var result = await exchangeService.HandlePlaceOrder(orderRequest);
                        if (result?.Price == null)
                        {
                            _logger.LogError("Failed to place order for {Symbol}", symbol);
                            continue;
                        }

                        var order = new OrderResult
                        {
                            ExchangeOrderResult = result,
                            Exit = new ExitStrategy
                            {
                                TakeProfitPrice = CalculatePrice(result.Price, _botConfig.TakeProfit),
                                StopLossPrice = CalculatePrice(result.Price, -_botConfig.StopLoss)
                            }
                        };

                        dbContext.OrderResults.Add(order);
                        await dbContext.SaveChangesAsync(stoppingToken);
                        
                        _logger.LogInformation("{Symbol} bought successfully at {Price}", 
                            symbol, result.Price);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing {Symbol}", symbol);
                        await Task.Delay(5000, stoppingToken);
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in worker");
                await Task.Delay(30000, stoppingToken);
            }
        }
    }

    private static decimal CalculatePrice(decimal basePrice, decimal percentage)
        => basePrice + (basePrice * percentage / 100);
}

