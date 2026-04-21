using System.Collections.Generic;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Badges
{
    /// <summary>
    /// 매 턴/스테이지 종료 시 BadgeRegistry 를 스캔하여 신규 획득 배지를 보고.
    /// WHY stateful: 이미 획득한 배지를 재보고하지 않도록 내부 HashSet 소유.
    /// Evaluate 는 할당 없이 재사용 가능한 출력 버퍼(List&lt;BadgeId&gt;) 를 요구.
    /// </summary>
    public sealed class BadgeEvaluator
    {
        private readonly HashSet<BadgeId> _awarded = new HashSet<BadgeId>();
        private readonly IReadOnlyList<Badge> _catalog;

        public BadgeEvaluator(IReadOnlyList<Badge> catalog = null)
        {
            _catalog = catalog ?? BadgeRegistry.All;
        }

        public IReadOnlyCollection<BadgeId> Awarded => _awarded;

        public bool HasAwarded(BadgeId id) => _awarded.Contains(id);

        /// <summary>
        /// 현재 컨텍스트로 배지 조건을 스캔. 새로 달성된 항목을 newlyAwarded 에 append.
        /// WHY List 재사용: 턴마다 호출되므로 GC 최소화.
        /// </summary>
        public void Evaluate(
            IPromptContext ctx,
            Score score,
            BadgeEvaluationStats extra,
            List<BadgeId> newlyAwarded)
        {
            if (newlyAwarded == null) return;
            for (int i = 0; i < _catalog.Count; i++)
            {
                var badge = _catalog[i];
                if (_awarded.Contains(badge.Id)) continue;
                if (badge.Condition == null) continue;
                if (badge.Condition.Evaluate(ctx, score, extra))
                {
                    _awarded.Add(badge.Id);
                    newlyAwarded.Add(badge.Id);
                }
            }
        }

        /// <summary>테스트/새 스테이지 용 초기화.</summary>
        public void Reset()
        {
            _awarded.Clear();
        }
    }
}
