namespace Alchemist.Services.Audio
{
    /// <summary>
    /// SFX 라이브러리 키. WHY: AudioClip 레퍼런스를 코드에 직접 두지 않고 enum 으로 참조해
    /// AudioLibrary ScriptableObject 에서 중앙 관리. 신규 SFX 추가 시 여기 + Library 만 수정.
    /// </summary>
    public enum SfxId
    {
        /// <summary>플롭 — 1차 혼합 성공.</summary>
        MixPlop = 0,
        /// <summary>슈욱 — 폭발 / 매치 해소.</summary>
        ExplodeWhoosh = 1,
        /// <summary>벨 — 프롬프트 성공 달성.</summary>
        PromptSuccessBell = 2,
        /// <summary>쉿 — 무효 입력.</summary>
        InvalidHiss = 3,
        /// <summary>코드 — 연쇄 2차 이상.</summary>
        ChainChord = 4,
        /// <summary>팡파레 — 스테이지 성공.</summary>
        StageFanfare = 5,
        /// <summary>심장박동 — 턴 부족 경고.</summary>
        TurnsLowHeartbeat = 6,
    }
}
