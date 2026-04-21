namespace Alchemist.Domain.Meta
{
    /// <summary>
    /// 챕터/명화 초안 데이터. Phase 4 에서 ScriptableObject / Addressables 로 마이그레이션 예정.
    /// WHY: 하드코딩된 데이터라도 테스트에서 결정적 값을 얻기 위해 static 접근 제공.
    /// </summary>
    public static class ArtworkRegistry
    {
        public const string Chapter1Id = "chapter1_sunset";
        public const string Chapter2Id = "chapter2_oceanfloor";

        /// <summary>챕터 1 "잃어버린 노을": 12조각 (스테이지당 약 8~10% 복원).</summary>
        public const int Chapter1Fragments = 12;

        /// <summary>챕터 2 "해저의 정원": 16조각(초안).</summary>
        public const int Chapter2Fragments = 16;

        public static Artwork CreateChapter1()
        {
            return new Artwork(Chapter1Id, chapter: 1, titleKey: "artwork.chapter1.title", totalFragments: Chapter1Fragments);
        }

        public static Artwork CreateChapter2()
        {
            return new Artwork(Chapter2Id, chapter: 2, titleKey: "artwork.chapter2.title", totalFragments: Chapter2Fragments);
        }

        public static Artwork[] CreateAll()
        {
            return new[] { CreateChapter1(), CreateChapter2() };
        }
    }
}
