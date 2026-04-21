using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>Orange, Green, Purple 각각 1회 이상 생성.</summary>
    public sealed class AllSecondariesCondition : BadgeCondition
    {
        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            return ctx.GetColorsCreated(ColorId.Orange) >= 1
                && ctx.GetColorsCreated(ColorId.Green)  >= 1
                && ctx.GetColorsCreated(ColorId.Purple) >= 1;
        }
    }
}
