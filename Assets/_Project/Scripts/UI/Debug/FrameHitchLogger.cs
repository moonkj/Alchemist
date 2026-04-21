using UnityEngine;

namespace Alchemist.UI.DebugOverlay
{
    /// <summary>
    /// 프레임이 33ms 초과 시 최근 1초 간의 프레임 기록을 Debug.Log 로 덤프.
    /// WHY: Editor 프로파일러 없이도 기기 build 에서 hitch 원인을 대략 잡기 위함.
    /// </summary>
    public sealed class FrameHitchLogger : MonoBehaviour
    {
        [SerializeField] private float _hitchThresholdMs = 33f;
        [SerializeField] private float _windowSec = 1f;
        [SerializeField] private bool _enabledOnStart = true;

        private const int Capacity = 128;
        private readonly float[] _ring = new float[Capacity];
        private readonly float[] _timeRing = new float[Capacity];
        private int _head;
        private int _count;
        private float _lastHitchAt = -999f;

        private void OnEnable()
        {
            enabled = _enabledOnStart;
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float ms = dt * 1000f;
            float now = Time.realtimeSinceStartup;
            _ring[_head] = ms;
            _timeRing[_head] = now;
            _head = (_head + 1) % Capacity;
            if (_count < Capacity) _count++;

            // WHY: hitch 감지 후 3초 간은 재로깅 억제 — 로그 스팸 방지.
            if (ms > _hitchThresholdMs && now - _lastHitchAt > 3f)
            {
                _lastHitchAt = now;
                DumpRecent(now);
            }
        }

        private void DumpRecent(float now)
        {
            var sb = new System.Text.StringBuilder(512);
            sb.Append("[FrameHitch] recent frames (ms): ");
            for (int i = 0; i < _count; i++)
            {
                int idx = (_head - 1 - i + Capacity) % Capacity;
                if (now - _timeRing[idx] > _windowSec) break;
                sb.Append(_ring[idx].ToString("0.0")).Append(' ');
            }
            Debug.LogWarning(sb.ToString());
        }
    }
}
