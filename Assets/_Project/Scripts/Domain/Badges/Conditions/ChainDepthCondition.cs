using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>MaxChainDepthAchieved &gt;= minDepth.</summary>
    public sealed class ChainDepthCondition : BadgeCondition
    {
        private readonly int _minDepth;

        public ChainDepthCondition(int minDepth)
        {
            _minDepth = minDepth;
        }

        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            return score != null && score.MaxChainDepthAchieved >= _minDepth;
        }
    }
}
