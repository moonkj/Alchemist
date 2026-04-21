using UnityEngine;

namespace Alchemist.View.Effects
{
    /// <summary>
    /// URP Renderer Feature 를 직접 건드리지 않고 Material property block 으로
    /// Metaball2D.shader 에 파라미터 (색/반지름/위치) 주입하는 래퍼.
    /// WHY: ShaderGraph 없이 HLSL .shader 만으로 2D metaball blend 구현. Material instancing
    /// 피하고 MaterialPropertyBlock 사용해 DrawCall 증가 방지.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MetaballRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _radius = 0.5f;
        [SerializeField] private float _threshold = 0.6f;
        [SerializeField] private Color _tint = Color.white;

        private static readonly int RadiusId = Shader.PropertyToID("_MetaRadius");
        private static readonly int ThresholdId = Shader.PropertyToID("_MetaThreshold");
        private static readonly int TintId = Shader.PropertyToID("_MetaTint");

        private MaterialPropertyBlock _mpb;
        private bool _active;

        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            _mpb = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 품질 레벨별 on/off. WHY: Low 에서는 MaterialPropertyBlock 세팅 자체를 건너뛰어
        /// 드로콜 / PerRenderer upload 비용 제거.
        /// </summary>
        public void ApplyQuality(GraphicsQualityLevel level)
        {
            _active = level >= GraphicsQualityLevel.Mid;
            if (!_active) return;
            float th = level == GraphicsQualityLevel.High ? _threshold : 0.85f; // Mid = 단순 cutoff
            PushBlock(th);
        }

        public void SetTint(Color c)
        {
            _tint = c;
            if (_active) PushBlock(_threshold);
        }

        public void SetRadius(float r)
        {
            _radius = r;
            if (_active) PushBlock(_threshold);
        }

        private void PushBlock(float threshold)
        {
            if (_spriteRenderer == null) return;
            _spriteRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat(RadiusId, _radius);
            _mpb.SetFloat(ThresholdId, threshold);
            _mpb.SetColor(TintId, _tint);
            _spriteRenderer.SetPropertyBlock(_mpb);
        }
    }
}
