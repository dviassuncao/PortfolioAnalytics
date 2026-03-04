namespace PortfolioAnalyticsApi.DTOs
{
    public record RiskAnalysisResponse
    {
        public required string OverallRisk { get; init; }
        public double? SharpeRatio { get; init; }
        public required ConcentrationRisk ConcentrationRisk { get; init; }
        public required List<SectorDiversification> SectorDiversification { get; init; }
    }

    public record ConcentrationRisk
    {
        public required LargestPosition LargestPosition { get; init; }
        public required double Top3Concentration { get; init; }
    }

    public record LargestPosition
    {
        public required string Symbol { get; init; }
        public required double Percentage { get; init; }
    }

    public record SectorDiversification
    {
        public required string Sector { get; init; }
        public required double Percentage { get; init; }
        public required string Risk { get; init; }
    }
}
