using System;
using UnityEngine;

namespace Alchemist.UI.Onboarding
{
    /// <summary>
    /// 첫 실행 감지 + 단계별 프롬프트 순회. WHY: MonoBehaviour 는 IO 진입점만 담당하고
    /// 순수 진행 상태 (현재 인덱스 / 완료 플래그) 는 검증 가능한 메서드로 분리 — 테스트 용이.
    /// </summary>
    public sealed class OnboardingController : MonoBehaviour
    {
        private const string PrefKeyCompleted = "onboarding.stage0.completed";

        [SerializeField] private TutorialStage0Data _stage0;

        private int _currentIndex = -1;
        private float _stageElapsed;

        public event Action<TutorialStep> OnStepShown;
        public event Action OnCompleted;

        public bool IsActive => _currentIndex >= 0;
        public int CurrentIndex => _currentIndex;
        public TutorialStage0Data Stage => _stage0;

        /// <summary>이전에 스킵/완료 했으면 true. WHY: 재접속 시 중복 노출 방지.</summary>
        public static bool WasCompleted()
        {
            return PlayerPrefs.GetInt(PrefKeyCompleted, 0) == 1;
        }

        /// <summary>완료 플래그 초기화 (디버그 / 설정 메뉴 "튜토리얼 다시보기").</summary>
        public static void ResetCompletion()
        {
            PlayerPrefs.DeleteKey(PrefKeyCompleted);
        }

        /// <summary>Begin if not completed. 외부에서 호출 — GameRoot 초기화 후.</summary>
        public void TryBegin()
        {
            if (WasCompleted() || _stage0 == null || _stage0.StepCount == 0) return;
            _currentIndex = 0;
            _stageElapsed = 0f;
            ShowCurrent();
        }

        /// <summary>스텝 진행. 외부 입력 (탭) 에서 호출. WHY: input 과 분리해 테스트에서 순수 호출 가능.</summary>
        public void Advance()
        {
            if (!IsActive || _stage0 == null) return;
            var step = _stage0.GetStep(_currentIndex);
            if (step != null && _stageElapsed < step.MinDurationSec) return; // 최소 시간 미충족 시 무시
            _currentIndex++;
            _stageElapsed = 0f;
            if (_currentIndex >= _stage0.StepCount)
            {
                Complete();
                return;
            }
            ShowCurrent();
        }

        /// <summary>명시적 스킵. 플래그 기록.</summary>
        public void Skip()
        {
            Complete();
        }

        private void Update()
        {
            if (!IsActive || _stage0 == null) return;
            _stageElapsed += Time.deltaTime;
            // WHY: 30 초 경과 시 auto-skip — 초보자 이탈률 최소화.
            if (_stageElapsed > _stage0.AutoSkipSec)
            {
                Skip();
            }
        }

        private void ShowCurrent()
        {
            if (_stage0 == null) return;
            var step = _stage0.GetStep(_currentIndex);
            if (step != null) OnStepShown?.Invoke(step);
        }

        private void Complete()
        {
            _currentIndex = -1;
            PlayerPrefs.SetInt(PrefKeyCompleted, 1);
            OnCompleted?.Invoke();
        }
    }
}
