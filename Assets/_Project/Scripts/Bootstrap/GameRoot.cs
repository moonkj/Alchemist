using System.Threading;
using UnityEngine;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;
using Alchemist.UI;
using Alchemist.View;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// Manual DI composition root for Phase 1 MVP.
    /// Wave3 additions (F5/F7/F8): initial board refill, PromptContext wiring,
    /// moves-remaining HUD propagation, Scorer injection into ChainProcessor.
    /// </summary>
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
        private GameContext _promptCtx;
        private ChainProcessor _chain;
        private DeterministicBlockSpawner _spawner;
        private CancellationTokenSource _cts;

        public Board Board => _board;
        public Score Score => _score;
        public Scorer Scorer => _scorer;
        public ChainProcessor Chain => _chain;
        public CancellationToken Ct => _cts != null ? _cts.Token : CancellationToken.None;

        private void Awake()
        {
            // Warm the color-mix cache once so the first mix during play has no spike.
            ColorMixCache.Lookup(ColorId.Red, ColorId.Red);

            _board = new Board();
            _score = new Score();
            _scorer = new Scorer(_score);
            _activePrompt = Prompt.SamplePurple10;
            _spawner = new DeterministicBlockSpawner(_randomSeed);
            _promptCtx = new GameContext(_score, _movesLimit);

            if (_boardView != null)
            {
                _boardView.Bind(_board);
                _chain = new ChainProcessor(_board, _boardView, _spawner, _scorer);
            }

            if (_hud != null)
            {
                _hud.Bind(_score, _movesLimit, _activePrompt);
                _hud.SetPromptContext(_promptCtx);
            }

            _scorer.BeginStage(_movesLimit);
            _cts = new CancellationTokenSource();

            InitialRefill();
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>F5: Fill all 42 cells on startup so the player sees a populated board.</summary>
        private void InitialRefill()
        {
            for (int r = 0; r < Board.Rows; r++)
            {
                for (int c = 0; c < Board.Cols; c++)
                {
                    Block b = _spawner.SpawnRandom(r, c);
                    _board.SetBlock(r, c, b);
                    _scorer.OnColorCreated(b.Color, 1);
                }
            }
            if (_boardView != null) _boardView.RebuildAllCells();
        }

        /// <summary>Called after a player move commits; updates scorer + HUD counters.</summary>
        public void NotifyMoveCommitted()
        {
            _scorer.OnMoveCommitted();
            int remaining = _movesLimit - _score.MovesUsed;
            if (remaining < 0) remaining = 0;
            _promptCtx.SetMovesUsed(_score.MovesUsed);
            if (_hud != null) _hud.SetMovesRemaining(remaining);
        }
    }
}
