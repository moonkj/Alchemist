using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>
    /// 회색을 1회 이상 생성(또는 회색 해제)하고 다른 1차/2차 색은 전혀 생성하지 않음.
    /// WHY: "회색만으로 클리어" 의 결정론적 근사 — 색 카운터 기반.
    /// </summary>
    public sealed class GrayOnlyCondition : BadgeCondition
    {
        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd || !extra.PromptSatisfied) return false;
            if (ctx.GetColorsCreated(ColorId.Gray) < 1) return false;

            // 다른 색 0 체크 — 1차/2차/특수(Black/White/Prism) 전부.
            if (ctx.GetColorsCreated(ColorId.Red)    > 0) return false;
            if (ctx.GetColorsCreated(ColorId.Yellow) > 0) return false;
            if (ctx.GetColorsCreated(ColorId.Blue)   > 0) return false;
            if (ctx.GetColorsCreated(ColorId.Orange) > 0) return false;
            if (ctx.GetColorsCreated(ColorId.Green)  > 0) return false;
            if (ctx.GetColorsCreated(ColorId.Purple) > 0) return false;
            if (ctx.GetColorsCreated(ColorId.White)  > 0) return false;
            if (ctx.GetColorsCreated(ColorId.Black)  > 0) return false;
            return true;
        }
    }
}
