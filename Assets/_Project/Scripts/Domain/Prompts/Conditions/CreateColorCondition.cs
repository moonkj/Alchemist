using Alchemist.Domain.Colors;

namespace Alchemist.Domain.Prompts.Conditions
{
    /// <summary>
    /// Requires the player to create at least <see cref="Count"/> blocks of <see cref="Target"/> color.
    /// Progress = min(1, created / Count).
    /// </summary>
    public readonly struct CreateColorCondition : IPromptCondition
    {
        public readonly ColorId Target;
        public readonly int Count;

        public CreateColorCondition(ColorId target, int count)
        {
            Target = target;
            Count = count;
        }

        public bool Evaluate(IPromptContext ctx)
        {
            return ctx.GetColorsCreated(Target) >= Count;
        }

        public float Progress(IPromptContext ctx)
        {
            if (Count <= 0) return 1f;
            int made = ctx.GetColorsCreated(Target);
            if (made >= Count) return 1f;
            if (made <= 0) return 0f;
            return (float)made / Count;
        }
    }
}
