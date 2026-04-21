using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>팔레트 슬롯 5회 이상 사용 + 프롬프트 퍼펙트.</summary>
    public sealed class PaletteMasterCondition : BadgeCondition
    {
        private readonly int _minUses;

        public PaletteMasterCondition(int minUses = 5)
        {
            _minUses = minUses;
        }

        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd || !extra.PromptSatisfied) return false;
            return ctx.PaletteSlotUses >= _minUses && extra.PromptProgress >= 0.999f;
        }
    }
}
