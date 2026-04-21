using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>
    /// 프리즘 블록을 생성/관여(ColorsCreated[Prism] &gt;= 1) 하면서 필터 통과/팔레트 슬롯 사용 0.
    /// WHY: "프리즘만 사용" 히든 배지 — 다른 보조 메카닉을 쓰지 않음을 확인.
    /// </summary>
    public sealed class PrismOnlyCondition : BadgeCondition
    {
        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd || !extra.PromptSatisfied) return false;
            if (ctx.GetColorsCreated(ColorId.Prism) < 1) return false;
            return ctx.FilterTransits == 0 && ctx.PaletteSlotUses == 0;
        }
    }
}
