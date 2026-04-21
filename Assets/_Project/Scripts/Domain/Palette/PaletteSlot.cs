using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Palette
{
    /// <summary>
    /// 팔레트의 한 저장 칸. Color 와 참조되는 Block 을 함께 들고 있다.
    /// WHY: Board 와 분리된 "보관소" — 저장된 블록은 연쇄/매치에 참여하지 않는다(D11).
    /// Block 참조를 유지해 꺼낼 때 Kind/Color 보존이 가능하도록.
    /// </summary>
    public sealed class PaletteSlot
    {
        public readonly int Index;
        public bool IsLocked;
        public ColorId Stored;
        public Block StoredBlock;

        public PaletteSlot(int index, bool isLocked)
        {
            Index = index;
            IsLocked = isLocked;
            Stored = ColorId.None;
            StoredBlock = null;
        }

        public bool IsEmpty
        {
            get { return Stored == ColorId.None && StoredBlock == null; }
        }

        public void Clear()
        {
            Stored = ColorId.None;
            StoredBlock = null;
        }
    }
}
