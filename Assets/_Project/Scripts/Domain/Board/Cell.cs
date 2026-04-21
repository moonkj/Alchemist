using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Board
{
    /// <summary>
    /// Per-coordinate slot on the board. Owns an optional Block reference and terrain metadata.
    /// Value-identity (Row/Col) is fixed at construction; mutable fields are Block/Layer/Filter/Dirty.
    /// POCO — no UnityEngine references. GC-free in steady state (Block slot is a reference swap).
    /// </summary>
    public sealed class Cell
    {
        public readonly int Row;
        public readonly int Col;

        /// <summary>Occupying block, or null if empty. Owned by Board; set via Board.SetBlock.</summary>
        public Block Block;

        public CellLayer Layer;

        /// <summary>Filter terrain tint if Layer == Filter, else ColorId.None.</summary>
        public ColorId FilterColor;

        /// <summary>Flagged by mutating operations; consumed by view/diff layer each frame.</summary>
        public bool IsDirty;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
            Block = null;
            Layer = CellLayer.Ground;
            FilterColor = ColorId.None;
            IsDirty = false;
        }
    }
}
