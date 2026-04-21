using System.Threading;
using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// Phase 2 P2-01: 필터 벽 통과 시 색 변환(ColorMixCache 기반) 동작을 검증.
    /// WHY trigger match 방식: ChainProcessor 의 Gravity 단계는 최소 1회의 match 가 발생해
    /// 빈 칸이 생겨야 실행된다. 따라서 각 케이스는 바닥에 Purple 3연속을 두어 explosion→gravity 유도.
    /// </summary>
    [TestFixture]
    public sealed class FilterWallTests
    {
        private static Block NewBlock(ColorId color, BlockState st = BlockState.Idle, BlockKind kind = BlockKind.Normal)
        {
            var b = new Block();
            b.Reset();
            b.Color = color;
            b.State = st;
            b.Kind = kind;
            return b;
        }

        /// <summary>Refill 을 억제하기 위해 같은 Red(primary) 만 스폰 — primary 는 매치되지 않음.</summary>
        private sealed class RedSpawner : IBlockSpawner
        {
            private int _id = 9000;
            public Block SpawnRandom(int row, int col)
            {
                var b = NewBlock(ColorId.Red);
                b.Id = _id++;
                b.Row = row;
                b.Col = col;
                return b;
            }
        }

        private static ChainProcessor NewProc(Board board)
        {
            return new ChainProcessor(board, new NoOpAnimationHub(), new RedSpawner());
        }

        private static void FillRed(Board board)
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    board.SetBlock(r, c, NewBlock(ColorId.Red));
                }
            }
        }

        /// <summary>SetCellLayer 로 Filter 지정 시 Cell.Layer/FilterColor 동기화.</summary>
        [Test]
        public void SetCellLayer_FilterAssignsBothFields()
        {
            var board = new Board();
            board.SetCellLayer(3, 2, CellLayer.Filter, ColorId.Red);
            var cell = board.CellAt(3, 2);
            Assert.That(cell.Layer, Is.EqualTo(CellLayer.Filter));
            Assert.That(cell.FilterColor, Is.EqualTo(ColorId.Red));
        }

        /// <summary>Layer != Filter 일 때 FilterColor 는 None 으로 클램프된다.</summary>
        [Test]
        public void SetCellLayer_NonFilterClampsFilterColorToNone()
        {
            var board = new Board();
            board.SetCellLayer(3, 2, CellLayer.Wall, ColorId.Red);
            Assert.That(board.CellAt(3, 2).FilterColor, Is.EqualTo(ColorId.None));
        }

        /// <summary>BoardTestFixtures 세로 파랑 필터 기대 배치 검증.</summary>
        [Test]
        public void BoardTestFixtures_VerticalBlueFilterSetsCol0()
        {
            var board = new Board();
            BoardTestFixtures.ApplyVerticalBlueFilter(board);
            for (int r = 0; r < Board.Rows; r++)
            {
                Assert.That(board.CellAt(r, 0).Layer, Is.EqualTo(CellLayer.Filter));
                Assert.That(board.CellAt(r, 0).FilterColor, Is.EqualTo(ColorId.Blue));
            }
        }

        /// <summary>낙하가 1칸만 발생하면 그 경로에 Filter 가 없으면 색 무변동.</summary>
        [Test]
        public void ShortGravityNoFilterOnPath_ColorUnchanged()
        {
            var board = new Board();
            FillRed(board);
            // col 1 row 6 만 비우기 위해 Purple 삼총사 (6,0)(6,1)(6,2).
            board.SetBlock(6, 0, NewBlock(ColorId.Purple));
            board.SetBlock(6, 1, NewBlock(ColorId.Purple));
            board.SetBlock(6, 2, NewBlock(ColorId.Purple));

            // col 1 row 0 을 Yellow 로 치환. row 4 에 Red Filter 를 두지만, Yellow 는 row 1 까지만 낙하.
            board.SetBlock(0, 1, NewBlock(ColorId.Yellow));
            board.SetCellLayer(4, 1, CellLayer.Filter, ColorId.Red);

            var proc = NewProc(board);
            proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            // Yellow 는 (0,1) → (1,1) 로 1칸만 내려오며 Filter(4,1) 통과 경로가 아님.
            Assert.That(board.BlockAt(1, 1).Color, Is.EqualTo(ColorId.Yellow));
        }

        /// <summary>col 전체가 비워지도록 세팅 후 낙하 중 Yellow 블록이 Red Filter 통과 → Orange.</summary>
        [Test]
        public void FullColumnGravity_YellowThroughRedFilterBecomesOrange()
        {
            var board = new Board();
            FillRed(board);
            // col 3 의 row 0~5 를 모두 null (빈 칸) — col 3 row 6 는 그대로 Red 로 두지 않고, 폭발로 비우기 위해
            // col 3 row 6 을 Purple 삼총사의 일부로 포함.
            for (int r = 0; r <= 5; r++) board.SetBlock(r, 3, null);
            // Purple 매치 구성: (6,3)(6,4)(6,5) — col 3/4/5 row 6 폭발. col 3 전체가 비게 됨.
            board.SetBlock(6, 3, NewBlock(ColorId.Purple));
            board.SetBlock(6, 4, NewBlock(ColorId.Purple));
            board.SetBlock(6, 5, NewBlock(ColorId.Purple));

            // col 3 의 row 0 에 Yellow 블록 배치, row 3 에 Red Filter.
            board.SetBlock(0, 3, NewBlock(ColorId.Yellow));
            board.SetCellLayer(3, 3, CellLayer.Filter, ColorId.Red);

            var proc = NewProc(board);
            proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            // Yellow 는 row 0 → row 6 으로 낙하하며 row 3 Red Filter 통과 → Orange.
            // 이후 Refill 로 row 0~5 는 Red 로 채워짐.
            var landed = board.BlockAt(6, 3);
            Assert.That(landed, Is.Not.Null);
            Assert.That(landed.Color, Is.EqualTo(ColorId.Orange));
        }

        /// <summary>Filter 셀에 "착지" 하는 경우도 변환이 발생한다.</summary>
        [Test]
        public void LandingOnFilter_AppliesColorMix()
        {
            var board = new Board();
            FillRed(board);
            // 폭발로 col 3 전체 비우기 — Purple 삼총사 (6,3)(6,4)(6,5).
            for (int r = 0; r <= 5; r++) board.SetBlock(r, 3, null);
            board.SetBlock(6, 3, NewBlock(ColorId.Purple));
            board.SetBlock(6, 4, NewBlock(ColorId.Purple));
            board.SetBlock(6, 5, NewBlock(ColorId.Purple));

            // 단일 Yellow 를 col 3 row 0 에 두고 row 6 를 Red Filter 로 지정.
            board.SetBlock(0, 3, NewBlock(ColorId.Yellow));
            board.SetCellLayer(6, 3, CellLayer.Filter, ColorId.Red);

            var proc = NewProc(board);
            proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            var landed = board.BlockAt(6, 3);
            Assert.That(landed, Is.Not.Null);
            Assert.That(landed.Color, Is.EqualTo(ColorId.Orange));
        }

        /// <summary>Prism 블록은 Filter 영향을 받지 않음(Kind != Normal 은 면역).</summary>
        [Test]
        public void PrismBlock_IgnoresFilterColor()
        {
            var board = new Board();
            FillRed(board);
            for (int r = 0; r <= 5; r++) board.SetBlock(r, 3, null);
            board.SetBlock(6, 3, NewBlock(ColorId.Purple));
            board.SetBlock(6, 4, NewBlock(ColorId.Purple));
            board.SetBlock(6, 5, NewBlock(ColorId.Purple));

            board.SetBlock(0, 3, NewBlock(ColorId.Prism, BlockState.Idle, BlockKind.Prism));
            board.SetCellLayer(3, 3, CellLayer.Filter, ColorId.Red);

            var proc = NewProc(board);
            proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            var landed = board.BlockAt(6, 3);
            Assert.That(landed, Is.Not.Null);
            Assert.That(landed.Kind, Is.EqualTo(BlockKind.Prism));
            Assert.That(landed.Color, Is.EqualTo(ColorId.Prism));
        }

        /// <summary>두 개의 Filter 를 연속 통과 시 Mix 누적. Yellow→Red(3)→Orange→Yellow(5)→Orange(불변).</summary>
        [Test]
        public void MultipleFilters_AccumulateMixes()
        {
            var board = new Board();
            FillRed(board);
            for (int r = 0; r <= 5; r++) board.SetBlock(r, 2, null);
            board.SetBlock(6, 2, NewBlock(ColorId.Purple));
            board.SetBlock(6, 3, NewBlock(ColorId.Purple));
            board.SetBlock(6, 4, NewBlock(ColorId.Purple));

            board.SetBlock(0, 2, NewBlock(ColorId.Yellow));
            board.SetCellLayer(3, 2, CellLayer.Filter, ColorId.Red);
            board.SetCellLayer(5, 2, CellLayer.Filter, ColorId.Yellow);

            var proc = NewProc(board);
            proc.ProcessTurnAsync(CancellationToken.None).GetAwaiter().GetResult();

            // Yellow + Red = Orange. Orange + Yellow = R|Y|Y = R|Y = Orange(불변).
            var landed = board.BlockAt(6, 2);
            Assert.That(landed, Is.Not.Null);
            Assert.That(landed.Color, Is.EqualTo(ColorId.Orange));
        }

        /// <summary>ClearAllLayers 후 모든 셀이 Ground/None 으로 복원된다.</summary>
        [Test]
        public void ClearAllLayers_ResetsAll()
        {
            var board = new Board();
            BoardTestFixtures.ApplyHorizontalRedFilter(board);
            BoardTestFixtures.ClearAllLayers(board);
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    Assert.That(board.CellAt(r, c).Layer, Is.EqualTo(CellLayer.Ground));
                    Assert.That(board.CellAt(r, c).FilterColor, Is.EqualTo(ColorId.None));
                }
            }
        }

        /// <summary>체커보드 Yellow Filter 픽스처가 적용된 셀이 기대된 패턴인지 표본 검증.</summary>
        [Test]
        public void CheckerYellowFilter_SamplePatternMatches()
        {
            var board = new Board();
            BoardTestFixtures.ApplyCheckerYellowFilter(board);
            // (0,0): (0+0)&1 == 0 → Filter
            Assert.That(board.CellAt(0, 0).Layer, Is.EqualTo(CellLayer.Filter));
            // (0,1): (0+1)&1 == 1 → Ground
            Assert.That(board.CellAt(0, 1).Layer, Is.EqualTo(CellLayer.Ground));
            // (1,0): (1+0)&1 == 1 → Ground
            Assert.That(board.CellAt(1, 0).Layer, Is.EqualTo(CellLayer.Ground));
        }
    }
}
