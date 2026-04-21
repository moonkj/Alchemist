using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges.Conditions
{
    /// <summary>
    /// 프롬프트 Goal 을 완전 충족하고 진행률이 1 에 도달한 경우.
    /// WHY extra.IsStageEnd 체크: 턴 중간 평가에서 오판 방지.
    /// </summary>
    public sealed class PerfectPromptCondition : BadgeCondition
    {
        public override bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra)
        {
            if (!extra.IsStageEnd) return false;
            if (!extra.PromptSatisfied) return false;
            // WHY 0.999 허용치: float 누적 오차 대비.
            return extra.PromptProgress >= 0.999f;
        }
    }
}
