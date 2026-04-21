using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>스테이지 종료까지 Black 생성 0. Goal 충족 필요.</summary>
    public sealed class NoBlackCondition : BadgeCondition
    {
        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd || !extra.PromptSatisfied) return false;
            return ctx.GetColorsCreated(ColorId.Black) == 0;
        }
    }
}
