using UnityEngine;

namespace Alchemist.View.Effects
{
    /// <summary>
    /// 저사양 대체: 12~16 프레임 물방울 애니를 SpriteRenderer 로 순환. WHY: metaball/jelly shader
    /// 를 비활성화하는 Low 모드에서도 시각적 "액체감" 을 유지하기 위한 프리렌더링 스프라이트 루프.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSheetFallback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _sr;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _frameRate = 14f;  // 12~16 fps 권장

        private float _accum;
        private int _index;
        private bool _active;

        private void Reset()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        /// <summary>품질별 활성화. Low 에서만 스프라이트 루프 on.</summary>
        public void ApplyQuality(GraphicsQualityLevel level)
        {
            _active = level == GraphicsQualityLevel.Low;
            enabled = _active && _frames != null && _frames.Length > 0;
            if (!enabled && _sr != null)
            {
                // WHY: Mid/High 로 돌아갈 때 마지막 프레임이 남지 않도록 첫 프레임으로 리셋.
                if (_frames != null && _frames.Length > 0) _sr.sprite = _frames[0];
            }
        }

        private void Update()
        {
            if (!_active || _frames == null || _frames.Length == 0) return;
            _accum += Time.deltaTime * _frameRate;
            if (_accum < 1f) return;
            _accum = 0f;
            _index = (_index + 1) % _frames.Length;
            if (_sr != null) _sr.sprite = _frames[_index];
        }
    }
}
