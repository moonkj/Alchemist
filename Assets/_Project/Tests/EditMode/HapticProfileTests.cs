using NUnit.Framework;
using UnityEngine;
using Alchemist.Services.Haptic;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// WHY: 세션 캡 / 강도 스케일 / Off 단락 분기가 규격대로 동작함을 검증.
    /// ScriptableObject 는 ScriptableObject.CreateInstance 로 EditMode 에서 인스턴스화.
    /// </summary>
    [TestFixture]
    public sealed class HapticProfileTests
    {
        [Test]
        public void ResolveScale_OffIntensity_ReturnsZero()
        {
            var p = ScriptableObject.CreateInstance<HapticProfile>();
            p.SetIntensity(HapticIntensity.Off);
            for (int i = 0; i < 7; i++)
            {
                Assert.That(p.ResolveScale((HapticEvent)i), Is.EqualTo(0f));
            }
        }

        [Test]
        public void ResolveScale_RichClampsAt1()
        {
            var p = ScriptableObject.CreateInstance<HapticProfile>();
            p.SetIntensity(HapticIntensity.Rich);
            // StageSuccess = 1.0 * 1.4 → clamp to 1.0
            Assert.That(p.ResolveScale(HapticEvent.StageSuccess), Is.EqualTo(1f));
        }

        [Test]
        public void UnityHapticService_SessionCap_GuardsTriggers()
        {
            var p = ScriptableObject.CreateInstance<HapticProfile>();
            p.SetIntensity(HapticIntensity.Basic);
            var svc = new UnityHapticService(p);
            // WHY: 기본 cap=80. 200 회 호출해도 세션 카운트는 80 에서 멈춰야 함.
            for (int i = 0; i < 200; i++) svc.Trigger(HapticEvent.Hover);
            Assert.That(svc.SessionTriggerCount, Is.EqualTo(80));
            svc.ResetSession();
            Assert.That(svc.SessionTriggerCount, Is.EqualTo(0));
        }

        [Test]
        public void UnityHapticService_OffProfile_DoesNotIncrement()
        {
            var p = ScriptableObject.CreateInstance<HapticProfile>();
            p.SetIntensity(HapticIntensity.Off);
            var svc = new UnityHapticService(p);
            svc.Trigger(HapticEvent.Mix1);
            Assert.That(svc.SessionTriggerCount, Is.EqualTo(0));
        }

        [Test]
        public void HapticEventEnum_HasExactly7Members()
        {
            // WHY: UX §4.2 규격 7종 고정. enum 드리프트 방지.
            Assert.That(System.Enum.GetValues(typeof(HapticEvent)).Length, Is.EqualTo(7));
        }
    }
}
