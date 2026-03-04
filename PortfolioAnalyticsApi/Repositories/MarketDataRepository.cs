using PortfolioAnalyticsApi.Infrastructure;

namespace PortfolioAnalyticsApi.Repositories
{
    public interface IMarketDataRepository
    {
        double GetSelicRate();
    }
    public class MarketDataRepository(InMemoryDataContext dataContext) : IMarketDataRepository
    {
        public double GetSelicRate() => dataContext.MarketData.SelicRate;
    }
}
