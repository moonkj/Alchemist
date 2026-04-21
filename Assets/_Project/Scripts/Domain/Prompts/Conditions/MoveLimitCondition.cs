namespace Alchemist.Domain.Prompts.Conditions
{
    /// <summary>
    /// Constraint-type condition: passes while MovesUsed &lt;= <see cref="MaxMoves"/>, fails once exceeded.
    /// When included in <c>PromptGoal.All</c>, exceeding the limit causes the whole goal to fail (Evaluate=false).
    /// Progress is binary: 1.0 while within budget, 0.0 once exceeded (no partial credit).
    /// </summary>
    public readonly struct MoveLimitCondition : IPromptCondition
    {
        public readonly int MaxMoves;

        public MoveLimitCondition(int maxMoves)
        {
            MaxMoves = maxMoves;
        }

        public bool Evaluate(IPromptContext ctx)
        {
            return ctx.MovesUsed <= MaxMoves;
        }

        public float Progress(IPromptContext ctx)
        {
            return ctx.MovesUsed <= MaxMoves ? 1f : 0f;
        }
    }
}
