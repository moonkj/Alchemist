using System;
using Alchemist.Domain.Colors;

namespace Alchemist.View
{
    /// <summary>Swap intent from hybrid drag/drop or tap-neighbor. Row/Col is the origin cell.</summary>
    public readonly struct SwapEvent
    {
        public readonly int FromRow;
        public readonly int FromCol;
        public readonly int ToRow;
        public readonly int ToCol;
        public SwapEvent(int fr, int fc, int tr, int tc) { FromRow = fr; FromCol = fc; ToRow = tr; ToCol = tc; }
    }

    /// <summary>Single tap at a board cell (short press, no drag).</summary>
    public readonly struct TapEvent
    {
        public readonly int Row;
        public readonly int Col;
        public TapEvent(int row, int col) { Row = row; Col = col; }
    }

    /// <summary>Palette slot selection / color pick.</summary>
    public readonly struct PaletteSelectEvent
    {
        public readonly int SlotIndex;
        public readonly ColorId Color;
        public PaletteSelectEvent(int slotIndex, ColorId color) { SlotIndex = slotIndex; Color = color; }
    }

    /// <summary>
    /// Input event surface consumed by GameLogic / Services. Phase 1 uses Action delegates
    /// (no MessagePipe yet — Wave 1 addendum C5). Subscribers attach once; Invoke passes
    /// readonly structs so publication is alloc-free.
    /// </summary>
    public interface IInputEventBus
    {
        event Action<SwapEvent> OnSwap;
        event Action<TapEvent> OnTap;
        event Action<PaletteSelectEvent> OnPaletteSelect;
    }
}
