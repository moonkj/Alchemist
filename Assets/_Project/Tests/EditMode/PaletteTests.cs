using NUnit.Framework;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Palette;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// 팔레트 도메인 단위 테스트: Store/Use 기본 경로, 오버플로(점유 중 저장), 잠금, 해금 축소.
    /// </summary>
    [TestFixture]
    public sealed class PaletteTests
    {
        private static Block MakeBlock(ColorId c)
        {
            return new Block { Id = 1, Color = c, Kind = BlockKind.Normal };
        }

        [Test]
        public void Store_EmptySlot_Succeeds()
        {
            var p = new Palette(unlockedCount: 3);
            Assert.That(p.Store(0, MakeBlock(ColorId.Red)), Is.True);
            Assert.That(p.GetSlot(0).Stored, Is.EqualTo(ColorId.Red));
            Assert.That(p.GetSlot(0).IsEmpty, Is.False);
        }

        [Test]
        public void Store_OccupiedSlot_Fails()
        {
            var p = new Palette(unlockedCount: 3);
            p.Store(0, MakeBlock(ColorId.Red));
            Assert.That(p.Store(0, MakeBlock(ColorId.Blue)), Is.False);
            Assert.That(p.GetSlot(0).Stored, Is.EqualTo(ColorId.Red));
        }

        [Test]
        public void Store_LockedSlot_Fails()
        {
            var p = new Palette(unlockedCount: 1);
            // Slot 0 해금, Slot 1/2 잠김
            Assert.That(p.Store(1, MakeBlock(ColorId.Green)), Is.False);
            Assert.That(p.Store(2, MakeBlock(ColorId.Green)), Is.False);
        }

        [Test]
        public void Store_OutOfRange_Fails()
        {
            var p = new Palette(unlockedCount: 3);
            Assert.That(p.Store(-1, MakeBlock(ColorId.Red)), Is.False);
            Assert.That(p.Store(99, MakeBlock(ColorId.Red)), Is.False);
        }

        [Test]
        public void Use_FilledSlot_ReturnsBlockAndClears()
        {
            var p = new Palette(unlockedCount: 3);
            var b = MakeBlock(ColorId.Purple);
            p.Store(2, b);
            var used = p.Use(2);
            Assert.That(used, Is.SameAs(b));
            Assert.That(p.GetSlot(2).IsEmpty, Is.True);
        }

        [Test]
        public void Use_EmptySlot_ReturnsNull()
        {
            var p = new Palette(unlockedCount: 3);
            Assert.That(p.Use(0), Is.Null);
        }

        [Test]
        public void SlotChanged_FiresOnStoreAndUse()
        {
            var p = new Palette(unlockedCount: 3);
            int lastIdx = -1;
            int fired = 0;
            p.SlotChanged += (i) => { lastIdx = i; fired++; };

            p.Store(1, MakeBlock(ColorId.Red));
            p.Use(1);

            Assert.That(fired, Is.EqualTo(2));
            Assert.That(lastIdx, Is.EqualTo(1));
        }

        [Test]
        public void SetUnlockedCount_ShrinkClearsAccessibleOnly()
        {
            var p = new Palette(unlockedCount: 3);
            p.Store(0, MakeBlock(ColorId.Red));
            p.Store(2, MakeBlock(ColorId.Blue));
            p.SetUnlockedCount(1);

            Assert.That(p.UnlockedCount, Is.EqualTo(1));
            Assert.That(p.GetSlot(0).IsEmpty, Is.False);   // 유지
            Assert.That(p.GetSlot(2).IsEmpty, Is.True);    // 접근 불가 → 비움
        }

        [Test]
        public void FindFirstEmpty_SkipsOccupied()
        {
            var p = new Palette(unlockedCount: 3);
            p.Store(0, MakeBlock(ColorId.Red));
            Assert.That(p.FindFirstEmpty(), Is.EqualTo(1));
        }

        [Test]
        public void RemainingEmptyUnlocked_CountsOnlyAccessible()
        {
            var p = new Palette(unlockedCount: 2);
            p.Store(0, MakeBlock(ColorId.Red));
            // slot 1 비어있음(해금), slot 2 잠김
            Assert.That(p.RemainingEmptyUnlocked(), Is.EqualTo(1));
        }
    }
}
