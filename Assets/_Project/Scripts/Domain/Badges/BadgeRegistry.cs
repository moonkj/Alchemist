using System.Collections.Generic;
using Alchemist.Domain.Colors;
using Alchemist.Domain.Badges.Conditions;

namespace Alchemist.Domain.Badges
{
    /// <summary>
    /// Phase 3 고정 배지 카탈로그. 전부 readonly — 런타임 추가 금지.
    /// WHY static: DI 대상 아님. 스테이지/프롬프트별 확장이 필요할 때 Wave 2 에서 레지스트리화.
    /// </summary>
    public static class BadgeRegistry
    {
        private static readonly Badge[] _all = BuildAll();

        public static IReadOnlyList<Badge> All => _all;

        public static Badge Get(BadgeId id)
        {
            for (int i = 0; i < _all.Length; i++)
            {
                if (_all[i].Id == id) return _all[i];
            }
            return null;
        }

        private static Badge[] BuildAll()
        {
            return new Badge[]
            {
                // --- 조합 ---
                new Badge(BadgeId.FirstPurple,    "badge.first_purple.name",    "badge.first_purple.hint",    "icon.badge.first_purple",    isHidden: false, new FirstCreationCondition(ColorId.Purple)),
                new Badge(BadgeId.FirstOrange,    "badge.first_orange.name",    "badge.first_orange.hint",    "icon.badge.first_orange",    isHidden: false, new FirstCreationCondition(ColorId.Orange)),
                new Badge(BadgeId.FirstGreen,     "badge.first_green.name",     "badge.first_green.hint",     "icon.badge.first_green",     isHidden: false, new FirstCreationCondition(ColorId.Green)),
                new Badge(BadgeId.AllSecondaries, "badge.all_secondaries.name", "badge.all_secondaries.hint", "icon.badge.all_secondaries", isHidden: false, new AllSecondariesCondition()),
                new Badge(BadgeId.FirstBlack,     "badge.first_black.name",     "badge.first_black.hint",     "icon.badge.first_black",     isHidden: false, new FirstCreationCondition(ColorId.Black)),
                new Badge(BadgeId.FirstWhite,     "badge.first_white.name",     "badge.first_white.hint",     "icon.badge.first_white",     isHidden: false, new FirstCreationCondition(ColorId.White)),

                // --- 플레이 스타일 ---
                new Badge(BadgeId.Chain5,         "badge.chain5.name",          "badge.chain5.hint",          "icon.badge.chain5",          isHidden: false, new ChainDepthCondition(5)),
                new Badge(BadgeId.MinMoves,       "badge.min_moves.name",       "badge.min_moves.hint",       "icon.badge.min_moves",       isHidden: false, new MinMovesCondition()),
                new Badge(BadgeId.PromptPerfect,  "badge.prompt_perfect.name",  "badge.prompt_perfect.hint",  "icon.badge.prompt_perfect",  isHidden: false, new PerfectPromptCondition()),
                new Badge(BadgeId.NoBlack,        "badge.no_black.name",        "badge.no_black.hint",        "icon.badge.no_black",        isHidden: false, new NoBlackCondition()),
                new Badge(BadgeId.ChainStreak,    "badge.chain_streak.name",    "badge.chain_streak.hint",    "icon.badge.chain_streak",    isHidden: false, new ChainStreakCondition(minDepth: 2, occurrences: 3)),

                // --- 히든 ---
                new Badge(BadgeId.FilterOnly,    "badge.filter_only.name",    "badge.filter_only.hint",    "icon.badge.filter_only",    isHidden: true, new FilterOnlyCondition()),
                new Badge(BadgeId.GrayOnly,      "badge.gray_only.name",      "badge.gray_only.hint",      "icon.badge.gray_only",      isHidden: true, new GrayOnlyCondition()),
                new Badge(BadgeId.PrismOnly,     "badge.prism_only.name",     "badge.prism_only.hint",     "icon.badge.prism_only",     isHidden: true, new PrismOnlyCondition()),
                new Badge(BadgeId.PaletteMaster, "badge.palette_master.name", "badge.palette_master.hint", "icon.badge.palette_master", isHidden: true, new PaletteMasterCondition()),
                new Badge(BadgeId.SpeedRun,      "badge.speed_run.name",      "badge.speed_run.hint",      "icon.badge.speed_run",      isHidden: true, new SpeedRunCondition()),
            };
        }
    }
}
