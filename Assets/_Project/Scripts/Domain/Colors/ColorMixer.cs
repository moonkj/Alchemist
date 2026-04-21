namespace Alchemist.Domain.Colors
{
    /// <summary>Deterministic color mixing rules (pure, GC-free).</summary>
    public static class ColorMixer
    {
        // Mask of the three primary bits (R|Y|B = 0b0000_0111).
        private const byte PrimaryMask = (byte)(ColorId.Red | ColorId.Yellow | ColorId.Blue);
        private const byte SpecialMask = (byte)(ColorId.Black | ColorId.Prism | ColorId.Gray);

        /// <summary>Mix two colors; returns None/Black on invalid/oversaturated input.</summary>
        public static ColorId Mix(ColorId a, ColorId b)
        {
            // Prism is wildcard: preserve the other side (Prism+Prism => Prism).
            if (a == ColorId.Prism) return b;
            if (b == ColorId.Prism) return a;

            byte ba = (byte)a;
            byte bb = (byte)b;

            // None is a no-op sink: mixing with nothing yields nothing useful.
            if (ba == 0 || bb == 0) return ColorId.None;

            // Gray (absorbed) and Black (contaminated) cannot participate in normal mixes.
            if (((ba | bb) & SpecialMask) != 0) return ColorId.None;

            byte combined = (byte)(ba | bb);
            byte primaryBits = (byte)(combined & PrimaryMask);

            // Any non-primary bits leaking through means invalid recipe.
            if ((combined & ~PrimaryMask) != 0) return ColorId.None;

            // Oversaturation: if both operands already contain White's full set,
            // or combined equals White but either operand alone was White plus extra, contaminate.
            // White + any primary = Black (already-complete palette + more pigment).
            if (ba == (byte)ColorId.White && (bb & PrimaryMask) != 0 && bb != (byte)ColorId.White)
                return ColorId.Black;
            if (bb == (byte)ColorId.White && (ba & PrimaryMask) != 0 && ba != (byte)ColorId.White)
                return ColorId.Black;
            // White + White = Black as well (double-saturated).
            if (ba == (byte)ColorId.White && bb == (byte)ColorId.White)
                return ColorId.Black;

            return (ColorId)primaryBits;
        }

        /// <summary>True if color is one of the 7 valid recipe outputs (1/2/3차).</summary>
        public static bool IsValidRecipe(ColorId c)
        {
            switch (c)
            {
                case ColorId.Red:
                case ColorId.Yellow:
                case ColorId.Blue:
                case ColorId.Orange:
                case ColorId.Green:
                case ColorId.Purple:
                case ColorId.White:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>True if color is a primary (Red/Yellow/Blue).</summary>
        public static bool IsPrimary(ColorId c)
        {
            return c == ColorId.Red || c == ColorId.Yellow || c == ColorId.Blue;
        }

        /// <summary>True if color is a secondary (Orange/Green/Purple).</summary>
        public static bool IsSecondary(ColorId c)
        {
            return c == ColorId.Orange || c == ColorId.Green || c == ColorId.Purple;
        }

        /// <summary>True if color is a tertiary (White only in this palette).</summary>
        public static bool IsTertiary(ColorId c)
        {
            return c == ColorId.White;
        }
    }
}
