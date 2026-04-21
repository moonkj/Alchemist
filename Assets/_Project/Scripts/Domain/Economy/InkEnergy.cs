using System;

namespace Alchemist.Domain.Economy
{
    /// <summary>
    /// 잉크 에너지(스테이지 입장 티켓). 최대 5, 5분당 1회복.
    /// WHY: 스테이지 입장 시 1회 소모만 발생. 플레이 중에는 추가 소모 없음.
    /// WHY: DateTime.UtcNow 대신 IClock 을 통해 회복 타이밍을 테스트 가능하게 주입.
    /// </summary>
    public sealed class InkEnergy
    {
        public const int DefaultMax = 5;
        public const int RefillSeconds = 300; // 5분

        private readonly IClock _clock;
        private int _current;
        private int _max;
        private DateTime _lastRefillUtc;

        public int Current => _current;
        public int Max => _max;
        public DateTime LastRefillUtc => _lastRefillUtc;
        public bool IsFull => _current >= _max;

        public InkEnergy(IClock clock, int current = DefaultMax, int max = DefaultMax, DateTime? lastRefillUtc = null)
        {
            if (clock == null) throw new ArgumentNullException(nameof(clock));
            if (max <= 0) throw new ArgumentOutOfRangeException(nameof(max));
            _clock = clock;
            _max = max;
            _current = Clamp(current, 0, max);
            // WHY: 세이브에서 복원할 때 기존 타임스탬프 사용. null 이면 현재 시점 기준으로 카운터 시작.
            _lastRefillUtc = lastRefillUtc ?? clock.UtcNow;
        }

        /// <summary>스테이지 입장 등 소모 가능 여부 확인. 먼저 회복 시도 후 체크.</summary>
        public bool CanConsume(int amount = 1)
        {
            Refill();
            return _current >= amount;
        }

        /// <summary>1 단위 소모. 성공 시 true. WHY: 회복 후 반영하므로 경계값 안정.</summary>
        public bool Consume(int amount = 1)
        {
            if (amount <= 0) return false;
            Refill();
            if (_current < amount) return false;
            _current -= amount;
            // WHY: full → 비는 순간 회복 타이머를 "지금"으로 재설정해야 일관된 5분 간격.
            if (_current == _max - amount)
            {
                _lastRefillUtc = _clock.UtcNow;
            }
            return true;
        }

        /// <summary>광고 시청/상점 구매 등 외부 충전. 상한 초과 저장 허용하지 않음.</summary>
        public void Grant(int amount)
        {
            if (amount <= 0) return;
            Refill();
            _current = Clamp(_current + amount, 0, _max);
        }

        /// <summary>
        /// 경과 시간에 따라 자동 회복. 5분당 1, 다수 경과 시 한꺼번에 누적.
        /// WHY: 현재 ≥ 최대면 타이머를 전진시키지 않음(오프라인 누적 방지).
        /// </summary>
        public void Refill()
        {
            if (_current >= _max)
            {
                _lastRefillUtc = _clock.UtcNow;
                return;
            }
            var now = _clock.UtcNow;
            var elapsed = now - _lastRefillUtc;
            if (elapsed.TotalSeconds < RefillSeconds) return;

            int ticks = (int)(elapsed.TotalSeconds / RefillSeconds);
            int missingToMax = _max - _current;
            int grant = ticks < missingToMax ? ticks : missingToMax;
            _current += grant;
            _lastRefillUtc = _lastRefillUtc.AddSeconds(grant * (double)RefillSeconds);
            // WHY: full 도달 시 타이머를 현재 시점에 고정해 이후 드레인부터 5분이 시작되도록 함.
            if (_current >= _max)
            {
                _lastRefillUtc = now;
            }
        }

        /// <summary>다음 1 회복까지 남은 초. 최대일 때 0.</summary>
        public int SecondsUntilNext()
        {
            if (_current >= _max) return 0;
            var elapsed = (_clock.UtcNow - _lastRefillUtc).TotalSeconds;
            double remaining = RefillSeconds - (elapsed % RefillSeconds);
            if (remaining < 0) remaining = 0;
            return (int)remaining;
        }

        public void SetMax(int newMax)
        {
            if (newMax <= 0) throw new ArgumentOutOfRangeException(nameof(newMax));
            _max = newMax;
            if (_current > _max) _current = _max;
        }

        private static int Clamp(int v, int lo, int hi)
        {
            if (v < lo) return lo;
            if (v > hi) return hi;
            return v;
        }
    }
}
