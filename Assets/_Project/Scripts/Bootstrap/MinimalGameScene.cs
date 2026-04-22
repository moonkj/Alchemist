using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Chain;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// 완전한 1-세션 루프: 로비 → 스테이지 선택 → 플레이 → 결과 → 다음/재도전.
    /// 5개 스테이지, 프로시저얼 사운드, PlayerPrefs 별점 영속화.
    /// TMP/에셋 없이 프로시저얼로만 구성.
    /// </summary>
    public sealed class MinimalGameScene : MonoBehaviour
    {
        private const int Rows = 7;
        private const int Cols = 6;
        private const float CellSize = 0.9f;
        private const float Gap = 0.06f;

        // --------------- Stage catalog ---------------
        private sealed class StageConfig
        {
            public string Id, Title;
            public ColorId GoalColor;
            public int GoalCount, MoveLimit, Seed;
            public StageConfig(string i, string t, ColorId g, int c, int m, int s)
            { Id = i; Title = t; GoalColor = g; GoalCount = c; MoveLimit = m; Seed = s; }
        }
        private static readonly StageConfig[] Stages = new[]
        {
            // 챕터 1 — 잃어버린 노을 (따뜻한 톤)
            new StageConfig("s1",  "1. 노을의 주홍",   ColorId.Orange, 3, 10, 42),
            new StageConfig("s2",  "2. 여름의 초록",   ColorId.Green,  4, 12, 100),
            new StageConfig("s3",  "3. 달빛 보라",     ColorId.Purple, 5, 12, 200),
            new StageConfig("s4",  "4. 깊은 숲",      ColorId.Green,  6, 14, 300),
            new StageConfig("s5",  "5. 무지개 끝",     ColorId.White,  3, 16, 500),
            // 챕터 2 — 바다의 기억 (차가운 톤)
            new StageConfig("s6",  "6. 수평선의 주황", ColorId.Orange, 5, 12, 1001),
            new StageConfig("s7",  "7. 파도의 초록",   ColorId.Green,  5, 12, 1020),
            new StageConfig("s8",  "8. 깊은 바다",    ColorId.Purple, 5, 13, 1040),
            new StageConfig("s9",  "9. 산호초",       ColorId.Orange, 6, 13, 1060),
            new StageConfig("s10", "10. 해무 하양",    ColorId.White,  4, 14, 1080),
            new StageConfig("s11", "11. 폭풍우 보라",  ColorId.Purple, 7, 15, 1100),
            new StageConfig("s12", "12. 대양의 시작",  ColorId.White,  5, 18, 1120),
        };
        private const int Chapter1End = 5; // index 0~4

        // --------------- Screen state ---------------
        private enum ScreenState { Lobby, Playing, Result, Gallery, Tutorial, Settings, Paused }
        private ScreenState _screen = ScreenState.Lobby;
        private int _stageIdx;
        private StageConfig _stage;

        // --------------- Tutorial ---------------
        private int _tutorialPage;
        private const int TutorialPages = 3;
        private static bool HasSeenTutorial => PlayerPrefs.GetInt("alchemist.tut_done", 0) == 1;
        private static void MarkTutorialDone() { PlayerPrefs.SetInt("alchemist.tut_done", 1); PlayerPrefs.Save(); }

        // --------------- Settings (M5) ---------------
        private static bool SettingSfxOn => PlayerPrefs.GetInt("alchemist.cfg.sfx", 1) == 1;
        private static bool SettingHapticOn => PlayerPrefs.GetInt("alchemist.cfg.haptic", 1) == 1;
        private static float SettingVolume => PlayerPrefs.GetInt("alchemist.cfg.vol", 80) / 100f;
        private bool _confirmReset; // 데이터 초기화 확인 단계

        // --------------- Gallery catalog (M4) ---------------
        /// <summary>Chapter → 총 조각 수. 스테이지 클리어 시 별×1 = 조각 1 적립.</summary>
        private sealed class ArtworkConfig
        {
            public string Id, Title, Subtitle;
            public int TotalFragments;
            public int[] FeedingStages;   // 조각 기여 스테이지 index
            public Color TopColor, BottomColor; // 언락 시 드러나는 그라디언트 (노을/바다)
            public ArtworkConfig(string i, string t, string sub, int f, int[] fs, Color a, Color b)
            { Id = i; Title = t; Subtitle = sub; TotalFragments = f; FeedingStages = fs; TopColor = a; BottomColor = b; }
        }
        private static readonly ArtworkConfig[] Artworks = new[]
        {
            // 챕터 1 — 5 스테이지 × 3 별 = 15 조각
            new ArtworkConfig("art_sunset", "잃어버린 노을", "챕터 1 · 모든 색이 사라진 하늘",
                15, new[] { 0, 1, 2, 3, 4 },
                new Color(1f, 0.55f, 0.25f, 1f), new Color(0.55f, 0.22f, 0.52f, 1f)),
            // 챕터 2 — 7 스테이지 × 3 별 = 21 조각
            new ArtworkConfig("art_sea", "바다의 기억", "챕터 2 · 파도에 휩쓸린 색들",
                21, new[] { 5, 6, 7, 8, 9, 10, 11 },
                new Color(0.35f, 0.72f, 0.92f, 1f), new Color(0.12f, 0.20f, 0.42f, 1f)),
        };
        private Texture2D _artworkCanvasTex;

        // --------------- Board state ---------------
        private SpriteRenderer[,] _blocks;
        private ColorId[,] _colorGrid;
        private Vector3[,] _basePos;
        private float[,] _jellyPhase;   // 각 블록의 브리딩 위상(0..2π)
        private bool[,] _bouncing;      // Mix/폭발 애니 중엔 브리딩 건너뜀
        private Sprite _squareSprite;
        private DeterministicBlockSpawner _spawner;

        // --------------- Drag ---------------
        private int _dragR = -1, _dragC = -1;
        private bool _inputLocked;
        // Drag follow 물리 — critically-damped spring + velocity stretch
        private Vector3 _dragVel;
        private Vector3 _lastDragWorld;
        private Vector2 _dragVelEma;

        // --------------- Palette Slots (D4) ---------------
        private const int PaletteCount = 3;
        private SpriteRenderer[] _paletteSprites;
        private ColorId[] _paletteColors;
        private Vector3[] _palettePos;
        private enum DragSource { None, Board, Palette }
        private DragSource _dragSource = DragSource.None;
        private int _dragSlotIdx = -1;

        // --------------- Play state ---------------
        private int _score, _moves, _maxChainDepth;
        private int _goalProgress;
        private bool _stageCleared;
        private int _stars;
        private string _toast = "";
        private float _toastUntil;

        // --------------- Audio ---------------
        private AudioSource _audio;
        private AudioClip _sfxMix, _sfxExplode, _sfxClear, _sfxFail;

        // --------------- UI ---------------
        private Texture2D _panelBg, _barBg, _barFill, _overlayTex, _stageBtnBg;
        private Texture2D _toastSuccessBg, _toastWarnBg, _toastDangerBg, _toastNeutralBg;
        private Texture2D _primaryBtnBg, _ghostBtnBg, _dimOverlay;
        private GUIStyle _display, _heading, _body, _caption, _goalLabel, _overlayTitle, _overlayBody, _primaryBtn, _ghostBtn, _stageBtn, _scoreBig;
        private ColorId _lastBarColor = ColorId.None;

        // Toast 종류
        private enum ToastKind { Success, Warn, Danger, Neutral }
        private ToastKind _toastKind = ToastKind.Neutral;
        // 입력 잠금 인디케이터 용 타이밍
        private float _cascadeStartTime;

        // 결과 화면 시퀀스
        private float _resultEnterT; // 0~1 진행도
        private int _displayedScore;
        private float[] _starLit = new float[3];

        // 화면 전환 페이드
        private float _screenFade = 1f; // 1 = 완전 표시
        private ScreenState _pendingScreen = ScreenState.Lobby;

        // --------------- Lifecycle ---------------
        private void Start()
        {
            Application.targetFrameRate = 60;
            _squareSprite = BuildSquareSprite();
            _blocks = new SpriteRenderer[Rows, Cols];
            _colorGrid = new ColorId[Rows, Cols];
            _basePos = new Vector3[Rows, Cols];
            _jellyPhase = new float[Rows, Cols];
            _bouncing = new bool[Rows, Cols];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    _jellyPhase[r, c] = Random.value * Mathf.PI * 2f;
            BuildGrid(seed: Stages[0].Seed);
            BuildPaletteSlots();
            FitCameraToBoard();
            SetupAudio();
            SetBoardVisible(false); // 로비/튜토리얼에선 숨김
            // 첫 실행 유저는 튜토리얼부터
            if (!HasSeenTutorial) { _screen = ScreenState.Tutorial; _tutorialPage = 0; }
        }

        // --------------- Board construction ---------------
        private void BuildGrid(int seed)
        {
            _spawner = new DeterministicBlockSpawner(seed);
            float totalW = Cols * (CellSize + Gap) - Gap;
            float totalH = Rows * (CellSize + Gap) - Gap;
            float originX = -totalW / 2f + CellSize / 2f;
            float originY = totalH / 2f - CellSize / 2f;

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var block = _spawner.SpawnRandom(r, c);
                    _colorGrid[r, c] = block.Color;
                    var pos = new Vector3(originX + c * (CellSize + Gap), originY - r * (CellSize + Gap), 0f);
                    _basePos[r, c] = pos;
                    if (_blocks[r, c] == null)
                    {
                        var go = new GameObject("Block_" + r + "_" + c);
                        go.transform.parent = transform;
                        var sr = go.AddComponent<SpriteRenderer>();
                        sr.sprite = _squareSprite;
                        _blocks[r, c] = sr;
                    }
                    _blocks[r, c].transform.position = pos;
                    _blocks[r, c].transform.localScale = Vector3.one * CellSize;
                    _blocks[r, c].color = ColorToUnity(block.Color);
                    var col = _blocks[r, c].color; col.a = 1f; _blocks[r, c].color = col;
                }
            }
        }

        private void SetBoardVisible(bool on)
        {
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_blocks[r, c] != null) _blocks[r, c].enabled = on;
            }
            if (_paletteSprites != null)
            {
                for (int i = 0; i < _paletteSprites.Length; i++)
                    if (_paletteSprites[i] != null) _paletteSprites[i].enabled = on;
            }
        }

        /// <summary>팔레트 슬롯 3개를 보드 하단에 월드 스페이스 오브젝트로 배치 (1회).</summary>
        private void BuildPaletteSlots()
        {
            if (_paletteSprites != null) return;
            _paletteSprites = new SpriteRenderer[PaletteCount];
            _paletteColors = new ColorId[PaletteCount];
            _palettePos = new Vector3[PaletteCount];
            float step = CellSize + Gap;
            float totalW = PaletteCount * step - Gap;
            float originX = -totalW / 2f + CellSize / 2f;
            float bottomY = _basePos[Rows - 1, 0].y - step * 1.2f;
            for (int i = 0; i < PaletteCount; i++)
            {
                var go = new GameObject("Slot_" + i);
                go.transform.parent = transform;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _squareSprite;
                sr.color = new Color(0.56f, 0.48f, 0.85f, 0.28f);
                sr.sortingOrder = 2;
                _paletteSprites[i] = sr;
                _paletteColors[i] = ColorId.None;
                _palettePos[i] = new Vector3(originX + i * step, bottomY, 0f);
                go.transform.position = _palettePos[i];
                go.transform.localScale = Vector3.one * (CellSize * 0.85f);
            }
        }

        private void ResetPaletteColors()
        {
            if (_paletteSprites == null) return;
            for (int i = 0; i < PaletteCount; i++)
            {
                _paletteColors[i] = ColorId.None;
                _paletteSprites[i].color = new Color(0.56f, 0.48f, 0.85f, 0.28f);
                _paletteSprites[i].transform.position = _palettePos[i];
                _paletteSprites[i].transform.localScale = Vector3.one * (CellSize * 0.85f);
                _paletteSprites[i].transform.rotation = Quaternion.identity;
            }
        }

        private void FitCameraToBoard()
        {
            var cam = Camera.main;
            if (cam == null) return;
            // 팔레트 슬롯 한 줄 추가 공간 확보 (약 1.2 step 아래로 확장)
            float halfH = (Rows * (CellSize + Gap) - Gap) / 2f + (CellSize + Gap) * 0.9f;
            float halfW = (Cols * (CellSize + Gap) - Gap) / 2f;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float szH = halfH;
            float szW = halfW / Mathf.Max(0.01f, aspect);
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(szH, szW) * 1.1f;
            // 카메라 중심을 살짝 아래로 내려 보드 위쪽 여백 확보 (HUD 공간)
            cam.transform.position = new Vector3(0f, -(CellSize + Gap) * 0.5f, -10f);
        }

        // --------------- Stage management ---------------
        private void StartStage(int idx)
        {
            _stageIdx = Mathf.Clamp(idx, 0, Stages.Length - 1);
            _stage = Stages[_stageIdx];
            _score = _moves = _maxChainDepth = _goalProgress = 0;
            _stageCleared = false; _stars = 0;
            _toast = ""; _toastUntil = 0f;
            _inputLocked = false;
            BuildGrid(_stage.Seed);
            ResetPaletteColors();
            SetBoardVisible(true);
            _screen = ScreenState.Playing;
        }

        private void ExitToLobby()
        {
            SetBoardVisible(false);
            _screen = ScreenState.Lobby;
        }

        private static int GetStoredStars(string stageId) => PlayerPrefs.GetInt("stars_" + stageId, 0);
        private static void SaveStars(string stageId, int stars)
        {
            int prev = GetStoredStars(stageId);
            if (stars > prev) { PlayerPrefs.SetInt("stars_" + stageId, stars); PlayerPrefs.Save(); }
        }
        private bool IsStageUnlocked(int idx)
        {
            if (idx == 0) return true;
            // 챕터 2 시작: 챕터 1 전체 클리어 필요
            if (idx == Chapter1End) return IsChapter2Unlocked();
            // 그 외: 직전 스테이지 클리어
            return GetStoredStars(Stages[idx - 1].Id) > 0;
        }

        // M4: Gallery — 전체 획득 별의 합 = 총 복원 조각 수.
        private int GetTotalUnlockedFragments()
        {
            int sum = 0;
            for (int i = 0; i < Stages.Length; i++) sum += GetStoredStars(Stages[i].Id);
            return sum;
        }

        // --------------- Input ---------------
        private void Update()
        {
            UpdateScaleDecay();

            // HUD 점수 부드러운 count-up — 목표값으로 0.3s 동안 수렴
            if (_displayedScore != _score)
            {
                int diff = _score - _displayedScore;
                int step = Mathf.Max(1, (int)(Mathf.Abs(diff) * Time.deltaTime * 6f));
                if (Mathf.Abs(diff) <= step) _displayedScore = _score;
                else _displayedScore += (diff > 0 ? step : -step);
            }

            if (_screen != ScreenState.Playing || _inputLocked) return;

            bool began = false, moved = false, ended = false;
            Vector2 screenPos = default;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0)) { began = true; screenPos = Input.mousePosition; }
            else if (Input.GetMouseButton(0)) { moved = true; screenPos = Input.mousePosition; }
            else if (Input.GetMouseButtonUp(0)) { ended = true; screenPos = Input.mousePosition; }
#endif
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                screenPos = t.position;
                if (t.phase == TouchPhase.Began) began = true;
                else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary) moved = true;
                else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) ended = true;
            }

            if (!began && !moved && !ended) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            world.z = 0f;

            // 상단 HUD 영역 터치는 블록 입력으로 먹지 않도록 차단
            if (screenPos.y > Screen.height - 180) return;

            if (began) OnDragBegin(world);
            else if (moved) OnDragMove(world);
            else if (ended) OnDragEnd(world);
        }

        private void OnDragBegin(Vector3 world)
        {
            // 보드 먼저 탐색
            if (FindCell(world, out int r, out int c) && _colorGrid[r, c] != ColorId.None)
            {
                _dragR = r; _dragC = c;
                _dragSource = DragSource.Board;
                _blocks[r, c].sortingOrder = 10;
            }
            else if (FindPaletteSlot(world, out int slotIdx) && _paletteColors[slotIdx] != ColorId.None)
            {
                // 팔레트 슬롯에서 꺼내기
                _dragSlotIdx = slotIdx;
                _dragSource = DragSource.Palette;
                _paletteSprites[slotIdx].sortingOrder = 10;
            }
            else return;

            _dragVel = Vector3.zero;
            _lastDragWorld = world;
            _dragVelEma = Vector2.zero;
        }

        private void OnDragMove(Vector3 world)
        {
            Transform tr = GetDragTransform();
            if (tr == null) return;
            float dt = Mathf.Max(0.0001f, Time.deltaTime);

            Vector2 inst = (Vector2)(world - _lastDragWorld) / dt;
            _dragVelEma = _dragVelEma * 0.75f + inst * 0.25f;
            _lastDragWorld = world;

            Vector3 target = new Vector3(world.x, world.y, -0.1f);
            tr.position = Vector3.SmoothDamp(tr.position, target, ref _dragVel, 0.08f, 40f);

            float speedN = Mathf.Clamp01(_dragVelEma.magnitude / 6f);
            float sx = 1.0f + 0.28f * speedN;
            float sy = 1.0f - 0.18f * speedN;
            float lift = 1.12f;
            tr.localScale = new Vector3(CellSize * lift * sx, CellSize * lift * sy, CellSize);

            float rotZ = 0f;
            if (_dragVelEma.sqrMagnitude > 0.25f)
                rotZ = Mathf.Atan2(_dragVelEma.y, _dragVelEma.x) * Mathf.Rad2Deg * 0.18f;
            rotZ = Mathf.Clamp(rotZ, -10f, 10f);
            tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.Euler(0f, 0f, rotZ), dt * 12f);
        }

        private void OnDragEnd(Vector3 world)
        {
            if (_dragSource == DragSource.None) return;

            bool overBoard = FindCell(world, out int tr, out int tc);
            bool overSlot = FindPaletteSlot(world, out int tsIdx);

            if (_dragSource == DragSource.Board)
            {
                int sr = _dragR, sc = _dragC;
                _blocks[sr, sc].sortingOrder = 0;
                _blocks[sr, sc].transform.rotation = Quaternion.identity;
                _dragR = _dragC = -1;

                if (overSlot && _paletteColors[tsIdx] == ColorId.None)
                {
                    // board → 빈 슬롯 = 저장
                    StoreBoardToSlot(sr, sc, tsIdx);
                }
                else if (overBoard && !(tr == sr && tc == sc) && IsAdjacent(sr, sc, tr, tc))
                {
                    SnapBack(sr, sc);
                    TryMix(sr, sc, tr, tc);
                }
                else
                {
                    SnapBack(sr, sc);
                }
            }
            else if (_dragSource == DragSource.Palette)
            {
                int slot = _dragSlotIdx;
                _paletteSprites[slot].sortingOrder = 2;
                _paletteSprites[slot].transform.rotation = Quaternion.identity;
                _dragSlotIdx = -1;

                if (overBoard && _colorGrid[tr, tc] != ColorId.None)
                {
                    // 팔레트 → 보드 셀 = 혼합
                    UseSlotOnBoard(slot, tr, tc);
                }
                else
                {
                    SnapBackSlot(slot);
                }
            }

            _dragSource = DragSource.None;
        }

        private Transform GetDragTransform()
        {
            if (_dragSource == DragSource.Board && _dragR >= 0) return _blocks[_dragR, _dragC].transform;
            if (_dragSource == DragSource.Palette && _dragSlotIdx >= 0) return _paletteSprites[_dragSlotIdx].transform;
            return null;
        }

        private void SnapBack(int r, int c)
        {
            StartCoroutine(SpringBackAnim(r, c));
        }

        private void SnapBackSlot(int idx)
        {
            if (idx < 0) return;
            var t = _paletteSprites[idx].transform;
            t.position = _palettePos[idx];
            t.localScale = Vector3.one * (CellSize * 0.85f);
        }

        private static bool IsAdjacent(int r1, int c1, int r2, int c2)
            => (Mathf.Abs(r1 - r2) == 1 && c1 == c2) || (Mathf.Abs(c1 - c2) == 1 && r1 == r2);

        private bool FindCell(Vector3 world, out int row, out int col)
        {
            row = -1; col = -1;
            float best = CellSize * 0.7f;
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                float d = Vector2.Distance(new Vector2(world.x, world.y), _basePos[r, c]);
                if (d < best) { best = d; row = r; col = c; }
            }
            return row >= 0;
        }

        private bool FindPaletteSlot(Vector3 world, out int idx)
        {
            idx = -1;
            if (_palettePos == null) return false;
            float best = CellSize * 0.8f;
            for (int i = 0; i < PaletteCount; i++)
            {
                float d = Vector2.Distance(new Vector2(world.x, world.y), _palettePos[i]);
                if (d < best) { best = d; idx = i; }
            }
            return idx >= 0;
        }

        /// <summary>보드 블록을 빈 팔레트 슬롯에 저장. 원본 셀은 None, 중력/리필 트리거.</summary>
        private void StoreBoardToSlot(int sr, int sc, int slotIdx)
        {
            ColorId stored = _colorGrid[sr, sc];
            _paletteColors[slotIdx] = stored;
            _paletteSprites[slotIdx].color = ColorToUnity(stored);
            _paletteSprites[slotIdx].transform.position = _palettePos[slotIdx];
            _paletteSprites[slotIdx].transform.localScale = Vector3.one * (CellSize * 0.85f);

            _colorGrid[sr, sc] = ColorId.None;
            _blocks[sr, sc].color = ColorToUnity(ColorId.None);
            _moves++;
            ShowToast(ColorLabel(stored) + " 저장", ToastKind.Neutral);
            // 사운드 재활용
            if (_audio != null && _sfxMix != null) _audio.PlayOneShot(_sfxMix, 0.5f);
            StartCoroutine(ResolveCascadeCoroutine(ColorId.None));
        }

        /// <summary>팔레트 슬롯 색을 보드 셀에 섞기. 혼합 불가 시 스냅백.</summary>
        private void UseSlotOnBoard(int slotIdx, int tr, int tc)
        {
            ColorId slotColor = _paletteColors[slotIdx];
            ColorId boardColor = _colorGrid[tr, tc];
            ColorId mixed = ColorMixer.Mix(slotColor, boardColor);
            if (mixed == ColorId.None)
            {
                ShowToast("혼합 불가", ToastKind.Danger);
                SnapBackSlot(slotIdx);
                return;
            }

            // 혼합 적용
            _colorGrid[tr, tc] = mixed;
            _paletteColors[slotIdx] = ColorId.None;
            _paletteSprites[slotIdx].color = new Color(0.56f, 0.48f, 0.85f, 0.28f);
            SnapBackSlot(slotIdx);

            // 타깃 물감 흐름 애니 (기존 MixPaintFlow 재활용 — 소스 셀은 존재하지 않으므로
            //   가상 소스 위치를 슬롯으로 주기 위해 별도 경로 사용)
            StartCoroutine(MixFromSlotFlow(slotIdx, tr, tc, slotColor, boardColor, mixed));

            if (mixed == _stage.GoalColor) _goalProgress++;
            _moves++;
            TriggerMixFeedback(_basePos[tr, tc], mixed);
            Vector3 mid = (_palettePos[slotIdx] + _basePos[tr, tc]) * 0.5f;
            SpawnPaintSplash(mid, ColorToUnity(slotColor), 6, 0.30f);
            SpawnPaintSplash(mid, ColorToUnity(boardColor), 6, 0.30f);
            StartCoroutine(ResolveCascadeCoroutine(mixed));
        }

        /// <summary>팔레트 슬롯에서 발사된 물감 흐름 — 슬롯 위치에서 타깃 셀로 흘러들어가는 연출.</summary>
        private IEnumerator MixFromSlotFlow(int slotIdx, int tr, int tc,
            ColorId sourceColor, ColorId fromTarget, ColorId toColor)
        {
            _bouncing[tr, tc] = true;
            var tgtSr = _blocks[tr, tc];
            var tgtT = tgtSr.transform;
            Vector3 targetPos = _basePos[tr, tc];
            Color fromC = ColorToUnity(fromTarget);
            Color toC = ColorToUnity(toColor);

            // Phase 1 (0.22s): 소스 색 구체 하나를 슬롯→타깃으로 흘려보냄
            Vector3 start = _palettePos[slotIdx];
            var ghost = new GameObject("SlotGhost");
            var gsr = ghost.AddComponent<SpriteRenderer>();
            gsr.sprite = _squareSprite;
            gsr.color = ColorToUnity(sourceColor);
            gsr.sortingOrder = 15;
            ghost.transform.position = start;
            ghost.transform.localScale = Vector3.one * (CellSize * 0.55f);

            float p1 = 0.22f, e1 = 0f;
            while (e1 < p1)
            {
                float u = e1 / p1;
                float ease = u * u;
                ghost.transform.position = Vector3.Lerp(start, targetPos, ease);
                float shrink = Mathf.Lerp(0.55f, 0.20f, ease);
                ghost.transform.localScale = Vector3.one * (CellSize * shrink);
                var c = gsr.color; c.a = 1f - ease * 0.9f; gsr.color = c;
                e1 += Time.deltaTime;
                yield return null;
            }
            Destroy(ghost);

            // Phase 2 (0.32s): 타깃 soft pulse + 색 블렌드
            float p2 = 0.32f, e2 = 0f;
            while (e2 < p2)
            {
                float u = e2 / p2;
                float pulse = Mathf.Sin(u * Mathf.PI) * 0.18f;
                float s = 1f + pulse;
                tgtT.localScale = new Vector3(CellSize * s, CellSize * s, CellSize);
                float cu = Mathf.SmoothStep(0f, 1f, u);
                tgtSr.color = Color.Lerp(fromC, toC, cu);
                e2 += Time.deltaTime;
                yield return null;
            }

            // Phase 3 잔향
            float p3 = 0.18f, e3 = 0f;
            while (e3 < p3)
            {
                float u = e3 / p3;
                float decay = (1f - u) * (1f - u);
                float osc = Mathf.Sin(e3 * 2f * Mathf.PI * 4f) * 0.020f * decay;
                tgtT.localScale = new Vector3(CellSize * (1f + osc), CellSize * (1f - osc), CellSize);
                e3 += Time.deltaTime;
                yield return null;
            }

            tgtT.localScale = Vector3.one * CellSize;
            tgtSr.color = toC;
            _bouncing[tr, tc] = false;
        }

        // --------------- Gameplay ---------------
        private void TryMix(int sr, int sc, int tr, int tc)
        {
            ColorId src = _colorGrid[sr, sc];
            ColorId dst = _colorGrid[tr, tc];
            if (src == ColorId.None || dst == ColorId.None) { ShowToast("빈 칸", ToastKind.Warn); return; }
            ColorId mixed = ColorMixer.Mix(src, dst);
            if (mixed == ColorId.None) { ShowToast("혼합 불가", ToastKind.Danger); return; }

            _colorGrid[tr, tc] = mixed;
            _colorGrid[sr, sc] = ColorId.None;
            // 소스 블록이 타깃으로 흘러들어가며 녹는 연출. 타깃은 부드럽게 받아내며 색 그라디언트.
            StartCoroutine(MixPaintFlow(sr, sc, tr, tc, src, dst, mixed));

            if (mixed == _stage.GoalColor) _goalProgress++;
            _moves++;
            TriggerMixFeedback(_basePos[tr, tc], mixed);
            // 두 원본 색이 '혼합 지점' 에서 섞이는 작은 파편 (세기 축소).
            Vector3 mid = (_basePos[sr, sc] + _basePos[tr, tc]) * 0.5f;
            SpawnPaintSplash(mid, ColorToUnity(src), 6, 0.30f);
            SpawnPaintSplash(mid, ColorToUnity(dst), 6, 0.30f);
            StartCoroutine(ResolveCascadeCoroutine(mixed));
        }

        private IEnumerator ResolveCascadeCoroutine(ColorId mixedColor)
        {
            _inputLocked = true;
            int totalScored = 0, depth = 0;

            yield return new WaitForSeconds(0.10f);
            ApplyGravity();
            yield return new WaitForSeconds(0.22f);
            Refill();
            yield return new WaitForSeconds(0.22f);

            while (true)
            {
                var hits = DetectMatches();
                if (hits.Count == 0) break;
                depth++;
                yield return StartCoroutine(ExplodeAnim(hits, depth));
                foreach (var rc in hits)
                {
                    var col = _colorGrid[rc.r, rc.c];
                    totalScored += ScoreFor(col);
                    _colorGrid[rc.r, rc.c] = ColorId.None;
                    _blocks[rc.r, rc.c].color = ColorToUnity(ColorId.None);
                    var c2 = _blocks[rc.r, rc.c].color; c2.a = 1f; _blocks[rc.r, rc.c].color = c2;
                    _blocks[rc.r, rc.c].transform.localScale = Vector3.one * CellSize;
                }
                yield return new WaitForSeconds(0.08f);
                ApplyGravity();
                yield return new WaitForSeconds(0.20f);
                Refill();
                yield return new WaitForSeconds(0.22f);
                if (depth >= 10) break;
            }

            if (depth > _maxChainDepth) _maxChainDepth = depth;
            if (totalScored > 0)
            {
                _score += totalScored + Mathf.Max(0, (depth - 1)) * 50;
                ShowToast(depth > 1 ? ("연쇄 " + depth + "!  +" + totalScored) : ("폭발! +" + totalScored), ToastKind.Success);
            }
            else if (mixedColor != ColorId.None)
            {
                ShowToast(ColorLabel(mixedColor), ToastKind.Neutral);
            }

            EvaluateStageOutcome();
            _inputLocked = false;
        }

        private void EvaluateStageOutcome()
        {
            if (_screen != ScreenState.Playing) return;
            if (_goalProgress >= _stage.GoalCount)
            {
                _stageCleared = true;
                int remaining = _stage.MoveLimit - _moves;
                if (remaining >= _stage.MoveLimit / 2) _stars = 3;
                else if (remaining >= 2) _stars = 2;
                else _stars = 1;
                _score += 200 * _stars;
                bool wasBelowThresh = !WasChapter1Complete();
                bool wasBelowCh2 = !WasChapter2Complete();
                SaveStars(_stage.Id, _stars);
                // 챕터 완주 축하 메시지 (우선 순위: ch2 > ch1)
                if (wasBelowCh2 && WasChapter2Complete())
                    ShowToast("🌊 챕터 2 완주! +" + (200 * _stars), ToastKind.Success);
                else if (wasBelowThresh && WasChapter1Complete())
                    ShowToast("🎉 챕터 1 완주! 다음 챕터 해금", ToastKind.Success);
                else
                    ShowToast("STAGE CLEAR! +" + (200 * _stars), ToastKind.Success);
                _resultEnterT = 0f;
                for (int i = 0; i < _starLit.Length; i++) _starLit[i] = 0f;
                PlaySfx(_sfxClear);
                _inputLocked = true;
                StartCoroutine(EndStageAfter(0.8f));
            }
            else if (_moves >= _stage.MoveLimit)
            {
                _stageCleared = false;
                ShowToast("STAGE FAILED", ToastKind.Danger);
                _resultEnterT = 0f;
                PlaySfx(_sfxFail);
                _inputLocked = true;
                StartCoroutine(EndStageAfter(0.6f));
            }
        }
        private IEnumerator EndStageAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            _screen = ScreenState.Result;
        }

        private struct RC { public int r, c; public RC(int a, int b) { r = a; c = b; } }
        private List<RC> DetectMatches()
        {
            var hits = new List<RC>();
            bool[,] hit = new bool[Rows, Cols];
            for (int r = 0; r < Rows; r++)
            {
                int s = 0;
                for (int c = 1; c <= Cols; c++)
                {
                    bool end = c == Cols || _colorGrid[r, c] != _colorGrid[r, s];
                    if (end) {
                        int len = c - s; ColorId col = _colorGrid[r, s];
                        if (len >= 3 && IsMatchable(col)) for (int k = s; k < c; k++) hit[r, k] = true;
                        s = c;
                    }
                }
            }
            for (int c = 0; c < Cols; c++)
            {
                int s = 0;
                for (int r = 1; r <= Rows; r++)
                {
                    bool end = r == Rows || _colorGrid[r, c] != _colorGrid[s, c];
                    if (end) {
                        int len = r - s; ColorId col = _colorGrid[s, c];
                        if (len >= 3 && IsMatchable(col)) for (int k = s; k < r; k++) hit[k, c] = true;
                        s = r;
                    }
                }
            }
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++) if (hit[r, c]) hits.Add(new RC(r, c));
            return hits;
        }
        private static bool IsMatchable(ColorId c) => c == ColorId.Orange || c == ColorId.Green || c == ColorId.Purple || c == ColorId.White;
        private static int ScoreFor(ColorId c)
        {
            if (c == ColorId.White) return 100;
            if (c == ColorId.Orange || c == ColorId.Green || c == ColorId.Purple) return 30;
            if (c == ColorId.Black) return -20;
            return 10;
        }

        private IEnumerator ExplodeAnim(List<RC> cells, int depth = 1)
        {
            TriggerExplosionFeedback(cells, depth);
            const float dur = 0.35f;
            float t = 0f;
            while (t < dur)
            {
                float u = t / dur;
                float scale = Mathf.Lerp(CellSize, CellSize * 1.55f, u);
                float alpha = Mathf.Lerp(1f, 0f, u * u);
                for (int i = 0; i < cells.Count; i++)
                {
                    var rc = cells[i];
                    _blocks[rc.r, rc.c].transform.localScale = Vector3.one * scale;
                    var col = _blocks[rc.r, rc.c].color; col.a = alpha; _blocks[rc.r, rc.c].color = col;
                }
                t += Time.deltaTime; yield return null;
            }
        }

        private void ApplyGravity()
        {
            for (int c = 0; c < Cols; c++)
            {
                int writeRow = Rows - 1;
                for (int r = Rows - 1; r >= 0; r--)
                {
                    if (_colorGrid[r, c] == ColorId.None) continue;
                    if (r != writeRow)
                    {
                        _colorGrid[writeRow, c] = _colorGrid[r, c];
                        _blocks[writeRow, c].color = ColorToUnity(_colorGrid[writeRow, c]);
                        // 낙하 애니: 이전 위치에서 출발 → EaseOutBounce + 착지 squash
                        Vector3 fromPos = _basePos[r, c];
                        Vector3 toPos = _basePos[writeRow, c];
                        _blocks[writeRow, c].transform.position = fromPos;
                        StartCoroutine(GravityLandBounce(writeRow, c, fromPos, toPos));
                        _colorGrid[r, c] = ColorId.None;
                        _blocks[r, c].color = ColorToUnity(ColorId.None);
                    }
                    writeRow--;
                }
            }
        }
        private void Refill()
        {
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_colorGrid[r, c] != ColorId.None) continue;
                var nb = _spawner.SpawnRandom(r, c);
                _colorGrid[r, c] = nb.Color;
                _blocks[r, c].color = ColorToUnity(nb.Color);
                float spawnY = _basePos[0, c].y + (CellSize + Gap) * (r + 1);
                Vector3 spawn = new Vector3(_basePos[r, c].x, spawnY, 0f);
                Vector3 tgt = _basePos[r, c];
                _blocks[r, c].transform.position = spawn;
                _blocks[r, c].transform.localScale = Vector3.one * (CellSize * 0.30f);
                StartCoroutine(RefillSpawnAnim(r, c, spawn, tgt));
            }
        }

        /// <summary>Gravity 낙하 Ease-Out-Bounce + 착지 Y squash (열별 스태거).</summary>
        private IEnumerator GravityLandBounce(int row, int col, Vector3 fromPos, Vector3 toPos)
        {
            if (_blocks[row, col] == null) yield break;
            _bouncing[row, col] = true;
            var t = _blocks[row, col].transform;

            float delay = (col % 3) * 0.025f;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            float fallDur = 0.32f, fT = 0f;
            while (fT < fallDur)
            {
                float u = fT / fallDur;
                float e = EaseOutBounce(u);
                t.position = Vector3.LerpUnclamped(fromPos, toPos, e);
                fT += Time.deltaTime;
                yield return null;
            }
            t.position = toPos;

            float squashDur = 0.14f, sT = 0f;
            while (sT < squashDur)
            {
                float u = sT / squashDur;
                float e = EaseOutBack(u);
                float sy = Mathf.LerpUnclamped(0.82f, 1.0f, e);
                float sx = Mathf.LerpUnclamped(1.10f, 1.0f, e);
                t.localScale = new Vector3(CellSize * sx, CellSize * sy, CellSize);
                sT += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one * CellSize;
            _bouncing[row, col] = false;
        }

        /// <summary>Refill 블록 등장 스프링 + 알파 fade-in + 행 스태거.</summary>
        private IEnumerator RefillSpawnAnim(int row, int col, Vector3 spawnPos, Vector3 targetPos)
        {
            if (_blocks[row, col] == null) yield break;
            _bouncing[row, col] = true;
            var sr = _blocks[row, col];
            var t = sr.transform;

            float delay = row * 0.018f;
            if (delay > 0f) yield return new WaitForSeconds(delay);

            Color baseCol = sr.color; baseCol.a = 0f; sr.color = baseCol;

            float dur = 0.28f, elapsed = 0f;
            while (elapsed < dur)
            {
                float u = elapsed / dur;
                float pe = 1f - Mathf.Pow(1f - u, 4f);
                t.position = Vector3.LerpUnclamped(spawnPos, targetPos, pe);

                float s;
                if (u < 0.35f) s = Mathf.Lerp(0.30f, 1.15f, EaseOutCubic(u / 0.35f));
                else if (u < 0.70f) s = Mathf.Lerp(1.15f, 0.92f, (u - 0.35f) / 0.35f);
                else s = Mathf.Lerp(0.92f, 1.00f, (u - 0.70f) / 0.30f);
                t.localScale = Vector3.one * (CellSize * s);

                float aU = Mathf.Clamp01(elapsed / 0.18f);
                var curCol = sr.color;
                curCol.a = 1f - Mathf.Pow(1f - aU, 4f);
                sr.color = curCol;

                elapsed += Time.deltaTime;
                yield return null;
            }
            t.position = targetPos;
            t.localScale = Vector3.one * CellSize;
            var finalCol = sr.color; finalCol.a = 1f; sr.color = finalCol;
            _bouncing[row, col] = false;
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f, d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1; return n1 * t * t + 0.984375f;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        private void UpdateScaleDecay()
        {
            // WHY(유저 피드백): '가만히 있을 때 막 움직이는 게 아님' — 브리딩 진폭을
            //                   ±7/8.5% → ±1% 로 극단 축소. 거의 정지 상태지만 완전 경직은 피함.
            //                   회전 wobble 도 제거.
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_blocks[r, c] == null || !_blocks[r, c].enabled) continue;
                if (_dragR == r && _dragC == c) continue;
                if (_bouncing[r, c]) continue;

                var tr = _blocks[r, c].transform;
                float ph = _jellyPhase[r, c];
                // 매우 미세한 호흡 (인지 한계에 가까움) — 완전 정적도 아니고 움직임도 아님
                float breatheX = 1f + Mathf.Sin(Time.time * 1.6f + ph) * 0.010f;
                float breatheY = 1f + Mathf.Sin(Time.time * 1.8f + ph + Mathf.PI) * 0.012f;
                Vector3 targetScale = new Vector3(CellSize * breatheX, CellSize * breatheY, CellSize);
                tr.localScale = Vector3.Lerp(tr.localScale, targetScale, Time.deltaTime * 10f);

                // 회전은 0 으로 수렴 (wobble 제거)
                tr.rotation = Quaternion.Slerp(tr.rotation, Quaternion.identity, Time.deltaTime * 10f);

                tr.position = Vector3.Lerp(tr.position, _basePos[r, c], Time.deltaTime * 14f);
            }
        }

        /// <summary>
        /// 물감 흐름 연출: 별도 ghost 스프라이트가 소스→타깃으로 slide.
        /// WHY: 이전엔 소스 블록 SpriteRenderer 를 직접 움직여 gravity 코루틴과
        ///      충돌 → 낙하 블록이 회색/검정으로 변하는 버그 발생. ghost 분리로 해소.
        /// </summary>
        private IEnumerator MixPaintFlow(int srcR, int srcC, int tr, int tc,
            ColorId sourceColor, ColorId fromTarget, ColorId toColor)
        {
            if (tr < 0 || tc < 0) yield break;

            // 소스 블록은 즉시 None 시각화 (gravity 가 바로 인수 가능)
            var srcSr = _blocks[srcR, srcC];
            srcSr.color = ColorToUnity(ColorId.None);
            srcSr.transform.position = _basePos[srcR, srcC];
            srcSr.transform.localScale = Vector3.one * CellSize;
            srcSr.transform.rotation = Quaternion.identity;

            // Ghost 스프라이트 생성 — 소스 색을 타깃으로 부드럽게 흘려보냄
            var ghost = new GameObject("MixGhost");
            var gsr = ghost.AddComponent<SpriteRenderer>();
            gsr.sprite = _squareSprite;
            Color srcCol = ColorToUnity(sourceColor);
            gsr.color = srcCol;
            gsr.sortingOrder = 20;
            ghost.transform.position = _basePos[srcR, srcC];
            ghost.transform.localScale = Vector3.one * (CellSize * 0.98f);

            Vector3 start = _basePos[srcR, srcC];
            Vector3 targetPos = _basePos[tr, tc];

            // Phase 1: ghost 가 타깃으로 흘러들어감 (0.24s, SmoothStep)
            float p1 = 0.24f, e1 = 0f;
            while (e1 < p1)
            {
                float u = e1 / p1;
                float ease = Mathf.SmoothStep(0f, 1f, u);
                ghost.transform.position = Vector3.Lerp(start, targetPos, ease);
                float shrink = Mathf.Lerp(0.98f, 0.35f, ease);
                ghost.transform.localScale = Vector3.one * (CellSize * shrink);
                var c = srcCol; c.a = 1f - ease * ease; gsr.color = c;
                e1 += Time.deltaTime;
                yield return null;
            }
            Destroy(ghost);

            // Phase 2: 타깃이 부드럽게 pulse 하며 색 블렌드 (매우 soft)
            _bouncing[tr, tc] = true;
            var tSr = _blocks[tr, tc];
            var tT = tSr.transform;
            Color fromC = ColorToUnity(fromTarget);
            Color toC = ColorToUnity(toColor);

            // 펄스를 1.10 으로 축소 (이전 1.18 → 더 부드럽게), sin 이아닌 ease curve 사용
            float p2 = 0.36f, e2 = 0f;
            while (e2 < p2)
            {
                float u = e2 / p2;
                // 부드러운 상승-하강: t < 0.4 에서 상승, 이후 하강
                float pulse;
                if (u < 0.4f) pulse = Mathf.Sin((u / 0.4f) * Mathf.PI * 0.5f) * 0.10f; // ease-out 상승
                else pulse = Mathf.Cos(((u - 0.4f) / 0.6f) * Mathf.PI * 0.5f) * 0.10f; // ease-out 하강
                float s = 1f + pulse;
                tT.localScale = new Vector3(CellSize * s, CellSize * s, CellSize);
                float cu = Mathf.SmoothStep(0f, 1f, u);
                tSr.color = Color.Lerp(fromC, toC, cu);
                e2 += Time.deltaTime;
                yield return null;
            }

            // Phase 3: 매우 작은 잔향 (±1% sin, 3Hz, 0.15s) — 딱딱한 기색 완전 제거
            float p3 = 0.15f, e3 = 0f;
            while (e3 < p3)
            {
                float u = e3 / p3;
                float decay = (1f - u);
                float osc = Mathf.Sin(e3 * 2f * Mathf.PI * 3f) * 0.010f * decay;
                tT.localScale = new Vector3(CellSize * (1f + osc), CellSize * (1f - osc), CellSize);
                e3 += Time.deltaTime;
                yield return null;
            }

            tT.localScale = Vector3.one * CellSize;
            tSr.color = toC;
            _bouncing[tr, tc] = false;
        }

        private static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
        }

        /// <summary>Drag 실패 시 원본 셀 스프링-백 바운스.</summary>
        private IEnumerator SpringBackAnim(int sr, int sc)
        {
            if (sr < 0 || sc < 0) yield break;
            _bouncing[sr, sc] = true;
            var t = _blocks[sr, sc].transform;
            float dur = 0.30f, elapsed = 0f;
            while (elapsed < dur)
            {
                float u = elapsed / dur;
                float s = 1f + Mathf.Sin(u * Mathf.PI * 3f) * 0.15f * (1f - u);
                t.localScale = new Vector3(CellSize * s, CellSize * s, CellSize);
                t.position = Vector3.Lerp(t.position, _basePos[sr, sc], Time.deltaTime * 18f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.position = _basePos[sr, sc];
            t.localScale = Vector3.one * CellSize;
            _bouncing[sr, sc] = false;
        }

        // --------------- Feedback ---------------
        private void TriggerMixFeedback(Vector3 worldPos, ColorId mixed)
        {
            // WHY: 유저 피드백 '옮길때마다 진동 과함' — 진동은 폭발 전용.
            //      Mix 는 미세 흔들림 + 스플래시 + 사운드만 유지.
            StartCoroutine(ScreenShake(0.12f, 0.05f));
            SpawnPaintSplash(worldPos, ColorToUnity(mixed), 8, 0.35f);
            PlaySfx(_sfxMix);
        }
        private void TriggerExplosionFeedback(List<RC> cells, int depth)
        {
            // Chain depth 에스컬레이션 (Juice #1)
            int d = Mathf.Clamp(depth, 1, 10);
            float shakeMag = Mathf.Min(0.22f, 0.05f + (d - 1) * 0.035f);
            float shakeDur = Mathf.Min(0.75f, 0.30f + (d - 1) * 0.05f);
            int particlesPerCell = Mathf.Min(48, 14 + (d - 1) * 4);
            float sfxPitch = Mathf.Min(1.5f, 1.0f + (d - 1) * 0.06f);

            TryVibrate();
            if (d >= 3) TryVibrate(); // 깊은 연쇄는 2회 tap
            StartCoroutine(ScreenShake(shakeDur, shakeMag));
            for (int i = 0; i < cells.Count; i++)
                SpawnPaintSplash(_basePos[cells[i].r, cells[i].c], _blocks[cells[i].r, cells[i].c].color, particlesPerCell, 0.5f);

            // 깊은 연쇄엔 풀스크린 플래시 (fade-out via coroutine)
            if (d >= 3) StartCoroutine(FullScreenFlash(d));

            // SFX — pitch 변화 후 재생, 다음 호출 전 리셋
            if (_audio != null && _sfxExplode != null)
            {
                float oldPitch = _audio.pitch;
                _audio.pitch = sfxPitch;
                _audio.PlayOneShot(_sfxExplode, 0.85f);
                _audio.pitch = oldPitch;
            }
        }

        private IEnumerator FullScreenFlash(int depth)
        {
            var go = new GameObject("FSFlash");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _squareSprite;
            sr.color = depth >= 5 ? new Color(1f, 0.88f, 0.45f, 0.7f) : new Color(1f, 1f, 1f, 0.45f);
            sr.sortingOrder = 100;
            var cam = Camera.main;
            if (cam != null)
            {
                float size = cam.orthographicSize * 2.5f;
                go.transform.position = cam.transform.position + Vector3.forward * 9.5f;
                go.transform.localScale = Vector3.one * size;
            }
            float t = 0f, dur = depth >= 5 ? 0.28f : 0.18f;
            while (t < dur)
            {
                float u = t / dur;
                var c = sr.color;
                c.a = Mathf.Lerp(sr.color.a, 0f, u * u);
                sr.color = c;
                t += Time.deltaTime;
                yield return null;
            }
            Destroy(go);
        }
        private IEnumerator ScreenShake(float duration, float magnitude)
        {
            var cam = Camera.main;
            if (cam == null || duration <= 0f) yield break;
            Vector3 orig = cam.transform.position;
            float t = 0f;
            while (t < duration)
            {
                float decay = 1f - (t / duration);
                float ox = (Random.value - 0.5f) * 2f * magnitude * decay;
                float oy = (Random.value - 0.5f) * 2f * magnitude * decay;
                cam.transform.position = new Vector3(orig.x + ox, orig.y + oy, orig.z);
                t += Time.deltaTime; yield return null;
            }
            cam.transform.position = orig;
        }
        private void SpawnPaintSplash(Vector3 origin, Color color, int count, float lifetime)
        {
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Splash");
                go.transform.position = origin;
                go.transform.localScale = Vector3.one * Random.Range(0.18f, 0.42f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _squareSprite;
                sr.color = color;
                sr.sortingOrder = 50;
                float ang = Random.Range(0f, Mathf.PI * 2f);
                float spd = Random.Range(1.2f, 3.6f);
                var vel = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd;
                StartCoroutine(SplashMotion(go, vel, lifetime));
            }
        }
        private IEnumerator SplashMotion(GameObject go, Vector2 vel, float lifetime)
        {
            if (go == null) yield break;
            var tr = go.transform; var sr = go.GetComponent<SpriteRenderer>();
            float t = 0f; Vector3 pos = tr.position;
            while (t < lifetime && go != null)
            {
                float u = t / lifetime;
                vel *= 0.92f;
                pos += (Vector3)(vel * Time.deltaTime);
                tr.position = pos;
                tr.localScale = Vector3.one * Mathf.Lerp(tr.localScale.x, 0.02f, u * 0.6f);
                var c = sr.color; c.a = 1f - u; sr.color = c;
                t += Time.deltaTime; yield return null;
            }
            if (go != null) Destroy(go);
        }

        // --------------- Audio (procedural) ---------------
        private void SetupAudio()
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _sfxMix = GenerateTone(640f, 0.10f, 10f);
            _sfxExplode = GenerateRumble(0.28f);
            _sfxClear = GenerateArpeggio(new[] { 523f, 659f, 784f, 1047f }, 0.09f);
            _sfxFail = GenerateArpeggio(new[] { 440f, 370f, 294f }, 0.12f);
        }
        private void PlaySfx(AudioClip c)
        {
            if (c == null || _audio == null) return;
            if (!SettingSfxOn) return;
            _audio.PlayOneShot(c, 0.6f * SettingVolume);
        }
        private void TryVibrate()
        {
            if (!SettingHapticOn) return;
#if UNITY_IOS || UNITY_ANDROID
            try { Handheld.Vibrate(); } catch { }
#endif
        }
        private static AudioClip GenerateTone(float freq, float dur, float decay)
        {
            const int rate = 44100; int n = (int)(rate * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++) {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * decay);
                d[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.5f;
            }
            var clip = AudioClip.Create("t", n, 1, rate, false); clip.SetData(d, 0); return clip;
        }
        private static AudioClip GenerateRumble(float dur)
        {
            const int rate = 44100; int n = (int)(rate * dur);
            var d = new float[n];
            for (int i = 0; i < n; i++) {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 5f);
                float freq = Mathf.Lerp(180f, 80f, t / dur);
                float noise = (Random.value - 0.5f) * 0.3f;
                d[i] = (Mathf.Sin(2f * Mathf.PI * freq * t) + noise) * env * 0.55f;
            }
            var clip = AudioClip.Create("r", n, 1, rate, false); clip.SetData(d, 0); return clip;
        }
        private static AudioClip GenerateArpeggio(float[] freqs, float stepDur)
        {
            const int rate = 44100; int step = (int)(rate * stepDur); int total = step * freqs.Length;
            var d = new float[total];
            for (int s = 0; s < freqs.Length; s++)
            {
                float f = freqs[s];
                for (int i = 0; i < step; i++)
                {
                    float tt = i / (float)rate;
                    float env = Mathf.Sin(Mathf.PI * i / step) * 0.45f;
                    d[s * step + i] = Mathf.Sin(2f * Mathf.PI * f * tt) * env;
                }
            }
            var clip = AudioClip.Create("a", total, 1, rate, false); clip.SetData(d, 0); return clip;
        }

        // --------------- Utility ---------------
        private static Color ColorToUnity(ColorId c)
        {
            switch (c)
            {
                case ColorId.Red:    return new Color(0.90f, 0.22f, 0.27f);
                case ColorId.Yellow: return new Color(0.97f, 0.83f, 0.30f);
                case ColorId.Blue:   return new Color(0.28f, 0.58f, 0.94f);
                case ColorId.Orange: return new Color(0.96f, 0.64f, 0.38f);
                case ColorId.Green:  return new Color(0.32f, 0.72f, 0.53f);
                case ColorId.Purple: return new Color(0.62f, 0.31f, 0.87f);
                case ColorId.White:  return new Color(0.96f, 0.96f, 0.94f);
                case ColorId.Black:  return new Color(0.10f, 0.10f, 0.12f);
                case ColorId.Gray:   return new Color(0.42f, 0.44f, 0.48f);
                default:             return new Color(0.18f, 0.18f, 0.22f);
            }
        }
        private static string ColorLabel(ColorId c)
        {
            if (c == ColorId.Red) return "빨강";
            if (c == ColorId.Yellow) return "노랑";
            if (c == ColorId.Blue) return "파랑";
            if (c == ColorId.Orange) return "주황";
            if (c == ColorId.Green) return "초록";
            if (c == ColorId.Purple) return "보라";
            if (c == ColorId.White) return "하양";
            if (c == ColorId.Black) return "검정(오염)";
            return c.ToString();
        }
        private void ShowToast(string msg, ToastKind kind = ToastKind.Neutral)
        {
            _toast = msg;
            _toastKind = kind;
            _toastUntil = Time.time + 1.4f;
        }

        private static Sprite BuildSquareSprite()
        {
            const int sz = 128;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var px = new Color32[sz * sz];
            float cx = sz / 2f, cy = sz / 2f;
            float innerHalf = sz * 0.40f, cornerR = sz * 0.14f, aa = 2f;
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float rx = Mathf.Abs(x - cx) - innerHalf;
                float ry = Mathf.Abs(y - cy) - innerHalf;
                float dxc = Mathf.Max(0f, rx), dyc = Mathf.Max(0f, ry);
                float sdf = Mathf.Sqrt(dxc * dxc + dyc * dyc) - cornerR;
                float alpha = Mathf.Clamp01(-sdf / aa + 0.5f);
                if (alpha <= 0f) { px[y * sz + x] = new Color32(0, 0, 0, 0); continue; }
                float topBias = 1f - (y / (float)sz);
                float bright = Mathf.Lerp(0.70f, 1.05f, topBias);
                float hdx = (x - cx + sz * 0.18f) / (sz * 0.20f);
                float hdy = (y - cy - sz * 0.14f) / (sz * 0.14f);
                float hl = Mathf.Clamp01(1f - (hdx * hdx + hdy * hdy)) * 0.35f;
                bright = Mathf.Clamp01(bright + hl);
                byte v = (byte)(bright * 255f), a = (byte)(alpha * 255f);
                px[y * sz + x] = new Color32(v, v, v, a);
            }
            tex.SetPixels32(px); tex.filterMode = FilterMode.Bilinear; tex.wrapMode = TextureWrapMode.Clamp; tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        }
        private static Texture2D MakeSolidTexture(Color c)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { c, c, c, c }); tex.Apply(); return tex;
        }

        // --------------- GUI ---------------
        private void EnsureStyles()
        {
            if (_display != null) return;
            // 타이포 스케일 — display 40 / heading 28 / score_big 32 / body 16 / caption 12 (UX v3 #4)
            _display = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) } };
            _heading = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.97f, 0.99f, 1f) } };
            _scoreBig = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.92f, 0.55f, 1f) } };
            _goalLabel = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) } };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.75f, 0.78f, 0.85f, 1f) } };
            _caption = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.58f, 0.62f, 0.70f, 1f) } };
            _overlayTitle = new GUIStyle(GUI.skin.label) { fontSize = 52, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) } };
            _overlayBody = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.88f, 0.93f, 1f) } };
            _primaryBtn = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _ghostBtn = new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            _stageBtn = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            _panelBg = MakeSolidTexture(new Color(0.08f, 0.09f, 0.12f, 0.92f));
            _barBg = MakeSolidTexture(new Color(0.20f, 0.22f, 0.28f, 1f));
            _barFill = MakeSolidTexture(new Color(0.62f, 0.31f, 0.87f, 1f));
            _overlayTex = MakeSolidTexture(new Color(0f, 0f, 0f, 0.82f));
            _stageBtnBg = MakeSolidTexture(new Color(0.18f, 0.20f, 0.28f, 1f));
            // Toast 시맨틱 컬러 (UX v3 #3) — success/warn/danger/neutral
            _toastSuccessBg = MakeSolidTexture(new Color(0.13f, 0.77f, 0.37f, 0.95f));
            _toastWarnBg = MakeSolidTexture(new Color(0.96f, 0.62f, 0.04f, 0.95f));
            _toastDangerBg = MakeSolidTexture(new Color(0.94f, 0.27f, 0.27f, 0.95f));
            _toastNeutralBg = MakeSolidTexture(new Color(0.22f, 0.24f, 0.30f, 0.95f));
            _primaryBtnBg = MakeSolidTexture(new Color(0.62f, 0.31f, 0.87f, 1f));
            _ghostBtnBg = MakeSolidTexture(new Color(0.16f, 0.18f, 0.24f, 0.6f));
            _dimOverlay = MakeSolidTexture(new Color(0f, 0f, 0f, 0.35f));
        }

        /// <summary>Safe area 대응 (노치/다이나믹 아일랜드/홈 인디케이터).</summary>
        private Rect GetSafeArea()
        {
            var sa = Screen.safeArea;
            // OnGUI 는 상단 원점 / Screen.safeArea 는 하단 원점 — Y 변환.
            float top = Screen.height - (sa.y + sa.height);
            return new Rect(sa.x, top, sa.width, sa.height);
        }

        private void OnDestroy()
        {
            // Coroutine + 런타임 생성 텍스처 누수 방지 (Arch v3 #1)
            StopAllCoroutines();
            DestroyTex(ref _panelBg);
            DestroyTex(ref _barBg);
            DestroyTex(ref _barFill);
            DestroyTex(ref _overlayTex);
            DestroyTex(ref _stageBtnBg);
            DestroyTex(ref _toastSuccessBg);
            DestroyTex(ref _toastWarnBg);
            DestroyTex(ref _toastDangerBg);
            DestroyTex(ref _toastNeutralBg);
            DestroyTex(ref _primaryBtnBg);
            DestroyTex(ref _ghostBtnBg);
            DestroyTex(ref _dimOverlay);
            DestroyTex(ref _lastArtProgTex);
            if (_solidCache != null)
            {
                foreach (var kv in _solidCache) if (kv.Value != null) Destroy(kv.Value);
                _solidCache.Clear();
            }
        }

        private static void DestroyTex(ref Texture2D t)
        {
            if (t != null) { Destroy(t); t = null; }
        }

        private void OnGUI()
        {
            EnsureStyles();
            switch (_screen)
            {
                case ScreenState.Lobby: DrawLobby(); break;
                case ScreenState.Playing: DrawPlayingHud(); DrawToast(); break;
                case ScreenState.Result: DrawPlayingHud(); DrawResult(); break;
                case ScreenState.Gallery: DrawGallery(); break;
                case ScreenState.Tutorial: DrawTutorial(); break;
                case ScreenState.Settings: DrawSettings(); break;
                case ScreenState.Paused: DrawPlayingHud(); DrawPauseOverlay(); break;
            }
        }

        private void DrawLobby()
        {
            var sa = GetSafeArea();
            int w = Screen.width;
            int titleY = (int)(sa.y + 48);
            GUI.Label(new Rect(0, titleY, w, 56), "Color Mix: Alchemist", _display);
            GUI.Label(new Rect(0, titleY + 64, w, 22), "색을 설계하고 폭발시켜 세상을 복원하라", _overlayBody);

            // 우상단 갤러리 버튼 (M4) — 크게
            int totalFrag = GetTotalUnlockedFragments();
            int maxFrag = Stages.Length * 3;
            var galleryRect = new Rect(w - 176, titleY - 20, 160, 56);
            var galleryStyle = new GUIStyle(_ghostBtn) { fontSize = 18, fontStyle = FontStyle.Bold };
            if (GUI.Button(galleryRect, "🎨 " + totalFrag + "/" + maxFrag, galleryStyle))
            {
                _screen = ScreenState.Gallery;
            }

            // 좌상단 도움말 + 설정 버튼 (큰 터치 타깃)
            var lobbyIconStyle = new GUIStyle(_ghostBtn) { fontSize = 24, fontStyle = FontStyle.Bold };
            var helpRect = new Rect(16, titleY - 20, 64, 56);
            if (GUI.Button(helpRect, "❓", lobbyIconStyle))
            {
                _tutorialPage = 0;
                _screen = ScreenState.Tutorial;
            }
            var settingsRect = new Rect(86, titleY - 20, 64, 56);
            if (GUI.Button(settingsRect, "⚙", lobbyIconStyle))
            {
                _confirmReset = false;
                _screen = ScreenState.Settings;
            }

            int btnW = Mathf.Min(w - 40, 420);
            int btnH = 80;
            int startY = titleY + 140;
            int gap = 10;
            int chapterHeaderH = 42;
            int chapter2HeaderGap = 20; // 챕터 사이 여백

            // 스크롤 뷰 영역 — safeArea 기반
            float bottomSafeY = Screen.height - (sa.y + sa.height);
            int scrollBottomMargin = (int)bottomSafeY + 40;
            int scrollRectH = Screen.height - startY - scrollBottomMargin;

            // 콘텐츠 전체 높이 계산: 챕터 1 헤더 + 5 버튼 + gap + 챕터 2 헤더 + 7 버튼
            int content1H = chapterHeaderH + Chapter1End * (btnH + gap) - gap;
            int content2H = chapterHeaderH + (Stages.Length - Chapter1End) * (btnH + gap) - gap;
            int totalContentH = content1H + chapter2HeaderGap + content2H + 16;

            var viewRect = new Rect(0, startY, w, scrollRectH);
            var contentRect = new Rect(0, 0, w, totalContentH);
            _lobbyScroll = GUI.BeginScrollView(viewRect, _lobbyScroll, contentRect, false, false);

            int cy = 0;
            // 챕터 1 헤더
            GUI.Label(new Rect(0, cy, w, chapterHeaderH), "챕터 1 · 잃어버린 노을", _heading);
            cy += chapterHeaderH;
            for (int i = 0; i < Chapter1End; i++)
            {
                DrawStageButton(i, (w - btnW) / 2, cy, btnW, btnH);
                cy += btnH + gap;
            }

            cy += chapter2HeaderGap;

            // 챕터 2 헤더 — 잠금 표시
            bool chapter2Unlocked = IsChapter2Unlocked();
            string header2 = chapter2Unlocked
                ? "챕터 2 · 바다의 기억"
                : "🔒 챕터 2 · 바다의 기억 (챕터 1 클리어 필요)";
            GUI.Label(new Rect(0, cy, w, chapterHeaderH), header2, _heading);
            cy += chapterHeaderH;
            for (int i = Chapter1End; i < Stages.Length; i++)
            {
                DrawStageButton(i, (w - btnW) / 2, cy, btnW, btnH);
                cy += btnH + gap;
            }

            GUI.EndScrollView();
        }

        private Vector2 _lobbyScroll;

        private void DrawStageButton(int i, int x, int y, int bw, int bh)
        {
            var s = Stages[i];
            bool unlocked = IsStageUnlocked(i);
            int earnedStars = GetStoredStars(s.Id);
            var rect = new Rect(x, y, bw, bh);
            GUI.backgroundColor = unlocked ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.4f);
            string stars = "";
            for (int k = 0; k < 3; k++) stars += (k < earnedStars) ? "★" : "☆";
            string lockIcon = unlocked ? "" : "🔒 ";
            string label = lockIcon + s.Title + "\n" + stars + "  " + ColorLabel(s.GoalColor) + " " + s.GoalCount + "개 / " + s.MoveLimit + "턴";
            if (GUI.Button(rect, label, _stageBtn) && unlocked)
            {
                StartStage(i);
            }
            GUI.backgroundColor = Color.white;
        }

        private bool IsChapter2Unlocked()
        {
            for (int i = 0; i < Chapter1End; i++)
                if (GetStoredStars(Stages[i].Id) <= 0) return false;
            return true;
        }
        private bool WasChapter1Complete()
        {
            for (int i = 0; i < Chapter1End; i++)
                if (GetStoredStars(Stages[i].Id) <= 0) return false;
            return true;
        }
        private bool WasChapter2Complete()
        {
            for (int i = Chapter1End; i < Stages.Length; i++)
                if (GetStoredStars(Stages[i].Id) <= 0) return false;
            return true;
        }

        /// <summary>일시정지 오버레이 — 재개 / 재시작 / 로비 3선택.</summary>
        private void DrawPauseOverlay()
        {
            int w = Screen.width, h = Screen.height;
            GUI.DrawTexture(new Rect(0, 0, w, h), _overlayTex);

            GUI.Label(new Rect(0, h / 2 - 200, w, 60), "일시정지", _overlayTitle);
            GUI.Label(new Rect(0, h / 2 - 130, w, 28), "스테이지를 잠시 멈췄습니다", _overlayBody);

            int btnW = Mathf.Min(w - 48, 340);
            int btnH = 60;
            int x0 = (w - btnW) / 2;
            int gap = 14;
            int y0 = h / 2 - 40;

            GUI.backgroundColor = new Color(0.62f, 0.31f, 0.87f, 1f);
            if (GUI.Button(new Rect(x0, y0, btnW, btnH), "▶ 재개", _primaryBtn))
            {
                _screen = ScreenState.Playing;
            }
            GUI.backgroundColor = new Color(0.32f, 0.34f, 0.40f, 1f);
            if (GUI.Button(new Rect(x0, y0 + btnH + gap, btnW, btnH), "↻ 재시작", _ghostBtn))
            {
                StartStage(_stageIdx);
            }
            if (GUI.Button(new Rect(x0, y0 + (btnH + gap) * 2, btnW, btnH), "로비로", _ghostBtn))
            {
                ExitToLobby();
            }
            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// M5 설정 화면 — SFX / Haptic 토글, 음량 슬라이더, 튜토리얼 재시작, 데이터 초기화.
        /// </summary>
        private void DrawSettings()
        {
            var sa = GetSafeArea();
            int w = Screen.width, h = Screen.height;
            GUI.DrawTexture(new Rect(0, 0, w, h), _overlayTex);

            int topY = (int)(sa.y + 40);
            GUI.Label(new Rect(0, topY, w, 52), "⚙ 설정", _display);

            // 뒤로 버튼 좌상단
            if (GUI.Button(new Rect(16, topY + 8, 84, 40), "◀ 뒤로", _ghostBtn))
            {
                _confirmReset = false;
                _screen = ScreenState.Lobby;
                return;
            }

            int rowH = 64;
            int rowW = Mathf.Min(w - 40, 420);
            int x0 = (w - rowW) / 2;
            int y = topY + 110;
            int gap = 14;

            // 1) SFX 토글
            y = DrawSettingToggle(x0, y, rowW, rowH, "효과음", SettingSfxOn, () =>
            {
                PlayerPrefs.SetInt("alchemist.cfg.sfx", SettingSfxOn ? 0 : 1);
                PlayerPrefs.Save();
            });
            y += gap;

            // 2) 햅틱 토글
            y = DrawSettingToggle(x0, y, rowW, rowH, "진동(햅틱)", SettingHapticOn, () =>
            {
                PlayerPrefs.SetInt("alchemist.cfg.haptic", SettingHapticOn ? 0 : 1);
                PlayerPrefs.Save();
            });
            y += gap;

            // 3) 음량 슬라이더
            y = DrawSettingSlider(x0, y, rowW, rowH, "음량", PlayerPrefs.GetInt("alchemist.cfg.vol", 80), v =>
            {
                PlayerPrefs.SetInt("alchemist.cfg.vol", v);
                PlayerPrefs.Save();
            });
            y += gap * 2;

            // 4) 튜토리얼 다시보기
            y = DrawSettingActionRow(x0, y, rowW, rowH, "튜토리얼 다시보기", "▶", () =>
            {
                _tutorialPage = 0;
                _screen = ScreenState.Tutorial;
            });
            y += gap;

            // 5) 데이터 초기화 (2단계 확인)
            if (!_confirmReset)
            {
                y = DrawSettingActionRow(x0, y, rowW, rowH, "모든 데이터 초기화", "⚠", () =>
                {
                    _confirmReset = true;
                }, dangerStyle: true);
            }
            else
            {
                // 확인 박스
                var bgRect = new Rect(x0, y, rowW, rowH * 2 + 10);
                GUI.DrawTexture(bgRect, _toastDangerBg);
                GUI.Label(new Rect(x0, y + 10, rowW, 28), "정말 초기화? 별점·진행·설정 모두 삭제", _overlayBody);
                int btnW = (rowW - 24) / 2;
                GUI.backgroundColor = new Color(0.32f, 0.34f, 0.40f, 1f);
                if (GUI.Button(new Rect(x0 + 8, y + rowH, btnW, rowH - 6), "취소", _ghostBtn))
                {
                    _confirmReset = false;
                }
                GUI.backgroundColor = new Color(0.94f, 0.27f, 0.27f, 1f);
                if (GUI.Button(new Rect(x0 + 16 + btnW, y + rowH, btnW, rowH - 6), "초기화 실행", _primaryBtn))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    _confirmReset = false;
                    ShowToast("초기화 완료", ToastKind.Warn);
                    _screen = ScreenState.Lobby;
                }
                GUI.backgroundColor = Color.white;
                y += rowH * 2 + 10;
            }
            y += gap * 2;

            // 버전
            GUI.Label(new Rect(0, y, w, 22), "컬러 믹스: 연금술사 v1.0.0", _caption);
        }

        private int DrawSettingToggle(int x, int y, int w, int h, string label, bool on, System.Action onToggle)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), _panelBg);
            GUI.Label(new Rect(x + 20, y, w - 140, h), label, _body);

            int tW = 72, tH = 36;
            int tX = x + w - tW - 16;
            int tY = y + (h - tH) / 2;
            var bg = on ? _toastSuccessBg : _stageBtnBg;
            GUI.DrawTexture(new Rect(tX, tY, tW, tH), bg);
            int knobSz = 28;
            int knobX = on ? tX + tW - knobSz - 4 : tX + 4;
            GUI.DrawTexture(new Rect(knobX, tY + 4, knobSz, knobSz), _primaryBtnBg);

            // 전체 영역 버튼
            if (GUI.Button(new Rect(x, y, w, h), "", GUIStyle.none))
            {
                onToggle?.Invoke();
            }
            return y + h;
        }

        private int DrawSettingSlider(int x, int y, int w, int h, string label, int initial, System.Action<int> onChange)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), _panelBg);
            GUI.Label(new Rect(x + 20, y, 140, h), label, _body);
            GUI.Label(new Rect(x + w - 68, y, 48, h), initial + "%", _body);

            var sliderRect = new Rect(x + 180, y + (h - 20) / 2, w - 260, 20);
            float nv = GUI.HorizontalSlider(sliderRect, initial, 0f, 100f);
            int newVal = Mathf.RoundToInt(nv);
            if (newVal != initial)
            {
                onChange?.Invoke(newVal);
            }
            return y + h;
        }

        private int DrawSettingActionRow(int x, int y, int w, int h, string label, string icon, System.Action onTap, bool dangerStyle = false)
        {
            GUI.DrawTexture(new Rect(x, y, w, h), _panelBg);
            var labelStyle = dangerStyle
                ? new GUIStyle(_body) { normal = { textColor = new Color(0.96f, 0.55f, 0.55f, 1f) } }
                : _body;
            GUI.Label(new Rect(x + 20, y, w - 80, h), label, labelStyle);
            GUI.Label(new Rect(x + w - 60, y, 40, h), icon, _heading);
            if (GUI.Button(new Rect(x, y, w, h), "", GUIStyle.none))
            {
                onTap?.Invoke();
            }
            return y + h;
        }

        /// <summary>
        /// 튜토리얼 Stage 0 — 3페이지 규칙 안내. 첫 실행 시 자동 진입, 로비 ?버튼으로 재진입.
        /// </summary>
        private void DrawTutorial()
        {
            var sa = GetSafeArea();
            int w = Screen.width, h = Screen.height;

            // 배경
            GUI.DrawTexture(new Rect(0, 0, w, h), _overlayTex);

            int topY = (int)(sa.y + 40);

            // 페이지 인디케이터 (도트)
            int dotW = 14, dotGap = 8;
            int totalDotsW = TutorialPages * dotW + (TutorialPages - 1) * dotGap;
            int dotX0 = (w - totalDotsW) / 2;
            int dotY = topY + 4;
            for (int i = 0; i < TutorialPages; i++)
            {
                var dotTex = (i == _tutorialPage) ? _primaryBtnBg : _stageBtnBg;
                GUI.DrawTexture(new Rect(dotX0 + i * (dotW + dotGap), dotY, dotW, dotW), dotTex);
            }

            // 스킵 버튼 (우상단)
            if (GUI.Button(new Rect(w - 100, topY, 84, 36), "건너뛰기", _ghostBtn))
            {
                FinishTutorial();
                return;
            }

            // 페이지 컨텐츠
            switch (_tutorialPage)
            {
                case 0: DrawTutorialPage0(w, h, topY); break;
                case 1: DrawTutorialPage1(w, h, topY); break;
                case 2: DrawTutorialPage2(w, h, topY); break;
            }

            // 하단 버튼
            float bottomSafeY = Screen.height - (sa.y + sa.height);
            int btnY = (int)(h - bottomSafeY - 100);
            int btnH = 60;
            int btnW = 180;
            int gap = 16;

            // "이전" 버튼 (0페이지 아닐 때)
            if (_tutorialPage > 0)
            {
                GUI.backgroundColor = new Color(0.32f, 0.34f, 0.40f, 1f);
                if (GUI.Button(new Rect((w - (btnW * 2 + gap)) / 2, btnY, btnW, btnH), "◀ 이전", _ghostBtn))
                {
                    _tutorialPage--;
                }
                GUI.backgroundColor = Color.white;
            }

            // "다음" 또는 "시작하기"
            bool isLast = _tutorialPage == TutorialPages - 1;
            int nextX = _tutorialPage > 0
                ? (w - (btnW * 2 + gap)) / 2 + btnW + gap
                : (w - btnW) / 2;

            GUI.backgroundColor = new Color(0.62f, 0.31f, 0.87f, 1f);
            if (GUI.Button(new Rect(nextX, btnY, btnW, btnH), isLast ? "시작하기 ▶" : "다음 ▶", _primaryBtn))
            {
                if (isLast) FinishTutorial();
                else _tutorialPage++;
            }
            GUI.backgroundColor = Color.white;
        }

        private void FinishTutorial()
        {
            MarkTutorialDone();
            _screen = ScreenState.Lobby;
        }

        private void DrawTutorialPage0(int w, int h, int topY)
        {
            // 환영 페이지
            GUI.Label(new Rect(0, topY + 40, w, 60), "컬러 믹스", _display);
            GUI.Label(new Rect(0, topY + 100, w, 44), "연금술사", _heading);

            int artY = h / 2 - 80;
            int sz = 72, gap = 24;
            int totalW = 3 * sz + 2 * gap;
            int x0 = (w - totalW) / 2;

            // 3 원색 블록 시연
            DrawDemoBlock(x0, artY, sz, ColorId.Red);
            DrawDemoBlock(x0 + sz + gap, artY, sz, ColorId.Yellow);
            DrawDemoBlock(x0 + (sz + gap) * 2, artY, sz, ColorId.Blue);

            GUI.Label(new Rect(0, artY + sz + 30, w, 28), "세 가지 원색으로", _overlayBody);
            GUI.Label(new Rect(0, artY + sz + 60, w, 28), "모든 색을 만들어내세요", _overlayBody);
        }

        private void DrawTutorialPage1(int w, int h, int topY)
        {
            GUI.Label(new Rect(0, topY + 40, w, 48), "색 조합 규칙", _heading);

            int rowH = 90, sz = 60, plus = 28, eq = 28, resSz = 60;
            int totalRowW = sz + plus + sz + eq + resSz;

            // 3 rules: R+Y=O, Y+B=G, R+B=P
            var rules = new[]
            {
                (ColorId.Red, ColorId.Yellow, ColorId.Orange),
                (ColorId.Yellow, ColorId.Blue, ColorId.Green),
                (ColorId.Red, ColorId.Blue, ColorId.Purple),
            };

            int startY = h / 2 - (rowH * rules.Length) / 2 - 20;
            for (int i = 0; i < rules.Length; i++)
            {
                var (a, b, outC) = rules[i];
                int x = (w - totalRowW) / 2;
                int y = startY + i * rowH;
                DrawDemoBlock(x, y, sz, a);
                GUI.Label(new Rect(x + sz, y + (sz - 36) / 2, plus, 36), "+", _display);
                DrawDemoBlock(x + sz + plus, y, sz, b);
                GUI.Label(new Rect(x + sz + plus + sz, y + (sz - 36) / 2, eq, 36), "=", _display);
                DrawDemoBlock(x + sz + plus + sz + eq, y, resSz, outC);
            }

            GUI.Label(new Rect(0, startY + rowH * rules.Length + 10, w, 26), "원색을 섞어 2차색을 만드세요", _overlayBody);
        }

        private void DrawTutorialPage2(int w, int h, int topY)
        {
            GUI.Label(new Rect(0, topY + 40, w, 48), "3연결 폭발 + 팔레트", _heading);

            // 예시: 3연결 Purple
            int sz = 56, gap = 6;
            int demoW = sz * 3 + gap * 2;
            int demoY = h / 2 - 150;
            int demoX0 = (w - demoW) / 2;
            for (int i = 0; i < 3; i++) DrawDemoBlock(demoX0 + i * (sz + gap), demoY, sz, ColorId.Purple);
            GUI.Label(new Rect(0, demoY + sz + 14, w, 26), "같은 2차색 3개 가로/세로 연결 → 폭발!", _overlayBody);

            // 팔레트 슬롯 예시
            int palY = demoY + sz + 70;
            int palSz = 50;
            int palW = palSz * 3 + gap * 2;
            int palX0 = (w - palW) / 2;
            // 2 빈 슬롯 + 1 저장된 (Yellow)
            DrawPaletteDemo(palX0, palY, palSz, ColorId.None);
            DrawPaletteDemo(palX0 + palSz + gap, palY, palSz, ColorId.Yellow);
            DrawPaletteDemo(palX0 + (palSz + gap) * 2, palY, palSz, ColorId.None);

            GUI.Label(new Rect(0, palY + palSz + 14, w, 26), "하단 팔레트에 저장 · 드래그로 꺼내기", _overlayBody);
        }

        private void DrawDemoBlock(int x, int y, int sz, ColorId color)
        {
            var tex = EnsureCachedSolid(ColorToUnity(color));
            // 약간 안쪽에 그려 rounded 느낌
            GUI.DrawTexture(new Rect(x + 3, y + 3, sz - 6, sz - 6), tex);
        }

        private void DrawPaletteDemo(int x, int y, int sz, ColorId color)
        {
            var bgTex = (color == ColorId.None)
                ? EnsureCachedSolid(new Color(0.56f, 0.48f, 0.85f, 0.28f))
                : EnsureCachedSolid(ColorToUnity(color));
            GUI.DrawTexture(new Rect(x, y, sz, sz), bgTex);
        }

        /// <summary>
        /// M4 갤러리 복원 — 총 획득 별 수를 픽셀 그리드로 시각화.
        /// 스테이지 클리어 별점이 누적될수록 노을 그라디언트가 드러남.
        /// </summary>
        private void DrawGallery()
        {
            var sa = GetSafeArea();
            int w = Screen.width;
            int topY = (int)(sa.y + 48);

            GUI.Label(new Rect(0, topY, w, 52), "🎨 갤러리", _display);

            if (GUI.Button(new Rect(16, topY + 8, 84, 40), "◀ 뒤로", _ghostBtn))
            {
                _screen = ScreenState.Lobby;
            }

            // 스크롤 뷰로 2 챕터 세로 배치
            float bottomSafeY = Screen.height - (sa.y + sa.height);
            int listY = topY + 70;
            int listH = Screen.height - listY - (int)bottomSafeY - 20;

            int canvasSize = Mathf.Min(w - 40, 360);
            int cx = (w - canvasSize) / 2;

            // 각 챕터 섹션 높이: 제목 30 + 부제 20 + 캔버스 + 바 22 + 텍스트 24 + gap 32
            int sectionH = 30 + 20 + canvasSize + 22 + 32 + 48;
            int totalContentH = Artworks.Length * sectionH + 40;

            var viewRect = new Rect(0, listY, w, listH);
            var contentRect = new Rect(0, 0, w, totalContentH);
            _galleryScroll = GUI.BeginScrollView(viewRect, _galleryScroll, contentRect, false, false);

            int cy = 0;
            for (int a = 0; a < Artworks.Length; a++)
            {
                var art = Artworks[a];
                int chUnlocked = GetArtworkFragments(art);
                int chTotal = art.TotalFragments;
                float prog = Mathf.Clamp01((float)chUnlocked / chTotal);

                GUI.Label(new Rect(0, cy, w, 30), art.Title, _heading);
                cy += 30;
                GUI.Label(new Rect(0, cy, w, 20), art.Subtitle, _caption);
                cy += 28;

                DrawArtworkCanvas(cx, cy, canvasSize, chUnlocked, chTotal, art.TopColor, art.BottomColor);
                cy += canvasSize + 20;

                int barH = 20;
                GUI.DrawTexture(new Rect(cx, cy, canvasSize, barH), _barBg);
                var progCol = Color.Lerp(art.TopColor, art.BottomColor, 0.35f);
                var fill = EnsureCachedSolid(progCol);
                GUI.DrawTexture(new Rect(cx, cy, (int)(canvasSize * prog), barH), fill);
                GUI.Label(new Rect(cx, cy - 2, canvasSize, barH + 4), "복원 " + chUnlocked + " / " + chTotal, _goalLabel);
                cy += barH + 20;

                if (chUnlocked >= chTotal)
                {
                    GUI.Label(new Rect(0, cy, w, 22), "✨ " + art.Title + " 완전 복원! ✨", _goalLabel);
                }
                cy += 40;
            }

            GUI.EndScrollView();
        }

        private Vector2 _galleryScroll;

        /// <summary>해당 챕터의 FeedingStages 로부터 획득한 별점 합.</summary>
        private int GetArtworkFragments(ArtworkConfig art)
        {
            int sum = 0;
            for (int i = 0; i < art.FeedingStages.Length; i++)
            {
                int idx = art.FeedingStages[i];
                if (idx < 0 || idx >= Stages.Length) continue;
                sum += GetStoredStars(Stages[idx].Id);
            }
            return sum;
        }

        private Texture2D _lastArtProgTex;

        private void DrawArtworkCanvas(int x, int y, int size, int unlocked, int total, Color topC, Color botC)
        {
            // 4×4 그리드 중 15셀을 조각으로 삼고(순서 고정), 언락된 만큼 컬러 드러냄.
            int grid = 4;
            int cellSz = size / grid;
            // 배경 틀
            GUI.DrawTexture(new Rect(x, y, size, size), _panelBg);

            // 드러난 조각에 gradient 샘플링
            int[] order = { 5, 6, 9, 10, 4, 7, 8, 11, 1, 2, 13, 14, 0, 3, 12, 15 }; // 중앙→외곽
            int cellCount = grid * grid;

            for (int i = 0; i < cellCount; i++)
            {
                int gridIdx = (i < order.Length) ? order[i] : i;
                int gr = gridIdx / grid;
                int gc = gridIdx % grid;
                int px = x + gc * cellSz + 2;
                int py = y + gr * cellSz + 2;
                int sz = cellSz - 4;

                if (i < unlocked)
                {
                    // 그라디언트: 상단행 TopColor, 하단행 BottomColor 보간
                    float vy = (float)gr / (grid - 1);
                    var col = Color.Lerp(topC, botC, vy);
                    var tex = EnsureCachedSolid(col);
                    GUI.DrawTexture(new Rect(px, py, sz, sz), tex);
                }
                else
                {
                    // 잠긴 조각
                    var tex = EnsureCachedSolid(new Color(0.12f, 0.13f, 0.17f, 0.6f));
                    GUI.DrawTexture(new Rect(px, py, sz, sz), tex);
                }
            }
        }

        private readonly Dictionary<Color, Texture2D> _solidCache = new Dictionary<Color, Texture2D>();
        private Texture2D EnsureCachedSolid(Color c)
        {
            if (_solidCache.TryGetValue(c, out var t) && t != null) return t;
            t = MakeSolidTexture(c);
            _solidCache[c] = t;
            return t;
        }

        private void DrawPlayingHud()
        {
            var sa = GetSafeArea();
            int w = Screen.width;
            int topSafe = (int)sa.y + 8;
            int panelH = 130;

            // 상단 패널
            GUI.DrawTexture(new Rect(0, topSafe, w, panelH), _panelBg);
            GUI.Label(new Rect(16, topSafe + 10, w - 100, 32), _stage.Title, _heading);

            // 우상단: 일시정지 + 로비 (큰 터치 타깃)
            var bigBtnStyle = new GUIStyle(_ghostBtn) { fontSize = 22, fontStyle = FontStyle.Bold };
            if (GUI.Button(new Rect(w - 180, topSafe + 10, 80, 48), "⏸", bigBtnStyle))
            {
                if (_screen == ScreenState.Playing) _screen = ScreenState.Paused;
            }
            if (GUI.Button(new Rect(w - 92, topSafe + 10, 80, 48), "🏠", bigBtnStyle))
            {
                ExitToLobby();
            }

            // 진행 바 — 스테이지 목표 색 동적 적용 (UX v3 #1)
            EnsureBarFillFor(_stage.GoalColor);
            int barX = 16, barW = w - 32, barH = 24, barY = topSafe + 52;
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), _barBg);
            float prog = Mathf.Clamp01((float)_goalProgress / _stage.GoalCount);
            GUI.DrawTexture(new Rect(barX, barY, (int)(barW * prog), barH), _barFill);
            // 임계값 강조: 1개 남으면 바 펄스
            if (_goalProgress >= _stage.GoalCount - 1 && _goalProgress < _stage.GoalCount)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
                GUI.color = new Color(1f, 1f, 1f, 0.25f * pulse);
                GUI.DrawTexture(new Rect(barX, barY, (int)(barW * prog), barH), _barFill);
                GUI.color = Color.white;
            }
            string goalText = ColorLabel(_stage.GoalColor) + "  " + _goalProgress + " / " + _stage.GoalCount;
            GUI.Label(new Rect(barX, barY - 2, barW, barH + 4), goalText, _goalLabel);

            // 하단 정보 행: 턴(좌) + 점수(우) — 이모지 + 큰 숫자로 성취감
            int infoY = topSafe + 88;
            GUI.Label(new Rect(16, infoY, 260, 34), "⏱ " + _moves + " / " + _stage.MoveLimit, _body);
            GUI.Label(new Rect(w - 280 - 16, infoY - 6, 280, 44), "🏆 " + _displayedScore.ToString("N0"), _scoreBig);

            // WHY(유저 피드백): '합쳐질 때 화면 왜 어두워짐' — 일반 믹스에도 dim 이 뜨는 것이
            //                   원인. 이제 chain depth ≥ 2 실제 연쇄 구간에만 아주 짧게 보이게.
            // 단일 mix(비연쇄) 에선 아예 표시 안 함.

            // 팔레트 슬롯 오버레이 — "+" 아이콘 + 라벨 (월드→스크린 변환)
            DrawPaletteOverlay();

            // 하단 힌트 (safeArea 하단 여백 확보)
            float bottomSafeY = Screen.height - (sa.y + sa.height);
            int hintY = (int)(Screen.height - bottomSafeY - 32);
            GUI.Label(new Rect(0, hintY, w, 20), "블록을 드래그해서 섞어요 · 🎨 팔레트에 색을 저장해보세요", _caption);
        }

        /// <summary>진행 바를 목표 색으로 지연 교체.</summary>
        private void EnsureBarFillFor(ColorId goal)
        {
            if (_lastBarColor == goal && _barFill != null) return;
            if (_barFill != null) Destroy(_barFill);
            _barFill = MakeSolidTexture(ColorToUnity(goal));
            _lastBarColor = goal;
        }

        /// <summary>팔레트 슬롯 위에 "🎨 팔레트" 헤더 + 빈 슬롯 "+" 아이콘 렌더.</summary>
        private void DrawPaletteOverlay()
        {
            if (_paletteSprites == null || _palettePos == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            // 헤더 — 슬롯들의 x 평균 + 위쪽 오프셋
            Vector3 firstW = _palettePos[0];
            Vector3 lastW = _palettePos[PaletteCount - 1];
            Vector3 centerW = (firstW + lastW) * 0.5f + Vector3.up * (CellSize * 0.85f);
            Vector3 headerS = cam.WorldToScreenPoint(centerW);
            float screenYFlip = Screen.height - headerS.y;
            var headerRect = new Rect(headerS.x - 120, screenYFlip - 22, 240, 28);
            GUI.Label(headerRect, "🎨 팔레트", _goalLabel);

            // 각 슬롯 위 라벨
            for (int i = 0; i < PaletteCount; i++)
            {
                Vector3 s = cam.WorldToScreenPoint(_palettePos[i]);
                float sy = Screen.height - s.y;
                bool empty = _paletteColors[i] == ColorId.None;
                if (empty)
                {
                    // 펄스 "+" 아이콘
                    float pulse = 1f + Mathf.Sin(Time.time * 3f + i * 0.7f) * 0.08f;
                    var plusStyle = new GUIStyle(_overlayTitle)
                    {
                        fontSize = (int)(44 * pulse),
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.70f, 0.58f, 0.95f, 0.9f) }
                    };
                    GUI.Label(new Rect(s.x - 40, sy - 40, 80, 80), "+", plusStyle);
                }
                else
                {
                    // 저장된 색 하단에 작은 탭 힌트
                    var tagStyle = new GUIStyle(_caption)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = new Color(0.95f, 0.95f, 0.95f, 0.85f) }
                    };
                    GUI.Label(new Rect(s.x - 50, sy + 32, 100, 18), "꺼내기 ↑", tagStyle);
                }
            }
        }

        private void DrawToast()
        {
            if (Time.time >= _toastUntil || string.IsNullOrEmpty(_toast)) return;
            int w = Screen.width;
            var sa = GetSafeArea();
            float bottomSafeY = Screen.height - (sa.y + sa.height);
            int y = (int)(Screen.height - bottomSafeY - 100);
            float u = Mathf.Clamp01(1f - (_toastUntil - Time.time) / 0.35f); // fade-out 0.35s
            float alpha = (_toastUntil - Time.time > 0.35f) ? 1f : 1f - u;

            Texture2D bg;
            switch (_toastKind)
            {
                case ToastKind.Success: bg = _toastSuccessBg; break;
                case ToastKind.Warn: bg = _toastWarnBg; break;
                case ToastKind.Danger: bg = _toastDangerBg; break;
                default: bg = _toastNeutralBg; break;
            }
            int toastW = Mathf.Min(w - 48, 520);
            int toastH = 56;
            var rect = new Rect((w - toastW) / 2, y, toastW, toastH);
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(rect, bg);
            var labelStyle = new GUIStyle(_heading) { alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 1f, 1f, alpha) } };
            GUI.Label(rect, _toast, labelStyle);
            GUI.color = Color.white;
        }

        private void DrawResult()
        {
            int w = Screen.width, h = Screen.height;

            // 결과 오버레이 페이드 인 (Juice #4)
            _resultEnterT = Mathf.MoveTowards(_resultEnterT, 1f, Time.deltaTime / 0.55f);
            float alphaOv = Mathf.Clamp01(_resultEnterT) * 0.82f;
            GUI.color = new Color(1f, 1f, 1f, alphaOv);
            GUI.DrawTexture(new Rect(0, 0, w, h), _overlayTex);
            GUI.color = Color.white;

            // 보드 scale fade — 결과 진입 시 보드 0.7x 로 줄어듦
            float boardScale = Mathf.Lerp(1f, 0.7f, _resultEnterT);
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_blocks[r, c] == null) continue;
                _blocks[r, c].transform.localScale = Vector3.one * (CellSize * boardScale);
            }

            // 타이틀 slide-up + fade in
            float titleU = Mathf.Clamp01((_resultEnterT - 0.15f) / 0.45f);
            float titleOffset = Mathf.Lerp(40f, 0f, EaseOutQuart(titleU));
            GUI.color = new Color(1f, 1f, 1f, titleU);
            string title = _stageCleared ? "STAGE CLEAR" : "STAGE FAILED";
            var titleStyle = new GUIStyle(_overlayTitle) {
                normal = { textColor = _stageCleared ? new Color(1f, 0.83f, 0.30f, 1f) : new Color(0.94f, 0.27f, 0.27f, 1f) }
            };
            GUI.Label(new Rect(0, h / 2 - 210 + titleOffset, w, 68), title, titleStyle);
            GUI.color = Color.white;

            // 별점 순차 점등
            if (_stageCleared)
            {
                int starSize = 52;
                int starGap = 16;
                int totalStarW = 3 * starSize + 2 * starGap;
                int sx0 = (w - totalStarW) / 2;
                int sy = h / 2 - 120;
                for (int i = 0; i < 3; i++)
                {
                    float startT = 0.60f + i * 0.22f;
                    _starLit[i] = Mathf.MoveTowards(_starLit[i], (_resultEnterT >= startT && i < _stars) ? 1f : 0f, Time.deltaTime / 0.35f);
                    float lit = _starLit[i];
                    float scale = Mathf.Lerp(0.3f, 1f, lit < 0.5f ? lit * 2f : 1f - (lit - 0.5f) * 0.6f);
                    GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(lit));
                    var starStyle = new GUIStyle(_overlayTitle) {
                        fontSize = (int)(starSize * scale),
                        normal = { textColor = new Color(1f, 0.86f, 0.34f, 1f) }
                    };
                    GUI.Label(new Rect(sx0 + i * (starSize + starGap), sy, starSize, starSize), "★", starStyle);
                }
                GUI.color = Color.white;
            }

            // 점수 count-up (결과 진입 0.3s 부터 0.9s 에 걸쳐)
            float scoreU = Mathf.Clamp01((_resultEnterT - 0.30f) / 0.60f);
            int shownScore = (int)Mathf.Round(Mathf.Lerp(0f, _score, EaseOutQuart(scoreU)));
            GUI.color = new Color(1f, 1f, 1f, scoreU);
            GUI.Label(new Rect(0, h / 2 - 30, w, 40), shownScore.ToString("N0"), new GUIStyle(_display) { alignment = TextAnchor.MiddleCenter });
            GUI.Label(new Rect(0, h / 2 + 16, w, 28), "턴 " + _moves + " / " + _stage.MoveLimit + " · 최대연쇄 " + _maxChainDepth, _overlayBody);
            GUI.color = Color.white;

            // 버튼 레이아웃 수정 — 동적 폭 (UX v3 #2)
            int gap = 16;
            int btnH = 64;
            int avail = w - 32; // 16pt 좌우 여백
            int btnW = Mathf.Min(200, (avail - gap) / 2);
            bool hasNext = _stageCleared && _stageIdx < Stages.Length - 1;
            int totalW = 2 * btnW + gap;
            int x0 = (w - totalW) / 2;
            int by = h / 2 + 90;

            float btnU = Mathf.Clamp01((_resultEnterT - 0.80f) / 0.20f);
            GUI.color = new Color(1f, 1f, 1f, btnU);

            // 좌 = ghost 스타일 (재도전/다시)
            GUI.backgroundColor = new Color(0.32f, 0.34f, 0.40f, 1f);
            if (GUI.Button(new Rect(x0, by, btnW, btnH), _stageCleared ? "재도전" : "다시", _ghostBtn))
            {
                StartStage(_stageIdx);
            }

            // 우 = primary (다음 ▶) — 성공 시 강조
            GUI.backgroundColor = _stageCleared ? new Color(0.62f, 0.31f, 0.87f, 1f) : new Color(0.38f, 0.40f, 0.48f, 1f);
            string nextLabel = hasNext ? "다음 ▶" : "로비로";
            if (GUI.Button(new Rect(x0 + btnW + gap, by, btnW, btnH), nextLabel, _primaryBtn))
            {
                if (hasNext) StartStage(_stageIdx + 1);
                else ExitToLobby();
            }
            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
        }

        private static float EaseOutQuart(float t)
        {
            return 1f - Mathf.Pow(1f - t, 4f);
        }
    }
}
