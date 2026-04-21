using NUnit.Framework;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Prompts.Conditions;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Unit tests for Phase 1 prompt conditions and <see cref="PromptGoal"/>.
    /// C2 asserts: GetColorsCreated(Purple) is exact-match; White is a separate counter.
    /// </summary>
    [TestFixture]
    public sealed class PromptConditionTests
    {
        /// <summary>Minimal in-memory IPromptContext for isolated condition testing.</summary>
        private sealed class MockCtx : IPromptContext
        {
            private readonly int[] _colors = new int[256];
            public int[] ChainEventsByMinDepth = new int[16];
            public int MaxChainDepthAchieved { get; set; }
            public int MovesUsed { get; set; }
            public int MovesLimit { get; set; }

            public int GetColorsCreated(ColorId color) { return _colors[(byte)color]; }
            public void SetColorsCreated(ColorId color, int count) { _colors[(byte)color] = count; }

            public int ChainEventsCount(int minDepth)
            {
                if (minDepth < 0 || minDepth >= ChainEventsByMinDepth.Length) return 0;
                return ChainEventsByMinDepth[minDepth];
            }
        }

        // ---------- CreateColorCondition ----------

        [Test]
        public void CreateColor_9of10_EvaluateFalse()
        {
            var ctx = new MockCtx();
            ctx.SetColorsCreated(ColorId.Purple, 9);
            var cond = new CreateColorCondition(ColorId.Purple, 10);
            Assert.That(cond.Evaluate(ctx), Is.False);
        }

        [Test]
        public void CreateColor_9of10_ProgressZeroPoint9()
        {
            var ctx = new MockCtx();
            ctx.SetColorsCreated(ColorId.Purple, 9);
            var cond = new CreateColorCondition(ColorId.Purple, 10);
            Assert.That(cond.Progress(ctx), Is.EqualTo(0.9f).Within(0.001f));
        }

        [Test]
        public void CreateColor_10of10_EvaluateTrue()
        {
            var ctx = new MockCtx();
            ctx.SetColorsCreated(ColorId.Purple, 10);
            var cond = new CreateColorCondition(ColorId.Purple, 10);
            Assert.That(cond.Evaluate(ctx), Is.True);
        }

        [Test]
        public void CreateColor_10of10_ProgressOne()
        {
            var ctx = new MockCtx();
            ctx.SetColorsCreated(ColorId.Purple, 10);
            var cond = new CreateColorCondition(ColorId.Purple, 10);
            Assert.That(cond.Progress(ctx), Is.EqualTo(1f).Within(0.001f));
        }

        // ---------- C2: White blocks are NOT counted toward Purple ----------

        [Test]
        public void CreateColor_WhiteDoesNotCountTowardPurple_ExactMatchSemantics()
        {
            // C2: Purple counter is the (byte)Purple bucket only; White has its own bucket.
            var ctx = new MockCtx();
            ctx.SetColorsCreated(ColorId.White, 50);
            ctx.SetColorsCreated(ColorId.Purple, 0);
            var cond = new CreateColorCondition(ColorId.Purple, 10);
            Assert.That(cond.Evaluate(ctx), Is.False);
        }

        // ---------- ChainCondition ----------

        [Test]
        public void Chain_MinDepth2_Occ3_ExactlyThree_True()
        {
            var ctx = new MockCtx();
            ctx.ChainEventsByMinDepth[2] = 3;
            var cond = new ChainCondition(minChainDepth: 2, occurrences: 3);
            Assert.That(cond.Evaluate(ctx), Is.True);
        }

        [Test]
        public void Chain_MinDepth2_Occ3_Only2_False()
        {
            var ctx = new MockCtx();
            ctx.ChainEventsByMinDepth[2] = 2;
            var cond = new ChainCondition(minChainDepth: 2, occurrences: 3);
            Assert.That(cond.Evaluate(ctx), Is.False);
        }

        // ---------- MoveLimitCondition ----------

        [Test]
        public void MoveLimit_14of15_True()
        {
            var ctx = new MockCtx { MovesUsed = 14, MovesLimit = 15 };
            var cond = new MoveLimitCondition(15);
            Assert.That(cond.Evaluate(ctx), Is.True);
        }

        [Test]
        public void MoveLimit_16of15_False()
        {
            var ctx = new MockCtx { MovesUsed = 16, MovesLimit = 15 };
            var cond = new MoveLimitCondition(15);
            Assert.That(cond.Evaluate(ctx), Is.False);
        }

        // ---------- PromptGoal All (AND) ----------

        [Test]
        public void PromptGoal_All_AllTrue_EvaluatesTrue()
        {
            var ctx = new MockCtx { MovesUsed = 10 };
            ctx.SetColorsCreated(ColorId.Purple, 10);
            var goal = new PromptGoal(new IPromptCondition[]
            {
                new CreateColorCondition(ColorId.Purple, 10),
                new MoveLimitCondition(15),
            });
            Assert.That(goal.Evaluate(ctx), Is.True);
        }

        [Test]
        public void PromptGoal_All_OneFalse_EvaluatesFalse()
        {
            var ctx = new MockCtx { MovesUsed = 10 };
            ctx.SetColorsCreated(ColorId.Purple, 5); // below 10
            var goal = new PromptGoal(new IPromptCondition[]
            {
                new CreateColorCondition(ColorId.Purple, 10),
                new MoveLimitCondition(15),
            });
            Assert.That(goal.Evaluate(ctx), Is.False);
        }

        // ---------- PromptGoal Any (OR) ----------

        [Test]
        public void PromptGoal_Any_OneTrue_AndAllPass_EvaluatesTrue()
        {
            var ctx = new MockCtx { MovesUsed = 5 };
            ctx.SetColorsCreated(ColorId.Green, 5);
            ctx.ChainEventsByMinDepth[2] = 1;
            var goal = new PromptGoal(
                all: new IPromptCondition[]
                {
                    new CreateColorCondition(ColorId.Green, 5),
                    new MoveLimitCondition(12),
                },
                any: new IPromptCondition[]
                {
                    new ChainCondition(2, 1),
                });
            Assert.That(goal.Evaluate(ctx), Is.True);
        }

        [Test]
        public void PromptGoal_Any_AllFalse_EvaluatesFalse()
        {
            var ctx = new MockCtx { MovesUsed = 5 };
            ctx.SetColorsCreated(ColorId.Green, 5);
            // Any branch not satisfied
            ctx.ChainEventsByMinDepth[2] = 0;
            var goal = new PromptGoal(
                all: new IPromptCondition[]
                {
                    new CreateColorCondition(ColorId.Green, 5),
                    new MoveLimitCondition(12),
                },
                any: new IPromptCondition[]
                {
                    new ChainCondition(2, 1),
                });
            Assert.That(goal.Evaluate(ctx), Is.False);
        }
    }
}
