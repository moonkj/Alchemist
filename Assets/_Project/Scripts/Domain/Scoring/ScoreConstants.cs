namespace Alchemist.Domain.Scoring
{
    /// <summary>
    /// Scoring tuning constants (Phase 1 MVP).
    /// These are held as <c>const</c> so the JIT can fold them into call sites; post-playtest,
    /// migrate to Remote Config by replacing this class with a readonly singleton (same API).
    /// Design rationale: docs/architecture.md §2.6.
    /// </summary>
    public static class ScoreConstants
    {
        // ----- BaseColorValue (per block) -----
        public const int PrimaryValue   = 10;   // Red / Yellow / Blue
        public const int SecondaryValue = 30;   // Orange / Green / Purple
        public const int TertiaryValue  = 100;  // White (full palette)
        public const int BlackPenalty   = -20;  // Contaminated block

        // ----- ChainMultiplier breakpoints -----
        public const float ChainStep     = 0.5f; // Depth 1..3 linear step
        public const float ChainBaseHigh = 2.5f; // Depth 4 floor
        public const float ChainStepHigh = 0.3f; // Depth 4+ step

        // ----- Stage-end bonuses -----
        public const int EfficiencyMax           = 200;
        public const int ResidualPerRemainingMove = 50;
        public const int ResidualPerRemainingSlot = 30;

        // ----- Invariants -----
        public const int MinTotal = 0; // Total never dips below zero
    }
}
