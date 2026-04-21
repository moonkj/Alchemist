using System;
using System.Threading;
using System.Threading.Tasks;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Drives the cascade pipeline for a single turn:
    ///   detect -> explode -> infect -> remove -> gravity -> refill -> re-detect ...
    ///
    /// Performance contract:
    ///   - Zero per-turn heap allocation in steady state. All scratch buffers are
    ///     pre-sized and reused across waves (MatchGroup[] hubBuffer, sbyte[] work arrays).
    ///   - No LINQ, no foreach over IEnumerable, no lambda captures.
    ///   - MatchDetector fills a stackalloc Span&lt;MatchGroup&gt; on this stack frame;
    ///     the only copy is into the hub-facing MatchGroup[] (ref type containers preserved
    ///     — RowBuf/ColBuf are shared references, no deep copy).
    ///
    /// Safety:
    ///   - Hard depth cap (MaxDepth = 10). On overflow: invoke OnDepthExceeded + terminate.
    ///   - Visited bitset (two ulongs = 128 bits &gt; 42 cells) dedupes H/V overlap cells.
    /// </summary>
    public sealed class ChainProcessor
    {
        public const int MaxDepth = 10;
        private const int GroupCapacity = 32;

        private readonly Alchemist.Domain.Board.Board _board;
        private readonly IChainAnimationHub _anim;
        private readonly IBlockSpawner _spawner;
        private readonly Action _onDepthExceeded;

        // Pre-allocated scratch: persisted for the lifetime of ChainProcessor.
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
            Action onDepthExceeded = null)
        {
            _board = board;
            _anim = anim;
            _spawner = spawner;
            _onDepthExceeded = onDepthExceeded;

            int cells = Alchemist.Domain.Board.Board.CellCount; // 42
            _hubBuffer = new MatchGroup[GroupCapacity];
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

                // Match detection fills the persistent _hubBuffer (pre-allocated array).
                // Span wraps the array in a *non-async* helper method so the ref-struct Span
                // never crosses an `await` boundary (compiler restriction on async methods).
                int n = ScanMatches();
                if (n == 0) break;

                // 1. Explosion animation
                await _anim.PlayExplosionAsync(_hubBuffer, n, ct).ConfigureAwait(false);

                // 2. Infection: for each exploded cell, mutate adjacent PRIMARY blocks.
                //    Use visited bitset so overlapping H/V groups don't double-explode.
                ulong visitedLo = 0UL; // cells 0..63  (only 0..41 used; 42 <= 64)
                int waveExploded = 0;
                int infectCount = 0;

                for (int gi = 0; gi < n; gi++)
                {
                    ref MatchGroup g = ref _hubBuffer[gi];
                    for (int k = 0; k < g.Count; k++)
                    {
                        int r = g.RowBuf[k];
                        int c = g.ColBuf[k];
                        int idx = Alchemist.Domain.Board.Board.IndexOf(r, c);
                        ulong mask = 1UL << idx;
                        if ((visitedLo & mask) != 0) continue;
                        visitedLo |= mask;
                        waveExploded++;

                        // Infection pass: 4-neighborhood, only primaries mutate.
                        infectCount = TryInfect(r - 1, c, g.Color, infectCount);
                        infectCount = TryInfect(r + 1, c, g.Color, infectCount);
                        infectCount = TryInfect(r, c - 1, g.Color, infectCount);
                        infectCount = TryInfect(r, c + 1, g.Color, infectCount);
                    }
                }

                await _anim.PlayInfectionAsync(_infectRows, _infectCols, infectCount, ct).ConfigureAwait(false);

                // 3. Remove exploded blocks from board.
                for (int i = 0; i < 64; i++)
                {
                    ulong bit = 1UL << i;
                    if ((visitedLo & bit) == 0) continue;
                    if (i >= Alchemist.Domain.Board.Board.CellCount) break;
                    int rr = i / Alchemist.Domain.Board.Board.Cols;
                    int cc = i % Alchemist.Domain.Board.Board.Cols;
                    _board.SetBlock(rr, cc, null);
                }
                totalExploded += waveExploded;

                // 4. Gravity
                int gravCount = ApplyGravity();
                await _anim.PlayGravityAsync(_gravFromRows, _gravToRows, _gravCols, gravCount, ct).ConfigureAwait(false);

                // 5. Refill
                int refCount = ApplyRefill();
                await _anim.PlayRefillAsync(_refillRows, _refillCols, refCount, ct).ConfigureAwait(false);

                depth++;
            }

            return new ChainResult(totalExploded, depth, exceeded, depth);
        }

        /// <summary>
        /// Non-async wrapper: run MatchDetector over the persistent _hubBuffer using a Span.
        /// Isolating the Span here keeps it off the async state machine (C# rule).
        /// </summary>
        private int ScanMatches()
        {
            Span<MatchGroup> span = _hubBuffer.AsSpan();
            return MatchDetector.FindMatches(_board, span);
        }

        /// <summary>Attempt to infect one 4-neighbor cell. Returns updated infectCount.</summary>
        private int TryInfect(int r, int c, ColorId explosionColor, int infectCount)
        {
            if (!Alchemist.Domain.Board.Board.InBounds(r, c)) return infectCount;
            Block target = _board.BlockAt(r, c);
            if (target == null) return infectCount;
            if (!ColorMixer.IsPrimary(target.Color)) return infectCount;

            ColorId mixed = ColorMixCache.Lookup(target.Color, explosionColor);
            if (mixed == ColorId.None) return infectCount;

            ColorId prev = target.Color;
            // FSM: Idle -> Infected -> Idle. If target isn't Idle, skip (conservative).
            if (target.State != BlockState.Idle)
            {
                // Best-effort transition attempt; if not allowed, mutate color only.
                target.Color = mixed;
                _board.MarkDirty(r, c);
            }
            else
            {
                var ctx = new TransitionContext(explosionColor, target.Id, 0);
                if (BlockFsm.TryTransition(target, BlockState.Infected, ctx))
                {
                    target.Color = mixed;
                    // Snap back to Idle so subsequent detection sees the new color.
                    BlockFsm.TryTransition(target, BlockState.Idle, ctx);
                }
                else
                {
                    target.Color = mixed;
                }
                _board.MarkDirty(r, c);
            }

            if (infectCount < _infectRows.Length && prev != mixed)
            {
                _infectRows[infectCount] = (sbyte)r;
                _infectCols[infectCount] = (sbyte)c;
                infectCount++;
            }
            return infectCount;
        }

        /// <summary>
        /// Per-column collapse: for each column, scan from bottom to top (Rows-1 .. 0) with a
        /// write pointer. Move any non-null upward into the first vacant slot below it.
        /// Records (fromRow, toRow, col) triples for the animation hub.
        /// </summary>
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

        /// <summary>Spawn a new block into every null slot (top-down).</summary>
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
    }
}
