using NUnit.Framework;
using Alchemist.Domain.Meta;

namespace Alchemist.Tests.EditMode
{
    [TestFixture]
    public sealed class GalleryProgressTests
    {
        [Test]
        public void DefaultCtor_LoadsAllArtworks()
        {
            var g = new GalleryProgress();
            Assert.That(g.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(g.TryGet(ArtworkRegistry.Chapter1Id, out var ch1), Is.True);
            Assert.That(ch1.TotalFragments, Is.EqualTo(ArtworkRegistry.Chapter1Fragments));
            Assert.That(ch1.TotalFragments, Is.EqualTo(12));
        }

        [Test]
        public void AddFragmentForArtwork_AccumulatesProgress()
        {
            var g = new GalleryProgress();
            Assert.That(g.AddFragmentForArtwork(ArtworkRegistry.Chapter1Id), Is.True);
            Assert.That(g.AddFragmentForArtwork(ArtworkRegistry.Chapter1Id), Is.True);

            g.TryGet(ArtworkRegistry.Chapter1Id, out var ch1);
            Assert.That(ch1.SolvedFragments, Is.EqualTo(2));
            Assert.That(ch1.Progress, Is.EqualTo(2f / 12f).Within(0.0001f));
        }

        [Test]
        public void AddFragment_ReturnsFalseWhenCompleted()
        {
            var g = new GalleryProgress();
            for (int i = 0; i < 12; i++)
            {
                Assert.That(g.AddFragmentForArtwork(ArtworkRegistry.Chapter1Id), Is.True);
            }
            Assert.That(g.AddFragmentForArtwork(ArtworkRegistry.Chapter1Id), Is.False);
            g.TryGet(ArtworkRegistry.Chapter1Id, out var ch1);
            Assert.That(ch1.IsCompleted, Is.True);
            Assert.That(ch1.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void FindByChapter_ReturnsArtwork()
        {
            var g = new GalleryProgress();
            var ch1 = g.FindByChapter(1);
            Assert.That(ch1, Is.Not.Null);
            Assert.That(ch1.Id, Is.EqualTo(ArtworkRegistry.Chapter1Id));
        }

        [Test]
        public void OverallProgress_AveragesAcrossArtworks()
        {
            var g = new GalleryProgress();
            for (int i = 0; i < 6; i++) g.AddFragmentForArtwork(ArtworkRegistry.Chapter1Id);
            // chapter1 절반 완료. ch2 0. 전체 조각 = 12+16=28. solved=6. -> ~0.2142
            Assert.That(g.OverallProgress(), Is.EqualTo(6f / (12f + 16f)).Within(0.0001f));
        }

        [Test]
        public void Artwork_SolveFragment_RejectsDuplicateIndex()
        {
            var art = ArtworkRegistry.CreateChapter1();
            Assert.That(art.SolveFragment(3), Is.True);
            Assert.That(art.SolveFragment(3), Is.False);
            Assert.That(art.SolvedFragments, Is.EqualTo(1));
        }

        [Test]
        public void Artwork_RestoreMask_PreservesCount()
        {
            var art = ArtworkRegistry.CreateChapter1();
            art.SolveFragment(0);
            art.SolveFragment(5);
            var snap = art.SnapshotMask();

            var restored = ArtworkRegistry.CreateChapter1();
            restored.RestoreMask(snap);
            Assert.That(restored.SolvedFragments, Is.EqualTo(2));
            Assert.That(restored.IsFragmentSolved(0), Is.True);
            Assert.That(restored.IsFragmentSolved(5), Is.True);
            Assert.That(restored.IsFragmentSolved(3), Is.False);
        }
    }
}
