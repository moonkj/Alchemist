using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>
    /// 특정 색을 1회 이상 생성했는지.
    /// WHY: "첫 보라/오렌지/그린/블랙/화이트" 배지를 이 하나의 규칙으로 커버.
    /// </summary>
    public sealed class FirstCreationCondition : BadgeCondition
    {
        private readonly ColorId _target;

        public FirstCreationCondition(ColorId target)
        {
            _target = target;
        }

        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            return ctx.GetColorsCreated(_target) >= 1;
        }
    }
}
