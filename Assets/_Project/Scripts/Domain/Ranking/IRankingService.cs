using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Alchemist.Domain.Ranking
{
    /// <summary>
    /// 랭킹 Submit/Fetch 인터페이스. Phase 3 MVP는 로컬(LocalRankingService),
    /// Phase 3 Wave 2에서 서버 Adapter를 별도 구현으로 교체.
    /// WHY Task 기반: 서버 구현 교체 시 호출부(GameRoot 등) 변경 최소화.
    /// </summary>
    public interface IRankingService
    {
        /// <summary>신규 기록 제출. 구현체는 필요한 카테고리 브로드캐스팅(예: Global + Prompt) 책임.</summary>
        Task SubmitAsync(RankingEntry entry, CancellationToken ct = default);

        /// <summary>카테고리별 Top-N. stageId 는 <see cref="RankingCategory.Prompt"/> 에서만 의미 있음.</summary>
        Task<IReadOnlyList<RankingEntry>> FetchTopAsync(
            RankingCategory category,
            int topN,
            string stageId = null,
            CancellationToken ct = default);

        /// <summary>플레이어 개인 최고 기록(카테고리 기준). 없으면 default(RankingEntry).</summary>
        Task<RankingEntry> FetchPersonalBestAsync(
            string playerId,
            RankingCategory category,
            string stageId = null,
            CancellationToken ct = default);
    }
}
