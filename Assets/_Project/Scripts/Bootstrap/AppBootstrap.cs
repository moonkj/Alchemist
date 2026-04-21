using UnityEngine;
using UnityEngine.Audio;
using Alchemist.Services.Audio;
using Alchemist.Services.Haptic;
using Alchemist.Services.Theme;
using Alchemist.View.Effects;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// 앱 최초 실행 시 ThemeService / AudioService / HapticService / QualityManager 를 싱글톤으로 등록.
    /// WHY: 씬 전환 (로비 → 게임) 간에도 유지되어야 하는 cross-cutting 서비스를 DontDestroyOnLoad 로 고정.
    /// GameRoot 는 이 싱글톤을 조회해 바인딩만 수행 — 순환 의존 회피.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioLibrary _audioLibrary;
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioMixer _audioMixer;

        [Header("Haptic")]
        [SerializeField] private HapticProfile _hapticProfile;

        [Header("Quality")]
        [SerializeField] private GraphicsQualityLevel _initialQuality = GraphicsQualityLevel.High;

        public static AppBootstrap Instance { get; private set; }
        public IAudioService Audio { get; private set; }
        public IHapticService Haptic { get; private set; }
        public ThemeService Theme { get; private set; }
        public QualityManager Quality { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // WHY: 중복 인스턴스는 새로 로드된 씬의 오브젝트 — 파괴해 싱글톤 보존.
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Audio = new UnityAudioService(_audioLibrary, _sfxSource, _bgmSource, _audioMixer);
            Haptic = new UnityHapticService(_hapticProfile);
            Theme = new ThemeService();
            Quality = new QualityManager(_initialQuality);
        }

        private void Update()
        {
            // WHY: 싱글톤에서 프레임 누적해 p95 기반 auto-downgrade 평가 주도.
            Quality?.RecordFrame(Time.unscaledDeltaTime);
        }
    }
}
