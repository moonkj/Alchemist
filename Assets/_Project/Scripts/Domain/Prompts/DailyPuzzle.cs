using System;

namespace Alchemist.Domain.Prompts
{
    /// <summary>
    /// 일일 퍼즐 선택기. (yyyyMMdd) 기반 seed → 5 종 프롬프트 중 1개 결정적 선택.
    /// WHY: 서버 없이도 전 플레이어가 같은 날 동일 퍼즐을 풀도록 결정론 확보.
    /// 시드는 UTC 날짜 기준(타임존 편향 배제).
    /// </summary>
    public sealed class DailyPuzzle
    {
        private readonly Prompt[] _pool;

        public DailyPuzzle(Prompt[] pool)
        {
            if (pool == null || pool.Length == 0) throw new ArgumentException("pool empty", nameof(pool));
            _pool = pool;
        }

        /// <summary>주어진 (year, month, day) 에 해당하는 프롬프트 반환(결정론).</summary>
        public Prompt ForDate(int year, int month, int day)
        {
            int seed = year * 10000 + month * 100 + day;
            // WHY: % length 만 사용하지 않고 간단한 해시로 분산도 개선(연속 날짜 편향 완화).
            uint mixed = Mix((uint)seed);
            int idx = (int)(mixed % (uint)_pool.Length);
            return _pool[idx];
        }

        /// <summary>UTC 기준 오늘 날짜로 선택.</summary>
        public Prompt ForDate(DateTime utcDate)
        {
            return ForDate(utcDate.Year, utcDate.Month, utcDate.Day);
        }

        private static uint Mix(uint x)
        {
            // WHY: 간단한 xorshift 계열 — seed 에 의존한 분산만 필요, 암호학적 안정성 불필요.
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            return x;
        }

        /// <summary>Phase 2 기본 5 종 풀(Prompt 샘플 재사용 + Advanced/Daily1).</summary>
        public static DailyPuzzle DefaultPool()
        {
            return new DailyPuzzle(new[]
            {
                Prompt.SamplePurple10,
                Prompt.SampleChain3,
                Prompt.SampleMix,
                Prompt.SampleAdvanced1,
                Prompt.SampleDaily1,
            });
        }
    }
}
