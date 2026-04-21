using System;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Scoring
{
    /// <summary>Payload for a chain tick — struct so publishing is alloc-free.</summary>
    public readonly struct ChainAddedEvent
    {
        public readonly ColorId Color;
        public readonly int BlockCount;
        public readonly int ChainDepth;
        public readonly int PointsAwarded;
        public readonly int TotalAfter;

        public ChainAddedEvent(ColorId color, int blockCount, int chainDepth, int pointsAwarded, int totalAfter)
        {
            Color = color;
            BlockCount = blockCount;
            ChainDepth = chainDepth;
            PointsAwarded = pointsAwarded;
            TotalAfter = totalAfter;
        }
    }

    /// <summary>Payload for stage-end bonus settlement.</summary>
    public readonly struct StageEndedEvent
    {
        public readonly int EfficiencyBonus;
        public readonly int ResidualBonus;
        public readonly int TotalAfter;
        public readonly bool GoalAchieved;

        public StageEndedEvent(int efficiencyBonus, int residualBonus, int totalAfter, bool goalAchieved)
        {
            EfficiencyBonus = efficiencyBonus;
            ResidualBonus = residualBonus;
            TotalAfter = totalAfter;
            GoalAchieved = goalAchieved;
        }
    }

    /// <summary>
    /// Observable score feed. Action subscriptions allocate once at hook-up; per-event
    /// invocations pass struct payloads and do not allocate.
    /// </summary>
    public interface IScoreStream
    {
        /// <summary>Fires whenever <see cref="Score.Total"/> changes. Arg = new Total.</summary>
        event Action<int> ScoreChanged;

        /// <summary>Fires once per explosion resolution within a chain.</summary>
        event Action<ChainAddedEvent> ChainAdded;

        /// <summary>Fires when the active chain resolves (depth returns to 0).</summary>
        event Action<int> TurnEnded;

        /// <summary>Fires exactly once per stage at settlement.</summary>
        event Action<StageEndedEvent> StageEnded;
    }
}
