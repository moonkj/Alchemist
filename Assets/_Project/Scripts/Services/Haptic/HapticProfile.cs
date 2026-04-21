using UnityEngine;

namespace Alchemist.Services.Haptic
{
    /// <summary>
    /// 햅틱 강도 3단계. WHY: 설정 메뉴에서 "풍부/기본/끔" 으로 노출.
    /// </summary>
    public enum HapticIntensity
    {
        Off = 0,
        Basic = 1,
        Rich = 2,
    }

    /// <summary>
    /// HapticEvent → 강도 스케일 매핑. ScriptableObject 로 만들어 편집자가 밸런스 조정.
    /// WHY: 세션 당 트리거 ≤ 80회 상한을 여기서 가드해 서비스 코드와 분리.
    /// </summary>
    [CreateAssetMenu(menuName = "Alchemist/Haptic Profile", fileName = "HapticProfile")]
    public sealed class HapticProfile : ScriptableObject
    {
        [SerializeField] private HapticIntensity _intensity = HapticIntensity.Basic;
        [SerializeField, Range(0, 200)] private int _perSessionCap = 80;

        // WHY: 7 종 이벤트 × 3 강도 — 배열 대신 float[7] (Basic 기준) 만 저장하고 Rich/Off 는 계수로 파생.
        [SerializeField] private float[] _basicScale = new float[7]
        {
            0.35f, // BlockPickup
            0.15f, // Hover
            0.55f, // Mix1
            0.80f, // Chain2
            1.00f, // StageSuccess
            0.25f, // Invalid
            0.60f, // TurnsLow
        };

        public HapticIntensity Intensity => _intensity;
        public int PerSessionCap => _perSessionCap;

        public void SetIntensity(HapticIntensity i) => _intensity = i;

        /// <summary>
        /// 이벤트별 최종 스케일. WHY: Off=0, Basic=1x, Rich=1.4x (clamp 1.0).
        /// 반환값이 0 이면 호출 측에서 트리거 자체 생략 가능.
        /// </summary>
        public float ResolveScale(HapticEvent evt)
        {
            if (_intensity == HapticIntensity.Off) return 0f;
            int idx = (int)evt;
            if (idx < 0 || idx >= _basicScale.Length) return 0f;
            float baseScale = _basicScale[idx];
            float mult = _intensity == HapticIntensity.Rich ? 1.4f : 1.0f;
            float s = baseScale * mult;
            return s > 1f ? 1f : s;
        }
    }
}
