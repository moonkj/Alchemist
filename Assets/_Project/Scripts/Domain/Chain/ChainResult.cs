namespace Alchemist.Domain.Chain
{
    /// <summary>Per-turn summary of cascade processing. Returned by ChainProcessor.ProcessTurnAsync.</summary>
    public readonly struct ChainResult
    {
        public readonly int TotalExploded;
        public readonly int MaxDepth;
        public readonly bool DepthExceeded;
        public readonly int ChainCount;

        public ChainResult(int totalExploded, int maxDepth, bool depthExceeded, int chainCount)
        {
            TotalExploded = totalExploded;
            MaxDepth = maxDepth;
            DepthExceeded = depthExceeded;
            ChainCount = chainCount;
        }
    }
}
