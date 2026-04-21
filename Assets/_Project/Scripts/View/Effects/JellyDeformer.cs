using System.Collections;
using UnityEngine;

namespace Alchemist.View.Effects
{
    /// <summary>
    /// Spring 기반 scale/rotation 탄성. WHY: BlockView 선택·폭발 시 metaball shader 와 별도로
    /// Transform 레벨에서 jelly squish 효과를 줘 저사양 기기에서도 연출 유지. Mesh 변형 대신
    /// Transform.localScale 만 조작해 추가 vertex upload 없음.
    /// </summary>
    public sealed class JellyDeformer : MonoBehaviour
    {
        [SerializeField] private float _stiffness = 180f;  // spring k
        [SerializeField] private float _damping = 16f;     // damping c
        [SerializeField] private float _maxImpulse = 0.5f;

        private Vector3 _baseScale = Vector3.one;
        private Vector3 _velocity;
        private bool _running;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        /// <summary>외부 자극 주입 (선택/매치 폭발 등). 코루틴이 없으면 시작.</summary>
        public void Poke(Vector3 impulse)
        {
            impulse.x = Mathf.Clamp(impulse.x, -_maxImpulse, _maxImpulse);
            impulse.y = Mathf.Clamp(impulse.y, -_maxImpulse, _maxImpulse);
            impulse.z = 0f;
            _velocity += impulse;
            if (!_running) StartCoroutine(SpringRoutine());
        }

        private IEnumerator SpringRoutine()
        {
            _running = true;
            // WHY: 속도·변위 절댓값이 epsilon 미만이면 수렴 간주 → 코루틴 종료.
            const float eps = 0.001f;
            while (_running)
            {
                float dt = Time.deltaTime;
                if (dt <= 0f) { yield return null; continue; }

                Vector3 displacement = transform.localScale - _baseScale;
                Vector3 force = -_stiffness * displacement - _damping * _velocity;
                _velocity += force * dt;
                transform.localScale += _velocity * dt;

                if (displacement.sqrMagnitude < eps * eps && _velocity.sqrMagnitude < eps * eps)
                {
                    transform.localScale = _baseScale;
                    _velocity = Vector3.zero;
                    _running = false;
                }
                yield return null;
            }
        }
    }
}
