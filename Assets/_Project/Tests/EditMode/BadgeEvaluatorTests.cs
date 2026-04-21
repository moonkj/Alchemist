using System.Collections.Generic;
using NUnit.Framework;
using Alchemist.Domain.Badges;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// BadgeEvaluator + 조건 구현체 통합 테스트.
    /// 조합/스타일/히든 대표 6개 + Awarded idempotency.
    /// </summary>
    [TestFixture]
    public sealed class BadgeEvaluatorTests
    {
        private sealed class FakeCtx : IPromptContext
        {
            private readonly int[] _colors = new int[256];
            public int MaxChainDepthAchieved { get; set; }
            public int MovesUsed { get; set; }
            public int MovesLimit { get; set; }
            public int FilterTransits { get; set; }
            public int PaletteSlotUses { get; set; }
            public int ChainAtLeast2 { get; set; }

            public void SetColors(ColorId c, int count) => _colors[(byte)c] = count;
            public int GetColorsCreated(ColorId color) => _colors[(byte)color];
            public int ChainEventsCount(int minDepth) => minDepth <= 2 ? ChainAtLeast2 : 0;
        }

        private static (BadgeEvaluator eval, List<BadgeId> buf) Fresh()
        {
            return (new BadgeEvaluator(), new List<BadgeId>(8));
        }

        // --- 조합 ---

        [Test]
        public void FirstPurple_AwardedOnce()
        {
            var (eval, buf) = Fresh();
            var ctx = new FakeCtx();
            ctx.SetColors(ColorId.Purple, 1);
            var extra = BadgeEvaluationStats.MidStage(0, 0);

            eval.Evaluate(ctx, new Score(), extra, buf);
            Assert.That(buf, Contains.Item(BadgeId.FirstPurple));

            buf.Clear();
            eval.Evaluate(ctx, new Score(), extra, buf);
            Assert.That(buf, Has.No.Member(BadgeId.FirstPurple));
        }

        [Test]
        public void AllSecondaries_RequiresAllThree()
        {
            var (eval, buf) = Fresh();
            var ctx = new FakeCtx();
            ctx.SetColors(ColorId.Orange, 1);
            ctx.SetColors(ColorId.Green, 1);
            var extra = BadgeEvaluationStats.MidStage(0, 0);

            eval.Evaluate(ctx, new Score(), extra, buf);
            Assert.That(buf, Has.No.Member(BadgeId.AllSecondaries));

            ctx.SetColors(ColorId.Purple, 1);
            buf.Clear();
            eval.Evaluate(ctx, new Score(), extra, buf);
            Assert.That(buf, Contains.Item(BadgeId.AllSecondaries));
        }

        [Test]
        public void FirstWhite_Awarded()
        {
            var (eval, buf) = Fresh();
            var ctx = new FakeCtx();
            ctx.SetColors(ColorId.White, 1);
            eval.Evaluate(ctx, new Score(), BadgeEvaluationStats.MidStage(0, 0), buf);
            Assert.That(buf, Contains.Item(BadgeId.FirstWhite));
        }

        // --- 플레이 스타일 ---

        [Test]
        public void Chain5_TriggersOnMaxDepth5()
        {
            var (eval, buf) = Fresh();
            // Score 는 internal setter 접근 불가 → 리플렉션 대신 Scorer 경유.
            var score = new Score();
            var scorer = new Scorer(score);
            // depth 5 에서 SetChainDepth 내부 경로가 MaxChainDepthAchieved 를 갱신.
            scorer.OnBlocksExploded(ColorId.Red, 1, chainDepth: 5);

            eval.Evaluate(new FakeCtx(), score, BadgeEvaluationStats.MidStage(0, 0), buf);
            Assert.That(buf, Contains.Item(BadgeId.Chain5));
        }

        [Test]
        public void MinMoves_RequiresParAndPromptSatisfied()
        {
            var (eval, buf) = Fresh();
            var ctx = new FakeCtx { MovesUsed = 5 };
            var extra = new BadgeEvaluationStats(parMoves: 10, moveLimit: 15, promptProgress: 1f, promptSatisfied: true, isStageEnd: true);
            eval.Evaluate(ctx, new Score(), extra, buf);
            Assert.That(buf, Contains.Item(BadgeId.MinMoves));
        }

        [Test]
        public void PromptPerfect_OnlyAtStageEnd()
        {
            var (eval, buf) = Fresh();
            var ctx = new FakeCtx();
            var mid = new BadgeEvaluationStats(parMoves: 10, moveLimit: 15, promptProgress: 1f, promptSatisfied: true, isStageEnd: false);
            eval.Evaluate(ctx, new Score(), mid, buf);
            Assert.That(buf, Has.No.Member(BadgeId.PromptPerfect));

            var end = new BadgeEvaluationStats(parMoves: 10, moveLimit: 15, promptProgress: 1f, promptSatisfied: true, isStageEnd: true);
            eval.Evaluate(ctx, new Score(), end, buf);
            Assert.That(buf, Contains.Item(BadgeId.PromptPerfect));
        }

        // --- 히든 ---

        [Test]
        public void GrayOnly_RequiresOnlyGray()
        {
            var (eval, buf) = Fresh();
            var ctx = new FakeCtx();
            ctx.SetColors(ColorId.Gray, 3);
            var extra = new BadgeEvaluationStats(0, 0, 1f, true, true);
            eval.Evaluate(ctx, new Score(), extra, buf);
            Assert.That(buf, Contains.Item(BadgeId.GrayOnly));

            // 다른 색이 섞이면 재획득 불가(이미 부여된 상태에서 또 다른 평가에서는 FirstX 만 추가).
            var (eval2, buf2) = Fresh();
            var ctx2 = new FakeCtx();
            ctx2.SetColors(ColorId.Gray, 3);
            ctx2.SetColors(ColorId.Red, 1);
            eval2.Evaluate(ctx2, new Score(), extra, buf2);
            Assert.That(buf2, Has.No.Member(BadgeId.GrayOnly));
        }

        [Test]
        public void FilterOnly_ZeroPaletteWithTransits()
        {
            var (eval, buf) = Fresh();
            var ctx = new FakeCtx { FilterTransits = 3, PaletteSlotUses = 0 };
            ctx.SetColors(ColorId.Purple, 1);
            var extra = new BadgeEvaluationStats(0, 0, 1f, true, true);
            eval.Evaluate(ctx, new Score(), extra, buf);
            Assert.That(buf, Contains.Item(BadgeId.FilterOnly));
        }
    }
}
