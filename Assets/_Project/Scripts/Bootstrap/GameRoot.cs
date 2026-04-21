using System.Threading;
using UnityEngine;
using Alchemist.Domain.Blocks;
using Alchemist.Domain.Board;
using Alchemist.Domain.Chain;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Palette;
using Alchemist.Domain.Prompts;
using Alchemist.Domain.Scoring;
using Alchemist.Domain.Stages;
using Alchemist.UI;
using Alchemist.View;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// Manual DI composition root for Phase 2.
    /// Phase 2 추가: StageData 로드, Palette 구성, InputController.OnSwap 구독 훅.
    /// Phase 1 의 Wave3 연결(F5/F7/F8) 유지. D22: parMoves/maxMoves 분리.
    /// </summary>
    public sealed class GameRoot : MonoBehaviour
    {
        [Header("Scene refs")]
        [SerializeField] private BoardView _boardView;
        [SerializeField] private UIHud _hud;
        [SerializeField] private InputController _input;
        [SerializeField] private PaletteView _paletteView;

        [Header("Stage")]
        [Tooltip("Resources/Stages/{id}.asset 경로에서 로드. 비우면 CreateDefault().")]
        [SerializeField] private string _stageId = "";

        private Board _board;
        private Score _score;
        private Scorer _scorer;
        private Prompt _activePrompt;
        private GameContext _promptCtx;
        private ChainProcessor _chain;
        private DeterministicBlockSpawner _spawner;
        private Palette _palette;
        private StageData _stage;
        private CancellationTokenSource _cts;

        // 하드캡(D22) — Phase 1 의 _movesLimit 과 동일 역할.
        private int _movesLimit;

        public Board Board => _board;
        public Score Score => _score;
        public Scorer Scorer => _scorer;
        public ChainProcessor Chain => _chain;
        public Palette Palette => _palette;
        public StageData Stage => _stage;
        public CancellationToken Ct => _cts != null ? _cts.Token : CancellationToken.None;

        private void Awake()
        {
            // Warm the color-mix cache once so the first mix during play has no spike.
            ColorMixCache.Lookup(ColorId.Red, ColorId.Red);

            // --- Stage ---
            _stage = string.IsNullOrEmpty(_stageId)
                ? StageLoader.CreateDefault()
                : StageLoader.LoadOrFallback(_stageId, StageLoader.CreateDefault());
            _movesLimit = _stage.MaxMoves;

            // --- Domain ---
            _board = new Board();
            _score = new Score();
            _scorer = new Scorer(_score);
            _activePrompt = _stage.ResolveInitialPrompt();
            _spawner = new DeterministicBlockSpawner(_stage.BoardSeed);
            _promptCtx = new GameContext(_score, _movesLimit);
            _palette = new Palette(_stage.PaletteSlotCount);

            // WHY: 팔레트 Store/Use 이벤트를 프롬프트 조건(UsePaletteSlotCondition) 에 반영.
            _palette.SlotChanged += OnPaletteSlotChanged;

            // --- View wiring ---
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

            if (_paletteView != null)
            {
                _paletteView.Bind(_palette);
            }

            // WHY(M1 리뷰 지적): InputController.OnSwap 을 실제로 구독해야 이동 수가 증가.
            if (_input != null)
            {
                _input.OnSwap += OnInputSwap;
            }

            // D22: BeginStage 는 parMoves(효율 분모)로 호출. hard cap 은 별도 저장.
            _scorer.BeginStage(_stage.ParMoves);
            _cts = new CancellationTokenSource();

            ApplyInitialPlacements();
            InitialRefill();
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.OnSwap -= OnInputSwap;
            }
            if (_palette != null)
            {
                _palette.SlotChanged -= OnPaletteSlotChanged;
            }
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
                    // WHY: 특수 블록이 이미 배치된 셀은 리필 건너뜀.
                    if (_board.BlockAt(r, c) != null) continue;
                    Block b = _spawner.SpawnRandom(r, c);
                    _board.SetBlock(r, c, b);
                    _scorer.OnColorCreated(b.Color, 1);
                }
            }
            if (_boardView != null) _boardView.RebuildAllCells();
        }

        /// <summary>StageData.InitialPlacements 를 보드에 적용(특수 블록 배치).</summary>
        private void ApplyInitialPlacements()
        {
            var placements = _stage.InitialPlacements;
            for (int i = 0; i < placements.Length; i++)
            {
                var p = placements[i];
                if (!Board.InBounds(p.Row, p.Col)) continue;
                var block = new Block
                {
                    Id = -1 - i, // WHY: 스포너 풀과 id 충돌을 막기 위해 음수 id 사용.
                    Color = p.Color,
                    Kind = p.Kind,
                    State = BlockState.Spawned,
                    Row = p.Row,
                    Col = p.Col,
                };
                _board.SetBlock(p.Row, p.Col, block);
                if (p.Color != ColorId.None)
                {
                    _scorer.OnColorCreated(p.Color, 1);
                }
            }
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

        // ------------------------------------------------------------------
        // Input wiring
        // ------------------------------------------------------------------

        /// <summary>
        /// Swap intent 처리. Phase 2 범위: 보드 상 이동 수 증가만 책임(실제 Swap 적용은
        /// 추후 SwapCommand 가 도입될 예정). Coder-A 의 Board 편집 중 충돌 회피.
        /// </summary>
        private void OnInputSwap(SwapEvent e)
        {
            // WHY(M1): InputController.OnSwap 이벤트가 NotifyMoveCommitted 경로에 도달하도록 배선.
            NotifyMoveCommitted();
        }

        /// <summary>Palette 상태 변경 시 프롬프트 카운터에 1 추가(Store/Use 모두 1회).</summary>
        private void OnPaletteSlotChanged(int slotIndex)
        {
            _promptCtx.RecordPaletteSlotUse(1);
        }
    }
}
