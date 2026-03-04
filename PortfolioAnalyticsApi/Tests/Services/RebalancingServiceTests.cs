using Moq;
using NUnit.Framework;
using PortfolioAnalyticsApi.Infrastructure.Data;
using PortfolioAnalyticsApi.Repositories;
using PortfolioAnalyticsApi.Services;

namespace PortfolioAnalyticsApi.Tests.Services;

[TestFixture]
public class RebalancingServiceTests
{
    private Mock<IPortfolioCalculatorService> _calculatorMock = null!;
    private Mock<IAssetRepository> _assetRepositoryMock = null!;
    private RebalancingService _service = null!;

    [SetUp]
    public void Setup()
    {
        _calculatorMock = new Mock<IPortfolioCalculatorService>();
        _assetRepositoryMock = new Mock<IAssetRepository>();
        _service = new RebalancingService(_calculatorMock.Object, _assetRepositoryMock.Object);
    }

    [Test]
    public void GetRebalancing_WhenDeviationIsGreaterThanTwoPercent_ShouldSuggestTrade()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-001",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions = [new() { AssetSymbol = "ITUB4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d }]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(10000m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("ITUB4"))
            .Returns(new Asset { Symbol = "ITUB4", CurrentPrice = 20m, Sector = "Finance" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.SuggestedTrades, Is.Not.Empty);
        Assert.That(result.SuggestedTrades[0].Action, Is.EqualTo("SELL"));
    }

    [Test]
    public void GetRebalancing_WhenDeviationIsLessThanOrEqualToTwoPercent_ShouldNotSuggestTrade()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-002",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions = [new() { AssetSymbol = "ITUB4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.19d }]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(10000m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("ITUB4"))
            .Returns(new Asset { Symbol = "ITUB4", CurrentPrice = 20m, Sector = "Finance" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.SuggestedTrades, Is.Empty);
    }

    [Test]
    public void GetRebalancing_WhenTradeValueIsBelowOneHundred_ShouldNotSuggestTrade()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-003",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions = [new() { AssetSymbol = "MGLU3", Quantity = 10, AveragePrice = 5m, TargetAllocation = 0.05d }]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(100m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("MGLU3"))
            .Returns(new Asset { Symbol = "MGLU3", CurrentPrice = 10m, Sector = "Retail" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.SuggestedTrades, Is.Empty);
    }

    [Test]
    public void GetRebalancing_WhenDeviationIsNegativeAndGreaterThanTwoPercent_ShouldSuggestBuyTrade()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-004",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions = [new() { AssetSymbol = "ITUB4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.3d }]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(10000m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("ITUB4"))
            .Returns(new Asset { Symbol = "ITUB4", CurrentPrice = 20m, Sector = "Finance" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.SuggestedTrades, Has.Count.EqualTo(1));
        Assert.That(result.SuggestedTrades[0].Action, Is.EqualTo("BUY"));
        Assert.That(result.SuggestedTrades[0].Quantity, Is.EqualTo(50));
        Assert.That(result.NeedsRebalancing, Is.True);
    }

    [Test]
    public void GetRebalancing_WhenDeviationIsExactlyTwoPercent_ShouldNotSuggestTrade()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-005",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions = [new() { AssetSymbol = "ITUB4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.18d }]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(10000m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("ITUB4"))
            .Returns(new Asset { Symbol = "ITUB4", CurrentPrice = 20m, Sector = "Finance" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.SuggestedTrades, Is.Empty);
        Assert.That(result.NeedsRebalancing, Is.False);
    }

    [Test]
    public void GetRebalancing_WhenTradeIsSuggested_ShouldCalculateEstimatedValueAndTransactionCost()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-006",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions = [new() { AssetSymbol = "ITUB4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d }]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(10000m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("ITUB4"))
            .Returns(new Asset { Symbol = "ITUB4", CurrentPrice = 20m, Sector = "Finance" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.SuggestedTrades, Has.Count.EqualTo(1));
        Assert.That(result.SuggestedTrades[0].Quantity, Is.EqualTo(50));
        Assert.That(result.SuggestedTrades[0].EstimatedValue, Is.EqualTo(1000m));
        Assert.That(result.SuggestedTrades[0].TransactionCost, Is.EqualTo(3m));
        Assert.That(result.TotalTransactionCost, Is.EqualTo(3m));
    }

    [Test]
    public void GetRebalancing_WhenThereAreMultipleTrades_ShouldOrderByHighestDeviation()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-007",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions =
            [
                new() { AssetSymbol = "ITUB4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d },
                new() { AssetSymbol = "MGLU3", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.05d }
            ]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(10000m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("ITUB4"))
            .Returns(new Asset { Symbol = "ITUB4", CurrentPrice = 30m, Sector = "Finance" });

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("MGLU3"))
            .Returns(new Asset { Symbol = "MGLU3", CurrentPrice = 20m, Sector = "Retail" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.SuggestedTrades, Has.Count.EqualTo(2));
        Assert.That(result.SuggestedTrades[0].Symbol, Is.EqualTo("ITUB4"));
        Assert.That(result.SuggestedTrades[1].Symbol, Is.EqualTo("MGLU3"));
    }

    [Test]
    public void GetRebalancing_WhenAssetIsMissing_ShouldIgnorePositionWithoutAsset()
    {
        var portfolio = new Portfolio
        {
            Name = "Test Portfolio",
            UserId = "user-008",
            TotalInvestment = 1000m,
            CreatedAt = DateTime.UtcNow,
            Positions =
            [
                new() { AssetSymbol = "MISSING", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.2d },
                new() { AssetSymbol = "ITUB4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d }
            ]
        };

        _calculatorMock
            .Setup(calculator => calculator.CalculateCurrentValue(portfolio.Positions))
            .Returns(10000m);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("MISSING"))
            .Returns((Asset?)null);

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("ITUB4"))
            .Returns(new Asset { Symbol = "ITUB4", CurrentPrice = 20m, Sector = "Finance" });

        var result = _service.GetRebalancing(portfolio);

        Assert.That(result.CurrentAllocation.Select(a => a.Symbol), Does.Not.Contain("MISSING"));
        Assert.That(result.SuggestedTrades.Select(t => t.Symbol), Does.Not.Contain("MISSING"));
        Assert.That(result.SuggestedTrades.Select(t => t.Symbol), Does.Contain("ITUB4"));
    }
}
