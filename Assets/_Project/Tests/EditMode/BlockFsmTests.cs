using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="BlockFsm"/> transition table.
    /// Validates the 15 allowed transitions declared in BlockFsm.BuildTable and
    /// asserts invalid transitions are rejected. Includes C1 (Gray) data representation.
    /// </summary>
    [TestFixture]
    public sealed class BlockFsmTests
    {
        private static Block NewBlock(BlockState state = BlockState.Spawned, ColorId color = ColorId.Red, BlockKind kind = BlockKind.Normal)
        {
            var b = new Block();
            b.Reset();
            b.State = state;
            b.Color = color;
            b.Kind = kind;
            return b;
        }

        private static TransitionContext Ctx()
        {
            return new TransitionContext(ColorId.None, 0, 0);
        }

        // ---------- Allowed transitions (CanTransition) ----------

        [TestCase(BlockState.Spawned, BlockState.Idle)]
        [TestCase(BlockState.Idle, BlockState.Selected)]
        [TestCase(BlockState.Selected, BlockState.Idle)]
        [TestCase(BlockState.Selected, BlockState.Merging)]
        [TestCase(BlockState.Merging, BlockState.Exploding)]
        [TestCase(BlockState.Exploding, BlockState.Cleared)]
        [TestCase(BlockState.Exploding, BlockState.Infecting)]
        [TestCase(BlockState.Infecting, BlockState.Idle)]
        [TestCase(BlockState.Idle, BlockState.Infected)]
        [TestCase(BlockState.Infected, BlockState.Idle)]
        [TestCase(BlockState.Idle, BlockState.Absorbed)]
        [TestCase(BlockState.Absorbed, BlockState.Gray)]
        [TestCase(BlockState.Idle, BlockState.FilterTransit)]
        [TestCase(BlockState.FilterTransit, BlockState.Idle)]
        [TestCase(BlockState.Idle, BlockState.PrismCharging)]
        [TestCase(BlockState.PrismCharging, BlockState.Exploding)]
        public void CanTransition_AllowsDefinedEdges(BlockState from, BlockState to)
        {
            Assert.That(BlockFsm.CanTransition(from, to), Is.True);
        }

        // ---------- Disallowed transitions ----------

        [Test]
        public void CanTransition_RejectsSpawnedToExploding()
        {
            Assert.That(BlockFsm.CanTransition(BlockState.Spawned, BlockState.Exploding), Is.False);
        }

        [Test]
        public void CanTransition_RejectsIdleToCleared()
        {
            Assert.That(BlockFsm.CanTransition(BlockState.Idle, BlockState.Cleared), Is.False);
        }

        [Test]
        public void CanTransition_RejectsSelfLoopOnIdle()
        {
            Assert.That(BlockFsm.CanTransition(BlockState.Idle, BlockState.Idle), Is.False);
        }

        // ---------- Absorbed -> Gray is one-way (no return to Idle in Phase 1) ----------

        [Test]
        public void CanTransition_AbsorbedToGrayAllowed_GrayToIdleDenied()
        {
            // Addendum C1b: Gray -> Idle is deferred to Phase 2.
            Assert.That(BlockFsm.CanTransition(BlockState.Gray, BlockState.Idle), Is.False);
        }

        // ---------- TryTransition integration ----------

        [Test]
        public void TryTransition_MutatesStateWhenAllowed()
        {
            var b = NewBlock(BlockState.Spawned);
            var ctx = Ctx();
            BlockFsm.TryTransition(b, BlockState.Idle, ctx);
            Assert.That(b.State, Is.EqualTo(BlockState.Idle));
        }

        [Test]
        public void TryTransition_ReturnsFalseWhenRejected()
        {
            var b = NewBlock(BlockState.Spawned);
            var ctx = Ctx();
            bool ok = BlockFsm.TryTransition(b, BlockState.Exploding, ctx);
            Assert.That(ok, Is.False);
        }

        [Test]
        public void TryTransition_NullBlock_ReturnsFalse()
        {
            var ctx = Ctx();
            Assert.That(BlockFsm.TryTransition(null, BlockState.Idle, ctx), Is.False);
        }

        // ---------- FilterTransit round-trip ----------

        [Test]
        public void TryTransition_FilterTransitRoundTrip()
        {
            var b = NewBlock(BlockState.Idle);
            var ctx = Ctx();
            BlockFsm.TryTransition(b, BlockState.FilterTransit, ctx);
            BlockFsm.TryTransition(b, BlockState.Idle, ctx);
            Assert.That(b.State, Is.EqualTo(BlockState.Idle));
        }

        // ---------- C1: Gray block data shape is valid in FSM terms ----------

        [Test]
        public void GrayRepresentation_AllowsKindGrayWithColorNoneInAbsorbedState()
        {
            // Addendum C1: Kind=Gray + Color=None + State=Absorbed is the canonical Gray shape.
            // FSM must permit Idle -> Absorbed -> Gray walk without rejection.
            var b = NewBlock(BlockState.Idle, ColorId.None, BlockKind.Gray);
            var ctx = Ctx();
            BlockFsm.TryTransition(b, BlockState.Absorbed, ctx);
            BlockFsm.TryTransition(b, BlockState.Gray, ctx);
            Assert.That(b.State, Is.EqualTo(BlockState.Gray));
        }
    }
}
