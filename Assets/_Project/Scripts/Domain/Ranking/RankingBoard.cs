using System;
using System.Collections.Generic;

namespace Alchemist.Domain.Ranking
{
    /// <summary>
    /// 카테고리 하나에 대한 엔트리 모음. Add/Top-N 추출. 정렬 계약:
    ///   1순위: Score DESC (큰 점수가 상위)
    ///   2순위: Moves ASC (이동 수 적은 쪽)
    ///   3순위: TimestampUtcTicks ASC (먼저 달성한 쪽)
    /// WHY sealed class: 내부 List 보호 + 외부는 ReadOnly 스냅샷만 소비.
    /// </summary>
    public sealed class RankingBoard
    {
        /// <summary>비교자는 static — Top-N 정렬 시 할당 없이 재사용.</summary>
        private static readonly IComparer<RankingEntry> DefaultComparer = new EntryComparer();

        private readonly List<RankingEntry> _entries;
        public RankingCategory Category { get; }

        public RankingBoard(RankingCategory category, int initialCapacity = 32)
        {
            Category = category;
            _entries = new List<RankingEntry>(initialCapacity < 0 ? 0 : initialCapacity);
        }

        public int Count => _entries.Count;

        /// <summary>원본 리스트 뷰. 테스트/JSON 저장 목적 — 직접 수정 금지.</summary>
        public IReadOnlyList<RankingEntry> Entries => _entries;

        /// <summary>엔트리 추가. 카테고리 불일치 시 ArgumentException.</summary>
        public void Add(RankingEntry entry)
        {
            if (entry.Category != Category)
            {
                throw new ArgumentException(
                    $"Entry category {entry.Category} does not match board {Category}.",
                    nameof(entry));
            }
            _entries.Add(entry);
        }

        public void Clear() => _entries.Clear();

        /// <summary>
        /// Top-N 추출(신규 List 반환). topN &lt;= 0 이면 빈 리스트. topN 이 크면 전체 반환.
        /// WHY 사본 반환: 내부 리스트를 외부 정렬이 흔드는 것을 방지.
        /// </summary>
        public List<RankingEntry> Top(int topN)
        {
            if (topN <= 0 || _entries.Count == 0) return new List<RankingEntry>(0);

            var copy = new List<RankingEntry>(_entries);
            copy.Sort(DefaultComparer);
            if (copy.Count > topN) copy.RemoveRange(topN, copy.Count - topN);
            return copy;
        }

        /// <summary>
        /// stageId 한정 Top-N. null/empty stageId 는 전체 Top-N 과 동일.
        /// WHY 별도 메서드: Prompt 카테고리 + StageId 세부 필터용.
        /// </summary>
        public List<RankingEntry> TopForStage(string stageId, int topN)
        {
            if (topN <= 0 || _entries.Count == 0) return new List<RankingEntry>(0);
            if (string.IsNullOrEmpty(stageId)) return Top(topN);

            var filtered = new List<RankingEntry>(_entries.Count);
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].StageId == stageId) filtered.Add(_entries[i]);
            }
            filtered.Sort(DefaultComparer);
            if (filtered.Count > topN) filtered.RemoveRange(topN, filtered.Count - topN);
            return filtered;
        }

        /// <summary>해당 플레이어의 최고 기록(단일). 없으면 default.</summary>
        public RankingEntry BestOf(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return default;
            RankingEntry best = default;
            bool any = false;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (e.PlayerId != playerId) continue;
                if (!any || DefaultComparer.Compare(e, best) < 0)
                {
                    best = e;
                    any = true;
                }
            }
            return best;
        }

        private sealed class EntryComparer : IComparer<RankingEntry>
        {
            public int Compare(RankingEntry x, RankingEntry y)
            {
                // Score DESC
                if (x.Score != y.Score) return y.Score.CompareTo(x.Score);
                // Moves ASC
                if (x.Moves != y.Moves) return x.Moves.CompareTo(y.Moves);
                // Earlier timestamp first
                return x.TimestampUtcTicks.CompareTo(y.TimestampUtcTicks);
            }
        }
    }
}
