using PortfolioAnalyticsApi.DTOs;
using PortfolioAnalyticsApi.Infrastructure.Data;
using PortfolioAnalyticsApi.Repositories;

namespace PortfolioAnalyticsApi.Services;

public interface IPerformanceService
{
    PerformanceResponse GetPerformance(Portfolio portfolio);
}

public class PerformanceService(IPortfolioCalculatorService calculator, IAssetRepository assetRepository) : IPerformanceService
{
    public PerformanceResponse GetPerformance(Portfolio portfolio)
    {
        decimal invested = calculator.CalculateTotalInvested(portfolio.Positions);
        decimal current = calculator.CalculateCurrentValue(portfolio.Positions);
        decimal returnAmount = current - invested;

        double returnPercentage = calculator.CalculateTotalReturnPercentage(invested, current);
        double annualized = calculator.CalculateAnnualizedReturn(returnPercentage, portfolio.CreatedAt);

        double? volatility = calculator.CalculatePortfolioVolatility(portfolio.Positions);

        var positionsPerformance = GeneratePositionsPerformance(portfolio.Positions, current);

        return new PerformanceResponse()
        {
            TotalInvestment = invested,
            CurrentValue = current,
            TotalReturn = Math.Round(returnPercentage, 2),
            TotalReturnAmount = returnAmount,
            AnnualizedReturn = Math.Round(annualized, 2),
            Volatility = volatility.HasValue ? Math.Round(volatility.Value, 2) : null,
            PositionsPerformance = positionsPerformance
        };
    }

    private List<PositionPerformance> GeneratePositionsPerformance(IEnumerable<Position> positions, decimal totalCurrentValue) =>
        [.. positions.Select(pos =>
        {
            var asset = assetRepository.GetBySymbol(pos.AssetSymbol);
            decimal currentPrice = asset?.CurrentPrice ?? 0;

            decimal investedAmount = pos.Quantity * pos.AveragePrice;
            decimal currentValue = pos.Quantity * currentPrice;

            double individualReturn = investedAmount > 0
                ? (double)((currentValue - investedAmount) / investedAmount) * 100
                : 0;

            double weight = totalCurrentValue > 0
                ? (double)(currentValue / totalCurrentValue) * 100
                : 0;

            return new PositionPerformance
            {
                Symbol = pos.AssetSymbol,
                InvestedAmount = investedAmount,
                CurrentValue = currentValue,
                Return = Math.Round(individualReturn, 2),
                Weight = Math.Round(weight, 2)
            };
        })];
}
