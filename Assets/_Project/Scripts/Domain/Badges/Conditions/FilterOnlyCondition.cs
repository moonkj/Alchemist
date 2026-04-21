using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>
    /// 필터 통과 횟수가 1 이상이면서 팔레트 슬롯 사용은 0 인 상태로 스테이지를 완주.
    /// WHY: "필터만으로 완성" 히든 배지의 결정론적 근사.
    /// </summary>
    public sealed class FilterOnlyCondition : BadgeCondition
    {
        private readonly int _minTransits;

        public FilterOnlyCondition(int minTransits = 1)
        {
            _minTransits = minTransits;
        }

        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd || !extra.PromptSatisfied) return false;
            return ctx.FilterTransits >= _minTransits && ctx.PaletteSlotUses == 0;
        }
    }
}
