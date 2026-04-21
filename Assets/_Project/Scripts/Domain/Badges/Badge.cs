namespace Alchemist.Domain.Badges
{
    /// <summary>
    /// 배지 1개 정의. 불변 번들: 식별/로컬라이제이션 키/조건/히든 여부.
    /// WHY sealed class: 조건 평가와 메타데이터 묶음 — 구조체로는 조건 상속을 수용 어려움.
    /// </summary>
    public sealed class Badge
    {
        public readonly BadgeId Id;
        public readonly string LocalizedNameKey;
        public readonly string LocalizedHintKey;
        public readonly string IconKey;
        public readonly bool IsHidden;
        public readonly BadgeCondition Condition;

        public Badge(
            BadgeId id,
            string localizedNameKey,
            string localizedHintKey,
            string iconKey,
            bool isHidden,
            BadgeCondition condition)
        {
            Id = id;
            LocalizedNameKey = localizedNameKey ?? string.Empty;
            LocalizedHintKey = localizedHintKey ?? string.Empty;
            IconKey = iconKey ?? string.Empty;
            IsHidden = isHidden;
            Condition = condition;
        }
    }
}
