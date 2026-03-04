using PortfolioAnalyticsApi.Infrastructure;
using PortfolioAnalyticsApi.Infrastructure.Data;

namespace PortfolioAnalyticsApi.Repositories
{
    public interface IPriceHistoryRepository
    {
        List<PriceHistory>? GetOrderedPriceHistoryBySymbol(string symbol);
    }

    public class PriceHistoryRepository(InMemoryDataContext dataContext) : IPriceHistoryRepository
    {
        public List<PriceHistory>? GetOrderedPriceHistoryBySymbol(string symbol) =>
            dataContext.PriceHistory.TryGetValue(symbol, out var priceHistory)
                ? [.. priceHistory.OrderBy(p => p.Date)]
                : null;
    }
}