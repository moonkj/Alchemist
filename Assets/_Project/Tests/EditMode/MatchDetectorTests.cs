using System;
using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="MatchDetector"/>.
    /// Secondary/tertiary runs of length &gt;= 3 emit groups; primaries never match (Wave 1 §3).
    /// </summary>
    [TestFixture]
    public sealed class MatchDetectorTests
    {
        private static void Place(Board board, int row, int col, ColorId color)
        {
            var b = new Block();
            b.Reset();
            b.Color = color;
            b.State = BlockState.Idle;
            board.SetBlock(row, col, b);
        }

        private static MatchGroup[] NewBuffer(int capacity = 32)
        {
            var buf = new MatchGroup[capacity];
            for (int i = 0; i < buf.Length; i++) buf[i].EnsureBuffers();
            return buf;
        }

        // ---------- Horizontal run of secondaries ----------

        [Test]
        public void Horizontal_ThreePurple_EmitsOneGroupCount3()
        {
            var board = new Board();
            Place(board, 0, 0, ColorId.Purple);
            Place(board, 0, 1, ColorId.Purple);
            Place(board, 0, 2, ColorId.Purple);

            var buf = NewBuffer();
            int n = MatchDetector.FindMatches(board, buf);
            Assert.That(n == 1 && buf[0].Count == 3 && buf[0].Color == ColorId.Purple, Is.True);
        }

        // ---------- Vertical run of secondaries ----------

        [Test]
        public void Vertical_ThreeGreen_EmitsOneGroupCount3()
        {
            var board = new Board();
            Place(board, 0, 2, ColorId.Green);
            Place(board, 1, 2, ColorId.Green);
            Place(board, 2, 2, ColorId.Green);

            var buf = NewBuffer();
            int n = MatchDetector.FindMatches(board, buf);
            Assert.That(n == 1 && buf[0].Count == 3 && buf[0].Color == ColorId.Green, Is.True);
        }

        // ---------- L-shape: H+V sharing a cell => 2 groups (H/V split), 5 unique cells ----------

        [Test]
        public void LShape_HorizontalAndVertical_TwoGroupsFiveUniqueCells()
        {
            var board = new Board();
            // Horizontal run at row=0: (0,0),(0,1),(0,2)
            Place(board, 0, 0, ColorId.Purple);
            Place(board, 0, 1, ColorId.Purple);
            Place(board, 0, 2, ColorId.Purple);
            // Vertical run at col=0: (0,0),(1,0),(2,0)   — shares (0,0)
            Place(board, 1, 0, ColorId.Purple);
            Place(board, 2, 0, ColorId.Purple);

            var buf = NewBuffer();
            int n = MatchDetector.FindMatches(board, buf);

            // Dedupe shared cell via bitset (mirrors ChainProcessor's bitset strategy).
            ulong seen = 0UL;
            int unique = 0;
            for (int gi = 0; gi < n; gi++)
            {
                for (int k = 0; k < buf[gi].Count; k++)
                {
                    int idx = Board.IndexOf(buf[gi].RowBuf[k], buf[gi].ColBuf[k]);
                    ulong mask = 1UL << idx;
                    if ((seen & mask) != 0) continue;
                    seen |= mask;
                    unique++;
                }
            }

            Assert.That(n == 2 && unique == 5, Is.True);
        }

        // ---------- C3: Primary-only runs never match ----------

        [Test]
        public void Primary_ThreeRed_EmitsZeroGroups()
        {
            var board = new Board();
            Place(board, 0, 0, ColorId.Red);
            Place(board, 0, 1, ColorId.Red);
            Place(board, 0, 2, ColorId.Red);

            var buf = NewBuffer();
            int n = MatchDetector.FindMatches(board, buf);
            Assert.That(n, Is.EqualTo(0));
        }

        // ---------- Empty board ----------

        [Test]
        public void EmptyBoard_ZeroGroups()
        {
            var board = new Board();
            var buf = NewBuffer();
            int n = MatchDetector.FindMatches(board, buf);
            Assert.That(n, Is.EqualTo(0));
        }

        // ---------- Alternating colors never match ----------

        [Test]
        public void Alternating_RedYellowRed_ZeroGroups()
        {
            var board = new Board();
            Place(board, 0, 0, ColorId.Red);
            Place(board, 0, 1, ColorId.Yellow);
            Place(board, 0, 2, ColorId.Red);

            var buf = NewBuffer();
            int n = MatchDetector.FindMatches(board, buf);
            Assert.That(n, Is.EqualTo(0));
        }
    }
}
