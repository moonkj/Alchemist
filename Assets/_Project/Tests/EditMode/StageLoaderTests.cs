using NUnit.Framework;
using Alchemist.Domain.Stages;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// StageLoader: Resources 미존재 fallback 경로 검증.
    /// WHY: 실제 SO 에셋 없이 EditMode 에서 돌아야 개발 중 CI 차단을 막음.
    /// </summary>
    [TestFixture]
    public sealed class StageLoaderTests
    {
        [Test]
        public void Load_NullOrEmptyId_ReturnsNull()
        {
            Assert.That(StageLoader.Load(null), Is.Null);
            Assert.That(StageLoader.Load(string.Empty), Is.Null);
        }

        [Test]
        public void Load_NonexistentId_ReturnsNull()
        {
            var s = StageLoader.Load("this_does_not_exist_xyz_123");
            Assert.That(s, Is.Null);
        }

        [Test]
        public void LoadOrFallback_WhenMissing_ReturnsFallback()
        {
            var fallback = StageLoader.CreateDefault();
            var s = StageLoader.LoadOrFallback("missing_id_xyz", fallback);
            Assert.That(s, Is.SameAs(fallback));
        }

        [Test]
        public void CreateDefault_ReturnsUsableDefaults()
        {
            var s = StageLoader.CreateDefault();
            Assert.That(s, Is.Not.Null);
            Assert.That(s.MaxMoves, Is.GreaterThan(0));
            Assert.That(s.ParMoves, Is.GreaterThan(0));
            Assert.That(s.PaletteSlotCount, Is.GreaterThanOrEqualTo(0));
            Assert.That(s.PaletteSlotCount, Is.LessThanOrEqualTo(3));
            Assert.That(s.InitialPlacements, Is.Not.Null);
        }

        [Test]
        public void ResolveInitialPrompt_UnknownId_FallsBackToPurple10()
        {
            var s = StageData.ResolvePromptById("nonexistent.prompt.id");
            Assert.That(s, Is.Not.Null);
            Assert.That(s.Id, Is.EqualTo("p1.sample.purple10"));
        }

        [Test]
        public void ResolvePromptById_KnownIds_ReturnMatching()
        {
            Assert.That(StageData.ResolvePromptById("p1.sample.purple10").Id, Is.EqualTo("p1.sample.purple10"));
            Assert.That(StageData.ResolvePromptById("p2.sample.advanced1").Id, Is.EqualTo("p2.sample.advanced1"));
            Assert.That(StageData.ResolvePromptById("p2.daily.1").Id, Is.EqualTo("p2.daily.1"));
        }
    }
}
