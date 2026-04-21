using UnityEngine;

namespace Alchemist.UI.Onboarding
{
    /// <summary>
    /// Stage 0 튜토리얼 데이터 컨테이너. 4 스텝 (탭 → 드래그 → 혼합 → 성공).
    /// WHY: TutorialStep[] 를 직접 GameRoot 에 두지 않고 별도 SO 로 격리해 A/B 테스트 시 교체 쉬움.
    /// </summary>
    [CreateAssetMenu(menuName = "Alchemist/Tutorial Stage0", fileName = "TutorialStage0Data")]
    public sealed class TutorialStage0Data : ScriptableObject
    {
        [SerializeField] private TutorialStep[] _steps;
        [SerializeField, Min(5f)] private float _autoSkipSec = 30f; // WHY: 30 초 무반응 시 자동 스킵

        public TutorialStep[] Steps => _steps;
        public float AutoSkipSec => _autoSkipSec;

        public int StepCount => _steps != null ? _steps.Length : 0;

        public TutorialStep GetStep(int index)
        {
            if (_steps == null || index < 0 || index >= _steps.Length) return null;
            return _steps[index];
        }
    }
}
