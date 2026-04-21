using NUnit.Framework;
using Alchemist.Domain.Economy;

namespace Alchemist.Tests.EditMode
{
    [TestFixture]
    public sealed class InventoryTests
    {
        [Test]
        public void Add_IncreasesCount()
        {
            var inv = new Inventory();
            Assert.That(inv.Add(ItemId.MagicBrush, 3), Is.EqualTo(3));
            Assert.That(inv.Get(ItemId.MagicBrush), Is.EqualTo(3));
        }

        [Test]
        public void Add_ClampsAtMaxStack()
        {
            var inv = new Inventory();
            Assert.That(inv.Add(ItemId.Eraser, 200), Is.EqualTo(ItemIdConsts.MaxStack));
            Assert.That(inv.Get(ItemId.Eraser), Is.EqualTo(ItemIdConsts.MaxStack));
            // 이후 추가는 0 반영.
            Assert.That(inv.Add(ItemId.Eraser, 5), Is.EqualTo(0));
        }

        [Test]
        public void Use_FailsWithoutStock()
        {
            var inv = new Inventory();
            Assert.That(inv.Use(ItemId.ExtraPrism), Is.False);
        }

        [Test]
        public void Use_Succeeds_WhenAvailable()
        {
            var inv = new Inventory();
            inv.Add(ItemId.ExtraPrism, 1);
            Assert.That(inv.Use(ItemId.ExtraPrism), Is.True);
            Assert.That(inv.Get(ItemId.ExtraPrism), Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_ReturnsIndependentCopy()
        {
            var inv = new Inventory();
            inv.Add(ItemId.MagicBrush, 2);
            var snap = inv.Snapshot();
            snap[0] = 99;
            Assert.That(inv.Get(ItemId.MagicBrush), Is.EqualTo(2));
        }

        [Test]
        public void CountChanged_FiresOnAddAndUse()
        {
            var inv = new Inventory();
            int fires = 0;
            ItemId lastId = ItemId.InkRefill;
            inv.CountChanged += (id) => { fires++; lastId = id; };
            inv.Add(ItemId.MagicBrush, 1);
            inv.Use(ItemId.MagicBrush);
            Assert.That(fires, Is.EqualTo(2));
            Assert.That(lastId, Is.EqualTo(ItemId.MagicBrush));
        }

        [Test]
        public void Ctor_WithCounts_ClampsAndCopies()
        {
            var inv = new Inventory(new[] { -5, 200, 10, 7 });
            Assert.That(inv.Get(ItemId.MagicBrush), Is.EqualTo(0));
            Assert.That(inv.Get(ItemId.Eraser), Is.EqualTo(ItemIdConsts.MaxStack));
            Assert.That(inv.Get(ItemId.ExtraPrism), Is.EqualTo(10));
            Assert.That(inv.Get(ItemId.InkRefill), Is.EqualTo(7));
        }
    }
}
