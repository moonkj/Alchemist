using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Alchemist.Domain.Economy;
using Alchemist.Domain.Meta;
using Alchemist.Domain.Player;

namespace Alchemist.Tests.EditMode
{
    [TestFixture]
    public sealed class SaveServiceTests
    {
        private sealed class FixedClock : IClock
        {
            public DateTime Now { get; set; } = new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);
            public DateTime UtcNow => Now;
        }

        private sealed class TempPathProvider : IPathProvider, IDisposable
        {
            public string SaveRoot { get; }
            public TempPathProvider()
            {
                SaveRoot = Path.Combine(Path.GetTempPath(), "alchemist_test_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(SaveRoot);
            }
            public void Dispose()
            {
                try { if (Directory.Exists(SaveRoot)) Directory.Delete(SaveRoot, true); }
                catch { /* ignore */ }
            }
        }

        [Test]
        public async Task Load_WhenFileMissing_ReturnsNull()
        {
            using (var paths = new TempPathProvider())
            {
                var svc = new SaveService(paths, new FixedClock());
                var loaded = await svc.LoadAsync();
                Assert.That(loaded, Is.Null);
            }
        }

        [Test]
        public async Task SaveThenLoad_RoundTripsAllFields()
        {
            using (var paths = new TempPathProvider())
            {
                var clock = new FixedClock();
                var svc = new SaveService(paths, clock);
                var original = new PlayerProfile(clock)
                {
                    Nickname = "테스트",
                    Coins = 123,
                    RankingScore = 4567
                };
                original.Inventory.Add(ItemId.MagicBrush, 5);
                original.Inventory.Add(ItemId.Eraser, 2);
                original.Ink.Consume();
                original.UnlockedBadges.Add("first_clear");
                original.Gallery.AddFragmentForArtwork(ArtworkRegistry.Chapter1Id);
                original.Gallery.AddFragmentForArtwork(ArtworkRegistry.Chapter1Id);

                await svc.SaveAsync(original);
                var loaded = await svc.LoadAsync();

                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.Nickname, Is.EqualTo("테스트"));
                Assert.That(loaded.Coins, Is.EqualTo(123));
                Assert.That(loaded.RankingScore, Is.EqualTo(4567));
                Assert.That(loaded.Inventory.Get(ItemId.MagicBrush), Is.EqualTo(5));
                Assert.That(loaded.Inventory.Get(ItemId.Eraser), Is.EqualTo(2));
                Assert.That(loaded.Ink.Current, Is.EqualTo(4));
                Assert.That(loaded.UnlockedBadges, Contains.Item("first_clear"));
                loaded.Gallery.TryGet(ArtworkRegistry.Chapter1Id, out var ch1);
                Assert.That(ch1, Is.Not.Null);
                Assert.That(ch1.SolvedFragments, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task Load_CorruptedFile_ReturnsNull()
        {
            using (var paths = new TempPathProvider())
            {
                File.WriteAllText(Path.Combine(paths.SaveRoot, SaveService.FileName), "{not valid json");
                var svc = new SaveService(paths, new FixedClock());
                var loaded = await svc.LoadAsync();
                Assert.That(loaded, Is.Null);
            }
        }

        [Test]
        public async Task Save_IsAtomic_TempFileRemoved()
        {
            using (var paths = new TempPathProvider())
            {
                var clock = new FixedClock();
                var svc = new SaveService(paths, clock);
                var p = new PlayerProfile(clock);
                await svc.SaveAsync(p);
                string tmp = Path.Combine(paths.SaveRoot, SaveService.FileName + ".tmp");
                string final = Path.Combine(paths.SaveRoot, SaveService.FileName);
                Assert.That(File.Exists(final), Is.True);
                Assert.That(File.Exists(tmp), Is.False, "tmp 파일이 남아서는 안 됨");
            }
        }

        [Test]
        public async Task Load_RecoversFromBackup_WhenMainDeleted()
        {
            using (var paths = new TempPathProvider())
            {
                var clock = new FixedClock();
                var svc = new SaveService(paths, clock);
                var p = new PlayerProfile(clock) { Coins = 42 };
                await svc.SaveAsync(p);
                // 두 번째 저장 → main 을 .bak 으로 밀고 tmp→main 재생성.
                p.Coins = 99;
                await svc.SaveAsync(p);

                string mainPath = Path.Combine(paths.SaveRoot, SaveService.FileName);
                string bakPath = mainPath + ".bak";
                Assert.That(File.Exists(bakPath), Is.True);

                // main 삭제 → bak 에서 복구 로드.
                File.Delete(mainPath);
                var loaded = await svc.LoadAsync();
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.Coins, Is.EqualTo(42));
            }
        }
    }
}
