using System.Collections;
using UnityEngine;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Colors;

namespace Alchemist.View
{
    /// <summary>
    /// Visual representation of a single <see cref="Block"/>. MonoBehaviour kept thin:
    /// color swap, scale tween (EaseOutBack approximation), explode/infect triggers.
    /// No per-frame Update — animations are on-demand Coroutines.
    /// Phase 1 uses Coroutine + Time.deltaTime lerp (DOTween/UniTask optional).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BlockView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _explosionDuration = 0.18f;
        [SerializeField] private float _infectionDuration = 0.14f;
        [SerializeField] private float _spawnDuration     = 0.12f;

        private Block _model;
        private Coroutine _activeTween;
        private Vector3 _baseScale = Vector3.one;

        public Block Model => _model;

        private void Reset()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
        }

        /// <summary>Bind to a domain block. Does NOT copy row/col — caller places transform.</summary>
        public void Bind(Block block)
        {
            _model = block;
            if (block != null) SetColor(block.Color);
        }

        public void Unbind()
        {
            _model = null;
            if (_activeTween != null) { StopCoroutine(_activeTween); _activeTween = null; }
        }

        /// <summary>Color swap via pre-mapped Color32 table. GC-free.</summary>
        public void SetColor(ColorId color)
        {
            if (_spriteRenderer == null) return;
            if (AppColors.TryGet(color, out var c32))
                _spriteRenderer.color = c32;
            else
                _spriteRenderer.color = AppColors.Empty;
        }

        public void SetWorldPosition(Vector2 world)
        {
            var t = transform;
            var p = t.localPosition;
            p.x = world.x; p.y = world.y;
            t.localPosition = p;
        }

        /// <summary>Trigger explosion pop-out. Coroutine owner, caller can await via callback.</summary>
        public void PlayExplosion(System.Action onComplete)
        {
            if (_activeTween != null) StopCoroutine(_activeTween);
            _activeTween = StartCoroutine(ExplosionRoutine(onComplete));
        }

        public void PlayInfection(System.Action onComplete)
        {
            if (_activeTween != null) StopCoroutine(_activeTween);
            _activeTween = StartCoroutine(InfectionRoutine(onComplete));
        }

        public void PlaySpawn(System.Action onComplete)
        {
            if (_activeTween != null) StopCoroutine(_activeTween);
            _activeTween = StartCoroutine(SpawnRoutine(onComplete));
        }

        // ---------------- Coroutine bodies (GC-free lambdas avoided; use plain Action) ----------------

        private IEnumerator ExplosionRoutine(System.Action onComplete)
        {
            // EaseOutBack-ish: quick overshoot to 1.25x then collapse to 0.
            float t = 0f;
            float dur = _explosionDuration;
            Vector3 start = _baseScale;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float s = EaseOutBack(k);           // 0..1 with overshoot
                float scale = Mathf.Lerp(1f, 0f, s); // invert: full -> collapse
                transform.localScale = start * scale;
                yield return null;
            }
            transform.localScale = Vector3.zero;
            _activeTween = null;
            onComplete?.Invoke();
        }

        private IEnumerator InfectionRoutine(System.Action onComplete)
        {
            // Color pulse: flash to target color over half duration, hold for remainder.
            float t = 0f;
            float dur = _infectionDuration;
            Vector3 start = _baseScale;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float pulse = 1f + 0.15f * Mathf.Sin(k * Mathf.PI);
                transform.localScale = start * pulse;
                yield return null;
            }
            transform.localScale = start;
            _activeTween = null;
            onComplete?.Invoke();
        }

        private IEnumerator SpawnRoutine(System.Action onComplete)
        {
            float t = 0f;
            float dur = _spawnDuration;
            Vector3 target = _baseScale;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                float s = EaseOutBack(k);
                transform.localScale = target * s;
                yield return null;
            }
            transform.localScale = target;
            _activeTween = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// EaseOutBack with s = 1.70158 (standard constant). Pure math, no GC.
        /// Formula: 1 + c3 * (t-1)^3 + c1 * (t-1)^2, where c1=1.70158, c3=c1+1.
        /// </summary>
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}
