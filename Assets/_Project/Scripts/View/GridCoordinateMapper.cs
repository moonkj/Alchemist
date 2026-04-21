using UnityEngine;

namespace Alchemist.View
{
    /// <summary>
    /// Static, value-based utility for converting between grid (row,col) and world (x,y).
    /// Board spec (UX §3.1): 6 cols x 7 rows, cell=52px, gap=4px, centered at world (0,0).
    /// Unity world units == pixels in this project's 2D camera setup (PPU=1 at design scale).
    ///
    /// Convention: row 0 is the TOP row (matches Board POCO row-major where row grows downward
    /// visually); world Y decreases as row grows. Col 0 is LEFT.
    /// All methods are pure, static, and GC-free.
    /// </summary>
    public static class GridCoordinateMapper
    {
        public const int Rows = 7;
        public const int Cols = 6;
        public const float CellSize = 52f;
        public const float CellGap  = 4f;
        public const float Pitch    = CellSize + CellGap; // 56

        public const float BoardWidth  = Cols * CellSize + (Cols - 1) * CellGap; // 332
        public const float BoardHeight = Rows * CellSize + (Rows - 1) * CellGap; // 388

        /// <summary>
        /// Center of cell (row,col) in world space relative to board origin (0,0).
        /// x = -halfWidth + col*Pitch + halfCell
        /// y =  halfHeight - row*Pitch - halfCell
        /// </summary>
        public static Vector2 GridToWorld(int row, int col)
        {
            float halfW = BoardWidth * 0.5f;
            float halfH = BoardHeight * 0.5f;
            float halfC = CellSize * 0.5f;
            float x = -halfW + col * Pitch + halfC;
            float y =  halfH - row * Pitch - halfC;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Nearest cell to world coordinate. Returns false if outside the board footprint
        /// (pixel-accurate, ignoring gaps — callers may widen with <see cref="GridToWorldCellRadius"/>).
        /// </summary>
        public static bool WorldToGrid(Vector2 world, out int row, out int col)
        {
            float halfW = BoardWidth * 0.5f;
            float halfH = BoardHeight * 0.5f;

            float localX = world.x + halfW;
            float localY = halfH - world.y;

            // Integer divide by pitch; reject outside half-pitch past last cell.
            int c = Mathf.FloorToInt(localX / Pitch);
            int r = Mathf.FloorToInt(localY / Pitch);

            if ((uint)r >= (uint)Rows || (uint)c >= (uint)Cols)
            {
                row = -1; col = -1; return false;
            }
            row = r; col = c;
            return true;
        }

        /// <summary>Half-cell distance used by hit-testing (cell core without gap).</summary>
        public static float GridToWorldCellRadius => CellSize * 0.5f;

        /// <summary>Drop-hit expansion per UX §4.1 (16px tolerance around the cell core).</summary>
        public const float DropHitPadding = 16f;
    }
}
