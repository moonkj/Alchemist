using System.Threading;
using System.Threading.Tasks;

namespace Alchemist.Domain.Player
{
    /// <summary>
    /// 플레이어 프로필 영속화 인터페이스.
    /// WHY: UnitTest 에서 in-memory fake 를 주입 가능하게 추상화. 실제 구현은 SaveService(JSON).
    /// </summary>
    public interface IPlayerProfileStore
    {
        /// <summary>저장된 프로필 로드. 파일 부재 시 null 반환(호출자가 신규 생성).</summary>
        Task<PlayerProfile> LoadAsync(CancellationToken ct = default);

        /// <summary>프로필을 원자적으로 저장. WHY: 크래시 시 부분 기록 방지를 위해 tmp->rename 패턴.</summary>
        Task SaveAsync(PlayerProfile profile, CancellationToken ct = default);
    }
}
