namespace PortfolioAnalyticsApi.DTOs
{
    public record PerformanceResponse
    {
        public required decimal TotalInvestment { get; init; }     // Soma de (Qtde * Preço Médio)
        public required decimal CurrentValue { get; init; }        // Soma de (Qtde * Preço Atual)
        public required double TotalReturn { get; init; }          // A porcentagem (ex: 8.50)
        public required decimal TotalReturnAmount { get; init; }   // O lucro em Reais (Current - Invested)
        public required double AnnualizedReturn { get; init; }     // O cálculo que fizemos com a data
        public double? Volatility { get; init; }                   // Desvio padrão (pode ser null)
        public required List<PositionPerformance> PositionsPerformance { get; init; }
    }


    public record PositionPerformance
    {
        public required string Symbol { get; init; }
        public required decimal InvestedAmount { get; init; } // Qtde * Preço Médio
        public required decimal CurrentValue { get; init; }   // Qtde * Preço Atual
        public required double Return { get; init; }         // Retorno individual (%)
        public required double Weight { get; init; }         // Peso no portfólio (%)
    }
}
