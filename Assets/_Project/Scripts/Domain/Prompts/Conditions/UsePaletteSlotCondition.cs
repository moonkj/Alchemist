namespace Alchemist.Domain.Prompts.Conditions
{
    /// <summary>
    /// 팔레트 슬롯 Store/Use 사용 횟수가 <see cref="Count"/> 이상이면 충족.
    /// WHY: 팔레트 메카닉의 프롬프트화. Store 와 Use 각각이 1회로 카운트되며
    /// 상위 레이어(Palette 이벤트)가 호출 시점에 1씩 증가.
    /// </summary>
    public readonly struct UsePaletteSlotCondition : IPromptCondition
    {
        public readonly int Count;

        public UsePaletteSlotCondition(int count)
        {
            Count = count;
        }

        public bool Evaluate(IPromptContext ctx)
        {
            return ctx.PaletteSlotUses >= Count;
        }

        public float Progress(IPromptContext ctx)
        {
            if (Count <= 0) return 1f;
            int n = ctx.PaletteSlotUses;
            if (n >= Count) return 1f;
            if (n <= 0) return 0f;
            return (float)n / Count;
        }
    }
}
