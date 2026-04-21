using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alchemist.Domain.Economy;
using Alchemist.Domain.Meta;

namespace Alchemist.Domain.Player
{
    /// <summary>
    /// 영속 저장소 구현: JSON 직렬화 + 임시파일 쓰기 후 rename 으로 원자적 저장.
    /// WHY: UnityEngine.JsonUtility 는 Dictionary/중첩 제약이 있어 Meta 복원이 어려움.
    ///      외부 의존 없이 간단한 수동 JSON 인코더로 결정적 포맷 유지.
    /// WHY: I/O 는 비동기 Task 로 수행해 게임 루프 블로킹 방지(요구사항 "게임 중 I/O 금지").
    /// </summary>
    public sealed class SaveService : IPlayerProfileStore
    {
        public const string FileName = "player_profile.json";
        public const int Version = 1;

        private readonly IPathProvider _paths;
        private readonly IClock _clock;

        public SaveService(IPathProvider paths, IClock clock)
        {
            _paths = paths ?? throw new ArgumentNullException(nameof(paths));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        private string FullPath => Path.Combine(_paths.SaveRoot, FileName);
        private string TempPath => FullPath + ".tmp";
        private string BackupPath => FullPath + ".bak";

        public async Task<PlayerProfile> LoadAsync(CancellationToken ct = default)
        {
            string path = FullPath;
            if (!File.Exists(path))
            {
                // WHY: 이전 세션이 저장 중 크래시했으면 .bak 에서 복구 시도.
                if (File.Exists(BackupPath))
                {
                    path = BackupPath;
                }
                else
                {
                    return null;
                }
            }

            string text;
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    text = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
            }
            catch (IOException)
            {
                return null;
            }

            PlayerProfile profile;
            try
            {
                profile = Deserialize(text);
            }
            catch
            {
                // WHY: 손상 파일은 삭제하고 null 반환(호출자가 기본 프로필 생성).
                // 실제 운영에서는 로깅 필요(Phase 4 로깅 파이프라인 이후).
                profile = null;
            }
            return profile;
        }

        public async Task SaveAsync(PlayerProfile profile, CancellationToken ct = default)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            Directory.CreateDirectory(_paths.SaveRoot);

            string json = Serialize(profile);
            byte[] bytes = Encoding.UTF8.GetBytes(json);

            // 1) tmp 에 전량 기록
            using (var fs = new FileStream(TempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await fs.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false);
                await fs.FlushAsync(ct).ConfigureAwait(false);
            }

            // 2) 기존 파일을 .bak 으로 백업, 그 후 tmp->정본. WHY: 중간 크래시 시 .bak 으로 복구.
            try
            {
                if (File.Exists(FullPath))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Move(FullPath, BackupPath);
                }
                File.Move(TempPath, FullPath);
            }
            catch (IOException)
            {
                // 롤백: tmp 남아 있으면 다음 시도에서 덮어씀.
                if (File.Exists(BackupPath) && !File.Exists(FullPath))
                {
                    File.Move(BackupPath, FullPath);
                }
                throw;
            }
        }

        // ---------- Serialization (수동 JSON) ----------

        internal static string Serialize(PlayerProfile p)
        {
            var sb = new StringBuilder(512);
            sb.Append('{');
            W(sb, "version", Version); sb.Append(',');
            W(sb, "nickname", p.Nickname ?? ""); sb.Append(',');
            W(sb, "coins", p.Coins); sb.Append(',');
            W(sb, "ranking", p.RankingScore); sb.Append(',');

            // Ink
            sb.Append("\"ink\":{");
            W(sb, "current", p.Ink.Current); sb.Append(',');
            W(sb, "max", p.Ink.Max); sb.Append(',');
            W(sb, "lastRefillUtcTicks", p.Ink.LastRefillUtc.Ticks);
            sb.Append("},");

            // Inventory
            sb.Append("\"inventory\":[");
            var counts = p.Inventory.Snapshot();
            for (int i = 0; i < counts.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(counts[i]);
            }
            sb.Append("],");

            // Badges
            sb.Append("\"badges\":[");
            var badges = p.UnlockedBadges;
            for (int i = 0; i < badges.Count; i++)
            {
                if (i > 0) sb.Append(',');
                AppendString(sb, badges[i] ?? "");
            }
            sb.Append("],");

            // Gallery
            sb.Append("\"gallery\":[");
            bool first = true;
            foreach (var art in p.Gallery.All)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('{');
                W(sb, "id", art.Id); sb.Append(',');
                W(sb, "chapter", art.Chapter); sb.Append(',');
                W(sb, "titleKey", art.LocalizedTitleKey); sb.Append(',');
                W(sb, "total", art.TotalFragments); sb.Append(',');
                sb.Append("\"mask\":[");
                var mask = art.SnapshotMask();
                for (int i = 0; i < mask.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(mask[i] ? '1' : '0');
                }
                sb.Append("]}");
            }
            sb.Append(']');

            sb.Append('}');
            return sb.ToString();
        }

        internal PlayerProfile Deserialize(string json)
        {
            var tokens = MiniJson.Parse(json);
            var root = tokens as Dictionary<string, object>;
            if (root == null) throw new FormatException("root not object");

            int version = AsInt(root, "version", 0);
            if (version < 1 || version > Version) throw new FormatException("version mismatch");

            var profile = new PlayerProfile(_clock);
            profile.Nickname = AsString(root, "nickname", "Alchemist");
            profile.Coins = AsInt(root, "coins", 0);
            profile.RankingScore = AsInt(root, "ranking", 0);

            // Ink
            if (root.TryGetValue("ink", out var inkObj) && inkObj is Dictionary<string, object> inkDict)
            {
                int cur = AsInt(inkDict, "current", InkEnergy.DefaultMax);
                int max = AsInt(inkDict, "max", InkEnergy.DefaultMax);
                long ticks = AsLong(inkDict, "lastRefillUtcTicks", _clock.UtcNow.Ticks);
                DateTime last;
                try { last = new DateTime(ticks, DateTimeKind.Utc); }
                catch { last = _clock.UtcNow; }
                profile.RestoreInk(new InkEnergy(_clock, cur, max, last));
            }

            // Inventory
            if (root.TryGetValue("inventory", out var invObj) && invObj is List<object> invList)
            {
                var counts = new int[invList.Count];
                for (int i = 0; i < invList.Count; i++)
                {
                    counts[i] = (int)Convert.ToInt64(invList[i]);
                }
                profile.RestoreInventory(new Inventory(counts));
            }

            // Badges
            if (root.TryGetValue("badges", out var badgesObj) && badgesObj is List<object> badgesList)
            {
                var list = new List<string>(badgesList.Count);
                for (int i = 0; i < badgesList.Count; i++) list.Add(badgesList[i] as string ?? "");
                profile.RestoreBadges(list);
            }

            // Gallery
            if (root.TryGetValue("gallery", out var galObj) && galObj is List<object> galList)
            {
                var artworks = new List<Artwork>(galList.Count);
                for (int i = 0; i < galList.Count; i++)
                {
                    if (galList[i] is Dictionary<string, object> ad)
                    {
                        string id = AsString(ad, "id", "");
                        if (string.IsNullOrEmpty(id)) continue;
                        int chapter = AsInt(ad, "chapter", 1);
                        string titleKey = AsString(ad, "titleKey", "");
                        int total = AsInt(ad, "total", 1);
                        var art = new Artwork(id, chapter, titleKey, total);
                        if (ad.TryGetValue("mask", out var maskObj) && maskObj is List<object> maskList)
                        {
                            var mask = new bool[maskList.Count];
                            for (int k = 0; k < maskList.Count; k++)
                            {
                                var v = maskList[k];
                                mask[k] = v is bool bv ? bv : Convert.ToInt64(v) != 0;
                            }
                            art.RestoreMask(mask);
                        }
                        artworks.Add(art);
                    }
                }
                profile.RestoreGallery(new GalleryProgress(artworks.ToArray()));
            }

            return profile;
        }

        // ---------- 헬퍼 ----------

        private static void W(StringBuilder sb, string key, int value)
        {
            AppendString(sb, key); sb.Append(':'); sb.Append(value);
        }
        private static void W(StringBuilder sb, string key, long value)
        {
            AppendString(sb, key); sb.Append(':'); sb.Append(value);
        }
        private static void W(StringBuilder sb, string key, string value)
        {
            AppendString(sb, key); sb.Append(':'); AppendString(sb, value ?? "");
        }
        private static void AppendString(StringBuilder sb, string s)
        {
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4"));
                        }
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private static int AsInt(Dictionary<string, object> d, string key, int fallback)
        {
            if (d.TryGetValue(key, out var v) && v != null)
            {
                try { return (int)Convert.ToInt64(v); } catch { return fallback; }
            }
            return fallback;
        }
        private static long AsLong(Dictionary<string, object> d, string key, long fallback)
        {
            if (d.TryGetValue(key, out var v) && v != null)
            {
                try { return Convert.ToInt64(v); } catch { return fallback; }
            }
            return fallback;
        }
        private static string AsString(Dictionary<string, object> d, string key, string fallback)
        {
            if (d.TryGetValue(key, out var v) && v is string s) return s;
            return fallback;
        }
    }
}
