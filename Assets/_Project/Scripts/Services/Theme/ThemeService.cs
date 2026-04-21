using System;
using UnityEngine;

namespace Alchemist.Services.Theme
{
    /// <summary>
    /// 테마 상태 / 저장 / 관찰. WHY: URP 2D Global Light 은 View 레이어가 담당하고,
    /// 이 서비스는 enum 변경 이벤트만 방출해 UI/View 가 구독. 저장은 PlayerPrefs.
    /// </summary>
    public sealed class ThemeService
    {
        private const string PrefKey = "app.theme";

        public AppTheme Current { get; private set; }
        public event Action<AppTheme> OnThemeChanged;

        public ThemeService(AppTheme fallback = AppTheme.Light)
        {
            int saved = PlayerPrefs.GetInt(PrefKey, (int)fallback);
            Current = (AppTheme)saved;
        }

        /// <summary>테마 변경. 동일값이면 no-op. WHY: 중복 이벤트 차단으로 UI 리빌드 비용 제거.</summary>
        public void SetTheme(AppTheme theme)
        {
            if (theme == Current) return;
            Current = theme;
            PlayerPrefs.SetInt(PrefKey, (int)theme);
            OnThemeChanged?.Invoke(theme);
        }

        public void Toggle()
        {
            SetTheme(Current == AppTheme.Light ? AppTheme.Dark : AppTheme.Light);
        }
    }
}
