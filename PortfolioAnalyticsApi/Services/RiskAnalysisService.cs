using PortfolioAnalyticsApi.DTOs;
using PortfolioAnalyticsApi.Infrastructure.Data;
using PortfolioAnalyticsApi.Repositories;

namespace PortfolioAnalyticsApi.Services;

public interface IRiskAnalysisService
{
    RiskAnalysisResponse GetRiskAnalysis(Portfolio portfolio);
}

public class RiskAnalysisService(IPerformanceService performanceService, IMarketDataRepository marketDataRepository, IAssetRepository assetRepository, ILogger<RiskAnalysisService> logger) : IRiskAnalysisService
{
    public RiskAnalysisResponse GetRiskAnalysis(Portfolio portfolio)
    {
        var performance = performanceService.GetPerformance(portfolio);

        var sortedPositions = performance.PositionsPerformance
            .OrderByDescending(p => p.Weight)
            .ToList();

        var largest = sortedPositions.FirstOrDefault();
        double top3Sum = Math.Round(sortedPositions.Take(3).Sum(p => p.Weight), 2);

        var sectorDiversification = performance.PositionsPerformance
            .Select(p => new
            {
                p.Weight,
                Asset = assetRepository.GetBySymbol(p.Symbol)
            })
            .Where(x => x.Asset != null)
            .GroupBy(x => x.Asset!.Sector)
            .Select(g =>
            {
                double sectorPct = Math.Round(g.Sum(x => x.Weight), 2);
                return new SectorDiversification
                {
                    Sector = g.Key,
                    Percentage = sectorPct,
                    Risk = GetSectorRiskRating(sectorPct)
                };
            }).ToList();

        double maxSectorPct = sectorDiversification.Count != 0 ? sectorDiversification.Max(s => s.Percentage) : 0;

        logger.LogInformation("--- Risk Analysis Debug for Portfolio: {PortfolioName} ---", portfolio.Name);
        logger.LogInformation("Largest Position Weight (maxPos): {MaxPos}", largest?.Weight ?? 0);
        logger.LogInformation("Max Sector Weight (maxSector): {MaxSector}", maxSectorPct);
        foreach (var sector in sectorDiversification)
        {
            logger.LogInformation("Sector: {SectorName}, Weight: {SectorWeight}", sector.Sector, sector.Percentage);
        }
        
        string overallRisk = CalculateOverallRisk(largest?.Weight ?? 0, maxSectorPct);
        logger.LogInformation("Calculated Overall Risk: {OverallRisk}", overallRisk);
        logger.LogInformation("----------------------------------------------------");

        return new RiskAnalysisResponse
        {
            OverallRisk = overallRisk,
            SharpeRatio = CalculateSharpeRatio(performance.AnnualizedReturn, performance.Volatility),
            ConcentrationRisk = new ConcentrationRisk
            {
                LargestPosition = new LargestPosition
                {
                    Symbol = largest?.Symbol ?? "N/A",
                    Percentage = largest?.Weight ?? 0
                },
                Top3Concentration = top3Sum
            },
            SectorDiversification = sectorDiversification
        };
    }

    private double? CalculateSharpeRatio(double annualizedReturn, double? volatility)
    {
        if (volatility == null || volatility == 0) return null;

        var selicRate = marketDataRepository.GetSelicRate();

        return (annualizedReturn - (selicRate * 100)) / volatility;
    }

    private static string CalculateOverallRisk(double maxPos, double maxSector)
    {
        if (maxPos > 25 || maxSector > 40) return "High";
        if (maxPos >= 15 || maxSector >= 25) return "Medium";
        return "Low";
    }

    private static string GetSectorRiskRating(double percentage)
    {
        if (percentage > 40) return "High";
        if (percentage >= 25) return "Medium";
        return "Low";
    }
}
