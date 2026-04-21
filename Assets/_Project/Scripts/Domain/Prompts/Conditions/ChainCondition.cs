namespace Alchemist.Domain.Prompts.Conditions
{
    /// <summary>
    /// Requires <see cref="Occurrences"/> chain events at depth &gt;= <see cref="MinChainDepth"/>.
    /// Progress = min(1, events / Occurrences).
    /// </summary>
    public readonly struct ChainCondition : IPromptCondition
    {
        public readonly int MinChainDepth;
        public readonly int Occurrences;

        public ChainCondition(int minChainDepth, int occurrences)
        {
            MinChainDepth = minChainDepth;
            Occurrences = occurrences;
        }

        public bool Evaluate(IPromptContext ctx)
        {
            return ctx.ChainEventsCount(MinChainDepth) >= Occurrences;
        }

        public float Progress(IPromptContext ctx)
        {
            if (Occurrences <= 0) return 1f;
            int events = ctx.ChainEventsCount(MinChainDepth);
            if (events >= Occurrences) return 1f;
            if (events <= 0) return 0f;
            return (float)events / Occurrences;
        }
    }
}
