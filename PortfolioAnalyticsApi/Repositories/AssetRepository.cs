using PortfolioAnalyticsApi.Infrastructure;
using PortfolioAnalyticsApi.Infrastructure.Data;

namespace PortfolioAnalyticsApi.Repositories
{
    public interface IAssetRepository
    {
        Asset? GetBySymbol(string symbol);
    }

    public class AssetRepository(InMemoryDataContext dataContext) : IAssetRepository
    {
        private readonly IReadOnlyDictionary<string, Asset> _assetsBySymbol = dataContext
                .Assets
                .ToDictionary(a => a.Symbol, StringComparer.OrdinalIgnoreCase);

        public Asset? GetBySymbol(string symbol)
            => _assetsBySymbol.TryGetValue(symbol, out var asset)
                ? asset
                : null;
    }
}