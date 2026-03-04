using Moq;
using NUnit.Framework;
using PortfolioAnalyticsApi.Infrastructure.Data;
using PortfolioAnalyticsApi.Repositories;
using PortfolioAnalyticsApi.Services;

namespace PortfolioAnalyticsApi.Tests.Services;

[TestFixture]
public class PortfolioCalculatorServiceTests
{
    private Mock<IAssetRepository> _assetRepositoryMock = null!;
    private Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock = null!;
    private PortfolioCalculatorService _service = null!;

    [SetUp]
    public void Setup()
    {
        _assetRepositoryMock = new Mock<IAssetRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _service = new PortfolioCalculatorService(_assetRepositoryMock.Object, _priceHistoryRepositoryMock.Object);
    }

    [Test]
    public void CalculateTotalReturnPercentage_WhenInputsAreValid_ShouldReturnExpectedPercentage()
    {
        var actualReturn = _service.CalculateTotalReturnPercentage(100m, 110m);

        Assert.That(actualReturn, Is.EqualTo(10d));
    }

    [Test]
    public void CalculateTotalReturnPercentage_WhenInvestedIsZero_ShouldReturnZero()
    {
        var actualReturn = _service.CalculateTotalReturnPercentage(0m, 10m);

        Assert.That(actualReturn, Is.EqualTo(0d));
    }

    [Test]
    public void CalculateTotalInvested_WhenPositionsAreProvided_ShouldReturnExpectedTotal()
    {
        var positions = new List<Position>
        {
            new() { AssetSymbol = "PETR4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d },
            new() { AssetSymbol = "VALE3", Quantity = 50, AveragePrice = 20m, TargetAllocation = 0.2d }
        };

        var totalInvested = _service.CalculateTotalInvested(positions);

        Assert.That(totalInvested, Is.EqualTo(2000m));
    }

    [Test]
    public void CalculateCurrentValue_WhenAssetsExist_ShouldReturnExpectedTotal()
    {
        var positions = new List<Position>
        {
            new() { AssetSymbol = "PETR4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d },
            new() { AssetSymbol = "VALE3", Quantity = 50, AveragePrice = 20m, TargetAllocation = 0.2d }
        };

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("PETR4"))
            .Returns(new Asset { Symbol = "PETR4", CurrentPrice = 11m, Sector = "Energy" });

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("VALE3"))
            .Returns(new Asset { Symbol = "VALE3", CurrentPrice = 18m, Sector = "Mining" });

        var currentValue = _service.CalculateCurrentValue(positions);

        Assert.That(currentValue, Is.EqualTo(2000m));
    }

    [Test]
    public void CalculateCurrentValue_WhenAssetIsMissing_ShouldIgnoreMissingAsset()
    {
        var positions = new List<Position>
        {
            new() { AssetSymbol = "PETR4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d },
            new() { AssetSymbol = "VALE3", Quantity = 50, AveragePrice = 20m, TargetAllocation = 0.2d }
        };

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("PETR4"))
            .Returns(new Asset { Symbol = "PETR4", CurrentPrice = 11m, Sector = "Energy" });

        _assetRepositoryMock
            .Setup(repository => repository.GetBySymbol("VALE3"))
            .Returns((Asset?)null);

        var currentValue = _service.CalculateCurrentValue(positions);

        Assert.That(currentValue, Is.EqualTo(1100m));
    }

    [Test]
    public void CalculateAnnualizedReturn_WhenCreatedAtIsInFuture_ShouldReturnTotalReturn()
    {
        var annualizedReturn = _service.CalculateAnnualizedReturn(12d, DateTime.UtcNow.AddDays(10));

        Assert.That(annualizedReturn, Is.EqualTo(12d));
    }

    [Test]
    public void CalculatePortfolioVolatility_WhenHistoryIsAvailable_ShouldReturnExpectedValue()
    {
        var positions = new List<Position>
        {
            new() { AssetSymbol = "PETR4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d }
        };

        var priceHistory = new List<PriceHistory>
        {
            new() { Date = new DateTime(2023, 1, 1), Price = 10.0m },
            new() { Date = new DateTime(2023, 1, 2), Price = 11.0m },
            new() { Date = new DateTime(2023, 1, 3), Price = 10.5m },
            new() { Date = new DateTime(2023, 1, 4), Price = 12.0m }
        };

        _priceHistoryRepositoryMock
            .Setup(repository => repository.GetOrderedPriceHistoryBySymbol("PETR4"))
            .Returns(priceHistory);

        var volatility = _service.CalculatePortfolioVolatility(positions);

        Assert.That(volatility, Is.Not.Null);
        Assert.That(volatility!.Value, Is.EqualTo(127.93519611028874d).Within(0.0000001d));
    }

    [Test]
    public void CalculatePortfolioVolatility_WhenHistoryIsMissing_ShouldReturnNull()
    {
        var positions = new List<Position>
        {
            new() { AssetSymbol = "PETR4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d }
        };

        _priceHistoryRepositoryMock
            .Setup(repository => repository.GetOrderedPriceHistoryBySymbol("PETR4"))
            .Returns(new List<PriceHistory>());

        var volatility = _service.CalculatePortfolioVolatility(positions);

        Assert.That(volatility, Is.Null);
    }

    [Test]
    public void GetDailyPortfolioValues_WhenHistoriesAreAvailableForAllAssets_ShouldReturnExpectedTotals()
    {
        var positions = new List<Position>
        {
            new() { AssetSymbol = "PETR4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d },
            new() { AssetSymbol = "VALE3", Quantity = 50, AveragePrice = 20m, TargetAllocation = 0.2d }
        };

        var petr4History = new List<PriceHistory>
        {
            new() { Date = new DateTime(2023, 1, 1), Price = 10m },
            new() { Date = new DateTime(2023, 1, 2), Price = 11m }
        };

        var vale3History = new List<PriceHistory>
        {
            new() { Date = new DateTime(2023, 1, 1), Price = 20m },
            new() { Date = new DateTime(2023, 1, 2), Price = 18m }
        };

        _priceHistoryRepositoryMock
            .Setup(repository => repository.GetOrderedPriceHistoryBySymbol("PETR4"))
            .Returns(petr4History);

        _priceHistoryRepositoryMock
            .Setup(repository => repository.GetOrderedPriceHistoryBySymbol("VALE3"))
            .Returns(vale3History);

        var dailyValues = _service.GetDailyPortfolioValues(positions);

        Assert.That(dailyValues, Is.Not.Null);
        Assert.That(dailyValues, Has.Count.EqualTo(2));
        Assert.That(dailyValues![0], Is.EqualTo(2000m));
        Assert.That(dailyValues[1], Is.EqualTo(2000m));
    }

    [Test]
    public void CalculatePortfolioVolatility_WhenPriceHistoryHasSinglePoint_ShouldReturnNull()
    {
        var positions = new List<Position>
        {
            new() { AssetSymbol = "PETR4", Quantity = 100, AveragePrice = 10m, TargetAllocation = 0.1d }
        };

        var priceHistory = new List<PriceHistory>
        {
            new() { Date = new DateTime(2023, 1, 1), Price = 10.0m }
        };

        _priceHistoryRepositoryMock
            .Setup(repository => repository.GetOrderedPriceHistoryBySymbol("PETR4"))
            .Returns(priceHistory);

        var volatility = _service.CalculatePortfolioVolatility(positions);

        Assert.That(volatility, Is.Null);
    }
}
