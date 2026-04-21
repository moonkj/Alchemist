using System;

namespace Alchemist.Domain.Economy
{
    /// <summary>
    /// 아이템 인벤토리. 각 ItemId 슬롯당 0~99 보유.
    /// WHY: Dictionary 대신 int[] 로 고정 크기 유지(allocation-free, 세이브 결정적).
    /// </summary>
    public sealed class Inventory
    {
        private readonly int[] _counts;

        public event Action<ItemId> CountChanged;

        public Inventory()
        {
            _counts = new int[ItemIdConsts.Count];
        }

        /// <summary>세이브 복원 전용. counts.Length 가 현재 enum 과 다르면 가능한 만큼만 복사.</summary>
        public Inventory(int[] counts)
        {
            _counts = new int[ItemIdConsts.Count];
            if (counts == null) return;
            int n = counts.Length < _counts.Length ? counts.Length : _counts.Length;
            for (int i = 0; i < n; i++)
            {
                _counts[i] = Clamp(counts[i]);
            }
        }

        public int Get(ItemId id) => _counts[(int)id];

        public int[] Snapshot()
        {
            // WHY: 세이브 직렬화용 방어적 복사. 외부가 내부 배열을 변경하지 못하게 함.
            var copy = new int[_counts.Length];
            for (int i = 0; i < _counts.Length; i++) copy[i] = _counts[i];
            return copy;
        }

        /// <summary>amount 만큼 추가. 실제 반영된 수량(상한 초과분 제외) 반환.</summary>
        public int Add(ItemId id, int amount)
        {
            if (amount <= 0) return 0;
            int idx = (int)id;
            int before = _counts[idx];
            int next = before + amount;
            if (next > ItemIdConsts.MaxStack) next = ItemIdConsts.MaxStack;
            int delta = next - before;
            if (delta > 0)
            {
                _counts[idx] = next;
                CountChanged?.Invoke(id);
            }
            return delta;
        }

        /// <summary>1개 사용. 성공 시 true. WHY: 잔고 부족이면 부분 사용 금지.</summary>
        public bool Use(ItemId id, int amount = 1)
        {
            if (amount <= 0) return false;
            int idx = (int)id;
            if (_counts[idx] < amount) return false;
            _counts[idx] -= amount;
            CountChanged?.Invoke(id);
            return true;
        }

        public bool Has(ItemId id, int amount = 1) => _counts[(int)id] >= amount;

        private static int Clamp(int v)
        {
            if (v < 0) return 0;
            if (v > ItemIdConsts.MaxStack) return ItemIdConsts.MaxStack;
            return v;
        }
    }
}
