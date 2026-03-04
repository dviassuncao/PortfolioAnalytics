using PortfolioAnalyticsApi.Endpoints;
using PortfolioAnalyticsApi.Infrastructure;
using PortfolioAnalyticsApi.Repositories;
using PortfolioAnalyticsApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(sp =>
{
    var path = Path.Combine(builder.Environment.ContentRootPath, "Infrastructure", "Data", "SeedData.json");
    return new InMemoryDataContext(path);
});

builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<IPriceHistoryRepository, PriceHistoryRepository>();
builder.Services.AddScoped<IMarketDataRepository, MarketDataRepository>();

builder.Services.AddScoped<IPortfolioCalculatorService, PortfolioCalculatorService>();
builder.Services.AddScoped<IPerformanceService, PerformanceService>();
builder.Services.AddScoped<IRiskAnalysisService, RiskAnalysisService>();
builder.Services.AddScoped<IRebalancingService, RebalancingService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPortfolios();

app.Run();