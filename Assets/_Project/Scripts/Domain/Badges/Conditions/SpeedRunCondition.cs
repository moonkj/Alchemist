using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>
    /// ParMoves 의 절반 이하로 Goal 충족.
    /// WHY 절반 기준: MinMovesCondition(Par 이하) 과 구분되는 상위 도전 배지.
    /// </summary>
    public sealed class SpeedRunCondition : BadgeCondition
    {
        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd || !extra.PromptSatisfied) return false;
            if (extra.ParMoves <= 0) return false;
            // 정수 나눗셈 — Par=3 이면 threshold=1 (엄격한 해석).
            int threshold = extra.ParMoves / 2;
            if (threshold <= 0) return false;
            return ctx.MovesUsed <= threshold;
        }
    }
}
