using UnityEngine;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Chain;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// TMP/Prefab 없이 즉시 플레이어블한 최소 보드.
    /// 6×7 컬러 블록을 프로시저얼로 생성하고 탭 시 반응.
    /// WHY: BoardView/BlockView 의 prefab 의존성을 우회하여 첫 빌드에서부터
    ///      실제 게임 보드를 iPhone 에 바로 보여주기 위함.
    /// </summary>
    public sealed class MinimalGameScene : MonoBehaviour
    {
        private const int Rows = 7;
        private const int Cols = 6;
        private const float CellSize = 0.9f;
        private const float Gap = 0.06f;

        private SpriteRenderer[,] _blocks;
        private ColorId[,] _colorGrid;
        private Sprite _squareSprite;
        private DeterministicBlockSpawner _spawner;

        private GUIStyle _title;
        private GUIStyle _body;

        private void Start()
        {
            Application.targetFrameRate = 60;
            _squareSprite = BuildSquareSprite();
            _spawner = new DeterministicBlockSpawner(42);
            _blocks = new SpriteRenderer[Rows, Cols];
            _colorGrid = new ColorId[Rows, Cols];
            BuildGrid();
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
                    go.transform.position = new Vector3(
                        originX + c * (CellSize + Gap),
                        originY - r * (CellSize + Gap),
                        0f);
                    go.transform.localScale = Vector3.one * CellSize;
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _squareSprite;
                    sr.color = ColorToUnity(block.Color);
                    _blocks[r, c] = sr;
                }
            }
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
                case ColorId.White:  return new Color(0.95f, 0.95f, 0.93f);
                case ColorId.Black:  return new Color(0.12f, 0.12f, 0.14f);
                case ColorId.Gray:   return new Color(0.42f, 0.44f, 0.48f);
                default:             return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private static Sprite BuildSquareSprite()
        {
            const int sz = 64;
            var tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
            var px = new Color32[sz * sz];
            for (int y = 0; y < sz; y++)
            {
                for (int x = 0; x < sz; x++)
                {
                    bool edge = x < 2 || y < 2 || x >= sz - 2 || y >= sz - 2;
                    px[y * sz + x] = edge ? new Color32(0, 0, 0, 60) : new Color32(255, 255, 255, 255);
                }
            }
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
        }

        private void Update()
        {
            bool touched = false;
            Vector2 screenPos = default;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0)) { touched = true; screenPos = Input.mousePosition; }
#endif
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) { touched = true; screenPos = t.position; }
            }

            if (touched)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector2 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
                    PulseNearest(world);
                }
            }

            // Scale decay
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var tr = _blocks[r, c].transform;
                    tr.localScale = Vector3.Lerp(tr.localScale, Vector3.one * CellSize, Time.deltaTime * 8f);
                }
            }
        }

        private void PulseNearest(Vector2 world)
        {
            int hitR = -1, hitC = -1;
            float bestDist = float.MaxValue;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    Vector2 p = _blocks[r, c].transform.position;
                    float d = Vector2.Distance(p, world);
                    if (d < bestDist && d < CellSize)
                    {
                        bestDist = d;
                        hitR = r;
                        hitC = c;
                    }
                }
            }
            if (hitR < 0) return;
            _blocks[hitR, hitC].transform.localScale = Vector3.one * (CellSize * 1.35f);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) },
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.75f, 0.78f, 0.85f, 1f) },
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            GUI.Label(new Rect(0, 40, Screen.width, 44), "Color Mix: Alchemist", _title);
            GUI.Label(new Rect(0, Screen.height - 60, Screen.width, 24), "탭하면 블록이 반응합니다 · v1.0.0 preview", _body);
        }
    }
}
