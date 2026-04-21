using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Board
{
    /// <summary>
    /// Fixed-size 6x7 (Cols x Rows) board. Internal 1D Cell[] storage indexed r*Cols+c.
    /// All iteration must use nested for-loops (foreach forbidden — GC / IEnumerator boxing).
    /// POCO; no UnityEngine; zero runtime allocation after ctor.
    /// </summary>
    public sealed class Board
    {
        public const int Cols = 6;
        public const int Rows = 7;
        public const int CellCount = Rows * Cols; // 42

        private readonly Cell[] _cells;

        public Board()
        {
            _cells = new Cell[CellCount];
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    _cells[IndexOf(r, c)] = new Cell(r, c);
                }
            }
        }

        /// <summary>Row-major linear index. Inlined by JIT; no bounds check for perf-hot paths.</summary>
        public static int IndexOf(int row, int col)
        {
            return row * Cols + col;
        }

        public static bool InBounds(int row, int col)
        {
            return (uint)row < (uint)Rows && (uint)col < (uint)Cols;
        }

        public Cell CellAt(int row, int col)
        {
            return _cells[row * Cols + col];
        }

        /// <summary>Direct block accessor; null if slot empty. Does not bounds-check (callers must).</summary>
        public Block BlockAt(int row, int col)
        {
            return _cells[row * Cols + col].Block;
        }

        /// <summary>Install or clear a block slot and sync Row/Col on the block itself.</summary>
        public void SetBlock(int row, int col, Block block)
        {
            var cell = _cells[row * Cols + col];
            cell.Block = block;
            cell.IsDirty = true;
            if (block != null)
            {
                block.Row = row;
                block.Col = col;
            }
        }

        public void MarkDirty(int row, int col)
        {
            _cells[row * Cols + col].IsDirty = true;
        }

        /// <summary>
        /// Phase 2 P2-01: Filter/Wall 등 터레인 레이어를 지정하는 단일 API.
        /// WHY: 필터 벽 테스트 픽스처가 Cell.Layer/FilterColor 두 필드를 동시에 설정해야
        /// 일관성이 보장되므로(한 쪽만 설정 시 통과 로직 오작동), 세팅을 한 함수로 강제.
        /// Layer != Filter 일 때 filterColor 는 ColorId.None 으로 클램프된다.
        /// </summary>
        public void SetCellLayer(int row, int col, CellLayer layer, ColorId filterColor)
        {
            var cell = _cells[row * Cols + col];
            cell.Layer = layer;
            cell.FilterColor = layer == CellLayer.Filter ? filterColor : ColorId.None;
            cell.IsDirty = true;
        }

        /// <summary>Clear all IsDirty flags after view consumption. for-loop (no LINQ).</summary>
        public void ClearDirtyFlags()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].IsDirty = false;
            }
        }
    }
}
