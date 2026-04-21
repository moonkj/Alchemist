using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;

namespace Alchemist.Tests.EditMode
{
    /// <summary>Phase 2 P2-02: Gray block release after accumulated adjacent explosions.</summary>
    public class GrayReleaseTests
    {
        private static Board NewBoard() => new Board();

        private static Block GrayBlock(int id, int r, int c)
        {
            var b = new Block
            {
                Id = id,
                Color = ColorId.None,
                Kind = BlockKind.Gray,
                State = BlockState.Absorbed,
                Row = r,
                Col = c,
            };
            return b;
        }

        [Test]
        public void NoBump_NoRelease()
        {
            var board = NewBoard();
            board.SetBlock(1, 1, GrayBlock(1, 1, 1));
            var tracker = new GrayReleaseTracker(board);
            tracker.Reset();
            int released = tracker.SweepRelease();
            Assert.That(released, Is.EqualTo(0));
            Assert.That(board.BlockAt(1, 1).Kind, Is.EqualTo(BlockKind.Gray));
        }

        [Test]
        public void OneAdjacentExplosion_NoRelease()
        {
            var board = NewBoard();
            board.SetBlock(1, 1, GrayBlock(1, 1, 1));
            var tracker = new GrayReleaseTracker(board);
            tracker.Reset();
            tracker.NotifyExplosionAt(1, 2, ColorId.Purple);
            int released = tracker.SweepRelease();
            Assert.That(released, Is.EqualTo(0));
            Assert.That(tracker.GetHitCount(1, 1), Is.EqualTo(1));
        }

        [Test]
        public void TwoAdjacentExplosions_ReleaseToNormalWithLastColor()
        {
            var board = NewBoard();
            board.SetBlock(2, 2, GrayBlock(1, 2, 2));
            var tracker = new GrayReleaseTracker(board);
            tracker.Reset();
            tracker.NotifyExplosionAt(2, 1, ColorId.Purple);
            tracker.NotifyExplosionAt(2, 3, ColorId.Green);
            int released = tracker.SweepRelease();
            Assert.That(released, Is.EqualTo(1));
            var b = board.BlockAt(2, 2);
            Assert.That(b.Kind, Is.EqualTo(BlockKind.Normal));
            Assert.That(b.Color, Is.EqualTo(ColorId.Green));
        }

        [Test]
        public void Reset_ClearsCounters()
        {
            var board = NewBoard();
            board.SetBlock(0, 0, GrayBlock(1, 0, 0));
            var tracker = new GrayReleaseTracker(board);
            tracker.Reset();
            tracker.NotifyExplosionAt(0, 1, ColorId.Orange);
            Assert.That(tracker.GetHitCount(0, 0), Is.EqualTo(1));
            tracker.Reset();
            Assert.That(tracker.GetHitCount(0, 0), Is.EqualTo(0));
        }

        [Test]
        public void NonGrayBlock_Ignored()
        {
            var board = NewBoard();
            var normal = new Block
            {
                Id = 99,
                Color = ColorId.Red,
                Kind = BlockKind.Normal,
                State = BlockState.Idle,
                Row = 3, Col = 3,
            };
            board.SetBlock(3, 3, normal);
            var tracker = new GrayReleaseTracker(board);
            tracker.Reset();
            tracker.NotifyExplosionAt(3, 2, ColorId.Purple);
            tracker.NotifyExplosionAt(3, 4, ColorId.Green);
            int released = tracker.SweepRelease();
            Assert.That(released, Is.EqualTo(0));
            Assert.That(board.BlockAt(3, 3).Kind, Is.EqualTo(BlockKind.Normal));
        }
    }
}
