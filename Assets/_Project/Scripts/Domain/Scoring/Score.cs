using System;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Scoring
{
    /// <summary>
    /// Accumulating score state for a single stage run. Pure POCO, zero-alloc steady-state.
    /// ColorsCreated is an int[256] indexed by (byte)ColorId to avoid Dictionary boxing.
    /// Mutations are internal; external systems drive via <see cref="Scorer"/>.
    /// </summary>
    public sealed class Score
    {
        public int Total { get; private set; }
        public int ChainDepth { get; private set; }
        public int MovesUsed { get; private set; }
        public int MaxChainDepthAchieved { get; private set; }

        // ColorId is a [Flags] byte enum; Black=8, Prism=16, Gray=32 all fit.
        // 256 covers every bit combination reachable via byte-cast.
        private readonly int[] _colorsCreated = new int[256];

        public int GetColorsCreated(ColorId color)
        {
            return _colorsCreated[(byte)color];
        }

        internal void AddColorCreated(ColorId color, int count)
        {
            if (count <= 0) return;
            _colorsCreated[(byte)color] += count;
        }

        internal void AddPoints(int points)
        {
            // Clamp Total at MinTotal (configured 0). Penalties (Black) can shave but not sink.
            int next = Total + points;
            if (next < ScoreConstants.MinTotal) next = ScoreConstants.MinTotal;
            Total = next;
        }

        internal void SetChainDepth(int depth)
        {
            if (depth < 0) depth = 0;
            ChainDepth = depth;
            if (depth > MaxChainDepthAchieved) MaxChainDepthAchieved = depth;
        }

        internal void IncrementMoves()
        {
            MovesUsed++;
        }

        /// <summary>Clear all state for re-use across stages (GC-free).</summary>
        public void Reset()
        {
            Total = 0;
            ChainDepth = 0;
            MovesUsed = 0;
            MaxChainDepthAchieved = 0;
            Array.Clear(_colorsCreated, 0, _colorsCreated.Length);
        }
    }
}
