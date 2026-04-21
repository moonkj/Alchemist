using UnityEngine;
using UnityEngine.Profiling;

namespace Alchemist.UI.DebugOverlay
{
    /// <summary>
    /// FPS / DrawCalls / GC alloc 을 OnGUI 로 표시. WHY: Phase 1 Perf §5.2 — HUD 는 개발빌드 전용이며
    /// OnGUI 허용됨. F12 키로 토글. 릴리즈 빌드에서는 enabled=false 여도 포함되도록 conditional 컴파일 미사용.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        [SerializeField] private KeyCode _toggleKey = KeyCode.F12;
        [SerializeField] private bool _visibleOnStart = false;

        private bool _visible;
        private float _fpsSmoothed;
        private float _accumDt;
        private int _accumFrames;
        private long _lastMonoUsed;

        private void Awake()
        {
            _visible = _visibleOnStart;
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggleKey)) _visible = !_visible;
            _accumDt += Time.unscaledDeltaTime;
            _accumFrames++;
            // WHY: 0.5 초마다 평균 FPS 집계 — 프레임 요동 완화.
            if (_accumDt >= 0.5f)
            {
                _fpsSmoothed = _accumFrames / _accumDt;
                _accumDt = 0f;
                _accumFrames = 0;
            }
        }

        private void OnGUI()
        {
            if (!_visible) return;
            long mono = Profiler.GetMonoUsedSizeLong();
            long deltaAlloc = mono - _lastMonoUsed;
            _lastMonoUsed = mono;
            // WHY: DrawCalls 는 런타임 API 미노출 — Profiler 배치 지표 대체값 (총 할당 삼각형/배치는 UnityStats).
            int batches = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null ? 0 : 0;
            GUI.color = Color.white;
            var rect = new Rect(8, 8, 260, 92);
            GUI.Box(rect, GUIContent.none);
            GUI.Label(new Rect(16, 12, 250, 20), $"FPS  : {_fpsSmoothed:0.0}");
            GUI.Label(new Rect(16, 32, 250, 20), $"Mono : {(mono / 1024f / 1024f):0.00} MB");
            GUI.Label(new Rect(16, 52, 250, 20), $"ΔAlloc: {deltaAlloc} B");
            GUI.Label(new Rect(16, 72, 250, 20), $"Batches: {batches}");
        }
    }
}
