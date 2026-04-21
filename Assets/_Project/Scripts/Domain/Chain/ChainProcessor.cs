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
        }

        public async Task<ChainResult> ProcessTurnAsync(CancellationToken ct)
        {
            int depth = 0;
            int totalExploded = 0;
            bool exceeded = false;

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
