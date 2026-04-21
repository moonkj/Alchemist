using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>
    /// 스테이지 종료 시 MovesUsed &lt;= ParMoves 이면서 Goal 충족.
    /// WHY ParMoves &gt; 0 가드: ParMoves 미설정 스테이지(0)에서는 배지 비활성.
    /// </summary>
    public sealed class MinMovesCondition : BadgeCondition
    {
        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd || !extra.PromptSatisfied) return false;
            if (extra.ParMoves <= 0) return false;
            return ctx.MovesUsed <= extra.ParMoves;
        }
    }
}
