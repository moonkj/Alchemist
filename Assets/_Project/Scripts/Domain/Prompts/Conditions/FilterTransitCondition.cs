namespace Alchemist.Domain.Prompts.Conditions
{
    /// <summary>
    /// 블록이 필터 셀을 <see cref="Count"/> 회 통과하면 충족.
    /// WHY: 필터 메카닉의 프롬프트화 — "색 변환을 N번 경험" 요구. 색 무관 누적.
    /// Progress = min(1, transits / Count).
    /// </summary>
    public readonly struct FilterTransitCondition : IPromptCondition
    {
        public readonly int Count;

        public FilterTransitCondition(int count)
        {
            Count = count;
        }

        public bool Evaluate(IPromptContext ctx)
        {
            return ctx.FilterTransits >= Count;
        }

        public float Progress(IPromptContext ctx)
        {
            if (Count <= 0) return 1f;
            int n = ctx.FilterTransits;
            if (n >= Count) return 1f;
            if (n <= 0) return 0f;
            return (float)n / Count;
        }
    }
}
