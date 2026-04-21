using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// Phase 1 IPromptContext implementation. Reads from Score + tracked aggregates.
    /// Chain occurrences are maintained here rather than on Score (keeps Score focused on points).
    /// </summary>
    public sealed class GameContext : IPromptContext
    {
        private readonly Score _score;
        private readonly int[] _chainOccurrencesAtLeast; // indexed by min depth, up to 10
        private int _maxChainDepth;
        private int _movesUsed;
        private int _movesLimit;

        public GameContext(Score score, int movesLimit)
        {
            _score = score;
            _movesLimit = movesLimit;
            _chainOccurrencesAtLeast = new int[16];
        }

        public int GetColorsCreated(ColorId color) => _score.GetColorsCreated(color);
        public int MaxChainDepthAchieved => _maxChainDepth;

        public int ChainEventsCount(int minDepth)
        {
            if (minDepth <= 0) return _chainOccurrencesAtLeast[0];
            if (minDepth >= _chainOccurrencesAtLeast.Length) return 0;
            return _chainOccurrencesAtLeast[minDepth];
        }

        public int MovesUsed => _movesUsed;
        public int MovesLimit => _movesLimit;

        public void RecordChainEvent(int chainDepth)
        {
            if (chainDepth <= 0) return;
            if (chainDepth > _maxChainDepth) _maxChainDepth = chainDepth;
            int cap = _chainOccurrencesAtLeast.Length;
            for (int d = 0; d <= chainDepth && d < cap; d++)
            {
                _chainOccurrencesAtLeast[d]++;
            }
        }

        public void SetMovesUsed(int used)
        {
            _movesUsed = used < 0 ? 0 : used;
        }

        public void SetMovesLimit(int limit)
        {
            _movesLimit = limit < 0 ? 0 : limit;
        }
    }
}
