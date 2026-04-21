using System;

namespace Alchemist.Domain.Economy
{
    /// <summary>
    /// WHY: UtcNow 직접 호출 시 에너지 회복/만료 테스트가 flaky. 주입 가능한 시계 추상화.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }

    /// <summary>실제 런타임용 시계. 프로덕션 경로에서 사용.</summary>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
