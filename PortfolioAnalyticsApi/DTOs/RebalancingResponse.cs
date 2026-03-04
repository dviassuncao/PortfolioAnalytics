namespace PortfolioAnalyticsApi.DTOs
{
    public record RebalancingResponse
    {
        public bool NeedsRebalancing { get; init; }
        public required List<CurrentAllocation> CurrentAllocation { get; init; }
        public required List<SuggestedTrade> SuggestedTrades { get; init; }
        public decimal TotalTransactionCost { get; init; }
    }

    public record CurrentAllocation
    {
        public required string Symbol { get; init; }
        public double CurrentWeight { get; init; }
        public double TargetWeight { get; init; }
        public double Deviation { get; init; }
    }

    public record SuggestedTrade
    {
        public required string Symbol { get; init; }
        public required string Action { get; init; }
        public int Quantity { get; init; }
        public decimal EstimatedValue { get; init; }
        public decimal TransactionCost { get; init; }
    }
}
