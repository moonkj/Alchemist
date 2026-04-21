namespace Alchemist.Domain.Ranking
{
    /// <summary>
    /// Phase 3 — 4종 랭킹 분류.
    /// WHY 단일 enum: 카테고리별 로직 분기를 IRankingService 구현체 한 곳으로 모아
    /// 서버 Adapter 교체 시 클라 코드 변경을 최소화(Wave 2 대비).
    /// </summary>
    public enum RankingCategory : byte
    {
        /// <summary>전 세계(또는 로컬 전체) 누적 랭킹.</summary>
        Global = 0,

        /// <summary>친구 한정 랭킹. Phase 3 MVP에서는 로컬 플레이어 풀로 시뮬레이션.</summary>
        Friend = 1,

        /// <summary>하루 단위 랭킹. UTC 자정 기준 롤오버. DailyPuzzleId 혹은 당일 타임스탬프로 그룹핑.</summary>
        Daily = 2,

        /// <summary>특정 프롬프트(StageId) 전용 랭킹.</summary>
        Prompt = 3,
    }
}
