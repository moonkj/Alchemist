namespace Alchemist.Services.Haptic
{
    /// <summary>
    /// 플랫폼 독립 햅틱 API. WHY: View/Domain 이 Unity API 직접 호출하지 않고 서비스 통해
    /// iOS / Android / Editor(Null) 로 분기 가능하게 함.
    /// </summary>
    public interface IHapticService
    {
        /// <summary>현재 강도 프로파일. null 허용 시 Off 취급.</summary>
        HapticProfile Profile { get; }

        /// <summary>이벤트 트리거. 세션 캡 초과 / Off 인 경우 조용히 무시.</summary>
        void Trigger(HapticEvent evt);

        /// <summary>세션 카운트 리셋. WHY: 새 스테이지 시작 시 80 회 카운터 초기화.</summary>
        void ResetSession();

        /// <summary>현재 세션 트리거 수. 테스트/디버그 용도.</summary>
        int SessionTriggerCount { get; }
    }
}
