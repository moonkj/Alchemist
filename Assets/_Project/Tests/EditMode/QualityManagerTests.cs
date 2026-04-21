using NUnit.Framework;
using Alchemist.View.Effects;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// WHY: p95 기반 auto-downgrade 분기가 프레임 샘플 누적/쿨다운 조건을 올바로 지키는지 보장.
    /// Time API 는 주입된 timeSecProvider 로 조작해 결정적 테스트.
    /// </summary>
    [TestFixture]
    public sealed class QualityManagerTests
    {
        [Test]
        public void InitialLevel_DefaultsToHigh()
        {
            var qm = new QualityManager();
            Assert.That(qm.Current, Is.EqualTo(GraphicsQualityLevel.High));
        }

        [Test]
        public void SetLevel_RaisesEventOnce_AndIgnoresDuplicate()
        {
            int raised = 0;
            var qm = new QualityManager(GraphicsQualityLevel.High);
            qm.OnLevelChanged += _ => raised++;
            qm.SetLevel(GraphicsQualityLevel.Mid);
            qm.SetLevel(GraphicsQualityLevel.Mid); // 중복 — 이벤트 생략
            Assert.That(raised, Is.EqualTo(1));
            Assert.That(qm.Current, Is.EqualTo(GraphicsQualityLevel.Mid));
        }

        [Test]
        public void RecordFrame_BelowThreshold_DoesNotDowngrade()
        {
            float now = 100f;
            var qm = new QualityManager(GraphicsQualityLevel.High, () => now);
            // WHY: 전 샘플을 10ms 로 채움 — p95 = 10ms < 25ms 이므로 유지되어야 함.
            for (int i = 0; i < 200; i++)
            {
                now += 0.010f;
                qm.RecordFrame(0.010f);
            }
            Assert.That(qm.Current, Is.EqualTo(GraphicsQualityLevel.High));
        }

        [Test]
        public void RecordFrame_AboveThreshold_DowngradesHighToMid()
        {
            float now = 1000f;
            var qm = new QualityManager(GraphicsQualityLevel.High, () => now);
            // WHY: 40ms 프레임 200 개 → p95 = 40ms > 25ms. 쿨다운을 넘기기 위해 초기 시간 여유.
            for (int i = 0; i < 200; i++)
            {
                now += 0.040f;
                qm.RecordFrame(0.040f);
            }
            Assert.That(qm.Current, Is.EqualTo(GraphicsQualityLevel.Mid));
        }

        [Test]
        public void RecordFrame_DoesNotDowngradeBelowLow()
        {
            float now = 1000f;
            var qm = new QualityManager(GraphicsQualityLevel.Low, () => now);
            for (int i = 0; i < 200; i++)
            {
                now += 0.050f;
                qm.RecordFrame(0.050f);
            }
            Assert.That(qm.Current, Is.EqualTo(GraphicsQualityLevel.Low));
        }
    }
}
