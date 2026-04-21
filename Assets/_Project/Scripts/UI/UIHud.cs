using TMPro;
using UnityEngine;
using Alchemist.Domain.Scoring;
using Alchemist.Domain.Prompts;

namespace Alchemist.UI
{
    public sealed class UIHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _scoreLabel;
        [SerializeField] private TextMeshProUGUI _movesLabel;
        [SerializeField] private PromptBanner _promptBanner;

        private Score _score;
        private int _movesRemaining;
        private int _lastDisplayedScore = -1;
        private int _lastDisplayedMoves = -1;

        public void Bind(Score score, int movesLimit, Prompt prompt)
        {
            _score = score;
            _movesRemaining = movesLimit;
            if (_promptBanner != null) _promptBanner.Bind(prompt);
            ForceRefresh();
        }

        public void SetPromptContext(IPromptContext ctx)
        {
            if (_promptBanner != null) _promptBanner.SetContext(ctx);
        }

        public void SetMovesRemaining(int value)
        {
            _movesRemaining = value;
        }

        private void LateUpdate()
        {
            if (_score != null && _score.Total != _lastDisplayedScore)
            {
                _lastDisplayedScore = _score.Total;
                if (_scoreLabel != null) _scoreLabel.SetText("{0}", _lastDisplayedScore);
            }
            if (_movesRemaining != _lastDisplayedMoves)
            {
                _lastDisplayedMoves = _movesRemaining;
                if (_movesLabel != null) _movesLabel.SetText("{0}", _lastDisplayedMoves);
            }
        }

        public void ForceRefresh()
        {
            _lastDisplayedScore = -1;
            _lastDisplayedMoves = -1;
        }
    }
}
