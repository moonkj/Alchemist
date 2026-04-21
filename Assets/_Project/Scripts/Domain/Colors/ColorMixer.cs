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

            // D26 (Wave3-R3): 새 원색이 추가되지 않는 혼합은 과포화 → Black.
            // - 예: Purple(R|B) + Red(R) → 새 원색 없음 → Black
            //      Purple + Purple → 같은 색 반복 → Black
            //      Orange + Green → 모든 원색 합 → White (기존 유지)
            // 예외: 순수 1-bit 동일 원색 간 혼합(Red+Red 등)은 no-op 으로 그 원색 유지.
            int aCnt = PopCount3(ba);
            int bCnt = PopCount3(bb);
            int combinedCnt = PopCount3(combined);
            int popMax = aCnt > bCnt ? aCnt : bCnt;

            if (combinedCnt == popMax)
            {
                if (ba == bb && aCnt == 1) return (ColorId)(combined & PrimaryMask);
                return ColorId.Black;
            }

            return (ColorId)(combined & PrimaryMask);
        }

        /// <summary>1/2/3차 색의 원색 비트(3비트 내) 개수. Primary=1, Secondary=2, White=3.</summary>
        private static int PopCount3(byte v)
        {
            v = (byte)(v & PrimaryMask);
            int c = 0;
            if ((v & (byte)ColorId.Red) != 0) c++;
            if ((v & (byte)ColorId.Yellow) != 0) c++;
            if ((v & (byte)ColorId.Blue) != 0) c++;
            return c;
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
