namespace Alchemist.Domain.Prompts
{
    /// <summary>
    /// A single evaluable predicate over <see cref="IPromptContext"/>.
    /// Implementations MUST be allocation-free during Evaluate/Progress
    /// (no LINQ, no string concat, no boxing inside the method body).
    /// </summary>
    public interface IPromptCondition
    {
        /// <summary>True when the condition is satisfied by the current context.</summary>
        bool Evaluate(IPromptContext ctx);

        /// <summary>Normalized progress in [0,1]. 1.0 implies <see cref="Evaluate"/> is true for goal conditions.</summary>
        float Progress(IPromptContext ctx);
    }
}
