using PortfolioAnalyticsApi.DTOs;
using PortfolioAnalyticsApi.Infrastructure.Data;
using PortfolioAnalyticsApi.Repositories;

namespace PortfolioAnalyticsApi.Services;

public interface IRebalancingService
{
    RebalancingResponse GetRebalancing(Portfolio portfolio);
}

public class RebalancingService(IPortfolioCalculatorService calculator, IAssetRepository assetRepository) : IRebalancingService
{
    public RebalancingResponse GetRebalancing(Portfolio portfolio)
    {
        decimal totalValue = calculator.CalculateCurrentValue(portfolio.Positions);
        var currentAllocations = new List<CurrentAllocation>();
        var trades = new List<SuggestedTrade>();
        decimal totalCosts = 0;

        foreach (var pos in portfolio.Positions)
        {
            var asset = assetRepository.GetBySymbol(pos.AssetSymbol);
            if (asset == null) continue;

            decimal assetValue = pos.Quantity * asset.CurrentPrice;
            double currentWeight = totalValue > 0 ? (double)(assetValue / totalValue) * 100 : 0;
            double targetWeightPercentage = pos.TargetAllocation * 100;
            double deviation = currentWeight - targetWeightPercentage;

            currentAllocations.Add(new CurrentAllocation
            {
                Symbol = pos.AssetSymbol,
                CurrentWeight = Math.Round(currentWeight, 2),
                TargetWeight = Math.Round(targetWeightPercentage, 2),
                Deviation = Math.Round(deviation, 2)
            });

            if (Math.Abs(deviation) > 2)
            {
                decimal targetValue = totalValue * (decimal)pos.TargetAllocation;
                decimal diffValue = Math.Abs(targetValue - assetValue);

                if (diffValue >= 100)
                {
                    int qty = (int)(diffValue / asset.CurrentPrice);

                    if (qty > 0)
                    {
                        decimal tradeValue = qty * asset.CurrentPrice;
                        decimal cost = tradeValue * 0.003m;

                        trades.Add(new SuggestedTrade
                        {
                            Symbol = pos.AssetSymbol,
                            Action = deviation > 0 ? "SELL" : "BUY",
                            Quantity = qty,
                            EstimatedValue = Math.Round(tradeValue, 2),
                            TransactionCost = Math.Round(cost, 2)
                        });

                        totalCosts += cost;
                    }
                }
            }
        }

        var sortedTrades = trades
            .OrderByDescending(t => {
                var alloc = currentAllocations.First(a => a.Symbol == t.Symbol);
                return Math.Abs(alloc.Deviation);
            }).ToList();

        return new RebalancingResponse
        {
            NeedsRebalancing = sortedTrades.Count != 0,
            CurrentAllocation = currentAllocations,
            SuggestedTrades = sortedTrades,
            TotalTransactionCost = Math.Round(totalCosts, 2)
        };
    }
}
