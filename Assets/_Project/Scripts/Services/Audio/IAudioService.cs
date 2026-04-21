namespace Alchemist.Services.Audio
{
    /// <summary>
    /// 오디오 서비스 계약. WHY: View 가 AudioSource 를 직접 조작하지 않고 서비스로 위임해
    /// 믹서 채널 / 음소거 / 볼륨 저장을 중앙화.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>SFX 원샷 재생. 파일이 없으면 조용히 skip.</summary>
        void PlaySfx(SfxId id);

        /// <summary>BGM 루프 시작. trackId 는 별도 리소스 키(간단화 — 문자열).</summary>
        void PlayBgm(string trackId);

        /// <summary>BGM 중지.</summary>
        void StopBgm();

        /// <summary>3 채널 볼륨 (0..1). WHY: UI/SFX/BGM 개별 조정.</summary>
        void SetVolume(AudioChannel channel, float volume01);

        /// <summary>전체 음소거 토글.</summary>
        void SetMuted(bool muted);

        bool IsMuted { get; }
    }

    /// <summary>AudioMixer 3채널. WHY: UX 상 BGM/SFX/UI 개별 슬라이더 노출.</summary>
    public enum AudioChannel
    {
        Bgm = 0,
        Sfx = 1,
        Ui = 2,
    }
}
