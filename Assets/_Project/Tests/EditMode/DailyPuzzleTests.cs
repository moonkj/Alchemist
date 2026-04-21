using NUnit.Framework;
using Alchemist.Domain.Prompts;

namespace Alchemist.Tests.EditMode
{
    /// <summary>
    /// DailyPuzzle: date seed 결정론 & 풀 커버리지 검증.
    /// </summary>
    [TestFixture]
    public sealed class DailyPuzzleTests
    {
        [Test]
        public void ForDate_SameDate_ReturnsSamePrompt()
        {
            var d = DailyPuzzle.DefaultPool();
            var a = d.ForDate(2026, 4, 21);
            var b = d.ForDate(2026, 4, 21);
            Assert.That(a, Is.SameAs(b));
        }

        [Test]
        public void ForDate_DifferentDates_MaySelectDifferentPrompts()
        {
            var d = DailyPuzzle.DefaultPool();
            // WHY: seed mix 로 인접 날짜가 다른 프롬프트를 낼 개연성 검증(1 년 중 ≥2 고유 기대).
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int day = 1; day <= 28; day++)
            {
                var p = d.ForDate(2026, 1, day);
                seen.Add(p.Id);
            }
            Assert.That(seen.Count, Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void ForDate_AllDates_ReturnPromptFromPool()
        {
            var d = DailyPuzzle.DefaultPool();
            for (int day = 1; day <= 28; day++)
            {
                var p = d.ForDate(2026, 4, day);
                Assert.That(p, Is.Not.Null);
                Assert.That(p.Id, Is.Not.Empty);
            }
        }

        [Test]
        public void DefaultPool_ContainsAdvancedAndDailySamples()
        {
            var d = DailyPuzzle.DefaultPool();
            // WHY: 샘플 Advanced1 / Daily1 가 풀에 포함되어 1년 이내에 적어도 한 번은 선택됨.
            bool sawAdv = false, sawDaily = false;
            for (int m = 1; m <= 12; m++)
            {
                for (int day = 1; day <= 28; day++)
                {
                    var p = d.ForDate(2026, m, day);
                    if (p.Id == Prompt.SampleAdvanced1.Id) sawAdv = true;
                    if (p.Id == Prompt.SampleDaily1.Id) sawDaily = true;
                }
            }
            Assert.That(sawAdv, Is.True);
            Assert.That(sawDaily, Is.True);
        }
    }
}
