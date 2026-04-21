using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// A detected contiguous match (H or V) of secondary/tertiary color.
    /// Wave3 F11: buffers pre-allocated via Init to avoid first-use spikes.
    /// </summary>
    public struct MatchGroup
    {
        public const int Capacity = 16;

        public ColorId Color;
        public int Count;

        public sbyte[] RowBuf;
        public sbyte[] ColBuf;

        public static MatchGroup CreatePooled()
        {
            return new MatchGroup
            {
                RowBuf = new sbyte[Capacity],
                ColBuf = new sbyte[Capacity],
                Color = ColorId.None,
                Count = 0,
            };
        }

        public void EnsureBuffers()
        {
            if (RowBuf == null) RowBuf = new sbyte[Capacity];
            if (ColBuf == null) ColBuf = new sbyte[Capacity];
        }

        public void Reset(ColorId color)
        {
            Color = color;
            Count = 0;
        }

        public void Add(int row, int col)
        {
            if (Count >= Capacity) return;
            RowBuf[Count] = (sbyte)row;
            ColBuf[Count] = (sbyte)col;
            Count++;
        }
    }
}
