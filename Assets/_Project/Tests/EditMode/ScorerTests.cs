using NUnit.Framework;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Scoring;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Unit tests for <see cref="Scorer"/> formula: BaseColorValue x ChainMultiplier x count.
    /// Verifies C3 semantics (count multiplies per-block), Black-penalty clamp at MinTotal,
    /// and ChainMultiplier breakpoints through depth 5.
    /// </summary>
    [TestFixture]
    public sealed class ScorerTests
    {
        private static (Scorer scorer, Score score) Fresh()
        {
            var score = new Score();
            var scorer = new Scorer(score);
            return (scorer, score);
        }

        // ---------- OnBlocksExploded: Purple / Red / White / Black ----------

        [Test]
        public void OnBlocksExploded_Purple3_Depth1_Returns90()
        {
            var (scorer, _) = Fresh();
            int pts = scorer.OnBlocksExploded(ColorId.Purple, 3, 1);
            Assert.That(pts, Is.EqualTo(90));
        }

        [Test]
        public void OnBlocksExploded_Purple3_Depth2_Returns135()
        {
            var (scorer, _) = Fresh();
            int pts = scorer.OnBlocksExploded(ColorId.Purple, 3, 2);
            Assert.That(pts, Is.EqualTo(135));
        }

        [Test]
        public void OnBlocksExploded_Red3_Depth1_Returns30()
        {
            var (scorer, _) = Fresh();
            int pts = scorer.OnBlocksExploded(ColorId.Red, 3, 1);
            Assert.That(pts, Is.EqualTo(30));
        }

        [Test]
        public void OnBlocksExploded_White1_Depth3_Returns200()
        {
            var (scorer, _) = Fresh();
            int pts = scorer.OnBlocksExploded(ColorId.White, 1, 3);
            Assert.That(pts, Is.EqualTo(200));
        }

        [Test]
        public void OnBlocksExploded_Black5_Depth1_TotalClampedToZero()
        {
            var (scorer, score) = Fresh();
            // Raw points = -20 * 1.0 * 5 = -100; Total clamps at MinTotal=0.
            scorer.OnBlocksExploded(ColorId.Black, 5, 1);
            Assert.That(score.Total, Is.EqualTo(0));
        }

        // ---------- OnStageEnded residual bonus ----------

        [Test]
        public void OnStageEnded_CapturesResidual210_ViaStageEndedEvent()
        {
            // Arrange: MovesUsed=12 by issuing 12 moves; set a dummy goalMoveCount=12 so
            // efficiency = 200 (cap); we assert only the residual component.
            var (scorer, _) = Fresh();
            scorer.BeginStage(goalMoveCount: 12);
            for (int i = 0; i < 12; i++) scorer.OnMoveCommitted();

            int observedResidual = -1;
            scorer.StageEnded += evt => { observedResidual = evt.ResidualBonus; };

            // movesLimit=15, slots=2, goalAchieved=true
            // remainingMoves = 15-12 = 3 -> 3*50 = 150
            // remainingSlots = 2           -> 2*30 =  60
            // residual                                   = 210
            scorer.OnStageEnded(movesLimit: 15, remainingPaletteSlots: 2, goalAchieved: true);

            Assert.That(observedResidual, Is.EqualTo(210));
        }

        // ---------- ChainMultiplier breakpoints ----------

        [Test]
        public void ChainMultiplier_Depth1_Is1Point0()
        {
            Assert.That(Scorer.ChainMultiplier(1), Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void ChainMultiplier_Depth2_Is1Point5()
        {
            Assert.That(Scorer.ChainMultiplier(2), Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void ChainMultiplier_Depth3_Is2Point0()
        {
            Assert.That(Scorer.ChainMultiplier(3), Is.EqualTo(2.0f).Within(0.001f));
        }

        [Test]
        public void ChainMultiplier_Depth4_Is2Point5()
        {
            Assert.That(Scorer.ChainMultiplier(4), Is.EqualTo(2.5f).Within(0.001f));
        }

        [Test]
        public void ChainMultiplier_Depth5_Is2Point8()
        {
            Assert.That(Scorer.ChainMultiplier(5), Is.EqualTo(2.8f).Within(0.001f));
        }
    }
}
