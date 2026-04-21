namespace Alchemist.Domain.Badges
{
    /// <summary>
    /// 배지 안정 식별자. 세 묶음(조합/플레이스타일/히든) 총 16종.
    /// WHY enum 고정 값: 로컬/서버 저장 간 일관성. 신규 배지는 끝에 추가(값 재사용 금지).
    /// </summary>
    public enum BadgeId : int
    {
        None = 0,

        // --- 조합 배지 (Combo, 6종) ---
        FirstPurple    = 1,  // 첫 보라 생성
        FirstOrange    = 2,  // 첫 오렌지
        FirstGreen     = 3,  // 첫 그린
        AllSecondaries = 4,  // 2차 색 (Orange+Green+Purple) 모두 1회 이상 생성
        FirstBlack     = 5,  // 첫 블랙 생성(페널티 감수)
        FirstWhite     = 6,  // 첫 화이트 생성(모든 1차 결합)

        // --- 플레이 스타일 (5종) ---
        Chain5         = 7,  // 깊이 5 이상의 연쇄 1회
        MinMoves       = 8,  // ParMoves 이하로 클리어(현재 Scorer 는 ParMoves 를 stage 시작 시 수신)
        PromptPerfect  = 9,  // 프롬프트 Goal All+Any 100 % 진행률 종료
        NoBlack        = 10, // 스테이지 내 블랙 생성 0
        ChainStreak    = 11, // 깊이 2 이상 연쇄 3회 누적

        // --- 숨겨진 (Hidden, 5종) ---
        FilterOnly     = 12, // 필터 통과만으로 프롬프트 달성(ColorsCreated 없음)
        GrayOnly       = 13, // 회색 블록만 남아 있는 상태에서 클리어
        PrismOnly      = 14, // 프리즘 사용만으로 주요 조건 충족(PaletteSlotUses 없음 + FilterTransits 없음)
        PaletteMaster  = 15, // 팔레트 슬롯 5회 이상 사용 + PromptPerfect
        SpeedRun       = 16, // 이동 수 ParMoves/2 이하 클리어
    }
}
