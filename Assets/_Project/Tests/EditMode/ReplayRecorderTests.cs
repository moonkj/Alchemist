using System;
using NUnit.Framework;
using Alchemist.Domain.Replay;

namespace Alchemist.Tests.EditMode
{
    /// <summary>ReplayRecorder 순환 버퍼 용량/순서 검증.</summary>
    [TestFixture]
    public sealed class ReplayRecorderTests
    {
        private static ReplayFrame F(int turn) =>
            new ReplayFrame(turn, 0, 0, 1, 1, 0);

        [Test]
        public void Append_IncreasesCountUntilCapacity()
        {
            var rec = new ReplayRecorder(capacity: 3);
            Assert.That(rec.Count, Is.EqualTo(0));
            rec.Append(F(1));
            rec.Append(F(2));
            Assert.That(rec.Count, Is.EqualTo(2));
            rec.Append(F(3));
            rec.Append(F(4));
            Assert.That(rec.Count, Is.EqualTo(3));
        }

        [Test]
        public void Append_OverwritesOldestWhenFull()
        {
            var rec = new ReplayRecorder(capacity: 3);
            rec.Append(F(1));
            rec.Append(F(2));
            rec.Append(F(3));
            rec.Append(F(4)); // overwrite turn 1
            rec.Append(F(5)); // overwrite turn 2

            Assert.That(rec.Count, Is.EqualTo(3));
            Assert.That(rec.Get(0).Turn, Is.EqualTo(3));
            Assert.That(rec.Get(1).Turn, Is.EqualTo(4));
            Assert.That(rec.Get(2).Turn, Is.EqualTo(5));
        }

        [Test]
        public void Get_OutOfRange_Throws()
        {
            var rec = new ReplayRecorder(4);
            rec.Append(F(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => rec.Get(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => rec.Get(1));
        }

        [Test]
        public void Snapshot_ReturnsFramesInOrder()
        {
            var rec = new ReplayRecorder(3);
            rec.Append(F(10));
            rec.Append(F(20));
            rec.Append(F(30));
            rec.Append(F(40));

            var snap = rec.Snapshot();
            Assert.That(snap.Length, Is.EqualTo(3));
            Assert.That(snap[0].Turn, Is.EqualTo(20));
            Assert.That(snap[1].Turn, Is.EqualTo(30));
            Assert.That(snap[2].Turn, Is.EqualTo(40));
        }

        [Test]
        public void Clear_ResetsCount()
        {
            var rec = new ReplayRecorder(2);
            rec.Append(F(1));
            rec.Append(F(2));
            rec.Clear();
            Assert.That(rec.Count, Is.EqualTo(0));
            rec.Append(F(99));
            Assert.That(rec.Get(0).Turn, Is.EqualTo(99));
        }

        [Test]
        public void DefaultCapacity_WhenZeroOrNegative()
        {
            var rec = new ReplayRecorder(0);
            Assert.That(rec.Capacity, Is.EqualTo(ReplayRecorder.DefaultCapacity));
        }
    }
}
