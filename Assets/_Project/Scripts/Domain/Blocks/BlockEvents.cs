using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Blocks
{
    public readonly struct BlockSpawned
    {
        public readonly int BlockId;
        public readonly int Row;
        public readonly int Col;
        public readonly ColorId Color;
        public readonly BlockKind Kind;

        public BlockSpawned(int blockId, int row, int col, ColorId color, BlockKind kind)
        {
            BlockId = blockId;
            Row = row;
            Col = col;
            Color = color;
            Kind = kind;
        }
    }

    public readonly struct BlockStateChanged
    {
        public readonly int BlockId;
        public readonly BlockState From;
        public readonly BlockState To;

        public BlockStateChanged(int blockId, BlockState from, BlockState to)
        {
            BlockId = blockId;
            From = from;
            To = to;
        }
    }

    public readonly struct BlockCleared
    {
        public readonly int BlockId;
        public readonly int Row;
        public readonly int Col;
        public readonly ColorId Color;

        public BlockCleared(int blockId, int row, int col, ColorId color)
        {
            BlockId = blockId;
            Row = row;
            Col = col;
            Color = color;
        }
    }

    public readonly struct BlockInfected
    {
        public readonly int BlockId;
        public readonly ColorId OldColor;
        public readonly ColorId NewColor;

        public BlockInfected(int blockId, ColorId oldColor, ColorId newColor)
        {
            BlockId = blockId;
            OldColor = oldColor;
            NewColor = newColor;
        }
    }

    public readonly struct BlockAbsorbed
    {
        public readonly int BlockId;

        public BlockAbsorbed(int blockId)
        {
            BlockId = blockId;
        }
    }

    public readonly struct TransitionContext
    {
        public readonly ColorId ContextColor;
        public readonly int SourceBlockId;
        public readonly int Param;

        public TransitionContext(ColorId contextColor, int sourceBlockId, int param)
        {
            ContextColor = contextColor;
            SourceBlockId = sourceBlockId;
            Param = param;
        }
    }

    public delegate void StateChangedCallback(in BlockStateChanged evt);
}
