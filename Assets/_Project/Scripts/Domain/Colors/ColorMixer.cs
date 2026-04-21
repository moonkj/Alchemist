namespace Alchemist.Domain.Colors
{
    /// <summary>Deterministic color mixing rules (pure, GC-free).</summary>
    public static class ColorMixer
    {
        private const byte PrimaryMask = (byte)(ColorId.Red | ColorId.Yellow | ColorId.Blue);

        /// <summary>
        /// Mix two colors. Precedence (per Wave3 D16/D17):
        ///   None → None, Gray → None (inactive), Black → Black (propagate contamination),
        ///   Prism → wildcard pass-through, White oversaturation → Black, else primary OR.
        /// </summary>
        public static ColorId Mix(ColorId a, ColorId b)
        {
            byte ba = (byte)a;
            byte bb = (byte)b;

            if (ba == 0 || bb == 0) return ColorId.None;

            bool grayA = (ba & (byte)ColorId.Gray) != 0;
            bool grayB = (bb & (byte)ColorId.Gray) != 0;
            if (grayA || grayB) return ColorId.None;

            bool blackA = (ba & (byte)ColorId.Black) != 0;
            bool blackB = (bb & (byte)ColorId.Black) != 0;
            if (blackA || blackB) return ColorId.Black;

            bool prismA = a == ColorId.Prism;
            bool prismB = b == ColorId.Prism;
            if (prismA && prismB) return ColorId.Prism;
            if (prismA) return b;
            if (prismB) return a;

            byte combined = (byte)(ba | bb);

            if ((combined & ~PrimaryMask) != 0) return ColorId.None;

            if (ba == (byte)ColorId.White && bb == (byte)ColorId.White)
                return ColorId.Black;
            if (ba == (byte)ColorId.White && bb != (byte)ColorId.White)
                return ColorId.Black;
            if (bb == (byte)ColorId.White && ba != (byte)ColorId.White)
                return ColorId.Black;

            return (ColorId)(combined & PrimaryMask);
        }

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

        public static bool IsPrimary(ColorId c)
        {
            return c == ColorId.Red || c == ColorId.Yellow || c == ColorId.Blue;
        }

        public static bool IsSecondary(ColorId c)
        {
            return c == ColorId.Orange || c == ColorId.Green || c == ColorId.Purple;
        }

        public static bool IsTertiary(ColorId c)
        {
            return c == ColorId.White;
        }
    }
}
