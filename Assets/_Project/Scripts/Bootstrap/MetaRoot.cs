using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Alchemist.Domain.Economy;
using Alchemist.Domain.Player;
using Alchemist.UI;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// 로비 씬 루트. PlayerProfile 을 디스크에서 로드/저장하고 InkEnergy HUD, GalleryScreen 을 바인딩.
    /// WHY: 게임 씬(GameRoot)과 분리해 퍼시스턴스 책임을 로비에서 단일화. 게임 진입 시 GameRoot.PendingProfile 로 전달.
    /// </summary>
    public sealed class MetaRoot : MonoBehaviour
    {
        [Header("UI refs")]
        [SerializeField] private InkEnergyDisplay _inkDisplay;
        [SerializeField] private GalleryScreen _gallery;

        private IClock _clock;
        private IPathProvider _paths;
        private SaveService _save;
        private PlayerProfile _profile;
        private CancellationTokenSource _cts;

        public PlayerProfile Profile => _profile;

        private async void Awake()
        {
            _clock = new SystemClock();
            _paths = new PersistentPathProvider();
            _save = new SaveService(_paths, _clock);
            _cts = new CancellationTokenSource();

            // WHY: 저장된 프로필이 있으면 로드, 없거나 손상이면 기본값으로 생성.
            try
            {
                _profile = await _save.LoadAsync(_cts.Token);
            }
            catch
            {
                _profile = null;
            }
            if (_profile == null)
            {
                _profile = new PlayerProfile(_clock);
                // WHY: 신규 프로필은 즉시 저장해 이후 크래시 시에도 기본 상태 복구 가능.
                await SafeSaveAsync();
            }

            if (_inkDisplay != null) _inkDisplay.Bind(_profile.Ink);
            if (_gallery != null) _gallery.Bind(_profile);

            // 게임 씬 전환 시 GameRoot 가 소비할 프로필을 정적 슬롯에 적재.
            GameRoot.PendingProfile = _profile;
        }

        private void OnApplicationPause(bool paused)
        {
            // WHY: 모바일 백그라운드 진입 시 프로필 저장(강제 종료 대비).
            if (paused) _ = SafeSaveAsync();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public async Task SafeSaveAsync()
        {
            if (_save == null || _profile == null) return;
            try
            {
                await _save.SaveAsync(_profile, _cts?.Token ?? CancellationToken.None);
            }
            catch (IOException ex)
            {
                Debug.LogWarning("[MetaRoot] save failed: " + ex.Message);
            }
        }
    }
}
