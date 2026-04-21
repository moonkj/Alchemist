using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Alchemist.Domain.Prompts;

namespace Alchemist.UI
{
    public sealed class PromptBanner : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private Image _progressBar;

        private Prompt _prompt;
        private IPromptContext _ctx;

        public void Bind(Prompt prompt)
        {
            _prompt = prompt;
            if (_titleLabel != null && prompt != null)
                _titleLabel.SetText(prompt.LocalizedTitleKey);
        }

        public void SetContext(IPromptContext ctx)
        {
            _ctx = ctx;
        }

        private void LateUpdate()
        {
            if (_prompt == null || _ctx == null || _progressBar == null) return;
            float progress = _prompt.Goal != null ? _prompt.Goal.Progress(_ctx) : 0f;
            if (progress < 0f) progress = 0f;
            else if (progress > 1f) progress = 1f;
            _progressBar.fillAmount = progress;
        }
    }
}
