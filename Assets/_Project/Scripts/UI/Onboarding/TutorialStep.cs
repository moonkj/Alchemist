using UnityEngine;

namespace Alchemist.UI.Onboarding
{
    /// <summary>
    /// 튜토리얼 한 단계. WHY: ScriptableObject 로 외부화해 기획자가 Inspector 에서 문구/하이라이트만 편집.
    /// </summary>
    [CreateAssetMenu(menuName = "Alchemist/Tutorial Step", fileName = "TutorialStep")]
    public sealed class TutorialStep : ScriptableObject
    {
        [SerializeField] private string _stepId = "";
        [SerializeField, TextArea] private string _copy = "";
        [SerializeField] private string _highlightTarget = ""; // WHY: Scene 내 Transform.name 또는 tag
        [SerializeField] private float _minDurationSec = 0.5f;  // 최소 표시시간
        [SerializeField] private bool _advanceOnTap = true;

        public string StepId => _stepId;
        public string Copy => _copy;
        public string HighlightTarget => _highlightTarget;
        public float MinDurationSec => _minDurationSec;
        public bool AdvanceOnTap => _advanceOnTap;
    }
}
