using System;
using System.Collections.Generic;
using Alchemist.Domain.Economy;
using Alchemist.Domain.Meta;

namespace Alchemist.Domain.Player
{
    /// <summary>
    /// 모든 영속 플레이어 데이터의 루트 Aggregate.
    /// WHY: SaveService 는 이 한 객체만 직렬화하면 되도록 모든 의존 서브도메인을 소유.
    /// </summary>
    public sealed class PlayerProfile
    {
        public string Nickname { get; set; }
        public int Coins { get; set; }
        public int RankingScore { get; set; }

        public InkEnergy Ink { get; private set; }
        public Inventory Inventory { get; private set; }
        public GalleryProgress Gallery { get; private set; }

        /// <summary>해금된 배지 id 목록. Coder-A 의 Badges 도메인과 공유.</summary>
        public List<string> UnlockedBadges { get; private set; }

        public PlayerProfile(IClock clock)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            Nickname = "Alchemist";
            Coins = 0;
            RankingScore = 0;
            Ink = new InkEnergy(clock);
            Inventory = new Inventory();
            Gallery = new GalleryProgress();
            UnlockedBadges = new List<string>();
        }

        /// <summary>SaveService 가 역직렬화된 상태로부터 복원할 때 호출.</summary>
        internal void RestoreInk(InkEnergy ink)
        {
            Ink = ink ?? throw new ArgumentNullException(nameof(ink));
        }
        internal void RestoreInventory(Inventory inv)
        {
            Inventory = inv ?? throw new ArgumentNullException(nameof(inv));
        }
        internal void RestoreGallery(GalleryProgress g)
        {
            Gallery = g ?? throw new ArgumentNullException(nameof(g));
        }
        internal void RestoreBadges(List<string> badges)
        {
            UnlockedBadges = badges ?? new List<string>();
        }
    }
}
