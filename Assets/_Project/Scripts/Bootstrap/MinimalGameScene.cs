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
            new StageConfig("s1", "1. 노을의 주홍", ColorId.Orange, 3, 10, 42),
            new StageConfig("s2", "2. 여름의 초록", ColorId.Green,  4, 12, 100),
            new StageConfig("s3", "3. 달빛 보라",  ColorId.Purple, 5, 12, 200),
            new StageConfig("s4", "4. 깊은 숲",   ColorId.Green,  6, 14, 300),
            new StageConfig("s5", "5. 무지개 끝", ColorId.White,  3, 16, 500),
        };

        // --------------- Screen state ---------------
        private enum ScreenState { Lobby, Playing, Result }
        private ScreenState _screen = ScreenState.Lobby;
        private int _stageIdx;
        private StageConfig _stage;

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
            FitCameraToBoard();
            SetupAudio();
            SetBoardVisible(false); // 로비에선 숨김
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
        }

        private void FitCameraToBoard()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float halfH = (Rows * (CellSize + Gap) - Gap) / 2f;
            float halfW = (Cols * (CellSize + Gap) - Gap) / 2f;
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float szH = halfH;
            float szW = halfW / Mathf.Max(0.01f, aspect);
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(szH, szW) * 1.1f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
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
            return GetStoredStars(Stages[idx - 1].Id) > 0;
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
            if (!FindCell(world, out int r, out int c)) return;
            if (_colorGrid[r, c] == ColorId.None) return;
            _dragR = r; _dragC = c;
            _blocks[r, c].sortingOrder = 10;
        }
        private void OnDragMove(Vector3 world)
        {
            if (_dragR < 0) return;
            _blocks[_dragR, _dragC].transform.position = new Vector3(world.x, world.y, 0f);
            _blocks[_dragR, _dragC].transform.localScale = Vector3.one * (CellSize * 1.15f);
        }
        private void OnDragEnd(Vector3 world)
        {
            if (_dragR < 0) return;
            int sr = _dragR, sc = _dragC;
            _dragR = _dragC = -1;
            _blocks[sr, sc].sortingOrder = 0;

            if (!FindCell(world, out int tr, out int tc) || (tr == sr && tc == sc)) { SnapBack(sr, sc); return; }
            if (!IsAdjacent(sr, sc, tr, tc)) { SnapBack(sr, sc); return; }
            SnapBack(sr, sc);
            TryMix(sr, sc, tr, tc);
        }

        private void SnapBack(int r, int c)
        {
            // WHY: 즉시 teleport 대신 SpringBackAnim 으로 말캉한 바운스 되돌림.
            StartCoroutine(SpringBackAnim(r, c));
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
            _blocks[sr, sc].color = ColorToUnity(ColorId.None);
            // 타깃은 squash-stretch + 색 그라디언트 보간 (물감이 섞이는 느낌).
            StartCoroutine(MixBounceAnim(tr, tc, dst, mixed));

            if (mixed == _stage.GoalColor) _goalProgress++;
            _moves++;
            TriggerMixFeedback(_basePos[tr, tc], mixed);
            // 두 원본 색이 '혼합 지점' 에서 섞이는 파편 (양쪽 색 동시 분사).
            Vector3 mid = (_basePos[sr, sc] + _basePos[tr, tc]) * 0.5f;
            SpawnPaintSplash(mid, ColorToUnity(src), 10, 0.42f);
            SpawnPaintSplash(mid, ColorToUnity(dst), 10, 0.42f);
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
                SaveStars(_stage.Id, _stars);
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
                        _blocks[writeRow, c].transform.position = _basePos[r, c];
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
                _blocks[r, c].transform.position = new Vector3(_basePos[r, c].x, spawnY, 0f);
                _blocks[r, c].transform.localScale = Vector3.one * (CellSize * 0.9f);
            }
        }

        private void UpdateScaleDecay()
        {
            // 말캉한 "숨쉬는" 애니: 모든 블록이 약하게 펄스 + 각자 다른 위상.
            // WHY: 정적 격자가 벽돌처럼 보인다는 유저 피드백. 이 작은 펄스가 살아있는 물감 느낌을 준다.
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_blocks[r, c] == null || !_blocks[r, c].enabled) continue;
                if (_dragR == r && _dragC == c) continue;
                if (_bouncing[r, c]) continue; // 바운스 애니 중엔 브리딩 비활성
                var tr = _blocks[r, c].transform;
                float breathe = 1f + Mathf.Sin(Time.time * 2.2f + _jellyPhase[r, c]) * 0.028f;
                float stretchY = 1f + Mathf.Sin(Time.time * 2.4f + _jellyPhase[r, c] + 1.1f) * 0.022f;
                Vector3 targetScale = new Vector3(CellSize * breathe, CellSize * stretchY, CellSize);
                tr.localScale = Vector3.Lerp(tr.localScale, targetScale, Time.deltaTime * 8f);
                tr.position = Vector3.Lerp(tr.position, _basePos[r, c], Time.deltaTime * 12f);
            }
        }

        /// <summary>Mix 직후 타깃 셀의 squash-stretch 바운스 + 색상 그라디언트 전환.</summary>
        private IEnumerator MixBounceAnim(int tr, int tc, ColorId fromColor, ColorId toColor)
        {
            if (tr < 0 || tc < 0) yield break;
            _bouncing[tr, tc] = true;
            var sr = _blocks[tr, tc];
            var t = sr.transform;
            Color a = ColorToUnity(fromColor);
            Color b = ColorToUnity(toColor);

            float dur = 0.45f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                float u = elapsed / dur;
                // 3단계 squash: 0.7 → 1.45 → 1.0 (스프링)
                float s = u < 0.25f
                    ? Mathf.Lerp(1f, 0.70f, u / 0.25f)
                    : u < 0.55f
                        ? Mathf.Lerp(0.70f, 1.45f, (u - 0.25f) / 0.30f)
                        : Mathf.Lerp(1.45f, 1.0f, (u - 0.55f) / 0.45f);
                // Y 반대로: 옆으로 눌리면 위로 늘어남 → 말캉한 느낌
                float sy = u < 0.25f
                    ? Mathf.Lerp(1f, 1.30f, u / 0.25f)
                    : u < 0.55f
                        ? Mathf.Lerp(1.30f, 0.75f, (u - 0.25f) / 0.30f)
                        : Mathf.Lerp(0.75f, 1.0f, (u - 0.55f) / 0.45f);
                t.localScale = new Vector3(CellSize * s, CellSize * sy, CellSize);

                // 색상 보간 (첫 0.2 초 구간에서 물감 블렌드 시각화)
                float cu = Mathf.Clamp01(u / 0.45f);
                sr.color = Color.Lerp(a, b, EaseInOutCubic(cu));
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localScale = Vector3.one * CellSize;
            sr.color = b;
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

#if UNITY_IOS || UNITY_ANDROID
            try { Handheld.Vibrate(); } catch { }
            if (d >= 3) { try { Handheld.Vibrate(); } catch { } } // 깊은 연쇄는 2회 tap
#endif
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
        private void PlaySfx(AudioClip c) { if (c != null && _audio != null) _audio.PlayOneShot(c, 0.6f); }
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
            }
        }

        private void DrawLobby()
        {
            var sa = GetSafeArea();
            int w = Screen.width;
            int titleY = (int)(sa.y + 48);
            GUI.Label(new Rect(0, titleY, w, 56), "Color Mix: Alchemist", _display);
            GUI.Label(new Rect(0, titleY + 64, w, 22), "색을 설계하고 폭발시켜 세상을 복원하라", _overlayBody);

            int btnW = Mathf.Min(w - 40, 420);
            int btnH = 88;
            int startY = titleY + 140;
            int gap = 12;

            for (int i = 0; i < Stages.Length; i++)
            {
                var s = Stages[i];
                bool unlocked = IsStageUnlocked(i);
                int earnedStars = GetStoredStars(s.Id);
                var rect = new Rect((w - btnW) / 2, startY + i * (btnH + gap), btnW, btnH);
                GUI.backgroundColor = unlocked ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 1f, 1f, 0.4f);
                string stars = "";
                for (int k = 0; k < 3; k++) stars += (k < earnedStars) ? "★" : "☆";
                string lockIcon = unlocked ? "" : "🔒 ";
                string label = lockIcon + s.Title + "\n" + stars + "  목표: " + ColorLabel(s.GoalColor) + " " + s.GoalCount + "개 / " + s.MoveLimit + "턴";
                if (GUI.Button(rect, label, _stageBtn) && unlocked)
                {
                    StartStage(i);
                }
            }
            GUI.backgroundColor = Color.white;
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

            // 우상단 로비 버튼
            if (GUI.Button(new Rect(w - 88, topSafe + 10, 72, 32), "로비", _ghostBtn))
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

            // 하단 정보 행: 턴(좌) + 점수(우) — scoreBig 스타일로 성취감
            int infoY = topSafe + 88;
            GUI.Label(new Rect(16, infoY, 260, 34), "턴 " + _moves + " / " + _stage.MoveLimit, _body);
            GUI.Label(new Rect(w - 240 - 16, infoY - 4, 240, 40), _displayedScore.ToString("N0"), _scoreBig);

            // 캐스케이드 중이면 입력 잠금 인디케이터 (UX v3 #6)
            if (_inputLocked && _screen == ScreenState.Playing)
            {
                GUI.DrawTexture(new Rect(0, topSafe + panelH, w, Screen.height - topSafe - panelH), _dimOverlay);
                GUI.Label(new Rect(0, (Screen.height + topSafe + panelH) / 2 - 16, w, 32), "연쇄 처리 중…", _overlayBody);
            }

            // 하단 힌트 (safeArea 하단 여백 확보)
            float bottomSafeY = Screen.height - (sa.y + sa.height);
            int hintY = (int)(Screen.height - bottomSafeY - 32);
            GUI.Label(new Rect(0, hintY, w, 20), "인접 셀로 드래그 · 같은 2차색 3개 연결 시 폭발", _caption);
        }

        /// <summary>진행 바를 목표 색으로 지연 교체.</summary>
        private void EnsureBarFillFor(ColorId goal)
        {
            if (_lastBarColor == goal && _barFill != null) return;
            if (_barFill != null) Destroy(_barFill);
            _barFill = MakeSolidTexture(ColorToUnity(goal));
            _lastBarColor = goal;
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
