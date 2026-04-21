using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="ChainProcessor"/> using <see cref="NoOpAnimationHub"/>.
    /// Verifies single-match handling, infection propagation onto primaries, and the
    /// depth=10 hard-cap fallback with the onDepthExceeded callback.
    /// </summary>
    [TestFixture]
    public sealed class ChainProcessorTests
    {
        // -------------------- helpers --------------------

        private static Block MakeBlock(ColorId color)
        {
            var b = new Block();
            b.Reset();
            b.Color = color;
            b.State = BlockState.Idle;
            b.Kind = BlockKind.Normal;
            return b;
        }

        private static void Place(Board board, int row, int col, ColorId color)
        {
            board.SetBlock(row, col, MakeBlock(color));
        }

        /// <summary>Spawn a fixed color into every refill slot — useful for forcing deterministic waves.</summary>
        private sealed class ConstantSpawner : IBlockSpawner
        {
            private readonly ColorId _color;
            private int _nextId = 1000;
            public ConstantSpawner(ColorId color) { _color = color; }
            public Block SpawnRandom(int row, int col)
            {
                var b = new Block();
                b.Reset();
                b.Id = _nextId++;
                b.Color = _color;
                b.Row = row;
                b.Col = col;
                b.State = BlockState.Idle;
                b.Kind = BlockKind.Normal;
                return b;
            }
        }

        /// <summary>Spawner that always produces Purple (a secondary). Each refill wave
        /// immediately reproduces a 3+ match somewhere, driving the depth cap.</summary>
        private sealed class PurpleSpawner : IBlockSpawner
        {
            private int _nextId = 5000;
            public Block SpawnRandom(int row, int col)
            {
                var b = new Block();
                b.Reset();
                b.Id = _nextId++;
                b.Color = ColorId.Purple;
                b.Row = row;
                b.Col = col;
                b.State = BlockState.Idle;
                b.Kind = BlockKind.Normal;
                return b;
            }
        }

        // -------------------- tests --------------------

        [Test]
        public void EmptyBoard_NoMatches_ZeroExplodedZeroDepth()
        {
            var board = new Board();
            var spawner = new ConstantSpawner(ColorId.Red); // refill never matches (primaries)
            // Fill every cell first so refill stage is a no-op on the first pass.
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    Place(board, r, c, ColorId.Red);
                }
            }

            var proc = new ChainProcessor(board, new NoOpAnimationHub(), spawner);
            var result = proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();
            Assert.That(result.TotalExploded == 0 && result.MaxDepth == 0 && result.DepthExceeded == false, Is.True);
        }

        [Test]
        public void SingleHorizontalPurpleMatch_ExplodesThreeAndCompletes()
        {
            var board = new Board();
            // Fill everything with Red (primary, no match) so only our triad triggers.
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    Place(board, r, c, ColorId.Red);

            // Overwrite a horizontal triad of Purple at the bottom row.
            Place(board, 6, 0, ColorId.Purple);
            Place(board, 6, 1, ColorId.Purple);
            Place(board, 6, 2, ColorId.Purple);

            var spawner = new ConstantSpawner(ColorId.Red); // refill with primaries: no cascade
            var proc = new ChainProcessor(board, new NoOpAnimationHub(), spawner);
            var result = proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.TotalExploded, Is.EqualTo(3));
        }

        [Test]
        public void SingleMatch_DepthEqualsOne()
        {
            var board = new Board();
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    Place(board, r, c, ColorId.Red);

            Place(board, 6, 0, ColorId.Purple);
            Place(board, 6, 1, ColorId.Purple);
            Place(board, 6, 2, ColorId.Purple);

            var spawner = new ConstantSpawner(ColorId.Red);
            var proc = new ChainProcessor(board, new NoOpAnimationHub(), spawner);
            var result = proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.MaxDepth, Is.EqualTo(1));
        }

        [Test]
        public void InfectionPropagation_AdjacentPrimaryMutates()
        {
            // A horizontal Purple triad with a Red primary neighbor directly above the
            // middle cell should get infected to Mix(Red, Purple) = None -> skipped.
            // To observe a visible mutation, use a Yellow neighbor instead:
            //     Mix(Yellow, Purple) = Mix(R|Y, ... wait) -> Yellow(R? no) => let's compute:
            // Yellow = 0b010, Purple = R|B = 0b101 -> combined = 0b111 = White. Valid!
            var board = new Board();
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                    Place(board, r, c, ColorId.Red);

            // Row 6 = Purple triad; row 5 col 1 = Yellow primary target.
            Place(board, 6, 0, ColorId.Purple);
            Place(board, 6, 1, ColorId.Purple);
            Place(board, 6, 2, ColorId.Purple);
            Place(board, 5, 1, ColorId.Yellow);

            // Capture the block reference BEFORE processing so we can inspect post-state.
            var target = board.BlockAt(5, 1);

            var spawner = new ConstantSpawner(ColorId.Red);
            var proc = new ChainProcessor(board, new NoOpAnimationHub(), spawner);
            proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            // After infection, target.Color should be Mix(Yellow, Purple) = White.
            Assert.That(target.Color, Is.EqualTo(ColorId.White));
        }

        [Test]
        public void InfiniteCascade_CappedAtDepth10_FiresOnDepthExceeded()
        {
            // Seed the board with nothing so gravity leaves slots empty, then refill
            // fills every cell with Purple. Each pass produces many 3+ matches ->
            // explode -> empty -> refill with Purple -> match again -> ... cascade.
            var board = new Board();
            // Leave the board empty; first ScanMatches returns 0 and we'd bail out.
            // So we must pre-seed one match to enter the loop.
            for (int c = 0; c < 3; c++)
            {
                Place(board, 0, c, ColorId.Purple);
            }

            bool exceededCallbackFired = false;
            Action onExceeded = () => { exceededCallbackFired = true; };

            var spawner = new PurpleSpawner();
            var proc = new ChainProcessor(board, new NoOpAnimationHub(), spawner, null, onExceeded);
            var result = proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.That(result.DepthExceeded && exceededCallbackFired && result.MaxDepth == ChainProcessor.MaxDepth, Is.True);
        }
    }
}
