namespace Alchemist.Domain.Prompts
{
    /// <summary>
    /// Reward granted on prompt completion.
    /// TODO(Phase 3): finalize reward structure — coins/badges/items are placeholders.
    /// Field semantics and currency/inventory wiring are deferred until economy design locks.
    /// </summary>
    public readonly struct PromptReward
    {
        public readonly int Coins;
        public readonly string Badge;  // TODO Phase 3: swap to BadgeId enum/struct
        public readonly string ItemId; // TODO Phase 3: swap to strongly typed item handle

        public PromptReward(int coins, string badge = null, string itemId = null)
        {
            Coins = coins;
            Badge = badge;
            ItemId = itemId;
        }

        public static readonly PromptReward None = new PromptReward(0, null, null);
    }
}
