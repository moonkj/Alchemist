using System;

namespace Alchemist.Domain.Colors
{
    /// <summary>Bitflag color identity (primary bits OR to form secondary/tertiary).</summary>
    [Flags]
    public enum ColorId : byte
    {
        None   = 0,
        Red    = 1 << 0,
        Yellow = 1 << 1,
        Blue   = 1 << 2,
        Orange = Red | Yellow,
        Green  = Yellow | Blue,
        Purple = Red | Blue,
        White  = Red | Yellow | Blue,
        Black  = 1 << 3,
        Prism  = 1 << 4,
        Gray   = 1 << 5,
    }
}
