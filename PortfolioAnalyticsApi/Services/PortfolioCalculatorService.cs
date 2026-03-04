using PortfolioAnalyticsApi.Infrastructure.Data;
using PortfolioAnalyticsApi.Repositories;

namespace PortfolioAnalyticsApi.Services;

public interface IPortfolioCalculatorService
{
    decimal CalculateTotalInvested(IEnumerable<Position> positions);
    decimal CalculateCurrentValue(IEnumerable<Position> positions);
    double CalculateTotalReturnPercentage(decimal invested, decimal current);
    double CalculateAnnualizedReturn(double totalReturnPercentage, DateTime createdAt);
    double? CalculatePortfolioVolatility(IEnumerable<Position> positions);
    List<decimal>? GetDailyPortfolioValues(IEnumerable<Position> positions);
}

public class PortfolioCalculatorService(IAssetRepository assetRepository, IPriceHistoryRepository priceHistoryRepository) : IPortfolioCalculatorService
{
    public decimal CalculateTotalInvested(IEnumerable<Position> positions) =>
        positions.Sum(p => p.Quantity * p.AveragePrice);

    public decimal CalculateCurrentValue(IEnumerable<Position> positions)
    {
        decimal total = 0;

        foreach (var position in positions)
        {
            var asset = assetRepository.GetBySymbol(position.AssetSymbol);
            if (asset == null)
                continue;

            total += position.Quantity * asset.CurrentPrice;
        }

        return total;
    }

    public double CalculateTotalReturnPercentage(decimal invested, decimal current)
    {
        if (invested == 0)
            return 0;

        return (double)((current - invested) / invested) * 100;
    }

    public double CalculateAnnualizedReturn(double totalReturnPercentage, DateTime createdAt)
    {
        double daysElapsed = (DateTime.UtcNow - createdAt).TotalDays;

        if (daysElapsed <= 0) return totalReturnPercentage;

        double totalReturnDecimal = totalReturnPercentage / 100;

        double annualizedReturn = Math.Pow(1 + totalReturnDecimal, 365.0 / daysElapsed) - 1;

        return annualizedReturn * 100;
    }

    public double? CalculatePortfolioVolatility(IEnumerable<Position> positions)
    {
        var dailyValues = GetDailyPortfolioValues(positions);

        if (dailyValues == null || dailyValues.Count < 2)
            return null;

        var dailyReturns = new List<double>();
        for (int i = 1; i < dailyValues.Count; i++)
        {
            if (dailyValues[i - 1] > 0)
            {
                double dayReturn = (double)((dailyValues[i] - dailyValues[i - 1]) / dailyValues[i - 1]);
                dailyReturns.Add(dayReturn);
            }
        }

        if (dailyReturns.Count == 0) return null;

        double average = dailyReturns.Average();
        double sumOfSquares = dailyReturns.Sum(r => Math.Pow(r - average, 2));
        double dailyVolatility = Math.Sqrt(sumOfSquares / dailyReturns.Count);

        return dailyVolatility * Math.Sqrt(252) * 100;
    }

    public List<decimal>? GetDailyPortfolioValues(IEnumerable<Position> positions)
    {
        var histories = new Dictionary<string, List<PriceHistory>>();

        foreach (var pos in positions)
        {
            var history = priceHistoryRepository.GetOrderedPriceHistoryBySymbol(pos.AssetSymbol);

            if (history == null || history.Count == 0)
                return null;

            histories.Add(pos.AssetSymbol, history);
        }

        var firstSymbol = positions.First().AssetSymbol;
        var availableDates = histories[firstSymbol].Select(h => h.Date).ToList();

        var dailyValues = new List<decimal>();

        foreach (var date in availableDates)
        {
            decimal dayTotal = 0;
            foreach (var position in positions)
            {
                var priceAtDate = histories[position.AssetSymbol]
                    .FirstOrDefault(h => h.Date == date)?.Price ?? 0;

                dayTotal += position.Quantity * priceAtDate;
            }
            dailyValues.Add(dayTotal);
        }

        return dailyValues;
    }
}
