using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Alchemist.Domain.Ranking;

namespace Alchemist.Services.Ranking
{
    /// <summary>
    /// 로컬 JSON 저장 기반 IRankingService 구현. JsonUtility 호환 DTO 를 경유한다.
    /// WHY 별도 Services 어셈블리: UnityEngine(Application.persistentDataPath) 의존을 도메인에서 분리.
    ///
    /// 저장 포맷:
    ///   path: {Application.persistentDataPath}/ranking_local.json
    ///   schema:
    ///     {
    ///       "entries": [
    ///         { "playerId": "...", "stageId": "...", "score": 0, "moves": 0,
    ///           "maxChain": 0, "timestampUtcTicks": 0, "category": 0 }, ...
    ///       ]
    ///     }
    ///
    /// Phase 3 MVP 범위: 단일 파일, 4 카테고리 엔트리 혼재. 서버 Adapter(Wave 2)는 별도 구현 예정.
    /// </summary>
    public sealed class LocalRankingService : IRankingService
    {
        public const string DefaultFileName = "ranking_local.json";

        private readonly string _filePath;
        private readonly object _gate = new object();

        // 메모리 캐시: 디스크 I/O 최소화. 첫 로드/Submit 시 채워진다.
        private Dictionary<RankingCategory, RankingBoard> _cache;
        private bool _loaded;

        public LocalRankingService(string filePath = null)
        {
            // WHY 지연 평가: 테스트에서 커스텀 경로 주입 가능, 기본은 persistentDataPath.
            _filePath = string.IsNullOrEmpty(filePath)
                ? Path.Combine(Application.persistentDataPath, DefaultFileName)
                : filePath;
        }

        public string FilePath => _filePath;

        public Task SubmitAsync(RankingEntry entry, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            lock (_gate)
            {
                EnsureLoadedLocked();
                GetBoardLocked(entry.Category).Add(entry);
                SaveLocked();
            }
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<RankingEntry>> FetchTopAsync(
            RankingCategory category,
            int topN,
            string stageId = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            List<RankingEntry> result;
            lock (_gate)
            {
                EnsureLoadedLocked();
                var board = GetBoardLocked(category);
                result = string.IsNullOrEmpty(stageId)
                    ? board.Top(topN)
                    : board.TopForStage(stageId, topN);
            }
            return Task.FromResult<IReadOnlyList<RankingEntry>>(result);
        }

        public Task<RankingEntry> FetchPersonalBestAsync(
            string playerId,
            RankingCategory category,
            string stageId = null,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            RankingEntry result;
            lock (_gate)
            {
                EnsureLoadedLocked();
                var board = GetBoardLocked(category);
                if (string.IsNullOrEmpty(stageId))
                {
                    result = board.BestOf(playerId);
                }
                else
                {
                    // stage 한정 BestOf: 임시 보드를 구성하지 않고 Top 결과를 스캔.
                    var top = board.TopForStage(stageId, int.MaxValue);
                    result = default;
                    for (int i = 0; i < top.Count; i++)
                    {
                        if (top[i].PlayerId == playerId) { result = top[i]; break; }
                    }
                }
            }
            return Task.FromResult(result);
        }

        /// <summary>테스트용 초기화 — 캐시/파일 모두 비운다.</summary>
        public void ResetForTests()
        {
            lock (_gate)
            {
                _cache = null;
                _loaded = false;
                try
                {
                    if (File.Exists(_filePath)) File.Delete(_filePath);
                }
                catch (IOException) { /* WHY: 테스트 초기화 실패는 무시(파일 잠김 등). */ }
            }
        }

        // --------------------------------------------------------------
        // locked helpers
        // --------------------------------------------------------------

        private void EnsureLoadedLocked()
        {
            if (_loaded) return;
            _cache = new Dictionary<RankingCategory, RankingBoard>(4);
            _loaded = true;
            if (!File.Exists(_filePath)) return;

            string json;
            try { json = File.ReadAllText(_filePath); }
            catch (IOException) { return; /* WHY: 손상 파일은 빈 상태로 재시작. */ }

            if (string.IsNullOrEmpty(json)) return;

            SerializableStore store;
            try { store = JsonUtility.FromJson<SerializableStore>(json); }
            catch (Exception) { return; }
            if (store?.entries == null) return;

            for (int i = 0; i < store.entries.Length; i++)
            {
                var dto = store.entries[i];
                var entry = new RankingEntry(
                    dto.playerId,
                    dto.stageId,
                    dto.score,
                    dto.moves,
                    dto.maxChain,
                    dto.timestampUtcTicks,
                    (RankingCategory)dto.category);
                GetBoardLocked(entry.Category).Add(entry);
            }
        }

        private RankingBoard GetBoardLocked(RankingCategory category)
        {
            if (!_cache.TryGetValue(category, out var board))
            {
                board = new RankingBoard(category);
                _cache[category] = board;
            }
            return board;
        }

        private void SaveLocked()
        {
            int total = 0;
            foreach (var b in _cache.Values) total += b.Count;
            var dto = new SerializableStore { entries = new SerializableEntry[total] };
            int idx = 0;
            foreach (var kv in _cache)
            {
                var list = kv.Value.Entries;
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    dto.entries[idx++] = new SerializableEntry
                    {
                        playerId = e.PlayerId,
                        stageId = e.StageId,
                        score = e.Score,
                        moves = e.Moves,
                        maxChain = e.MaxChain,
                        timestampUtcTicks = e.TimestampUtcTicks,
                        category = (byte)e.Category,
                    };
                }
            }

            string json = JsonUtility.ToJson(dto, prettyPrint: false);
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(_filePath, json);
        }

        // --------------------------------------------------------------
        // JsonUtility DTO — 반드시 [Serializable] 표기, public 필드.
        // WHY 별도 DTO: readonly struct 는 JsonUtility 직렬화 불가.
        // --------------------------------------------------------------

        [Serializable]
        private sealed class SerializableStore
        {
            public SerializableEntry[] entries;
        }

        [Serializable]
        private struct SerializableEntry
        {
            public string playerId;
            public string stageId;
            public int score;
            public int moves;
            public int maxChain;
            public long timestampUtcTicks;
            public byte category;
        }
    }
}
