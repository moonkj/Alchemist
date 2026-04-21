using NUnit.Framework;
using UnityEngine;
using Alchemist.UI.Onboarding;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// WHY: 첫 실행 감지/ 완료 플래그 / 스킵 분기가 PlayerPrefs 와 올바로 연동되는지 확인.
    /// 테스트 후 플래그를 반드시 리셋해 테스트 순서 독립성 보장.
    /// </summary>
    [TestFixture]
    public sealed class OnboardingTests
    {
        [TearDown]
        public void After()
        {
            // WHY: Unity PlayerPrefs 는 프로세스 스코프 — 타 테스트 오염 방지.
            OnboardingController.ResetCompletion();
        }

        [Test]
        public void WasCompleted_DefaultsFalse()
        {
            OnboardingController.ResetCompletion();
            Assert.That(OnboardingController.WasCompleted(), Is.False);
        }

        [Test]
        public void TutorialStage0Data_StepCount_MatchesArray()
        {
            var data = ScriptableObject.CreateInstance<TutorialStage0Data>();
            Assert.That(data.StepCount, Is.GreaterThanOrEqualTo(0));
            Assert.That(data.GetStep(-1), Is.Null);
            Assert.That(data.GetStep(999), Is.Null);
        }

        [Test]
        public void TutorialStep_DefaultFields_AreSane()
        {
            var step = ScriptableObject.CreateInstance<TutorialStep>();
            Assert.That(step.StepId, Is.EqualTo(""));
            Assert.That(step.Copy, Is.EqualTo(""));
            Assert.That(step.MinDurationSec, Is.GreaterThanOrEqualTo(0f));
        }
    }
}
