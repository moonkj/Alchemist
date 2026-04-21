namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Per-turn summary of cascade processing. Returned by ChainProcessor.ProcessTurnAsync.
    /// </summary>
    /// <remarks>
    /// Wave3-R2: <c>MaxDepth</c> 필드명은 <c>ChainProcessor.MaxDepth</c>(하드캡 상수)와 충돌해
    /// <c>PeakDepth</c>로 리네임. 의미는 '이번 턴 실제 도달한 최대 연쇄 깊이'.
    /// </remarks>
    public readonly struct ChainResult
    {
        /// <summary>이번 턴에 제거된 전체 블록 수.</summary>
        public readonly int TotalExploded;

        /// <summary>이번 턴 실제로 도달한 최대 연쇄 깊이 (하드캡 10 이하).</summary>
        public readonly int PeakDepth;

        /// <summary>하드캡 초과로 강제 종료 여부.</summary>
        public readonly bool DepthExceeded;

        /// <summary>이번 턴 발생한 총 체인 이벤트 수 (현재 모델에서는 PeakDepth와 동일).</summary>
        public readonly int ChainCount;

        public ChainResult(int totalExploded, int peakDepth, bool depthExceeded, int chainCount)
        {
            TotalExploded = totalExploded;
            PeakDepth = peakDepth;
            DepthExceeded = depthExceeded;
            ChainCount = chainCount;
        }
    }
}
