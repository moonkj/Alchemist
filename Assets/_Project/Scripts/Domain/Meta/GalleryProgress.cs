using System.Collections.Generic;

namespace Alchemist.Domain.Meta
{
    /// <summary>
    /// 플레이어 전체 갤러리 상태. 스테이지 클리어 시 해당 챕터의 Artwork 에 조각 추가.
    /// WHY: 스테이지 -> 챕터 매핑은 메타 레이어 책임이라 StageData 에서 분리.
    /// </summary>
    public sealed class GalleryProgress
    {
        private readonly Dictionary<string, Artwork> _artworks;

        public GalleryProgress() : this(ArtworkRegistry.CreateAll()) { }

        public GalleryProgress(Artwork[] artworks)
        {
            _artworks = new Dictionary<string, Artwork>(artworks != null ? artworks.Length : 0);
            if (artworks == null) return;
            for (int i = 0; i < artworks.Length; i++)
            {
                var a = artworks[i];
                if (a != null) _artworks[a.Id] = a;
            }
        }

        public bool TryGet(string id, out Artwork artwork) => _artworks.TryGetValue(id, out artwork);

        public IEnumerable<Artwork> All => _artworks.Values;

        public int Count => _artworks.Count;

        /// <summary>스테이지 클리어 시 호출: 다음 조각을 복원. 이미 완성이면 false.</summary>
        public bool AddFragmentForArtwork(string artworkId)
        {
            if (!_artworks.TryGetValue(artworkId, out var artwork)) return false;
            return artwork.SolveNext() >= 0;
        }

        /// <summary>챕터 번호로 Artwork 조회(첫 매치). WHY: 스테이지는 챕터 번호만 알고 Artwork Id 는 모를 수 있음.</summary>
        public Artwork FindByChapter(int chapter)
        {
            foreach (var kv in _artworks)
            {
                if (kv.Value.Chapter == chapter) return kv.Value;
            }
            return null;
        }

        /// <summary>전체 조각 기준 진행도(가중 평균 아닌 단순 합계).</summary>
        public float OverallProgress()
        {
            int total = 0, solved = 0;
            foreach (var kv in _artworks)
            {
                total += kv.Value.TotalFragments;
                solved += kv.Value.SolvedFragments;
            }
            return total == 0 ? 0f : (float)solved / total;
        }
    }
}
