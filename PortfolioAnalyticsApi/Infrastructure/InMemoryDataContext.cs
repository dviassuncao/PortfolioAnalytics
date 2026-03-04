using PortfolioAnalyticsApi.Infrastructure.Data;
using System.Text.Json;

namespace PortfolioAnalyticsApi.Infrastructure
{
    public interface IInMemoryDataContext
    {
        IReadOnlyCollection<Portfolio> Portfolios { get; }
        IReadOnlyCollection<Asset> Assets { get; }
    }

    public class InMemoryDataContext : IInMemoryDataContext
    {
        public IReadOnlyCollection<Portfolio> Portfolios { get; }
        public IReadOnlyCollection<Asset> Assets { get; }
        public IReadOnlyDictionary<string, IReadOnlyCollection<PriceHistory>> PriceHistory { get; }
        public MarketData MarketData { get; }

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        public InMemoryDataContext(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException("file not found.");

            var json = File.ReadAllText(jsonPath);

            var seedData = JsonSerializer.Deserialize<SeedData>(
                json, _jsonSerializerOptions) ?? throw new InvalidDataException("Invalid JSON structure.");

            Portfolios = seedData.Portfolios != null ? seedData.Portfolios.AsReadOnly() : [];
            Assets = seedData.Assets != null ? seedData.Assets.AsReadOnly() : [];
            PriceHistory = seedData.PriceHistory != null ? seedData.PriceHistory.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyCollection<PriceHistory>)kvp.Value.AsReadOnly()
                ) : [];
            MarketData = seedData.MarketData;
        }
    }
}