using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Alchemist.Domain.Ranking;
using Alchemist.Services.Ranking;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// LocalRankingService 의 Submit/Fetch round-trip + 디스크 지속성 검증.
    /// 임시 파일은 Application.temporaryCachePath 에 격리.
    /// </summary>
    [TestFixture]
    public sealed class LocalRankingServiceTests
    {
        private string _tempFile;

        [SetUp]
        public void SetUp()
        {
            _tempFile = Path.Combine(
                Application.temporaryCachePath,
                "ranking_test_" + System.Guid.NewGuid().ToString("N") + ".json");
            if (File.Exists(_tempFile)) File.Delete(_tempFile);
        }

        [TearDown]
        public void TearDown()
        {
            if (_tempFile != null && File.Exists(_tempFile)) File.Delete(_tempFile);
        }

        private RankingEntry NewEntry(string player, int score, RankingCategory cat = RankingCategory.Global, string stage = "s1")
        {
            return new RankingEntry(player, stage, score, 10, 2, 1000, cat);
        }

        [Test]
        public async Task Submit_Then_Fetch_RoundTrips()
        {
            var svc = new LocalRankingService(_tempFile);
            await svc.SubmitAsync(NewEntry("A", 100));
            await svc.SubmitAsync(NewEntry("B", 300));

            var top = await svc.FetchTopAsync(RankingCategory.Global, 5);
            Assert.That(top.Count, Is.EqualTo(2));
            Assert.That(top[0].PlayerId, Is.EqualTo("B"));
        }

        [Test]
        public async Task Persistence_ReloadsFromDisk()
        {
            var svc1 = new LocalRankingService(_tempFile);
            await svc1.SubmitAsync(NewEntry("persist", 777));

            var svc2 = new LocalRankingService(_tempFile);
            var top = await svc2.FetchTopAsync(RankingCategory.Global, 1);
            Assert.That(top.Count, Is.EqualTo(1));
            Assert.That(top[0].PlayerId, Is.EqualTo("persist"));
            Assert.That(top[0].Score, Is.EqualTo(777));
        }

        [Test]
        public async Task Categories_IsolatedFromEachOther()
        {
            var svc = new LocalRankingService(_tempFile);
            await svc.SubmitAsync(NewEntry("g", 10, RankingCategory.Global));
            await svc.SubmitAsync(NewEntry("d", 20, RankingCategory.Daily));

            var globals = await svc.FetchTopAsync(RankingCategory.Global, 10);
            var dailies = await svc.FetchTopAsync(RankingCategory.Daily, 10);
            Assert.That(globals.Count, Is.EqualTo(1));
            Assert.That(dailies.Count, Is.EqualTo(1));
            Assert.That(globals[0].PlayerId, Is.EqualTo("g"));
            Assert.That(dailies[0].PlayerId, Is.EqualTo("d"));
        }

        [Test]
        public async Task FetchPersonalBest_ReturnsHighestForPlayer()
        {
            var svc = new LocalRankingService(_tempFile);
            await svc.SubmitAsync(NewEntry("me", 100));
            await svc.SubmitAsync(NewEntry("me", 500));
            await svc.SubmitAsync(NewEntry("other", 999));

            var pb = await svc.FetchPersonalBestAsync("me", RankingCategory.Global);
            Assert.That(pb.PlayerId, Is.EqualTo("me"));
            Assert.That(pb.Score, Is.EqualTo(500));
        }

        [Test]
        public async Task FetchTopForStage_FiltersByStage()
        {
            var svc = new LocalRankingService(_tempFile);
            await svc.SubmitAsync(NewEntry("A", 300, RankingCategory.Prompt, stage: "s1"));
            await svc.SubmitAsync(NewEntry("B", 999, RankingCategory.Prompt, stage: "s2"));

            var top = await svc.FetchTopAsync(RankingCategory.Prompt, 10, stageId: "s1");
            Assert.That(top.Count, Is.EqualTo(1));
            Assert.That(top[0].PlayerId, Is.EqualTo("A"));
        }
    }
}
