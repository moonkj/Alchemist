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
        private int _filterTransits;
        private int _paletteSlotUses;

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
        public int FilterTransits => _filterTransits;
        public int PaletteSlotUses => _paletteSlotUses;

        /// <summary>Phase 2: 필터 셀 통과 1회 기록. ChainProcessor 가 호출.</summary>
        public void RecordFilterTransit(int count = 1)
        {
            if (count <= 0) return;
            _filterTransits += count;
        }

        /// <summary>Phase 2: 팔레트 슬롯 사용(저장/꺼내기) 기록. Palette 이벤트로부터 호출.</summary>
        public void RecordPaletteSlotUse(int count = 1)
        {
            if (count <= 0) return;
            _paletteSlotUses += count;
        }

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
