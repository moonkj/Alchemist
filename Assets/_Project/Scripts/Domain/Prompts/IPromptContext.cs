using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Prompts
{
    /// <summary>
    /// Read-only view of game state consumed by prompt conditions.
    /// Implemented by the runtime GameContext (Services layer); the domain
    /// depends only on this surface so conditions are unit-testable in isolation.
    /// </summary>
    public interface IPromptContext
    {
        /// <summary>Accumulated count of blocks/items of the given color created this stage.</summary>
        int GetColorsCreated(ColorId color);

        /// <summary>Highest chain depth observed so far in this stage.</summary>
        int MaxChainDepthAchieved { get; }

        /// <summary>Number of chain events whose depth is greater or equal to <paramref name="minDepth"/>.</summary>
        int ChainEventsCount(int minDepth);

        /// <summary>Moves consumed so far this stage.</summary>
        int MovesUsed { get; }

        /// <summary>Stage-wide hard cap on moves. Zero or negative means uncapped.</summary>
        int MovesLimit { get; }
    }
}
