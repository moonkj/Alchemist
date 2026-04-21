using System;

namespace Alchemist.Domain.Ranking
{
    /// <summary>
    /// 불변 랭킹 엔트리(한 개 기록). readonly struct — 값 복사로 스레드 안전.
    /// WHY struct: 서버 전송/정렬 시 GC 할당 최소. 필드는 JsonUtility 직렬화 호환을 위해 단순 타입만 사용.
    /// </summary>
    public readonly struct RankingEntry : IEquatable<RankingEntry>
    {
        public readonly string PlayerId;
        public readonly string StageId;
        public readonly int Score;
        public readonly int Moves;
        public readonly int MaxChain;
        public readonly long TimestampUtcTicks;
        public readonly RankingCategory Category;

        public RankingEntry(
            string playerId,
            string stageId,
            int score,
            int moves,
            int maxChain,
            long timestampUtcTicks,
            RankingCategory category)
        {
            // WHY null 방지: JsonUtility 역직렬화 시 null 필드가 Equals 폭발하는 것을 원천 차단.
            PlayerId = playerId ?? string.Empty;
            StageId = stageId ?? string.Empty;
            Score = score;
            Moves = moves;
            MaxChain = maxChain;
            TimestampUtcTicks = timestampUtcTicks;
            Category = category;
        }

        public DateTime TimestampUtc => new DateTime(TimestampUtcTicks, DateTimeKind.Utc);

        public bool Equals(RankingEntry other)
        {
            return PlayerId == other.PlayerId
                && StageId == other.StageId
                && Score == other.Score
                && Moves == other.Moves
                && MaxChain == other.MaxChain
                && TimestampUtcTicks == other.TimestampUtcTicks
                && Category == other.Category;
        }

        public override bool Equals(object obj) => obj is RankingEntry e && Equals(e);

        public override int GetHashCode()
        {
            // WHY unchecked: 해시 조합 과정 오버플로 허용(표준 관용구).
            unchecked
            {
                int h = PlayerId?.GetHashCode() ?? 0;
                h = (h * 397) ^ (StageId?.GetHashCode() ?? 0);
                h = (h * 397) ^ Score;
                h = (h * 397) ^ Moves;
                h = (h * 397) ^ MaxChain;
                h = (h * 397) ^ TimestampUtcTicks.GetHashCode();
                h = (h * 397) ^ (int)Category;
                return h;
            }
        }
    }
}
