namespace Alchemist.Domain.Blocks
{
    public enum BlockState : byte
    {
        Spawned = 0,
        Idle = 1,
        Selected = 2,
        Merging = 3,
        Exploding = 4,
        Cleared = 5,
        Infecting = 6,
        Infected = 7,
        Absorbed = 8,
        Gray = 9,
        FilterTransit = 10,
        PrismCharging = 11,
        Count = 12,
    }
}
