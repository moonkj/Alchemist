using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Alchemist.Domain.Economy;

namespace Alchemist.UI
{
    /// <summary>
    /// 게임 중 아이템 사용 UI 버튼. Inventory.Count 를 라벨로 표시, 탭 시 ActivationRequested 이벤트.
    /// WHY: 실제 아이템 타겟 선택(셀 탭)은 GameRoot/InputController 가 처리. 버튼은 모드 전환 트리거만 발행.
    /// </summary>
    public sealed class ItemButton : MonoBehaviour
    {
        [SerializeField] private ItemId _itemId;
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _countLabel;

        private Inventory _inventory;

        public ItemId ItemId => _itemId;
        public event Action<ItemId> ActivationRequested;

        public void Bind(Inventory inventory)
        {
            _inventory = inventory;
            if (_inventory != null)
            {
                _inventory.CountChanged += OnCountChanged;
            }
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
                _button.onClick.AddListener(OnClick);
            }
            Refresh();
        }

        private void OnDestroy()
        {
            if (_inventory != null) _inventory.CountChanged -= OnCountChanged;
            if (_button != null) _button.onClick.RemoveListener(OnClick);
        }

        private void OnCountChanged(ItemId id)
        {
            if (id == _itemId) Refresh();
        }

        private void OnClick()
        {
            if (_inventory == null || !_inventory.Has(_itemId)) return;
            ActivationRequested?.Invoke(_itemId);
        }

        private void Refresh()
        {
            int count = _inventory != null ? _inventory.Get(_itemId) : 0;
            if (_countLabel != null) _countLabel.SetText("{0}", count);
            if (_button != null) _button.interactable = count > 0;
        }
    }
}
