using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Chain;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// TMP/Prefab 없이 즉시 플레이어블한 최소 색-조합 매치-3 보드.
    /// - 6×7 보드 프로시저얼 생성
    /// - Drag-to-Mix: 블록을 인접 셀에 드롭하면 ColorMixer.Mix 결과로 합성
    /// - 2차+ 색 3연결 시 폭발 + 중력 + 리필
    /// - 점수/턴 수 OnGUI 표시
    /// </summary>
    public sealed class MinimalGameScene : MonoBehaviour
    {
        private const int Rows = 7;
        private const int Cols = 6;
        private const float CellSize = 0.9f;
        private const float Gap = 0.06f;
        private const int Seed = 42;

        private SpriteRenderer[,] _blocks;
        private ColorId[,] _colorGrid;
        private Vector3[,] _basePos;
        private Sprite _squareSprite;
        private DeterministicBlockSpawner _spawner;

        // Drag state
        private int _dragR = -1, _dragC = -1;
        private Vector3 _dragOrigWorld;
        private bool _inputLocked;

        // Gameplay state
        private int _score;
        private int _moves;
        private int _maxChainDepth;
        private string _toast = "";
        private float _toastUntil;

        private GUIStyle _title, _hud, _body;

        private void Start()
        {
            Application.targetFrameRate = 60;
            _squareSprite = BuildSquareSprite();
            _spawner = new DeterministicBlockSpawner(Seed);
            _blocks = new SpriteRenderer[Rows, Cols];
            _colorGrid = new ColorId[Rows, Cols];
            _basePos = new Vector3[Rows, Cols];
            BuildGrid();
            FitCameraToBoard();
        }

        private void BuildGrid()
        {
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

                    var go = new GameObject("Block_" + r + "_" + c);
                    go.transform.parent = transform;
                    var pos = new Vector3(
                        originX + c * (CellSize + Gap),
                        originY - r * (CellSize + Gap),
                        0f);
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * CellSize;
                    _basePos[r, c] = pos;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _squareSprite;
                    sr.color = ColorToUnity(block.Color);
                    _blocks[r, c] = sr;
                }
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

        private static Sprite BuildSquareSprite()
        {
            const int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var px = new Color32[sz * sz];
            for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                bool edge = x < 2 || y < 2 || x >= sz - 2 || y >= sz - 2;
                px[y * sz + x] = edge ? new Color32(0, 0, 0, 80) : new Color32(255, 255, 255, 255);
            }
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        }

        private void Update()
        {
            UpdateScaleDecay();

            if (_inputLocked) return;

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

            if (began) OnDragBegin(world);
            else if (moved) OnDragMove(world);
            else if (ended) OnDragEnd(world);
        }

        private void OnDragBegin(Vector3 world)
        {
            if (!FindCell(world, out int r, out int c)) return;
            if (_colorGrid[r, c] == ColorId.None) return;
            _dragR = r;
            _dragC = c;
            _dragOrigWorld = _basePos[r, c];
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

            if (!FindCell(world, out int tr, out int tc) || (tr == sr && tc == sc))
            {
                SnapBack(sr, sc);
                return;
            }
            if (!IsAdjacent(sr, sc, tr, tc))
            {
                SnapBack(sr, sc);
                return;
            }
            SnapBack(sr, sc);
            TryMix(sr, sc, tr, tc);
        }

        private void SnapBack(int r, int c)
        {
            _blocks[r, c].transform.position = _basePos[r, c];
            _blocks[r, c].transform.localScale = Vector3.one * CellSize;
        }

        private bool IsAdjacent(int r1, int c1, int r2, int c2)
        {
            return (Mathf.Abs(r1 - r2) == 1 && c1 == c2) || (Mathf.Abs(c1 - c2) == 1 && r1 == r2);
        }

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

        private void TryMix(int sr, int sc, int tr, int tc)
        {
            ColorId src = _colorGrid[sr, sc];
            ColorId dst = _colorGrid[tr, tc];
            if (src == ColorId.None || dst == ColorId.None)
            {
                ShowToast("빈 칸은 섞을 수 없음");
                return;
            }
            ColorId mixed = ColorMixer.Mix(src, dst);
            if (mixed == ColorId.None)
            {
                ShowToast("혼합 불가");
                return;
            }

            _colorGrid[tr, tc] = mixed;
            _colorGrid[sr, sc] = ColorId.None;
            _blocks[tr, tc].color = ColorToUnity(mixed);
            _blocks[sr, sc].color = ColorToUnity(ColorId.None);
            _blocks[tr, tc].transform.localScale = Vector3.one * (CellSize * 1.35f);

            _moves++;
            StartCoroutine(ResolveCascadeCoroutine(mixed));
        }

        /// <summary>
        /// 폭발 애니메이션(확대+페이드) 후 중력/리필을 단계별 지연으로 실행.
        /// 각 Step 사이 지연으로 "짧은 깜빡임" 이 아닌 체감 가능한 Juice 제공.
        /// </summary>
        private IEnumerator ResolveCascadeCoroutine(ColorId mixedColor)
        {
            _inputLocked = true;
            int totalScored = 0;
            int depth = 0;

            // 믹스로 생긴 원본 셀의 None 은 매치 여부와 무관하게 낙하/리필로 채움.
            // WHY: 유저 피드백 '빈칸은 낙하가 안됨. 폭발해야 채워짐' — 믹스만 해도 빈칸이 처리되도록.
            yield return new WaitForSeconds(0.10f);
            ApplyGravity();
            yield return new WaitForSeconds(0.22f);
            Refill();
            yield return new WaitForSeconds(0.26f);

            while (true)
            {
                var hits = DetectMatches();
                if (hits.Count == 0) break;
                depth++;

                // 1) 폭발 애니: 매치된 셀을 1.5배로 확대하며 알파 페이드.
                yield return StartCoroutine(ExplodeAnim(hits));

                // 2) 셀 클리어 + 점수 적립
                foreach (var rc in hits)
                {
                    var col = _colorGrid[rc.r, rc.c];
                    totalScored += ScoreFor(col);
                    _colorGrid[rc.r, rc.c] = ColorId.None;
                    _blocks[rc.r, rc.c].color = ColorToUnity(ColorId.None);
                    var sr = _blocks[rc.r, rc.c];
                    var col2 = sr.color; col2.a = 1f; sr.color = col2;
                    sr.transform.localScale = Vector3.one * CellSize;
                }
                yield return new WaitForSeconds(0.08f);

                // 3) 중력
                ApplyGravity();
                yield return new WaitForSeconds(0.22f);

                // 4) 리필
                Refill();
                yield return new WaitForSeconds(0.26f);

                if (depth >= 10) break;
            }

            if (depth > _maxChainDepth) _maxChainDepth = depth;
            if (totalScored > 0)
            {
                _score += totalScored + Mathf.Max(0, (depth - 1)) * 50;
                ShowToast(depth > 1 ? ("연쇄 " + depth + "!  +" + totalScored) : ("폭발! +" + totalScored));
            }
            else
            {
                ShowToast(ColorLabel(mixedColor));
            }

            _inputLocked = false;
        }

        private struct RC { public int r, c; public RC(int a, int b) { r = a; c = b; } }

        private List<RC> DetectMatches()
        {
            var hits = new List<RC>();
            bool[,] hit = new bool[Rows, Cols];

            for (int r = 0; r < Rows; r++)
            {
                int runStart = 0;
                for (int c = 1; c <= Cols; c++)
                {
                    bool endRun = c == Cols || _colorGrid[r, c] != _colorGrid[r, runStart];
                    if (endRun)
                    {
                        int len = c - runStart;
                        ColorId col = _colorGrid[r, runStart];
                        if (len >= 3 && IsMatchable(col))
                        {
                            for (int k = runStart; k < c; k++) hit[r, k] = true;
                        }
                        runStart = c;
                    }
                }
            }
            for (int c = 0; c < Cols; c++)
            {
                int runStart = 0;
                for (int r = 1; r <= Rows; r++)
                {
                    bool endRun = r == Rows || _colorGrid[r, c] != _colorGrid[runStart, c];
                    if (endRun)
                    {
                        int len = r - runStart;
                        ColorId col = _colorGrid[runStart, c];
                        if (len >= 3 && IsMatchable(col))
                        {
                            for (int k = runStart; k < r; k++) hit[k, c] = true;
                        }
                        runStart = r;
                    }
                }
            }

            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (hit[r, c]) hits.Add(new RC(r, c));
            return hits;
        }

        private IEnumerator ExplodeAnim(List<RC> cells)
        {
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
                    var sr = _blocks[rc.r, rc.c];
                    sr.transform.localScale = Vector3.one * scale;
                    var col = sr.color; col.a = alpha; sr.color = col;
                }
                t += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>Legacy (직접 ResolveMatches 대신 DetectMatches + Coroutine 로 교체됨).</summary>
        private bool ResolveMatches(ref int scored)
        {
            bool[,] hit = new bool[Rows, Cols];
            bool any = false;

            // horizontal
            for (int r = 0; r < Rows; r++)
            {
                int runStart = 0;
                for (int c = 1; c <= Cols; c++)
                {
                    bool endRun = c == Cols || _colorGrid[r, c] != _colorGrid[r, runStart];
                    if (endRun)
                    {
                        int len = c - runStart;
                        ColorId col = _colorGrid[r, runStart];
                        if (len >= 3 && IsMatchable(col))
                        {
                            for (int k = runStart; k < c; k++) hit[r, k] = true;
                            any = true;
                        }
                        runStart = c;
                    }
                }
            }
            // vertical
            for (int c = 0; c < Cols; c++)
            {
                int runStart = 0;
                for (int r = 1; r <= Rows; r++)
                {
                    bool endRun = r == Rows || _colorGrid[r, c] != _colorGrid[runStart, c];
                    if (endRun)
                    {
                        int len = r - runStart;
                        ColorId col = _colorGrid[runStart, c];
                        if (len >= 3 && IsMatchable(col))
                        {
                            for (int k = runStart; k < r; k++) hit[k, c] = true;
                            any = true;
                        }
                        runStart = r;
                    }
                }
            }

            if (!any) return false;

            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (!hit[r, c]) continue;
                ColorId col = _colorGrid[r, c];
                scored += ScoreFor(col);
                _colorGrid[r, c] = ColorId.None;
                _blocks[r, c].color = ColorToUnity(ColorId.None);
                _blocks[r, c].transform.localScale = Vector3.one * (CellSize * 1.3f);
            }
            return true;
        }

        private static bool IsMatchable(ColorId c)
        {
            // 2차 이상만 매치
            return c == ColorId.Orange || c == ColorId.Green || c == ColorId.Purple || c == ColorId.White;
        }

        private static int ScoreFor(ColorId c)
        {
            if (c == ColorId.White) return 100;
            if (c == ColorId.Orange || c == ColorId.Green || c == ColorId.Purple) return 30;
            if (c == ColorId.Black) return -20;
            return 10;
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
                        // 낙하 애니메이션: writeRow 블록을 출발(r 위치)에 순간이동시키고
                        // UpdateScaleDecay 의 lerp 가 자신의 _basePos[writeRow] 로 끌어내리도록 한다.
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
                // 상단 바깥에서 등장하는 낙하 효과: 보드 상단 위쪽 r+1칸 거리에서 시작.
                float spawnY = _basePos[0, c].y + (CellSize + Gap) * (r + 1);
                _blocks[r, c].transform.position = new Vector3(_basePos[r, c].x, spawnY, 0f);
                _blocks[r, c].transform.localScale = Vector3.one * (CellSize * 0.9f);
            }
        }

        private void UpdateScaleDecay()
        {
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_dragR == r && _dragC == c) continue;
                var tr = _blocks[r, c].transform;
                tr.localScale = Vector3.Lerp(tr.localScale, Vector3.one * CellSize, Time.deltaTime * 8f);
                tr.position = Vector3.Lerp(tr.position, _basePos[r, c], Time.deltaTime * 12f);
            }
        }

        private void ShowToast(string msg)
        {
            _toast = msg;
            _toastUntil = Time.time + 1.2f;
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

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) },
            };
            _hud = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.90f, 0.92f, 0.95f, 1f) },
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.78f, 0.85f, 1f) },
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.Label(new Rect(0, 30, Screen.width, 36), "Color Mix: Alchemist", _title);
            GUI.Label(new Rect(20, 70, 400, 28), "점수 " + _score, _hud);
            GUI.Label(new Rect(Screen.width - 220, 70, 200, 28), "턴 " + _moves + " · 최대연쇄 " + _maxChainDepth, _hud);

            if (Time.time < _toastUntil && !string.IsNullOrEmpty(_toast))
            {
                GUI.Label(new Rect(0, Screen.height - 140, Screen.width, 40), _toast, _title);
            }

            GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 24),
                "블록을 인접 셀로 드래그해 섞으세요 · 2차색 3연결 시 폭발",
                _body);
        }
    }
}
