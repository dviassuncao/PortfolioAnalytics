using Microsoft.AspNetCore.Mvc;
using PortfolioAnalyticsApi.Repositories;
using PortfolioAnalyticsApi.Services;

namespace PortfolioAnalyticsApi.Endpoints
{
    public static class PortfolioEndpoints
    {
        public static RouteGroupBuilder MapPortfolios(this IEndpointRouteBuilder app)
        {
            var portfoliosGroup = app.MapGroup("/api/portfolios")
                .WithTags("Portfolio Analytics")
                .WithOpenApi();

            portfoliosGroup.MapGet("/{userId}/performance", GetPerformance)
                .WithName("GetPerformance")
                .WithSummary("Retorna métricas de performance do portfólio")
                .WithDescription("Calcula retorno total, retorno anualizado, volatilidade e performance de cada posição")
                .Produces(200)
                .Produces(404);

            portfoliosGroup.MapGet("/{userId}/risk-analysis", GetRiskAnalysis)
                .WithName("GetRiskAnalysis")
                .WithSummary("Analisa risco e diversificação do portfólio")
                .WithDescription("Calcula Sharpe Ratio, concentração de risco, diversificação por setor e gera recomendações")
                .Produces(200)
                .Produces(404);

            portfoliosGroup.MapGet("/{userId}/rebalancing", GetRebalancing)
                .WithName("GetRebalancing")
                .WithSummary("Sugere ajustes para otimizar o portfólio")
                .WithDescription("Identifica desvios da estratégia target e sugere transações para rebalanceamento")
                .Produces(200)
                .Produces(404);

            return portfoliosGroup;
        }

        private static IResult GetPerformance(
            string userId,
            [FromServices] IPortfolioRepository portfolioRepository,
            [FromServices] IPerformanceService performanceService)
        {
            var portfolio = portfolioRepository.GetByUserId(userId);
            if (portfolio is null)
                return Results.NotFound();

            var performance = performanceService.GetPerformance(portfolio);
            return Results.Ok(performance);
        }

        private static IResult GetRiskAnalysis(
            string userId,
            [FromServices] IPortfolioRepository portfolioRepository,
            [FromServices] IRiskAnalysisService riskAnalysisService)
        {
            var portfolio = portfolioRepository.GetByUserId(userId);
            if (portfolio is null)
                return Results.NotFound();

            var riskAnalysis = riskAnalysisService.GetRiskAnalysis(portfolio);
            return Results.Ok(riskAnalysis);
        }

        private static IResult GetRebalancing(
            string userId,
            [FromServices] IPortfolioRepository portfolioRepository,
            [FromServices] IRebalancingService rebalancingService)
        {
            var portfolio = portfolioRepository.GetByUserId(userId);

            if (portfolio is null)
                return Results.NotFound();

            var rebalancing = rebalancingService.GetRebalancing(portfolio);
            return Results.Ok(rebalancing);
        }
    }
}
