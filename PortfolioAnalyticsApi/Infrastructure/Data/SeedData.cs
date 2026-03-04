namespace PortfolioAnalyticsApi.Infrastructure.Data
{
    public class SeedData
    {
        public required List<Asset> Assets { get; init; }
        public required List<Portfolio> Portfolios { get; init; }

        public required Dictionary<string, List<PriceHistory>> PriceHistory { get; init; }
        public required MarketData MarketData { get; init; }
    }

    public sealed class Portfolio
    {
        public required string Name { get; init; }
        public required string UserId { get; init; }
        public IEnumerable<Position> Positions { get; init; } = [];
        public required decimal TotalInvestment { get; init; }
        public required DateTime CreatedAt { get; init; }
    }

    public sealed class Asset
    {
        public required string Symbol { get; init; }
        public required decimal CurrentPrice { get; init; }
        public required string Sector { get; init; }
    }

    public sealed class Position
    {
        public required string AssetSymbol { get; init; }
        public required int Quantity { get; init; }
        public required decimal AveragePrice { get; init; }
        public required double TargetAllocation { get; init; }
    }

    public sealed class PriceHistory
    {
        public required DateTime Date { get; init; }
        public required decimal Price { get; init; }
    }

    public sealed class MarketData
    {
        public required double SelicRate { get; init; }
    }
}