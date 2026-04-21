using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Economy;

namespace Alchemist.Tests.EditMode
{
    [TestFixture]
    public sealed class ItemEffectProcessorTests
    {
        private static Block Make(ColorId c, BlockKind k = BlockKind.Normal)
            => new Block { Id = 1, Color = c, Kind = k };

        [Test]
        public void Brush_ChangesColor_AndConsumesItem()
        {
            var board = new Alchemist.Domain.Board.Board();
            board.SetBlock(2, 3, Make(ColorId.Red));
            var inv = new Inventory();
            inv.Add(ItemId.MagicBrush, 2);
            var fx = new ItemEffectProcessor(board, inv);

            Assert.That(fx.ApplyBrush(2, 3, ColorId.Blue), Is.True);
            Assert.That(board.BlockAt(2, 3).Color, Is.EqualTo(ColorId.Blue));
            Assert.That(inv.Get(ItemId.MagicBrush), Is.EqualTo(1));
        }

        [Test]
        public void Brush_FailsOnEmptyCell_NoConsume()
        {
            var board = new Alchemist.Domain.Board.Board();
            var inv = new Inventory();
            inv.Add(ItemId.MagicBrush, 1);
            var fx = new ItemEffectProcessor(board, inv);

            Assert.That(fx.ApplyBrush(0, 0, ColorId.Red), Is.False);
            Assert.That(inv.Get(ItemId.MagicBrush), Is.EqualTo(1));
        }

        [Test]
        public void Brush_FailsOnPrism()
        {
            var board = new Alchemist.Domain.Board.Board();
            board.SetBlock(1, 1, Make(ColorId.Prism, BlockKind.Prism));
            var inv = new Inventory();
            inv.Add(ItemId.MagicBrush, 1);
            var fx = new ItemEffectProcessor(board, inv);

            Assert.That(fx.ApplyBrush(1, 1, ColorId.Red), Is.False);
            Assert.That(inv.Get(ItemId.MagicBrush), Is.EqualTo(1));
        }

        [Test]
        public void Eraser_RemovesGrayBlock()
        {
            var board = new Alchemist.Domain.Board.Board();
            board.SetBlock(4, 2, Make(ColorId.Gray, BlockKind.Gray));
            var inv = new Inventory();
            inv.Add(ItemId.Eraser, 1);
            var fx = new ItemEffectProcessor(board, inv);

            Assert.That(fx.ApplyEraser(4, 2), Is.True);
            Assert.That(board.BlockAt(4, 2), Is.Null);
            Assert.That(inv.Get(ItemId.Eraser), Is.EqualTo(0));
        }

        [Test]
        public void Eraser_FailsWithoutStock()
        {
            var board = new Alchemist.Domain.Board.Board();
            board.SetBlock(0, 0, Make(ColorId.Red));
            var inv = new Inventory();
            var fx = new ItemEffectProcessor(board, inv);

            Assert.That(fx.ApplyEraser(0, 0), Is.False);
            Assert.That(board.BlockAt(0, 0), Is.Not.Null);
        }

        [Test]
        public void ExtraPrism_ConvertsBlockToPrism()
        {
            var board = new Alchemist.Domain.Board.Board();
            board.SetBlock(3, 3, Make(ColorId.Orange));
            var inv = new Inventory();
            inv.Add(ItemId.ExtraPrism, 1);
            var fx = new ItemEffectProcessor(board, inv);

            Assert.That(fx.ApplyExtraPrism(3, 3), Is.True);
            Assert.That(board.BlockAt(3, 3).Kind, Is.EqualTo(BlockKind.Prism));
            Assert.That(board.BlockAt(3, 3).Color, Is.EqualTo(ColorId.Prism));
            Assert.That(inv.Get(ItemId.ExtraPrism), Is.EqualTo(0));
        }

        [Test]
        public void ExtraPrism_FailsOnAlreadyPrism()
        {
            var board = new Alchemist.Domain.Board.Board();
            board.SetBlock(3, 3, Make(ColorId.Prism, BlockKind.Prism));
            var inv = new Inventory();
            inv.Add(ItemId.ExtraPrism, 1);
            var fx = new ItemEffectProcessor(board, inv);

            Assert.That(fx.ApplyExtraPrism(3, 3), Is.False);
            Assert.That(inv.Get(ItemId.ExtraPrism), Is.EqualTo(1));
        }
    }
}
