using UnityEngine;

namespace Alchemist.Services.Haptic
{
    /// <summary>
    /// Phase 4 MVP 구현: iOS UIImpactFeedbackGenerator / Android Handheld.Vibrate() 간소 래퍼.
    /// WHY: Core Haptics / AHAP 풀 스펙 대신 Unity 기본 vibrate 로 시작해 추후 네이티브 플러그인 교체.
    /// </summary>
    public sealed class UnityHapticService : IHapticService
    {
        private readonly HapticProfile _profile;
        private int _sessionCount;

        public HapticProfile Profile => _profile;
        public int SessionTriggerCount => _sessionCount;

        public UnityHapticService(HapticProfile profile)
        {
            _profile = profile;
        }

        public void ResetSession()
        {
            _sessionCount = 0;
        }

        public void Trigger(HapticEvent evt)
        {
            if (_profile == null) return;
            if (_profile.Intensity == HapticIntensity.Off) return;
            // WHY(가드): 세션 당 80 회 상한. 초과분은 무시해 배터리/감각 피로 방지.
            if (_sessionCount >= _profile.PerSessionCap) return;

            float scale = _profile.ResolveScale(evt);
            if (scale <= 0f) return;

            _sessionCount++;
            FireNative(evt, scale);
        }

        private static void FireNative(HapticEvent evt, float scale)
        {
            // WHY: iOS/Android 둘 다 Phase 4 MVP 는 Handheld.Vibrate() 단순 래퍼.
            // Phase 5 에서 iOS UIImpactFeedbackGenerator (light/medium/heavy) 분기 + AHAP 교체 예정.
            _ = scale; // intensity 는 현재 네이티브 API 한계로 단일 pulse.
            _ = evt;
#if UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#elif UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
