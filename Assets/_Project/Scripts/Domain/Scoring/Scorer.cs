using System;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Scoring
{
    /// <summary>
    /// Stateless formula engine over a <see cref="Score"/> accumulator.
    /// Receives gameplay events (block explosions, turn boundaries, stage end) and applies
    /// the Phase-0 scoring formula. All paths are allocation-free.
    /// </summary>
    public sealed class Scorer : IScoreStream
    {
        private readonly Score _score;
        private int _goalMoveCount; // Captured at stage start — target move count for efficiency bonus

        public Scorer(Score score)
        {
            _score = score ?? throw new ArgumentNullException(nameof(score));
        }

        public Score State => _score;

        // --- IScoreStream ---
        public event Action<int> ScoreChanged;
        public event Action<ChainAddedEvent> ChainAdded;
        public event Action<int> TurnEnded;
        public event Action<StageEndedEvent> StageEnded;

        /// <summary>
        /// Record the target (par) move count used by <see cref="OnStageEnded"/> to compute
        /// the efficiency bonus. Should be called once when the stage starts.
        /// </summary>
        public void BeginStage(int goalMoveCount)
        {
            _goalMoveCount = goalMoveCount < 0 ? 0 : goalMoveCount;
        }

        /// <summary>Mark a player-initiated move. Drives efficiency denominator.</summary>
        public void OnMoveCommitted()
        {
            _score.IncrementMoves();
        }

        /// <summary>
        /// Handle a set of blocks of the same color exploding at a given chain depth.
        /// Returns the points awarded (may be negative for Black contamination).
        /// </summary>
        public int OnBlocksExploded(ColorId color, int count, int chainDepth)
        {
            if (count <= 0) return 0;

            _score.AddColorCreated(color, count);
            _score.SetChainDepth(chainDepth);

            int baseValue = BaseColorValue(color);
            float multiplier = ChainMultiplier(chainDepth);

            // Truncate toward zero — consistent for both positive and negative (Black) payouts.
            // count scales per-block: N blocks exploding in one resolution pay N × base × mult.
            int points = (int)(baseValue * multiplier * count);

            _score.AddPoints(points);

            int total = _score.Total;
            ChainAdded?.Invoke(new ChainAddedEvent(color, count, chainDepth, points, total));
            ScoreChanged?.Invoke(total);
            return points;
        }

        /// <summary>Chain fully resolved — reset depth and notify.</summary>
        public void OnTurnEnded()
        {
            int depthAtEnd = _score.ChainDepth;
            _score.SetChainDepth(0);
            TurnEnded?.Invoke(depthAtEnd);
        }

        /// <summary>
        /// Settle the stage. Applies EfficiencyBonus (if goal achieved) and ResidualBonus.
        /// Returns the final Total after bonuses.
        /// </summary>
        public int OnStageEnded(int movesLimit, int remainingPaletteSlots, bool goalAchieved)
        {
            int efficiency = 0;
            if (goalAchieved)
            {
                int used = _score.MovesUsed;
                if (used > 0 && _goalMoveCount > 0)
                {
                    // ratio = goal / actual; clamp 0..1 then scale to EfficiencyMax.
                    float ratio = (float)_goalMoveCount / used;
                    if (ratio < 0f) ratio = 0f;
                    if (ratio > 1f) ratio = 1f;
                    efficiency = (int)(ratio * ScoreConstants.EfficiencyMax);
                }
                // No goal-move baseline ⇒ no efficiency bonus (defensive default).
            }

            int remainingMoves = movesLimit - _score.MovesUsed;
            if (remainingMoves < 0) remainingMoves = 0;
            if (remainingPaletteSlots < 0) remainingPaletteSlots = 0;

            int residual = remainingMoves * ScoreConstants.ResidualPerRemainingMove
                         + remainingPaletteSlots * ScoreConstants.ResidualPerRemainingSlot;

            _score.AddPoints(efficiency + residual);

            int total = _score.Total;
            StageEnded?.Invoke(new StageEndedEvent(efficiency, residual, total, goalAchieved));
            ScoreChanged?.Invoke(total);
            return total;
        }

        // -------------------- Pure helpers --------------------

        /// <summary>
        /// Depth 1→1.0, 2→1.5, 3→2.0, 4→2.5, 5→2.8, 6→3.1, ...
        /// Depth ≤ 0 defensively maps to 1.0.
        /// </summary>
        public static float ChainMultiplier(int depth)
        {
            if (depth <= 1) return 1.0f;
            if (depth <= 3) return 1.0f + ScoreConstants.ChainStep * (depth - 1);
            return ScoreConstants.ChainBaseHigh + ScoreConstants.ChainStepHigh * (depth - 4);
        }

        /// <summary>BaseColorValue per block for the given ColorId.</summary>
        public static int BaseColorValue(ColorId color)
        {
            // Explicit specials first — Black is a penalty; White is tertiary tier.
            if (color == ColorId.Black) return ScoreConstants.BlackPenalty;
            if (color == ColorId.White) return ScoreConstants.TertiaryValue;

            if (ColorMixer.IsPrimary(color))   return ScoreConstants.PrimaryValue;
            if (ColorMixer.IsSecondary(color)) return ScoreConstants.SecondaryValue;
            if (ColorMixer.IsTertiary(color))  return ScoreConstants.TertiaryValue; // covered by White above, kept for symmetry

            // None / Prism / Gray and unknown flag combinations score nothing.
            return 0;
        }
    }
}
