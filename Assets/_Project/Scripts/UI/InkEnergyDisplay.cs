using TMPro;
using UnityEngine;
using Alchemist.Domain.Economy;

namespace Alchemist.UI
{
    /// <summary>
    /// 상단바 잉크 HUD. current/max 와 다음 회복까지 mm:ss 표시.
    /// WHY: 1초 단위로 폴링(Update)해 이벤트 구독 없이 단순화(InkEnergy 는 순수 POCO).
    /// </summary>
    public sealed class InkEnergyDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countLabel;
        [SerializeField] private TextMeshProUGUI _timerLabel;

        private InkEnergy _ink;
        private int _lastCount = -1;
        private int _lastSeconds = -1;
        private float _pollAccum;

        public void Bind(InkEnergy ink)
        {
            _ink = ink;
            _lastCount = -1;
            _lastSeconds = -1;
            _pollAccum = 0f;
            Refresh(force: true);
        }

        private void Update()
        {
            if (_ink == null) return;
            // WHY: 초 단위 카운트 다운만 필요하므로 매 프레임 포맷팅하지 않고 ~0.25s 주기로 갱신.
            _pollAccum += Time.unscaledDeltaTime;
            if (_pollAccum < 0.25f) return;
            _pollAccum = 0f;
            Refresh(force: false);
        }

        private void Refresh(bool force)
        {
            _ink.Refill();
            int cur = _ink.Current;
            int sec = _ink.SecondsUntilNext();

            if (force || cur != _lastCount)
            {
                _lastCount = cur;
                if (_countLabel != null) _countLabel.SetText("{0}/{1}", cur, _ink.Max);
            }
            if (force || sec != _lastSeconds)
            {
                _lastSeconds = sec;
                if (_timerLabel != null)
                {
                    if (sec <= 0) _timerLabel.SetText("MAX");
                    else _timerLabel.SetText("{0:D2}:{1:D2}", sec / 60, sec % 60);
                }
            }
        }
    }
}
