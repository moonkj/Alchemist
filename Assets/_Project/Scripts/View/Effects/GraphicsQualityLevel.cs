namespace Alchemist.View.Effects
{
    /// <summary>
    /// 3 단계 품질 프리셋. WHY: 저사양 기기에서 metaball/jelly 전면 비활성화하고
    /// spriteSheet fallback 으로 그리도록 분기하기 위한 단일 축.
    /// </summary>
    public enum GraphicsQualityLevel
    {
        /// <summary>SpriteSheet fallback. metaball/jelly shader 비활성.</summary>
        Low = 0,
        /// <summary>Jelly deform 만 활성. metaball shader 는 단순 alpha cutoff.</summary>
        Mid = 1,
        /// <summary>full metaball blend + jelly deform + particle trails.</summary>
        High = 2,
    }
}
