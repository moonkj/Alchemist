using System;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Palette
{
    /// <summary>
    /// 최대 3 슬롯 팔레트(D4). 1→2→3 점진 해제는 StageData.PaletteSlotCount 로 제어.
    /// WHY: 슬롯 블록은 연쇄 참여 없음(D11) — Board 와 완전 분리된 도메인에 거주.
    /// 할당 제로: 내부 배열은 ctor 에서 고정, 재할당 없음. foreach 금지.
    /// </summary>
    public sealed class Palette : IPaletteEvents
    {
        public const int MaxSlots = 3;

        private readonly PaletteSlot[] _slots;
        private int _unlockedCount;

        public event Action<int> SlotChanged;
        public event Action<int> UnlockedCountChanged;

        public Palette(int unlockedCount)
        {
            if (unlockedCount < 0) unlockedCount = 0;
            if (unlockedCount > MaxSlots) unlockedCount = MaxSlots;

            _slots = new PaletteSlot[MaxSlots];
            for (int i = 0; i < MaxSlots; i++)
            {
                _slots[i] = new PaletteSlot(i, isLocked: i >= unlockedCount);
            }
            _unlockedCount = unlockedCount;
        }

        public int MaxSlotCount { get { return MaxSlots; } }
        public int UnlockedCount { get { return _unlockedCount; } }

        public PaletteSlot GetSlot(int index)
        {
            if ((uint)index >= (uint)MaxSlots) return null;
            return _slots[index];
        }

        /// <summary>해금된 슬롯 수 변경(스테이지 전환 등). 축소 시 초과 슬롯은 Clear.</summary>
        public void SetUnlockedCount(int count)
        {
            if (count < 0) count = 0;
            if (count > MaxSlots) count = MaxSlots;
            if (count == _unlockedCount) return;

            for (int i = 0; i < MaxSlots; i++)
            {
                bool lockedNow = i >= count;
                if (lockedNow && !_slots[i].IsEmpty)
                {
                    // WHY: 축소로 접근 불가 슬롯은 내용도 비워야 유령 참조 방지.
                    _slots[i].Clear();
                    SlotChanged?.Invoke(i);
                }
                _slots[i].IsLocked = lockedNow;
            }
            _unlockedCount = count;
            UnlockedCountChanged?.Invoke(count);
        }

        /// <summary>슬롯에 블록 저장. 이미 점유된 경우 false. 잠긴 슬롯도 false.</summary>
        public bool Store(int slotIndex, Block block)
        {
            if ((uint)slotIndex >= (uint)MaxSlots) return false;
            if (block == null) return false;
            var s = _slots[slotIndex];
            if (s.IsLocked) return false;
            if (!s.IsEmpty) return false;

            s.Stored = block.Color;
            s.StoredBlock = block;
            SlotChanged?.Invoke(slotIndex);
            return true;
        }

        /// <summary>슬롯에서 꺼내기. 비어있거나 잠겼으면 null. 내부 상태는 비움.</summary>
        public Block Use(int slotIndex)
        {
            if ((uint)slotIndex >= (uint)MaxSlots) return null;
            var s = _slots[slotIndex];
            if (s.IsLocked) return null;
            if (s.IsEmpty) return null;

            Block b = s.StoredBlock;
            s.Clear();
            SlotChanged?.Invoke(slotIndex);
            return b;
        }

        /// <summary>비어있고 잠기지 않은 첫 슬롯 index (없으면 -1).</summary>
        public int FindFirstEmpty()
        {
            for (int i = 0; i < _unlockedCount; i++)
            {
                if (_slots[i].IsEmpty) return i;
            }
            return -1;
        }

        /// <summary>현재 비어있는 슬롯 개수 (잠긴 것 제외). 잔여 보너스(Residual) 계산용.</summary>
        public int RemainingEmptyUnlocked()
        {
            int n = 0;
            for (int i = 0; i < _unlockedCount; i++)
            {
                if (_slots[i].IsEmpty) n++;
            }
            return n;
        }
    }
}
