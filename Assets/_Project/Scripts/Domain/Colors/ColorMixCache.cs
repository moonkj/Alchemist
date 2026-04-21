namespace Alchemist.Domain.Colors
{
    /// <summary>Precomputed 256x256 byte table for O(1) branchless Mix lookup (Perf §2.4).</summary>
    public static class ColorMixCache
    {
        // Flat 65,536-byte array beats 2D jagged; single heap alloc at static init.
        // Index = (min<<8) | max so commutativity halves the effective working set.
        private static readonly byte[] Table;

        static ColorMixCache()
        {
            Table = new byte[256 * 256];
            Initialize();
        }

        /// <summary>Idempotent: (re)populate the full lookup table.</summary>
        public static void Initialize()
        {
            for (int a = 0; a < 256; a++)
            {
                for (int b = a; b < 256; b++)
                {
                    byte result = (byte)ColorMixer.Mix((ColorId)a, (ColorId)b);
                    int idx = (a << 8) | b;
                    Table[idx] = result;
                }
            }
        }

        /// <summary>O(1) table lookup of Mix(a,b); zero allocations.</summary>
        public static ColorId Lookup(ColorId a, ColorId b)
        {
            byte ba = (byte)a;
            byte bb = (byte)b;
            // Normalize to (min,max) to exploit commutative storage.
            if (ba > bb)
            {
                byte tmp = ba;
                ba = bb;
                bb = tmp;
            }
            return (ColorId)Table[(ba << 8) | bb];
        }
    }
}
