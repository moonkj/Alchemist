using System.Threading;
using UnityEngine;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;
using Alchemist.UI;
using Alchemist.View;

namespace Alchemist.Bootstrap
{
    public sealed class GameRoot : MonoBehaviour
    {
        [Header("Scene refs")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private UIHud _hud;
        [SerializeField] private InputController _input;

        [Header("Config")]
        [SerializeField] private int _randomSeed = 12345;
        [SerializeField] private int _movesLimit = 15;

        private Board _board;
        private Score _score;
        private Scorer _scorer;
        private Prompt _activePrompt;
        private ChainProcessor _chain;
        private IBlockSpawner _spawner;
        private CancellationTokenSource _cts;

        private void Awake()
        {
            _board = new Board();
            _score = new Score();
            _scorer = new Scorer(_score);
            _activePrompt = Prompt.SamplePurple10;
            _spawner = new DeterministicBlockSpawner(_randomSeed);

            if (_boardView != null)
            {
                _boardView.Bind(_board);
                _chain = new ChainProcessor(_board, _boardView, _spawner);
            }

            if (_hud != null)
            {
                _hud.Bind(_score, _movesLimit, _activePrompt);
            }

            _scorer.BeginStage(_movesLimit);
            _cts = new CancellationTokenSource();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public Board Board => _board;
        public Score Score => _score;
        public Scorer Scorer => _scorer;
        public ChainProcessor Chain => _chain;
        public CancellationToken Ct => _cts != null ? _cts.Token : CancellationToken.None;
    }
}
