using System.Collections.Generic;
using NUnit.Framework;
using Alchemist.Domain.Badges;

namespace Alchemist.Tests.EditMode
{
    /// <summary>BadgeRegistry 건전성: 16 배지 모두 고유 ID, 로컬라이제이션 키 비어있지 않음.</summary>
    [TestFixture]
    public sealed class BadgeRegistryTests
    {
        [Test]
        public void All_ContainsSixteenBadges()
        {
            Assert.That(BadgeRegistry.All.Count, Is.EqualTo(16));
        }

        [Test]
        public void All_IdsAreUnique()
        {
            var seen = new HashSet<BadgeId>();
            for (int i = 0; i < BadgeRegistry.All.Count; i++)
            {
                var id = BadgeRegistry.All[i].Id;
                Assert.That(seen.Add(id), Is.True, "Duplicate badge id: " + id);
            }
        }

        [Test]
        public void All_NoneReservedForSentinel()
        {
            for (int i = 0; i < BadgeRegistry.All.Count; i++)
            {
                Assert.That(BadgeRegistry.All[i].Id, Is.Not.EqualTo(BadgeId.None));
            }
        }

        [Test]
        public void All_LocalizationKeysNonEmpty()
        {
            for (int i = 0; i < BadgeRegistry.All.Count; i++)
            {
                var b = BadgeRegistry.All[i];
                Assert.That(b.LocalizedNameKey, Is.Not.Empty, b.Id.ToString());
                Assert.That(b.LocalizedHintKey, Is.Not.Empty, b.Id.ToString());
                Assert.That(b.IconKey, Is.Not.Empty, b.Id.ToString());
                Assert.That(b.Condition, Is.Not.Null, b.Id.ToString());
            }
        }

        [Test]
        public void HiddenBadges_CountFive()
        {
            int hidden = 0;
            for (int i = 0; i < BadgeRegistry.All.Count; i++)
            {
                if (BadgeRegistry.All[i].IsHidden) hidden++;
            }
            Assert.That(hidden, Is.EqualTo(5));
        }

        [Test]
        public void Get_ReturnsCorrectBadge()
        {
            var b = BadgeRegistry.Get(BadgeId.FirstPurple);
            Assert.That(b, Is.Not.Null);
            Assert.That(b.Id, Is.EqualTo(BadgeId.FirstPurple));
        }

        [Test]
        public void Get_None_ReturnsNull()
        {
            Assert.That(BadgeRegistry.Get(BadgeId.None), Is.Null);
        }
    }
}
