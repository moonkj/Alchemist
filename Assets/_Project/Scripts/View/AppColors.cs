using UnityEngine;
using Alchemist.Domain.Colors;

namespace Alchemist.View
{
    /// <summary>
    /// Static palette mapping <see cref="ColorId"/> -> display <see cref="Color32"/>.
    /// Color32 is unmanaged (4 bytes) so lookups are zero-GC.
    /// Palette values come from UX ux_design.md §3.1 style spec:
    ///   Red #E63946, Yellow #F7D44C, Blue #4895EF,
    ///   Orange #F4A261, Green #52B788, Purple #9D4EDD,
    ///   White #F1FAEE, Black #1D1D1D, Prism #FFFFFF (rainbow overlay later),
    ///   Gray #6C757D.
    /// TryGet takes an out param to stay alloc-free; callers branch on the bool.
    /// </summary>
    public static class AppColors
    {
        // Centralized so re-skinning is a 1-file change.
        public static readonly Color32 Red    = new Color32(0xE6, 0x39, 0x46, 0xFF);
        public static readonly Color32 Yellow = new Color32(0xF7, 0xD4, 0x4C, 0xFF);
        public static readonly Color32 Blue   = new Color32(0x48, 0x95, 0xEF, 0xFF);
        public static readonly Color32 Orange = new Color32(0xF4, 0xA2, 0x61, 0xFF);
        public static readonly Color32 Green  = new Color32(0x52, 0xB7, 0x88, 0xFF);
        public static readonly Color32 Purple = new Color32(0x9D, 0x4E, 0xDD, 0xFF);
        public static readonly Color32 White  = new Color32(0xF1, 0xFA, 0xEE, 0xFF);
        public static readonly Color32 Black  = new Color32(0x1D, 0x1D, 0x1D, 0xFF);
        public static readonly Color32 Prism  = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color32 Gray   = new Color32(0x6C, 0x75, 0x7D, 0xFF);
        public static readonly Color32 Empty  = new Color32(0x00, 0x00, 0x00, 0x00);

        /// <summary>
        /// Look up a color. Returns false for <see cref="ColorId.None"/> / unmapped
        /// combinations (caller decides fallback — usually transparent).
        /// Switch on byte to avoid boxing / GetHashCode allocations.
        /// </summary>
        public static bool TryGet(ColorId id, out Color32 color)
        {
            switch (id)
            {
                case ColorId.Red:    color = Red;    return true;
                case ColorId.Yellow: color = Yellow; return true;
                case ColorId.Blue:   color = Blue;   return true;
                case ColorId.Orange: color = Orange; return true;
                case ColorId.Green:  color = Green;  return true;
                case ColorId.Purple: color = Purple; return true;
                case ColorId.White:  color = White;  return true;
                case ColorId.Black:  color = Black;  return true;
                case ColorId.Prism:  color = Prism;  return true;
                case ColorId.Gray:   color = Gray;   return true;
                default:             color = Empty;  return false;
            }
        }
    }
}
