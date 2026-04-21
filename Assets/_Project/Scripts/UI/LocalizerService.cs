using System.Collections.Generic;

namespace Alchemist.UI
{
    /// <summary>
    /// Phase 2 최소 로컬라이저(한국어 stub).
    /// WHY(BUG-H10): PromptBanner 가 key 문자열 그대로 노출 중이라 UX 검수 불가.
    /// 간단 Dictionary 로 "key → ko" 매핑만 제공. Phase 3에서 CSV 로더/ICU 로 대체 예정.
    /// Thread-unsafe 이지만 Unity 메인 스레드 전용이므로 허용.
    /// </summary>
    public static class LocalizerService
    {
        private static readonly Dictionary<string, string> _ko = BuildKo();

        /// <summary>key 를 한국어 문자열로 해석. 미존재 시 key 그대로 반환.</summary>
        public static string Localize(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            string v;
            if (_ko.TryGetValue(key, out v)) return v;
            return key;
        }

        /// <summary>테스트/런타임 오버라이드 용.</summary>
        public static void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(key)) return;
            _ko[key] = value ?? string.Empty;
        }

        private static Dictionary<string, string> BuildKo()
        {
            var d = new Dictionary<string, string>(16);
            // --- Phase 1 샘플 프롬프트 ---
            d["prompt.create_purple_10"] = "15수 이내에 보라 10개 만들기";
            d["prompt.chain_depth2_x3"] = "2단 이상 연쇄를 3회 달성";
            d["prompt.mix_green5_or_chain"] = "12수 이내에 초록 5개 (또는 2단 연쇄 1회)";
            // --- Phase 2 샘플 프롬프트 ---
            d["prompt.advanced_orange6_filter3"] = "16수 이내에 오렌지 6개, 필터 3회 통과";
            d["prompt.daily_purple5_slot3_filter2"] = "20수 이내에 보라 5, 팔레트 3회, 필터 2회";
            // --- HUD / 공용 ---
            d["hud.moves"] = "남은 수";
            d["hud.score"] = "점수";
            d["hud.palette"] = "팔레트";
            return d;
        }
    }
}
