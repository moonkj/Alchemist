using System;
using System.Threading;
using System.Threading.Tasks;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Scoring;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Cascade pipeline: detect → explode → infect → remove → gravity → refill → re-detect.
    /// Wave3 wiring:
    ///   F4  Scorer optional injection; OnBlocksExploded per group, per wave.
    ///   F6  infectedMask bitset (D20) — primary is infected at most once per wave.
    ///   D19 OnColorCreated fires at infection time (separate from explosion score).
    ///   F10 Removed blocks returned to spawner pool to keep steady-state GC near zero.
    /// </summary>
    public sealed class ChainProcessor
    {
        public const int MaxDepth = 10;
        private const int GroupCapacity = 32;

        private readonly Alchemist.Domain.Board.Board _board;
        private readonly IChainAnimationHub _anim;
        private readonly IBlockSpawner _spawner;
        private readonly Action _onDepthExceeded;
        private readonly Scorer _scorer;

        private readonly MatchGroup[] _hubBuffer;
        private readonly sbyte[] _infectRows;
        private readonly sbyte[] _infectCols;
        private readonly sbyte[] _gravFromRows;
        private readonly sbyte[] _gravToRows;
        private readonly sbyte[] _gravCols;
        private readonly sbyte[] _refillRows;
        private readonly sbyte[] _refillCols;

        // Phase 2 P2-02: 회색 블록 해제 카운터 — 턴마다 Reset, 폭발 때마다 누적.
        // WHY 프로세서 소유: 테스트가 별도 주입 없이도 재활성 로직을 검증하려면
        // ChainProcessor 가 기본적으로 tracker 를 포함하되 ReleaseTracker 프로퍼티로 노출.
        private readonly GrayReleaseTracker _grayTracker;
        public GrayReleaseTracker GrayTracker => _grayTracker;

        public ChainProcessor(
            Alchemist.Domain.Board.Board board,
            IChainAnimationHub anim,
            IBlockSpawner spawner,
            Scorer scorer = null,
            Action onDepthExceeded = null)
        {
            _board = board;
            _anim = anim;
            _spawner = spawner;
            _scorer = scorer;
            _onDepthExceeded = onDepthExceeded;

            int cells = Alchemist.Domain.Board.Board.CellCount;
            _hubBuffer = new MatchGroup[GroupCapacity];
            for (int i = 0; i < GroupCapacity; i++)
            {
                _hubBuffer[i] = MatchGroup.CreatePooled();
            }
            _infectRows = new sbyte[cells];
            _infectCols = new sbyte[cells];
            _gravFromRows = new sbyte[cells];
            _gravToRows = new sbyte[cells];
            _gravCols = new sbyte[cells];
            _refillRows = new sbyte[cells];
            _refillCols = new sbyte[cells];

            _grayTracker = new GrayReleaseTracker(board);
        }

        public async Task<ChainResult> ProcessTurnAsync(CancellationToken ct)
        {
            int depth = 0;
            int totalExploded = 0;
            bool exceeded = false;

            // Phase 2 P2-02: 턴 시작 시 회색 카운터 리셋 — 턴 경계 초과 누적 차단.
            _grayTracker.Reset();

            while (true)
            {
                if (ct.IsCancellationRequested) break;

                if (depth >= MaxDepth)
                {
                    exceeded = true;
                    _onDepthExceeded?.Invoke();
                    break;
                }

                int n = ScanMatches();
                if (n == 0) break;

                await _anim.PlayExplosionAsync(_hubBuffer, n, ct).ConfigureAwait(false);

                ulong visitedLo = 0UL;
                ulong infectedMask = 0UL;
                int waveExploded = 0;
                int infectCount = 0;
                int chainDepth = depth + 1;

                for (int gi = 0; gi < n; gi++)
                {
                    ref MatchGroup g = ref _hubBuffer[gi];
                    int uniqueInGroup = 0;
                    for (int k = 0; k < g.Count; k++)
                    {
                        int r = g.RowBuf[k];
                        int c = g.ColBuf[k];
                        int idx = Alchemist.Domain.Board.Board.IndexOf(r, c);
                        ulong mask = 1UL << idx;
                        if ((visitedLo & mask) != 0) continue;
                        visitedLo |= mask;
                        waveExploded++;
                        uniqueInGroup++;

                        infectCount = TryInfect(r - 1, c, g.Color, infectCount, ref infectedMask);
                        infectCount = TryInfect(r + 1, c, g.Color, infectCount, ref infectedMask);
                        infectCount = TryInfect(r, c - 1, g.Color, infectCount, ref infectedMask);
                        infectCount = TryInfect(r, c + 1, g.Color, infectCount, ref infectedMask);

                        // Phase 2 P2-02: 회색 블록 인접 폭발 카운트 — 중복 폭발 셀은 visitedLo 로 이미 배제됨.
                        _grayTracker.NotifyExplosionAt(r, c, g.Color);
                    }

                    if (_scorer != null && uniqueInGroup > 0)
                    {
                        _scorer.OnBlocksExploded(g.Color, uniqueInGroup, chainDepth);
                    }
                }

                await _anim.PlayInfectionAsync(_infectRows, _infectCols, infectCount, ct).ConfigureAwait(false);

                for (int i = 0; i < 64; i++)
                {
                    ulong bit = 1UL << i;
                    if ((visitedLo & bit) == 0) continue;
                    if (i >= Alchemist.Domain.Board.Board.CellCount) break;
                    int rr = i / Alchemist.Domain.Board.Board.Cols;
                    int cc = i % Alchemist.Domain.Board.Board.Cols;
                    Block removed = _board.BlockAt(rr, cc);
                    _board.SetBlock(rr, cc, null);
                    ReturnToPool(removed);
                }
                totalExploded += waveExploded;

                int gravCount = ApplyGravity();
                await _anim.PlayGravityAsync(_gravFromRows, _gravToRows, _gravCols, gravCount, ct).ConfigureAwait(false);

                int refCount = ApplyRefill();
                await _anim.PlayRefillAsync(_refillRows, _refillCols, refCount, ct).ConfigureAwait(false);

                depth++;
            }

            // Phase 2 P2-02: 캐스케이드 종료 후 누적 폭발 ≥ 임계치인 회색 블록 일괄 해제.
            // WHY 캐스케이드 중간이 아닌 종료 시점: 한 턴 내 여러 폭발 파도를 합산해야
            // "2회 이상" 규칙이 자연스럽게 성립하며, 중간 해제는 연쇄 매칭을 왜곡시킨다.
            _grayTracker.SweepRelease();

            // Phase 2 P2-03: 턴 종료 시 Prism 블록 승격 처리 — 보드가 stable 해진 후 1회.
            PrismAbsorbProcessor.Process(_board);

            if (_scorer != null && totalExploded > 0)
            {
                _scorer.OnTurnEnded();
            }

            return new ChainResult(totalExploded, depth, exceeded, depth);
        }

        private int ScanMatches()
        {
            Span<MatchGroup> span = _hubBuffer.AsSpan();
            return MatchDetector.FindMatches(_board, span);
        }

        private int TryInfect(int r, int c, ColorId explosionColor, int infectCount, ref ulong infectedMask)
        {
            if (!Alchemist.Domain.Board.Board.InBounds(r, c)) return infectCount;
            int idx = Alchemist.Domain.Board.Board.IndexOf(r, c);
            ulong mask = 1UL << idx;
            if ((infectedMask & mask) != 0) return infectCount;

            Block target = _board.BlockAt(r, c);
            if (target == null) return infectCount;
            if (!ColorMixer.IsPrimary(target.Color)) return infectCount;

            ColorId mixed = ColorMixCache.Lookup(target.Color, explosionColor);
            if (mixed == ColorId.None) return infectCount;

            ColorId prev = target.Color;
            if (target.State != BlockState.Idle)
            {
                target.Color = mixed;
                _board.MarkDirty(r, c);
            }
            else
            {
                var ctx = new TransitionContext(explosionColor, target.Id, 0);
                if (BlockFsm.TryTransition(target, BlockState.Infected, ctx))
                {
                    target.Color = mixed;
                    BlockFsm.TryTransition(target, BlockState.Idle, ctx);
                }
                else
                {
                    target.Color = mixed;
                }
                _board.MarkDirty(r, c);
            }

            infectedMask |= mask;

            if (_scorer != null && prev != mixed)
            {
                _scorer.OnColorCreated(mixed, 1);
            }

            if (infectCount < _infectRows.Length && prev != mixed)
            {
                _infectRows[infectCount] = (sbyte)r;
                _infectCols[infectCount] = (sbyte)c;
                infectCount++;
            }
            return infectCount;
        }

        private int ApplyGravity()
        {
            int count = 0;
            int rows = Alchemist.Domain.Board.Board.Rows;
            int cols = Alchemist.Domain.Board.Board.Cols;

            for (int c = 0; c < cols; c++)
            {
                int writeRow = rows - 1;
                for (int r = rows - 1; r >= 0; r--)
                {
                    Block b = _board.BlockAt(r, c);
                    if (b == null) continue;
                    if (r != writeRow)
                    {
                        // Phase 2 P2-01: 낙하 경로(r → writeRow) 중간 Filter 셀을 통과하면 색 변환.
                        // WHY r..writeRow 중간만: 출발 셀(r)은 이미 이전 상태, 착지 셀(writeRow)은 착지지점이므로
                        // "통과" 의미를 엄격히 적용하려면 (r, writeRow) 개구간이 타당하나,
                        // 블록이 Filter 셀 위에 정지할 수도 있으므로 착지 셀도 포함 — Wave1 FilterTransit 스펙 반영.
                        ApplyFilterTransits(b, r, writeRow, c);

                        _board.SetBlock(writeRow, c, b);
                        _board.SetBlock(r, c, null);
                        if (count < _gravFromRows.Length)
                        {
                            _gravFromRows[count] = (sbyte)r;
                            _gravToRows[count] = (sbyte)writeRow;
                            _gravCols[count] = (sbyte)c;
                            count++;
                        }
                    }
                    writeRow--;
                }
            }
            return count;
        }

        /// <summary>
        /// Phase 2 P2-01: 낙하 통과 경로의 Filter 셀마다 색을 Mix 하고 FilterTransit 전이.
        /// WHY: 블록이 위→아래 여러 Filter 를 누적 통과하면 각 단계마다 Mix 적용.
        /// WHY 특수 블록 제외: Prism/Gray 는 규칙 외 (wildcard/inert).
        /// </summary>
        private void ApplyFilterTransits(Block b, int fromRow, int toRow, int col)
        {
            if (b == null) return;
            if (b.Kind != BlockKind.Normal) return;

            for (int pr = fromRow + 1; pr <= toRow; pr++)
            {
                var cell = _board.CellAt(pr, col);
                if (cell.Layer != Alchemist.Domain.Board.CellLayer.Filter) continue;
                if (cell.FilterColor == ColorId.None) continue;

                ColorId mixed = ColorMixCache.Lookup(b.Color, cell.FilterColor);
                // WHY None 결과 스킵: 무효한 혼합(예: Yellow+Yellow=Yellow 아닌 미정)은 원색 유지.
                if (mixed == ColorId.None) continue;

                var ctx = new TransitionContext(cell.FilterColor, b.Id, 0);
                // FSM 가능 시 transit 상태를 한 사이클 거친 뒤 Idle 복귀 — 기존 전이 테이블 재사용.
                if (b.State == BlockState.Idle && BlockFsm.TryTransition(b, BlockState.FilterTransit, ctx))
                {
                    b.Color = mixed;
                    BlockFsm.TryTransition(b, BlockState.Idle, ctx);
                }
                else
                {
                    // Spawned/Infected 등 다른 상태라도 색 변환은 발생(시각 효과만 생략).
                    b.Color = mixed;
                }
                _board.MarkDirty(pr, col);
            }
        }

        private int ApplyRefill()
        {
            int count = 0;
            int rows = Alchemist.Domain.Board.Board.Rows;
            int cols = Alchemist.Domain.Board.Board.Cols;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (_board.BlockAt(r, c) != null) continue;
                    Block nb = _spawner.SpawnRandom(r, c);
                    _board.SetBlock(r, c, nb);
                    if (_scorer != null)
                    {
                        _scorer.OnColorCreated(nb.Color, 1);
                    }
                    if (count < _refillRows.Length)
                    {
                        _refillRows[count] = (sbyte)r;
                        _refillCols[count] = (sbyte)c;
                        count++;
                    }
                }
            }
            return count;
        }

        private void ReturnToPool(Block b)
        {
            if (b == null) return;
            if (_spawner is DeterministicBlockSpawner dps)
            {
                dps.Return(b);
            }
        }
    }
}
