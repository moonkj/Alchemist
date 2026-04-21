using System;
using NUnit.Framework;
using Alchemist.Domain.Economy;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// InkEnergy 회복/소모 경계값. IClock 주입으로 결정적 시간 진행 검증.
    /// </summary>
    [TestFixture]
    public sealed class InkEnergyTests
    {
        /// <summary>테스트 전용 수동 시계.</summary>
        internal sealed class FakeClock : IClock
        {
            public DateTime Now { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => Now;
            public void Advance(TimeSpan ts) { Now = Now + ts; }
        }

        [Test]
        public void NewInk_StartsFull()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c);
            Assert.That(ink.Current, Is.EqualTo(5));
            Assert.That(ink.Max, Is.EqualTo(5));
            Assert.That(ink.IsFull, Is.True);
        }

        [Test]
        public void Consume_DecreasesByOne()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c);
            Assert.That(ink.Consume(), Is.True);
            Assert.That(ink.Current, Is.EqualTo(4));
        }

        [Test]
        public void Consume_FailsWhenEmpty()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c, current: 0);
            Assert.That(ink.CanConsume(), Is.False);
            Assert.That(ink.Consume(), Is.False);
        }

        [Test]
        public void Refill_GrantsOneAfterFiveMinutes()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c, current: 0);
            c.Advance(TimeSpan.FromSeconds(299));
            ink.Refill();
            Assert.That(ink.Current, Is.EqualTo(0));
            c.Advance(TimeSpan.FromSeconds(1));
            ink.Refill();
            Assert.That(ink.Current, Is.EqualTo(1));
        }

        [Test]
        public void Refill_AccumulatesMultipleTicks()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c, current: 0);
            c.Advance(TimeSpan.FromSeconds(900)); // 3 tick
            ink.Refill();
            Assert.That(ink.Current, Is.EqualTo(3));
        }

        [Test]
        public void Refill_CapsAtMax()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c, current: 0);
            c.Advance(TimeSpan.FromHours(1)); // 12 ticks > max(5)
            ink.Refill();
            Assert.That(ink.Current, Is.EqualTo(5));
        }

        [Test]
        public void Grant_DoesNotExceedMax()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c, current: 4);
            ink.Grant(10);
            Assert.That(ink.Current, Is.EqualTo(5));
        }

        [Test]
        public void SecondsUntilNext_ReturnsZeroWhenFull()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c);
            Assert.That(ink.SecondsUntilNext(), Is.EqualTo(0));
        }

        [Test]
        public void SecondsUntilNext_ReturnsRemainingAfterConsume()
        {
            var c = new FakeClock();
            var ink = new InkEnergy(c);
            ink.Consume();
            // 막 비운 순간 -> 다음 회복까지 300초.
            Assert.That(ink.SecondsUntilNext(), Is.InRange(290, 300));
            c.Advance(TimeSpan.FromSeconds(100));
            Assert.That(ink.SecondsUntilNext(), Is.InRange(190, 210));
        }
    }
}
