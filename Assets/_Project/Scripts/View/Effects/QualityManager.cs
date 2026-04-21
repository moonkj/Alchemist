using System;
using UnityEngine;

namespace Alchemist.View.Effects
{
    /// <summary>
    /// 런타임 품질 다운그레이드 매니저. WHY(Perf Finale): p95 프레임 > 25ms 이 지속되면
    /// High → Mid → Low 로 1 단계씩 자동 강등해 저사양 기기에서도 30fps 상한 유지.
    /// 순수 C# (MonoBehaviour X) — GameRoot 가 Update 당 Tick() 호출.
    /// </summary>
    public sealed class QualityManager
    {
        private const int SampleWindow = 120;   // 약 2 초 (60fps 기준)
        private const float P95ThresholdMs = 25f;
        private const float CooldownSec = 3f;   // 강등 후 재평가 억제

        private readonly float[] _frameMsRing = new float[SampleWindow];
        private int _ringFill;
        private int _ringIndex;
        private float _lastDowngradeAt;
        private readonly Func<float> _timeSec;

        public GraphicsQualityLevel Current { get; private set; }
        public event Action<GraphicsQualityLevel> OnLevelChanged;

        public QualityManager(GraphicsQualityLevel initial = GraphicsQualityLevel.High, Func<float> timeSecProvider = null)
        {
            Current = initial;
            // WHY: 테스트 시 Time.realtimeSinceStartup 대신 주입 가능하도록.
            _timeSec = timeSecProvider ?? (() => Time.realtimeSinceStartup);
            _lastDowngradeAt = -999f;
        }

        /// <summary>
        /// 수동 설정. WHY: 옵션 메뉴에서 플레이어가 직접 바꾸는 경로.
        /// auto-downgrade 쿨다운도 리셋해 재평가가 다시 일어나게 함.
        /// </summary>
        public void SetLevel(GraphicsQualityLevel level)
        {
            if (Current == level) return;
            Current = level;
            _lastDowngradeAt = _timeSec();
            OnLevelChanged?.Invoke(level);
        }

        /// <summary>매 프레임 호출. deltaSec 을 ms 로 환산해 ring buffer 누적.</summary>
        public void RecordFrame(float deltaSec)
        {
            if (deltaSec <= 0f || float.IsNaN(deltaSec)) return;
            _frameMsRing[_ringIndex] = deltaSec * 1000f;
            _ringIndex = (_ringIndex + 1) % SampleWindow;
            if (_ringFill < SampleWindow) _ringFill++;

            // WHY: ring 이 가득 차고 쿨다운 경과 시에만 강등 평가 — 매 프레임 강등 방지.
            if (_ringFill < SampleWindow) return;
            if (_timeSec() - _lastDowngradeAt < CooldownSec) return;

            float p95 = ComputeP95();
            if (p95 > P95ThresholdMs && Current != GraphicsQualityLevel.Low)
            {
                var next = Current == GraphicsQualityLevel.High ? GraphicsQualityLevel.Mid : GraphicsQualityLevel.Low;
                Current = next;
                _lastDowngradeAt = _timeSec();
                ClearRing();
                OnLevelChanged?.Invoke(next);
            }
        }

        /// <summary>최근 샘플의 p95 ms 반환. 테스트에서 검증 가능하도록 public.</summary>
        public float ComputeP95()
        {
            // WHY: insertion sort — 120 sample 이면 O(n^2)=14400 OP, 1fps 내 여유.
            var copy = new float[_ringFill];
            for (int i = 0; i < _ringFill; i++) copy[i] = _frameMsRing[i];
            Array.Sort(copy);
            int idx = (int)(copy.Length * 0.95f);
            if (idx >= copy.Length) idx = copy.Length - 1;
            return copy[idx];
        }

        private void ClearRing()
        {
            _ringFill = 0;
            _ringIndex = 0;
        }
    }
}
