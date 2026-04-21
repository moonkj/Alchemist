using System;
using UnityEngine;
using Alchemist.Domain.Colors;

namespace Alchemist.View
{
    /// <summary>
    /// Hybrid drag/drop + swap input per UX §4.1.
    ///   - Down -> lookup origin cell. Track pointer delta.
    ///   - If release within 16px of origin AND cell pointing to a 4-neighbor axis -> Swap event.
    ///     (Tap without direction = Tap event.)
    ///   - If release beyond 16px -> Tap-drop at target cell (palette handled by caller).
    /// Phase 1 supports both touch (Input.GetTouch(0)) and mouse (editor convenience).
    /// GC budget: Update reads UnityEngine static input; event Invoke is struct-only.
    /// Subscribers are no-op if null (logging happens only in editor builds).
    /// </summary>
    public sealed class InputController : MonoBehaviour, IInputEventBus
    {
        public event Action<SwapEvent> OnSwap;
        public event Action<TapEvent> OnTap;
        public event Action<PaletteSelectEvent> OnPaletteSelect;

        [SerializeField] private Camera _camera;
        [SerializeField] private BoardView _board;
        [SerializeField] private float _swapThresholdPx = 16f;

        private bool _pressed;
        private Vector2 _downScreen;
        private int _downRow = -1;
        private int _downCol = -1;

        private void Awake()
        {
            if (_camera == null) _camera = Camera.main;
        }

        private void Update()
        {
            // Priority: touch when available, else mouse fallback.
            if (Input.touchCount > 0)
            {
                HandleTouch(Input.GetTouch(0));
                return;
            }

            if (Input.GetMouseButtonDown(0)) BeginPress(Input.mousePosition);
            else if (_pressed && Input.GetMouseButtonUp(0)) EndPress(Input.mousePosition);
        }

        private void HandleTouch(Touch t)
        {
            switch (t.phase)
            {
                case TouchPhase.Began:    BeginPress(t.position); break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled: EndPress(t.position);   break;
                // Moved / Stationary: no-op Phase 1 (preview toast is Phase 4).
            }
        }

        private void BeginPress(Vector2 screen)
        {
            _pressed = true;
            _downScreen = screen;
            _downRow = -1; _downCol = -1;

            if (_board == null || _camera == null) return;
            Vector2 world = _camera.ScreenToWorldPoint(screen);
            // World is relative to camera; BoardView's transform origin is the board center.
            Vector2 boardLocal = world - (Vector2)_board.transform.position;
            if (GridCoordinateMapper.WorldToGrid(boardLocal, out int r, out int c))
            {
                _downRow = r; _downCol = c;
            }
        }

        private void EndPress(Vector2 screen)
        {
            if (!_pressed) return;
            _pressed = false;

            if (_downRow < 0) return; // press started outside board — drop

            Vector2 delta = screen - _downScreen;
            float dist = delta.magnitude;

            if (dist <= _swapThresholdPx)
            {
                // Treat near-zero drag as Tap on origin.
                OnTap?.Invoke(new TapEvent(_downRow, _downCol));
                return;
            }

            // Direction-based adjacency swap — choose dominant axis.
            int dr = 0, dc = 0;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) dc = delta.x > 0 ?  1 : -1;
            else                                        dr = delta.y > 0 ? -1 :  1; // y+ screen = row- (up on board)

            int tr = _downRow + dr;
            int tc = _downCol + dc;
            if (tr < 0 || tr >= GridCoordinateMapper.Rows || tc < 0 || tc >= GridCoordinateMapper.Cols)
            {
                // Out-of-board release -> drop as tap on origin (Phase 1 simplification).
                OnTap?.Invoke(new TapEvent(_downRow, _downCol));
                return;
            }
            OnSwap?.Invoke(new SwapEvent(_downRow, _downCol, tr, tc));
        }

        /// <summary>External path — palette UI calls this on slot tap/selection.</summary>
        public void PublishPaletteSelect(int slotIndex, ColorId color)
        {
            OnPaletteSelect?.Invoke(new PaletteSelectEvent(slotIndex, color));
        }
    }
}
