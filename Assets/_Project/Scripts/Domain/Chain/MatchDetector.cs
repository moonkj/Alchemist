using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Chain
{
    /// <summary>
    /// Stateless scanner: finds 3+ contiguous same-color runs of secondary/tertiary blocks.
    /// Primary (Red/Yellow/Blue) runs are ignored — design rule (§2.3 architecture.md).
    /// GC-free: writes into a caller-provided Span&lt;MatchGroup&gt;; returns group count.
    /// Phase 1 emits horizontal and vertical runs as separate groups; ChainProcessor
    /// dedupes overlapping cells via a bitset (§ phase1_wave1_addendum C4).
    /// </summary>
    public static class MatchDetector
    {
        private const int MinRunLength = 3;

        /// <summary>Scan the board and populate up to output.Length groups. Returns emitted count.</summary>
        public static int FindMatches(Alchemist.Domain.Board.Board board, System.Span<MatchGroup> output)
        {
            if (board == null || output.Length == 0) return 0;

            int emitted = 0;

            // --- Horizontal pass ---
            for (int r = 0; r < Alchemist.Domain.Board.Board.Rows; r++)
            {
                ColorId runColor = ColorId.None;
                int runLen = 0;
                int runStart = 0;

                for (int c = 0; c < Alchemist.Domain.Board.Board.Cols; c++)
                {
                    Block b = board.BlockAt(r, c);
                    ColorId cur = (b != null) ? b.Color : ColorId.None;
                    bool eligible = b != null && IsMatchEligible(cur);

                    if (eligible && cur == runColor)
                    {
                        runLen++;
                    }
                    else
                    {
                        emitted = FlushHorizontal(output, emitted, r, runStart, runLen, runColor);
                        if (emitted >= output.Length) return emitted;
                        runColor = eligible ? cur : ColorId.None;
                        runLen = eligible ? 1 : 0;
                        runStart = c;
                    }
                }
                emitted = FlushHorizontal(output, emitted, r, runStart, runLen, runColor);
                if (emitted >= output.Length) return emitted;
            }

            // --- Vertical pass ---
            for (int c = 0; c < Alchemist.Domain.Board.Board.Cols; c++)
            {
                ColorId runColor = ColorId.None;
                int runLen = 0;
                int runStart = 0;

                for (int r = 0; r < Alchemist.Domain.Board.Board.Rows; r++)
                {
                    Block b = board.BlockAt(r, c);
                    ColorId cur = (b != null) ? b.Color : ColorId.None;
                    bool eligible = b != null && IsMatchEligible(cur);

                    if (eligible && cur == runColor)
                    {
                        runLen++;
                    }
                    else
                    {
                        emitted = FlushVertical(output, emitted, c, runStart, runLen, runColor);
                        if (emitted >= output.Length) return emitted;
                        runColor = eligible ? cur : ColorId.None;
                        runLen = eligible ? 1 : 0;
                        runStart = r;
                    }
                }
                emitted = FlushVertical(output, emitted, c, runStart, runLen, runColor);
                if (emitted >= output.Length) return emitted;
            }

            return emitted;
        }

        /// <summary>Only secondary (Orange/Green/Purple) or tertiary (White) colors match.</summary>
        private static bool IsMatchEligible(ColorId c)
        {
            return ColorMixer.IsSecondary(c) || ColorMixer.IsTertiary(c);
        }

        private static int FlushHorizontal(System.Span<MatchGroup> output, int emitted,
            int row, int runStart, int runLen, ColorId color)
        {
            if (runLen < MinRunLength) return emitted;
            if (emitted >= output.Length) return emitted;

            ref MatchGroup g = ref output[emitted];
            g.EnsureBuffers();
            g.Reset(color);
            for (int i = 0; i < runLen; i++)
            {
                g.Add(row, runStart + i);
            }
            return emitted + 1;
        }

        private static int FlushVertical(System.Span<MatchGroup> output, int emitted,
            int col, int runStart, int runLen, ColorId color)
        {
            if (runLen < MinRunLength) return emitted;
            if (emitted >= output.Length) return emitted;

            ref MatchGroup g = ref output[emitted];
            g.EnsureBuffers();
            g.Reset(color);
            for (int i = 0; i < runLen; i++)
            {
                g.Add(runStart + i, col);
            }
            return emitted + 1;
        }
    }
}
