using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// A detected contiguous match (horizontal or vertical) of secondary/tertiary color.
    /// Fixed-capacity inline buffers keep MatchGroup a value type (struct) with no heap alloc.
    /// Capacity 16 covers the theoretical max single-line run on a 6x7 board (7 rows / 6 cols)
    /// plus safety margin for L/T merges (Phase 1 keeps H/V split; Processor de-dupes via bitset).
    /// </summary>
    public struct MatchGroup
    {
        public const int Capacity = 16;

        public ColorId Color;
        public int Count;

        // Parallel fixed buffers — sbyte row/col pairs (boards never exceed ~127).
        // Public arrays are allocated inline at ctor-time; MatchDetector writes by index.
        // Kept as sbyte[] (reference) rather than `fixed` so we stay verifiable/safe-code
        // and the buffer is shared per-stackalloc-element. Since MatchDetector only ever
        // receives a Span<MatchGroup> allocated by the caller and the struct values are
        // *copied by value*, MatchDetector is responsible for (re)initializing buffers
        // via EnsureBuffers before use.
        public sbyte[] RowBuf;
        public sbyte[] ColBuf;

        /// <summary>Initialize or reuse row/col inline buffers. Called by MatchDetector exactly
        /// once per slot per FindMatches call. Allocates only on first use (cold path).</summary>
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
            if (Count >= Capacity) return; // defensive cap
            RowBuf[Count] = (sbyte)row;
            ColBuf[Count] = (sbyte)col;
            Count++;
        }
    }
}
