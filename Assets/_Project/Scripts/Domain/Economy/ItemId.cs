namespace Alchemist.Domain.Economy
{
    /// <summary>
    /// 아이템 식별자. 순서 변경 금지(세이브 파일의 int[] counts 인덱스와 1:1 매핑).
    /// WHY: enum 값을 int 인덱스로 캐스팅해 저장하므로, 중간 삽입은 기존 유저 데이터 파괴.
    /// </summary>
    public enum ItemId : byte
    {
        MagicBrush = 0,
        Eraser = 1,
        ExtraPrism = 2,
        InkRefill = 3,
    }

    public static class ItemIdConsts
    {
        public const int Count = 4;
        public const int MaxStack = 99; // 플레이어당 보유 상한
    }
}
