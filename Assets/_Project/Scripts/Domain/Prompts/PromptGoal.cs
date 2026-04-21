using System;

namespace Alchemist.Domain.Prompts
{
    /// <summary>
    /// Compound goal with AND (<see cref="All"/>) and OR (<see cref="Any"/>) sets.
    /// Success: every <c>All</c> condition true AND (<c>Any</c> empty OR at least one <c>Any</c> true).
    /// Progress: arithmetic mean of <c>All</c> condition progress values (Any is satisfaction-only, not aggregated here).
    /// Evaluate/Progress use index-based for-loops to avoid enumerator allocation.
    /// </summary>
    public sealed class PromptGoal
    {
        private static readonly IPromptCondition[] EmptyConditions = Array.Empty<IPromptCondition>();

        /// <summary>Conditions that must ALL be satisfied.</summary>
        public readonly IPromptCondition[] All;

        /// <summary>Optional OR-set; if non-empty at least one must be satisfied.</summary>
        public readonly IPromptCondition[] Any;

        public PromptGoal(IPromptCondition[] all, IPromptCondition[] any = null)
        {
            All = all ?? EmptyConditions;
            Any = any ?? EmptyConditions;
        }

        public bool Evaluate(IPromptContext ctx)
        {
            // AND block: every All condition must pass.
            for (int i = 0; i < All.Length; i++)
            {
                if (!All[i].Evaluate(ctx)) return false;
            }

            // OR block: skip when empty, else require at least one pass.
            if (Any.Length == 0) return true;
            for (int i = 0; i < Any.Length; i++)
            {
                if (Any[i].Evaluate(ctx)) return true;
            }
            return false;
        }

        /// <summary>Average progress of <see cref="All"/>. Returns 1.0 when <see cref="All"/> is empty.</summary>
        public float Progress(IPromptContext ctx)
        {
            int len = All.Length;
            if (len == 0) return 1f;

            float sum = 0f;
            for (int i = 0; i < len; i++)
            {
                sum += All[i].Progress(ctx);
            }
            return sum / len;
        }
    }
}
