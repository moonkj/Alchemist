using UnityEngine;
using UnityEngine.Audio;

namespace Alchemist.Services.Audio
{
    /// <summary>
    /// AudioMixer 기반 구현. WHY: 실행 중 볼륨 변경을 PlayerPrefs 에 영속화하고 AudioMixer 파라미터로
    /// 반영. SFX 는 one-shot pool 방지 차원에서 단일 AudioSource 재사용 (Phase 4 는 동시 1개).
    /// </summary>
    public sealed class UnityAudioService : IAudioService
    {
        private const string PrefKeyMuted = "audio.muted";
        private const string PrefKeyBgm = "audio.vol.bgm";
        private const string PrefKeySfx = "audio.vol.sfx";
        private const string PrefKeyUi = "audio.vol.ui";

        private readonly AudioLibrary _library;
        private readonly AudioSource _sfxSource;
        private readonly AudioSource _bgmSource;
        private readonly AudioMixer _mixer;   // nullable — 없으면 AudioSource.volume 직접 조정.
        private bool _muted;

        public bool IsMuted => _muted;

        public UnityAudioService(AudioLibrary library, AudioSource sfxSource, AudioSource bgmSource, AudioMixer mixer = null)
        {
            _library = library;
            _sfxSource = sfxSource;
            _bgmSource = bgmSource;
            _mixer = mixer;
            _muted = PlayerPrefs.GetInt(PrefKeyMuted, 0) == 1;
            ApplyMuteState();
            ApplyVolumeFromPrefs(AudioChannel.Bgm, PrefKeyBgm);
            ApplyVolumeFromPrefs(AudioChannel.Sfx, PrefKeySfx);
            ApplyVolumeFromPrefs(AudioChannel.Ui, PrefKeyUi);
        }

        public void PlaySfx(SfxId id)
        {
            if (_muted || _sfxSource == null || _library == null) return;
            var clip = _library.Resolve(id);
            if (clip == null) return; // WHY: 파일 없으면 조용히 skip — 빌드 에러 아님.
            _sfxSource.PlayOneShot(clip);
        }

        public void PlayBgm(string trackId)
        {
            if (_bgmSource == null) return;
            // WHY: trackId 로 Resources.Load 로 AudioClip 조회. 실패 시 재생 생략.
            if (string.IsNullOrEmpty(trackId)) return;
            var clip = Resources.Load<AudioClip>("Audio/Bgm/" + trackId);
            if (clip == null) return;
            _bgmSource.clip = clip;
            _bgmSource.loop = true;
            if (!_muted) _bgmSource.Play();
        }

        public void StopBgm()
        {
            if (_bgmSource != null) _bgmSource.Stop();
        }

        public void SetVolume(AudioChannel channel, float v)
        {
            v = Mathf.Clamp01(v);
            string prefKey = ChannelKey(channel);
            PlayerPrefs.SetFloat(prefKey, v);
            ApplyVolume(channel, v);
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
            PlayerPrefs.SetInt(PrefKeyMuted, muted ? 1 : 0);
            ApplyMuteState();
        }

        // ---------------- internals ----------------

        private void ApplyMuteState()
        {
            if (_sfxSource != null) _sfxSource.mute = _muted;
            if (_bgmSource != null) _bgmSource.mute = _muted;
        }

        private void ApplyVolumeFromPrefs(AudioChannel c, string key)
        {
            float v = PlayerPrefs.GetFloat(key, 0.8f);
            ApplyVolume(c, v);
        }

        private void ApplyVolume(AudioChannel c, float v)
        {
            if (_mixer != null)
            {
                // WHY: AudioMixer 는 log 스케일 (dB). 0..1 을 -80..0 dB 로 매핑.
                float db = v <= 0.0001f ? -80f : Mathf.Log10(v) * 20f;
                _mixer.SetFloat(MixerParam(c), db);
                return;
            }
            // WHY(fallback): 믹서 주입 안 된 경우 AudioSource 에 직접 적용 (Bgm/Sfx 만).
            if (c == AudioChannel.Bgm && _bgmSource != null) _bgmSource.volume = v;
            else if (c == AudioChannel.Sfx && _sfxSource != null) _sfxSource.volume = v;
        }

        private static string ChannelKey(AudioChannel c)
        {
            switch (c)
            {
                case AudioChannel.Bgm: return PrefKeyBgm;
                case AudioChannel.Sfx: return PrefKeySfx;
                default: return PrefKeyUi;
            }
        }

        private static string MixerParam(AudioChannel c)
        {
            switch (c)
            {
                case AudioChannel.Bgm: return "BgmVolume";
                case AudioChannel.Sfx: return "SfxVolume";
                default: return "UiVolume";
            }
        }
    }
}
