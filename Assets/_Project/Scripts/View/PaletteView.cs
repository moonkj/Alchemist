using UnityEngine;
using UnityEngine.UI;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Palette;

namespace Alchemist.View
{
    /// <summary>
    /// 팔레트 3 슬롯 UI 렌더러. 도메인 <see cref="Palette"/> 의 이벤트 구독으로 갱신.
    /// WHY: Bind 시 SlotChanged/UnlockedCountChanged 를 한 번 구독 → 변경 시에만 지역 슬롯 업데이트.
    /// 드래그/드롭 UX 는 입력 레이어(InputController) 가 담당하고, 본 View 는 시각만 책임.
    /// </summary>
    public sealed class PaletteView : MonoBehaviour
    {
        [SerializeField] private Image[] _slotImages; // length == Palette.MaxSlots(3)
        [SerializeField] private GameObject[] _lockOverlays; // length == Palette.MaxSlots

        private Palette _palette;

        /// <summary>팔레트 도메인에 연결. 기존 구독이 있으면 해제 후 재구독.</summary>
        public void Bind(Palette palette)
        {
            if (_palette != null)
            {
                _palette.SlotChanged -= OnSlotChanged;
                _palette.UnlockedCountChanged -= OnUnlockedChanged;
            }
            _palette = palette;
            if (_palette == null) return;

            _palette.SlotChanged += OnSlotChanged;
            _palette.UnlockedCountChanged += OnUnlockedChanged;
            RefreshAll();
        }

        private void OnDestroy()
        {
            if (_palette != null)
            {
                _palette.SlotChanged -= OnSlotChanged;
                _palette.UnlockedCountChanged -= OnUnlockedChanged;
                _palette = null;
            }
        }

        private void RefreshAll()
        {
            if (_palette == null) return;
            int n = _slotImages != null ? _slotImages.Length : 0;
            for (int i = 0; i < n; i++) OnSlotChanged(i);
            OnUnlockedChanged(_palette.UnlockedCount);
        }

        private void OnSlotChanged(int index)
        {
            if (_slotImages == null) return;
            if ((uint)index >= (uint)_slotImages.Length) return;

            var img = _slotImages[index];
            if (img == null) return;

            var slot = _palette.GetSlot(index);
            if (slot == null || slot.IsEmpty)
            {
                img.color = AppColors.Empty;
                return;
            }

            // WHY: 팔레트 시각은 저장된 블록의 원색만 표시 (상태 전이 애니메이션 없음).
            if (AppColors.TryGet(slot.Stored, out var c32)) img.color = c32;
            else img.color = AppColors.Empty;
        }

        private void OnUnlockedChanged(int unlockedCount)
        {
            if (_lockOverlays == null) return;
            for (int i = 0; i < _lockOverlays.Length; i++)
            {
                if (_lockOverlays[i] == null) continue;
                // 잠긴 슬롯 overlay 는 unlockedCount 이상 인덱스에서 활성.
                _lockOverlays[i].SetActive(i >= unlockedCount);
            }
        }

        /// <summary>입력 레이어에서 빈 공간으로 꺼내기 요청 시 호출(Phase 2 임시 핸들러).</summary>
        public ColorId TakeSlot(int index)
        {
            if (_palette == null) return ColorId.None;
            var slot = _palette.GetSlot(index);
            if (slot == null || slot.IsEmpty) return ColorId.None;
            var color = slot.Stored;
            _palette.Use(index);
            return color;
        }
    }
}
