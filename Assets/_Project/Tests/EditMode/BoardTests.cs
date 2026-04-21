using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Board;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="Board"/>: constants, indexing, bounds, and block slot I/O.
    /// </summary>
    [TestFixture]
    public sealed class BoardTests
    {
        // ---------- Constants ----------

        [Test]
        public void Constants_Rows_Is7()
        {
            Assert.That(Board.Rows, Is.EqualTo(7));
        }

        [Test]
        public void Constants_Cols_Is6()
        {
            Assert.That(Board.Cols, Is.EqualTo(6));
        }

        [Test]
        public void Constants_CellCount_Is42()
        {
            Assert.That(Board.CellCount, Is.EqualTo(42));
        }

        // ---------- IndexOf ----------

        [Test]
        public void IndexOf_FirstCell_IsZero()
        {
            Assert.That(Board.IndexOf(0, 0), Is.EqualTo(0));
        }

        [Test]
        public void IndexOf_LastCell_Is41()
        {
            Assert.That(Board.IndexOf(Board.Rows - 1, Board.Cols - 1), Is.EqualTo(Board.CellCount - 1));
        }

        [Test]
        public void IndexOf_SecondRowFirstCol_EqualsCols()
        {
            Assert.That(Board.IndexOf(1, 0), Is.EqualTo(Board.Cols));
        }

        [Test]
        public void IndexOf_FormulaIsRowTimesColsPlusCol()
        {
            bool allMatch = true;
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    if (Board.IndexOf(r, c) != r * Board.Cols + c) { allMatch = false; break; }
                }
                if (!allMatch) break;
            }
            Assert.That(allMatch, Is.True);
        }

        // ---------- InBounds ----------

        [Test]
        public void InBounds_ValidCorner_True()
        {
            Assert.That(Board.InBounds(0, 0), Is.True);
        }

        [Test]
        public void InBounds_FarCorner_True()
        {
            Assert.That(Board.InBounds(Board.Rows - 1, Board.Cols - 1), Is.True);
        }

        [Test]
        public void InBounds_NegativeRow_False()
        {
            Assert.That(Board.InBounds(-1, 0), Is.False);
        }

        [Test]
        public void InBounds_NegativeCol_False()
        {
            Assert.That(Board.InBounds(0, -1), Is.False);
        }

        [Test]
        public void InBounds_RowOverflow_False()
        {
            Assert.That(Board.InBounds(Board.Rows, 0), Is.False);
        }

        [Test]
        public void InBounds_ColOverflow_False()
        {
            Assert.That(Board.InBounds(0, Board.Cols), Is.False);
        }

        // ---------- BlockAt / SetBlock round-trip ----------

        [Test]
        public void SetBlockThenBlockAt_ReturnsSameReference()
        {
            var board = new Board();
            var b = new Block();
            b.Reset();
            b.Color = ColorId.Red;
            board.SetBlock(2, 3, b);
            Assert.That(board.BlockAt(2, 3), Is.SameAs(b));
        }

        [Test]
        public void SetBlock_SyncsRowColOnBlock()
        {
            var board = new Board();
            var b = new Block();
            b.Reset();
            board.SetBlock(4, 5, b);
            Assert.That(b.Row == 4 && b.Col == 5, Is.True);
        }

        [Test]
        public void BlockAt_InitiallyNull()
        {
            var board = new Board();
            Assert.That(board.BlockAt(0, 0), Is.Null);
        }

        [Test]
        public void SetBlock_Null_ClearsSlot()
        {
            var board = new Board();
            var b = new Block();
            b.Reset();
            board.SetBlock(1, 1, b);
            board.SetBlock(1, 1, null);
            Assert.That(board.BlockAt(1, 1), Is.Null);
        }

        [Test]
        public void CellAt_ReturnsCellWithCorrectCoords()
        {
            var board = new Board();
            var cell = board.CellAt(3, 4);
            Assert.That(cell.Row == 3 && cell.Col == 4, Is.True);
        }
    }
}
