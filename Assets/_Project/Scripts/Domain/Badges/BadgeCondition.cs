using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges
{
    /// <summary>
    /// 배지 획득 판정 계약. 결정론적(동일 입력 → 동일 출력).
    /// WHY abstract: 구현체가 readonly 필드/파생 파라미터를 자유롭게 보유하면서
    /// BadgeEvaluator 에서 단일 API 로 일괄 평가 가능.
    /// </summary>
    public abstract class BadgeCondition
    {
        public abstract bool Evaluate(IPromptContext ctx, Score score, BadgeEvaluationStats extra);
    }
}
