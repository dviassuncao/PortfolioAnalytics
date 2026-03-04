using Moq;
using NUnit.Framework;
using PortfolioAnalyticsApi.DTOs;
using PortfolioAnalyticsApi.Infrastructure.Data;
using PortfolioAnalyticsApi.Repositories;
using PortfolioAnalyticsApi.Services;
using Microsoft.Extensions.Logging;

namespace PortfolioAnalyticsApi.Tests.Services;

[TestFixture]
public class RiskAnalysisServiceTests
{
    private Mock<IPerformanceService> _performanceServiceMock = null!;
    private Mock<IMarketDataRepository> _marketDataRepositoryMock = null!;
    private Mock<IAssetRepository> _assetRepositoryMock = null!;
    private Mock<ILogger<RiskAnalysisService>> _loggerMock = null!;
    private RiskAnalysisService _service = null!;

    [SetUp]
    public void Setup()
    {
        _performanceServiceMock = new Mock<IPerformanceService>();
        _marketDataRepositoryMock = new Mock<IMarketDataRepository>();
        _assetRepositoryMock = new Mock<IAssetRepository>();
        _loggerMock = new Mock<ILogger<RiskAnalysisService>>();
        _service = new RiskAnalysisService(
            _performanceServiceMock.Object,
            _marketDataRepositoryMock.Object,
            _assetRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Test]
    public void GetRiskAnalysis_WhenLargestPositionExceedsThreshold_ShouldReturnHighRisk()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("PETR4", 30d),
            CreatePositionPerformance("VALE3", 20d),
            CreatePositionPerformance("ITUB4", 10d)
        ], annualizedReturn: 12d, volatility: 15d);

        SetupPerformance(performanceResponse);
        SetupAsset("PETR4", "Energy");
        SetupAsset("VALE3", "Mining");
        SetupAsset("ITUB4", "Finance");

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.OverallRisk, Is.EqualTo("High"));
        Assert.That(result.ConcentrationRisk.LargestPosition.Symbol, Is.EqualTo("PETR4"));
    }

    [Test]
    public void GetRiskAnalysis_WhenSectorExceedsThreshold_ShouldReturnHighRisk()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("BBDC4", 24d),
            CreatePositionPerformance("ITUB4", 24d),
            CreatePositionPerformance("WEGE3", 10d)
        ], annualizedReturn: 10d, volatility: 12d);

        SetupPerformance(performanceResponse);
        SetupAsset("BBDC4", "Financial");
        SetupAsset("ITUB4", "Financial");
        SetupAsset("WEGE3", "Industrial");

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.OverallRisk, Is.EqualTo("High"));
    }

    [Test]
    public void GetRiskAnalysis_WhenMaxPositionIsAtLeastFifteen_ShouldReturnMediumRisk()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("PETR4", 15d),
            CreatePositionPerformance("VALE3", 14d),
            CreatePositionPerformance("ITUB4", 10d)
        ], annualizedReturn: 10d, volatility: 12d);

        SetupPerformance(performanceResponse);
        SetupAsset("PETR4", "Energy");
        SetupAsset("VALE3", "Mining");
        SetupAsset("ITUB4", "Financial");

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.OverallRisk, Is.EqualTo("Medium"));
    }

    [Test]
    public void GetRiskAnalysis_WhenMaxSectorIsAtLeastTwentyFive_ShouldReturnMediumRisk()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("BBDC4", 13d),
            CreatePositionPerformance("ITUB4", 12d),
            CreatePositionPerformance("WEGE3", 14d)
        ], annualizedReturn: 10d, volatility: 12d);

        SetupPerformance(performanceResponse);
        SetupAsset("BBDC4", "Financial");
        SetupAsset("ITUB4", "Financial");
        SetupAsset("WEGE3", "Industrial");

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.OverallRisk, Is.EqualTo("Medium"));
    }

    [Test]
    public void GetRiskAnalysis_WhenPositionAndSectorAreBelowMediumThreshold_ShouldReturnLowRisk()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("PETR4", 14d),
            CreatePositionPerformance("VALE3", 11d),
            CreatePositionPerformance("ITUB4", 10d)
        ], annualizedReturn: 10d, volatility: 12d);

        SetupPerformance(performanceResponse);
        SetupAsset("PETR4", "Energy");
        SetupAsset("VALE3", "Mining");
        SetupAsset("ITUB4", "Financial");

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.OverallRisk, Is.EqualTo("Low"));
    }

    [Test]
    public void GetRiskAnalysis_WhenSharpeInputsAreValid_ShouldReturnExpectedSharpeRatio()
    {
        var performanceResponse = CreatePerformanceResponse([], annualizedReturn: 15d, volatility: 10d);
        SetupPerformance(performanceResponse);

        _marketDataRepositoryMock
            .Setup(repository => repository.GetSelicRate())
            .Returns(0.12);

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.SharpeRatio, Is.EqualTo(0.3).Within(0.0001));
    }

    [Test]
    public void GetRiskAnalysis_WhenVolatilityIsNull_ShouldReturnNullSharpeRatio()
    {
        var performanceResponse = CreatePerformanceResponse([], annualizedReturn: 15d, volatility: null);
        SetupPerformance(performanceResponse);

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.SharpeRatio, Is.Null);
    }

    [Test]
    public void GetRiskAnalysis_WhenVolatilityIsZero_ShouldReturnNullSharpeRatio()
    {
        var performanceResponse = CreatePerformanceResponse([], annualizedReturn: 15d, volatility: 0d);
        SetupPerformance(performanceResponse);

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.SharpeRatio, Is.Null);
    }

    [Test]
    public void GetRiskAnalysis_WhenPositionsExist_ShouldReturnTop3Concentration()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("PETR4", 30d),
            CreatePositionPerformance("VALE3", 20d),
            CreatePositionPerformance("ITUB4", 10d),
            CreatePositionPerformance("WEGE3", 5d)
        ], annualizedReturn: 10d, volatility: 12d);

        SetupPerformance(performanceResponse);
        SetupAsset("PETR4", "Energy");
        SetupAsset("VALE3", "Mining");
        SetupAsset("ITUB4", "Financial");
        SetupAsset("WEGE3", "Industrial");

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.ConcentrationRisk.LargestPosition.Symbol, Is.EqualTo("PETR4"));
        Assert.That(result.ConcentrationRisk.LargestPosition.Percentage, Is.EqualTo(30d));
        Assert.That(result.ConcentrationRisk.Top3Concentration, Is.EqualTo(60d));
    }

    [Test]
    public void GetRiskAnalysis_WhenSectorDiversificationIsCalculated_ShouldReturnExpectedPercentagesAndRatings()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("BBDC4", 20d),
            CreatePositionPerformance("ITUB4", 21d),
            CreatePositionPerformance("VALE3", 25d),
            CreatePositionPerformance("WEGE3", 24d)
        ], annualizedReturn: 10d, volatility: 12d);

        SetupPerformance(performanceResponse);
        SetupAsset("BBDC4", "Financial");
        SetupAsset("ITUB4", "Financial");
        SetupAsset("VALE3", "Mining");
        SetupAsset("WEGE3", "Industrial");

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        var financial = result.SectorDiversification.Single(s => s.Sector == "Financial");
        var mining = result.SectorDiversification.Single(s => s.Sector == "Mining");
        var industrial = result.SectorDiversification.Single(s => s.Sector == "Industrial");

        Assert.That(financial.Percentage, Is.EqualTo(41d));
        Assert.That(financial.Risk, Is.EqualTo("High"));
        Assert.That(mining.Percentage, Is.EqualTo(25d));
        Assert.That(mining.Risk, Is.EqualTo("Medium"));
        Assert.That(industrial.Percentage, Is.EqualTo(24d));
        Assert.That(industrial.Risk, Is.EqualTo("Low"));
    }

    [Test]
    public void GetRiskAnalysis_WhenNoPositionsPerformance_ShouldReturnDefaultConcentrationValues()
    {
        var performanceResponse = CreatePerformanceResponse([], annualizedReturn: 10d, volatility: 12d);
        SetupPerformance(performanceResponse);

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.ConcentrationRisk.LargestPosition.Symbol, Is.EqualTo("N/A"));
        Assert.That(result.ConcentrationRisk.LargestPosition.Percentage, Is.EqualTo(0d));
        Assert.That(result.ConcentrationRisk.Top3Concentration, Is.EqualTo(0d));
        Assert.That(result.OverallRisk, Is.EqualTo("Low"));
    }

    [Test]
    public void GetRiskAnalysis_WhenAssetIsMissing_ShouldIgnoreItInSectorDiversification()
    {
        var performanceResponse = CreatePerformanceResponse([
            CreatePositionPerformance("PETR4", 20d),
            CreatePositionPerformance("MISSING", 30d)
        ], annualizedReturn: 10d, volatility: 12d);

        SetupPerformance(performanceResponse);
        SetupAsset("PETR4", "Energy");
        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("MISSING"))
            .Returns((Asset?)null);

        var result = _service.GetRiskAnalysis(CreatePortfolio());

        Assert.That(result.SectorDiversification, Has.Count.EqualTo(1));
        Assert.That(result.SectorDiversification[0].Sector, Is.EqualTo("Energy"));
    }

    private void SetupPerformance(PerformanceResponse performanceResponse)
    {
        _performanceServiceMock
            .Setup(service => service.GetPerformance(It.IsAny<Portfolio>()))
            .Returns(performanceResponse);
    }

    private void SetupAsset(string symbol, string sector)
    {
        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol(symbol))
            .Returns(new Asset { Symbol = symbol, CurrentPrice = 1m, Sector = sector });
    }

    private static Portfolio CreatePortfolio() => new()
    {
        Name = "Test Portfolio",
        UserId = "test",
        TotalInvestment = 1000m,
        CreatedAt = DateTime.UtcNow,
        Positions = []
    };

    private static PerformanceResponse CreatePerformanceResponse(List<PositionPerformance> positions, double annualizedReturn, double? volatility) => new()
    {
        TotalInvestment = 1000m,
        CurrentValue = 1100m,
        TotalReturn = 10d,
        TotalReturnAmount = 100m,
        AnnualizedReturn = annualizedReturn,
        Volatility = volatility,
        PositionsPerformance = positions
    };

    private static PositionPerformance CreatePositionPerformance(string symbol, double weight) => new()
    {
        Symbol = symbol,
        Weight = weight,
        InvestedAmount = 100m,
        CurrentValue = 110m,
        Return = 10d
    };
}
