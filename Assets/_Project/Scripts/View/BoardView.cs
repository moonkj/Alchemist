using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;

namespace Alchemist.View
{
    /// <summary>
    /// MonoBehaviour bridging a domain <see cref="Board"/> to on-screen BlockViews.
    /// Implements <see cref="IChainAnimationHub"/> so ChainProcessor can drive visuals
    /// without a Unity reference leak. Pool-backed; no per-frame allocation.
    /// Layout lives in <see cref="GridCoordinateMapper"/>; this class owns the
    /// (row,col) -> BlockView table and re-uses TaskCompletionSource instances.
    /// </summary>
    public sealed class BoardView : MonoBehaviour, IChainAnimationHub
    {
        [SerializeField] private BlockView _blockPrefab;
        [SerializeField] private Transform _blockRoot;
        [SerializeField] private int _poolCapacity = 120;

        private Board _board;
        private BlockViewPool _pool;

        // (row,col) -> BlockView index; null = empty slot. Sized at Bind time.
        private BlockView[] _viewGrid;
        private int _rows, _cols;

        // Per-anim completion counters so we can await with minimal alloc.
        private int _animPending;
        private TaskCompletionSource<bool> _animTcs;
        private readonly object _animLock = new object();
        // F9 (Wave3): cache method-group delegate to avoid per-call allocation.
        private System.Action _onAnimStepCached;

        public Board Model => _board;

        private void Awake()
        {
            if (_blockRoot == null) _blockRoot = transform;
            if (_onAnimStepCached == null) _onAnimStepCached = OnAnimStep;
        }

        /// <summary>Bind to a domain Board. Creates the view grid + pool; populates visuals.</summary>
        public void Bind(Board board)
        {
            _board = board;
            _rows = Board.Rows;
            _cols = Board.Cols;
            _viewGrid = new BlockView[_rows * _cols];

            if (_pool == null && _blockPrefab != null)
                _pool = new BlockViewPool(_blockPrefab, _blockRoot, _poolCapacity);

            RebuildAllCells();
        }

        /// <summary>
        /// Full sync from domain model. Use sparingly (Bind + test bootstrap). Steady-state
        /// sync should happen via per-cell mutations hooked into Cell.IsDirty diff.
        /// </summary>
        public void RebuildAllCells()
        {
            if (_board == null || _pool == null) return;
            for (int r = 0; r < _rows; r++)
            {
                for (int c = 0; c < _cols; c++)
                {
                    SyncCell(r, c);
                }
            }
        }

        /// <summary>
        /// Sync a single cell. Acquires/releases views from the pool as needed.
        /// O(1); callable from ChainProcessor via a Domain-Events hook (Phase 2).
        /// </summary>
        public void SyncCell(int row, int col)
        {
            if (_board == null || _viewGrid == null || _pool == null) return;
            int idx = row * _cols + col;
            Block b = _board.BlockAt(row, col);
            BlockView v = _viewGrid[idx];

            if (b == null)
            {
                if (v != null) { _pool.Return(v); _viewGrid[idx] = null; }
                return;
            }

            if (v == null)
            {
                v = _pool.Rent();
                v.transform.SetParent(_blockRoot, false);
                _viewGrid[idx] = v;
            }
            v.Bind(b);
            v.SetWorldPosition(GridCoordinateMapper.GridToWorld(row, col));
        }

        /// <summary>Return an existing view (or null) without acquiring one.</summary>
        public BlockView ViewAt(int row, int col)
        {
            if (_viewGrid == null) return null;
            return _viewGrid[row * _cols + col];
        }

        // ---------------- IChainAnimationHub ----------------

        public Task PlayExplosionAsync(MatchGroup[] groups, int count, CancellationToken ct)
        {
            if (groups == null || count <= 0) return Task.CompletedTask;

            int total = 0;
            for (int i = 0; i < count; i++) total += groups[i].Count;
            if (total == 0) return Task.CompletedTask;

            var tcs = BeginBatch(total);
            for (int i = 0; i < count; i++)
            {
                ref var g = ref groups[i];
                int n = g.Count;
                for (int k = 0; k < n; k++)
                {
                    int r = g.RowBuf[k];
                    int c = g.ColBuf[k];
                    BlockView v = ViewAt(r, c);
                    if (v != null)
                        v.PlayExplosion(_onAnimStepCached);
                    else
                        _onAnimStepCached();
                }
            }
            return tcs.Task;
        }

        public Task PlayInfectionAsync(sbyte[] rows, sbyte[] cols, int count, CancellationToken ct)
        {
            if (count <= 0) return Task.CompletedTask;
            var tcs = BeginBatch(count);
            for (int i = 0; i < count; i++)
            {
                BlockView v = ViewAt(rows[i], cols[i]);
                // Re-sync color first (domain already mutated), then pulse.
                if (v != null)
                {
                    Block b = _board != null ? _board.BlockAt(rows[i], cols[i]) : null;
                    if (b != null) v.SetColor(b.Color);
                    v.PlayInfection(_onAnimStepCached);
                }
                else _onAnimStepCached();
            }
            return tcs.Task;
        }

        public Task PlayGravityAsync(sbyte[] fromRows, sbyte[] toRows, sbyte[] cols, int count, CancellationToken ct)
        {
            if (count <= 0 || _viewGrid == null) return Task.CompletedTask;
            // Phase 1 MVP: teleport visuals; no tween yet. Mark all sync'd, return complete.
            // We move the stored view references in _viewGrid to match new positions.
            for (int i = 0; i < count; i++)
            {
                int fr = fromRows[i], tr = toRows[i], c = cols[i];
                int src = fr * _cols + c;
                int dst = tr * _cols + c;
                if (src == dst) continue;
                BlockView v = _viewGrid[src];
                _viewGrid[src] = null;
                // If dst already has a view (shouldn't normally), return it.
                if (_viewGrid[dst] != null) { _pool.Return(_viewGrid[dst]); }
                _viewGrid[dst] = v;
                if (v != null) v.SetWorldPosition(GridCoordinateMapper.GridToWorld(tr, c));
            }
            return Task.CompletedTask;
        }

        public Task PlayRefillAsync(sbyte[] rows, sbyte[] cols, int count, CancellationToken ct)
        {
            if (count <= 0) return Task.CompletedTask;
            var tcs = BeginBatch(count);
            for (int i = 0; i < count; i++)
            {
                int r = rows[i], c = cols[i];
                SyncCell(r, c); // pulls fresh block from domain
                var v = ViewAt(r, c);
                if (v != null) v.PlaySpawn(_onAnimStepCached);
                else _onAnimStepCached();
            }
            return tcs.Task;
        }

        // ---------------- Batch completion plumbing ----------------

        private TaskCompletionSource<bool> BeginBatch(int expected)
        {
            lock (_animLock)
            {
                _animPending = expected;
                _animTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                return _animTcs;
            }
        }

        private void OnAnimStep()
        {
            TaskCompletionSource<bool> toSignal = null;
            lock (_animLock)
            {
                _animPending--;
                if (_animPending <= 0 && _animTcs != null)
                {
                    toSignal = _animTcs;
                    _animTcs = null;
                }
            }
            toSignal?.TrySetResult(true);
        }
    }
}
