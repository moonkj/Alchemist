using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Phase 2 P2-03: Prism 블록의 턴 종료 시점 승격 규칙을 검증.
    /// WHY: 승격 규칙은 "2개 이상 인접 1차 → 2차" / "2개 이상 인접 2차 → White" 로,
    /// 경계/조합별 오판 금지(단일 1차, 같은 색 두 개, Gray 인접 등).
    /// </summary>
    [TestFixture]
    public sealed class PrismAbsorbTests
    {
        private static Block NewBlock(ColorId color, BlockKind kind = BlockKind.Normal)
        {
            var b = new Block();
            b.Reset();
            b.Color = color;
            b.State = BlockState.Idle;
            b.Kind = kind;
            return b;
        }

        private static void Place(Board board, int r, int c, ColorId color, BlockKind kind = BlockKind.Normal)
        {
            board.SetBlock(r, c, NewBlock(color, kind));
        }

        [Test]
        public void TwoAdjacentPrimaries_DifferentColors_PromoteToSecondary()
        {
            var board = new Board();
            // Prism 중앙(3,3), 인접: 상(2,3)=Red, 하(4,3)=Yellow → Mix=Orange.
            Place(board, 3, 3, ColorId.Prism, BlockKind.Prism);
            Place(board, 2, 3, ColorId.Red);
            Place(board, 4, 3, ColorId.Yellow);

            int promoted = PrismAbsorbProcessor.Process(board);

            Assert.That(promoted, Is.EqualTo(1));
            var prism = board.BlockAt(3, 3);
            Assert.That(prism.Kind, Is.EqualTo(BlockKind.Normal));
            Assert.That(prism.Color, Is.EqualTo(ColorId.Orange));
        }

        [Test]
        public void TwoAdjacentPrimaries_SameColor_DoNotPromote()
        {
            var board = new Board();
            // Red + Red = Red (1차), 2차가 아니므로 승격 보류.
            Place(board, 3, 3, ColorId.Prism, BlockKind.Prism);
            Place(board, 2, 3, ColorId.Red);
            Place(board, 4, 3, ColorId.Red);

            int promoted = PrismAbsorbProcessor.Process(board);

            Assert.That(promoted, Is.EqualTo(0));
            Assert.That(board.BlockAt(3, 3).Kind, Is.EqualTo(BlockKind.Prism));
        }

        [Test]
        public void SinglePrimaryNeighbor_DoNotPromote()
        {
            var board = new Board();
            Place(board, 3, 3, ColorId.Prism, BlockKind.Prism);
            Place(board, 2, 3, ColorId.Red); // 단일 1차만

            int promoted = PrismAbsorbProcessor.Process(board);

            Assert.That(promoted, Is.EqualTo(0));
            Assert.That(board.BlockAt(3, 3).Kind, Is.EqualTo(BlockKind.Prism));
        }

        [Test]
        public void TwoAdjacentSecondaries_PromoteToWhite()
        {
            var board = new Board();
            Place(board, 3, 3, ColorId.Prism, BlockKind.Prism);
            Place(board, 2, 3, ColorId.Orange);
            Place(board, 4, 3, ColorId.Purple);

            int promoted = PrismAbsorbProcessor.Process(board);

            Assert.That(promoted, Is.EqualTo(1));
            var prism = board.BlockAt(3, 3);
            Assert.That(prism.Kind, Is.EqualTo(BlockKind.Normal));
            Assert.That(prism.Color, Is.EqualTo(ColorId.White));
        }

        [Test]
        public void PrimaryAndSecondaryMixed_DoNotPromote()
        {
            var board = new Board();
            // 1차 1개 + 2차 1개: 규칙상 승격 없음.
            Place(board, 3, 3, ColorId.Prism, BlockKind.Prism);
            Place(board, 2, 3, ColorId.Red);
            Place(board, 4, 3, ColorId.Orange);

            int promoted = PrismAbsorbProcessor.Process(board);

            Assert.That(promoted, Is.EqualTo(0));
            Assert.That(board.BlockAt(3, 3).Kind, Is.EqualTo(BlockKind.Prism));
        }

        [Test]
        public void GrayAndPrismNeighbors_Ignored()
        {
            var board = new Board();
            // Normal 이 아닌 이웃은 흡수 대상에서 제외.
            Place(board, 3, 3, ColorId.Prism, BlockKind.Prism);
            Place(board, 2, 3, ColorId.None, BlockKind.Gray);
            Place(board, 4, 3, ColorId.Prism, BlockKind.Prism);
            Place(board, 3, 2, ColorId.Red);
            Place(board, 3, 4, ColorId.Yellow);

            int promoted = PrismAbsorbProcessor.Process(board);

            // 실제 1차 이웃 2개(R,Y)만 집계 → Orange 승격.
            Assert.That(promoted, Is.GreaterThanOrEqualTo(1));
            Assert.That(board.BlockAt(3, 3).Color, Is.EqualTo(ColorId.Orange));
        }

        [Test]
        public void CornerPrism_OnlyTwoValidNeighbors()
        {
            var board = new Board();
            // 모서리(0,0): 인접 (0,1), (1,0) 만 유효. 둘 다 1차 서로 다른 색이면 승격.
            Place(board, 0, 0, ColorId.Prism, BlockKind.Prism);
            Place(board, 0, 1, ColorId.Blue);
            Place(board, 1, 0, ColorId.Red);

            int promoted = PrismAbsorbProcessor.Process(board);

            Assert.That(promoted, Is.EqualTo(1));
            Assert.That(board.BlockAt(0, 0).Color, Is.EqualTo(ColorId.Purple));
        }
    }
}
