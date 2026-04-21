namespace Alchemist.Services.Haptic
{
    /// <summary>
    /// UX 설계 §4.2 규격 7종 햅틱 이벤트. WHY: View/Domain 층에서 enum 으로만 참조해
    /// 플랫폼별 매핑 (iOS Core Haptics / Android Vibrator) 은 UnityHapticService 가 중앙 처리.
    /// </summary>
    public enum HapticEvent
    {
        /// <summary>블록 픽업 — soft, impact light.</summary>
        BlockPickup = 0,
        /// <summary>인접 셀 hover — selection feedback.</summary>
        Hover = 1,
        /// <summary>1차 혼합 성공 — impact medium.</summary>
        Mix1 = 2,
        /// <summary>연쇄 2차 이상 — impact heavy with rise.</summary>
        Chain2 = 3,
        /// <summary>스테이지 성공 — notification success.</summary>
        StageSuccess = 4,
        /// <summary>무효 입력 — notification warning (짧은 rigid).</summary>
        Invalid = 5,
        /// <summary>턴 부족 경고 — heartbeat (1초 간격 2회).</summary>
        TurnsLow = 6,
    }
}
