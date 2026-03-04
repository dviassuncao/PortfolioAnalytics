using PortfolioAnalyticsApi.Infrastructure;
using PortfolioAnalyticsApi.Infrastructure.Data;

namespace PortfolioAnalyticsApi.Repositories
{
    public interface IPortfolioRepository
    {
        Portfolio? GetByUserId(string userId);
    }

    public class PortfolioRepository(InMemoryDataContext dataContext) : IPortfolioRepository
    {
        public Portfolio? GetByUserId(string userId) => dataContext
            .Portfolios
            .FirstOrDefault(p => p.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
    }
}