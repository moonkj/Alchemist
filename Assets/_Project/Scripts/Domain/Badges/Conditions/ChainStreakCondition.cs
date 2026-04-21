using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>minDepth 이상의 연쇄가 occurrences 회 이상 발생.</summary>
    public sealed class ChainStreakCondition : BadgeCondition
    {
        private readonly int _minDepth;
        private readonly int _occurrences;

        public ChainStreakCondition(int minDepth, int occurrences)
        {
            _minDepth = minDepth;
            _occurrences = occurrences;
        }

        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            return ctx.ChainEventsCount(_minDepth) >= _occurrences;
        }
    }
}
