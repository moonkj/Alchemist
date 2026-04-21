using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Alchemist.Domain.Meta;
using Alchemist.Domain.Player;

namespace Alchemist.UI
{
    /// <summary>
    /// 갤러리 씬 간단 리스트 뷰. 명화 제목(로컬라이즈 키) + 진행 %.
    /// WHY: 실제 픽셀 이미지 렌더링은 Phase 4. 현재는 텍스트 프로토타입.
    /// </summary>
    public sealed class GalleryScreen : MonoBehaviour
    {
        [SerializeField] private Transform _listRoot;
        [SerializeField] private TextMeshProUGUI _entryTemplate;
        [SerializeField] private TextMeshProUGUI _overallLabel;

        private readonly List<TextMeshProUGUI> _spawned = new List<TextMeshProUGUI>();

        public void Bind(PlayerProfile profile)
        {
            Clear();
            if (profile == null || profile.Gallery == null) return;

            foreach (var art in profile.Gallery.All)
            {
                if (_entryTemplate == null || _listRoot == null) continue;
                var label = Instantiate(_entryTemplate, _listRoot);
                label.gameObject.SetActive(true);
                // WHY: LocalizerService 가 static 이라 직접 호출. 키 미등록 시 원문 반환.
                string title = LocalizerService.Localize(art.LocalizedTitleKey);
                int pct = Mathf.RoundToInt(art.Progress * 100f);
                label.SetText("Ch.{0} {1} — {2}%", art.Chapter, title, pct);
                _spawned.Add(label);
            }

            if (_overallLabel != null)
            {
                int overall = Mathf.RoundToInt(profile.Gallery.OverallProgress() * 100f);
                _overallLabel.SetText("Total: {0}%", overall);
            }
        }

        private void Clear()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
            }
            _spawned.Clear();
        }
    }
}
