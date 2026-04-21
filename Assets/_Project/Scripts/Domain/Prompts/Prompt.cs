using Alchemist.Domain.Colors;
using Alchemist.Domain.Prompts.Conditions;

namespace Alchemist.Domain.Prompts
{
    /// <summary>
    /// A single stage prompt: identity, localized title key, goal composition,
    /// stage move cap, and (TODO Phase 3) reward payload.
    /// Pure data container — evaluation lives on <see cref="PromptGoal"/>.
    /// </summary>
    public sealed class Prompt
    {
        /// <summary>Stable identifier (e.g. "p1.purple10").</summary>
        public readonly string Id;

        /// <summary>
        /// Localization key for the display title (e.g. "prompt.create_purple_10").
        /// Translation lookup is a Services-layer concern; domain only stores the key.
        /// </summary>
        public readonly string LocalizedTitleKey;

        public readonly PromptGoal Goal;

        /// <summary>Stage-level move cap. Zero or negative means uncapped.</summary>
        public readonly int MoveLimit;

        /// <summary>Reward struct. TODO(Phase 3): structure is placeholder.</summary>
        public readonly PromptReward Reward;

        public Prompt(string id, string localizedTitleKey, PromptGoal goal, int moveLimit, PromptReward reward)
        {
            Id = id;
            LocalizedTitleKey = localizedTitleKey;
            Goal = goal;
            MoveLimit = moveLimit;
            Reward = reward;
        }

        // ---------------------------------------------------------------------
        // Phase 1 sample prompts (data-test fixtures; NOT production content).
        // ---------------------------------------------------------------------

        /// <summary>Sample: create 10 purple blocks within 15 moves.</summary>
        public static readonly Prompt SamplePurple10 = new Prompt(
            id: "p1.sample.purple10",
            localizedTitleKey: "prompt.create_purple_10",
            goal: new PromptGoal(
                all: new IPromptCondition[]
                {
                    new CreateColorCondition(ColorId.Purple, 10),
                    new MoveLimitCondition(15),
                }),
            moveLimit: 15,
            reward: PromptReward.None);

        /// <summary>Sample: trigger a depth-2+ chain three times (no move cap).</summary>
        public static readonly Prompt SampleChain3 = new Prompt(
            id: "p1.sample.chain3",
            localizedTitleKey: "prompt.chain_depth2_x3",
            goal: new PromptGoal(
                all: new IPromptCondition[]
                {
                    new ChainCondition(minChainDepth: 2, occurrences: 3),
                }),
            moveLimit: 0,
            reward: PromptReward.None);

        /// <summary>Sample: create 5 greens within 12 moves; ALSO any one depth-2 chain satisfies the Any branch.</summary>
        public static readonly Prompt SampleMix = new Prompt(
            id: "p1.sample.mix",
            localizedTitleKey: "prompt.mix_green5_or_chain",
            goal: new PromptGoal(
                all: new IPromptCondition[]
                {
                    new CreateColorCondition(ColorId.Green, 5),
                    new MoveLimitCondition(12),
                },
                any: new IPromptCondition[]
                {
                    new ChainCondition(minChainDepth: 2, occurrences: 1),
                }),
            moveLimit: 12,
            reward: PromptReward.None);
    }
}
