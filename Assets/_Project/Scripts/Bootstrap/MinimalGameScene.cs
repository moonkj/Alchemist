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
        private GUIStyle _title, _hud, _body, _goalLabel, _overlayTitle, _overlayBody, _button, _stageBtn;

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
            if (src == ColorId.None || dst == ColorId.None) { ShowToast("빈 칸"); return; }
            ColorId mixed = ColorMixer.Mix(src, dst);
            if (mixed == ColorId.None) { ShowToast("혼합 불가"); return; }

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
                yield return StartCoroutine(ExplodeAnim(hits));
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
                ShowToast(depth > 1 ? ("연쇄 " + depth + "!  +" + totalScored) : ("폭발! +" + totalScored));
            }
            else if (mixedColor != ColorId.None)
            {
                ShowToast(ColorLabel(mixedColor));
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
                ShowToast("STAGE CLEAR! +" + (200 * _stars));
                PlaySfx(_sfxClear);
                _inputLocked = true;
                StartCoroutine(EndStageAfter(0.8f));
            }
            else if (_moves >= _stage.MoveLimit)
            {
                _stageCleared = false;
                ShowToast("STAGE FAILED");
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

        private IEnumerator ExplodeAnim(List<RC> cells)
        {
            TriggerExplosionFeedback(cells);
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
        private void TriggerExplosionFeedback(List<RC> cells)
        {
#if UNITY_IOS || UNITY_ANDROID
            try { Handheld.Vibrate(); } catch { }
#endif
            float mag = Mathf.Min(0.22f, 0.05f + cells.Count * 0.012f);
            StartCoroutine(ScreenShake(0.30f, mag));
            for (int i = 0; i < cells.Count; i++)
                SpawnPaintSplash(_basePos[cells[i].r, cells[i].c], _blocks[cells[i].r, cells[i].c].color, 14, 0.5f);
            PlaySfx(_sfxExplode);
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
        private void ShowToast(string msg) { _toast = msg; _toastUntil = Time.time + 1.2f; }

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
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) } };
            _hud = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.90f, 0.92f, 0.95f, 1f) } };
            _goalLabel = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) } };
            _body = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.70f, 0.73f, 0.80f, 1f) } };
            _overlayTitle = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) } };
            _overlayBody = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.90f, 0.92f, 0.95f, 1f) } };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold };
            _stageBtn = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            _panelBg = MakeSolidTexture(new Color(0.08f, 0.09f, 0.12f, 0.92f));
            _barBg = MakeSolidTexture(new Color(0.20f, 0.22f, 0.28f, 1f));
            _barFill = MakeSolidTexture(new Color(0.62f, 0.31f, 0.87f, 1f));
            _overlayTex = MakeSolidTexture(new Color(0f, 0f, 0f, 0.82f));
            _stageBtnBg = MakeSolidTexture(new Color(0.18f, 0.20f, 0.28f, 1f));
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
            int w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, 80, w, 70), "Color Mix: Alchemist", _overlayTitle);
            GUI.Label(new Rect(0, 150, w, 26), "색을 섞고 폭발시켜 세상을 복원하라", _overlayBody);

            int btnW = Mathf.Min(w - 60, 420);
            int btnH = 80;
            int startY = (int)(h * 0.30f);
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
            int w = Screen.width; int topSafe = 50; int panelH = 120;
            GUI.DrawTexture(new Rect(0, topSafe, w, panelH), _panelBg);
            GUI.Label(new Rect(20, topSafe + 8, w - 160, 28), _stage.Title, _title);

            // 좌상단 뒤로가기
            if (GUI.Button(new Rect(w - 90, topSafe + 10, 70, 30), "로비"))
            {
                ExitToLobby();
            }

            int barX = 20, barW = w - 40, barH = 22, barY = topSafe + 46;
            GUI.DrawTexture(new Rect(barX, barY, barW, barH), _barBg);
            float prog = Mathf.Clamp01((float)_goalProgress / _stage.GoalCount);
            GUI.DrawTexture(new Rect(barX, barY, (int)(barW * prog), barH), _barFill);
            string goalText = ColorLabel(_stage.GoalColor) + " " + _goalProgress + " / " + _stage.GoalCount;
            GUI.Label(new Rect(barX, barY - 2, barW, barH + 4), goalText, _goalLabel);

            int infoY = topSafe + 84;
            GUI.Label(new Rect(20, infoY, 200, 24), "턴 " + _moves + " / " + _stage.MoveLimit, _hud);
            GUI.Label(new Rect(w - 220, infoY, 200, 24), "점수 " + _score, _hud);

            GUI.Label(new Rect(0, Screen.height - 50, w, 22),
                "인접 셀로 드래그 · 같은 2차색 3개 연결 시 폭발",
                _body);
        }

        private void DrawToast()
        {
            if (Time.time >= _toastUntil || string.IsNullOrEmpty(_toast)) return;
            int w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, h - 140, w, 40), _toast, _overlayTitle);
        }

        private void DrawResult()
        {
            int w = Screen.width, h = Screen.height;
            GUI.DrawTexture(new Rect(0, 0, w, h), _overlayTex);
            string title = _stageCleared ? "STAGE CLEAR" : "STAGE FAILED";
            GUI.Label(new Rect(0, h / 2 - 200, w, 64), title, _overlayTitle);
            if (_stageCleared)
            {
                string stars = "";
                for (int i = 0; i < 3; i++) stars += (i < _stars) ? "★ " : "☆ ";
                GUI.Label(new Rect(0, h / 2 - 130, w, 60), stars, _overlayTitle);
            }
            GUI.Label(new Rect(0, h / 2 - 50, w, 34), "점수 " + _score, _overlayBody);
            GUI.Label(new Rect(0, h / 2 - 10, w, 28), "턴 " + _moves + " / " + _stage.MoveLimit + " · 최대연쇄 " + _maxChainDepth, _overlayBody);

            int btnW = 240, btnH = 60, gap = 16;
            bool hasNext = _stageCleared && _stageIdx < Stages.Length - 1;
            int btnsCount = 2;
            int totalW = btnsCount * btnW + (btnsCount - 1) * gap;
            int x0 = (w - totalW) / 2;
            int y = h / 2 + 60;

            if (GUI.Button(new Rect(x0, y, btnW, btnH), _stageCleared ? "재도전" : "다시", _button))
            {
                StartStage(_stageIdx);
            }
            string nextLabel = hasNext ? "다음 스테이지 ▶" : "로비로";
            if (GUI.Button(new Rect(x0 + btnW + gap, y, btnW, btnH), nextLabel, _button))
            {
                if (hasNext) StartStage(_stageIdx + 1);
                else ExitToLobby();
            }
        }
    }
}
