using UnityEngine;

namespace Alchemist.Bootstrap
{
    /// <summary>
    /// TMP 리소스 없이도 동작하는 기본 라벨. BuildScript 의 GameScene 에서 Camera 옆에 배치.
    /// WHY: TMP_Essential_Resources 가 프로젝트에 import 되지 않으면 TextMeshProUGUI 는 아무 것도
    ///      렌더링하지 않아 검은 화면이 된다. IMGUI 는 내장 폰트라 항상 표시됨.
    /// </summary>
    public sealed class DebugSplashLabel : MonoBehaviour
    {
        private GUIStyle _title;
        private GUIStyle _body;

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.94f, 0.82f, 1f) },
            };
            _body = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.85f, 0.85f, 0.90f, 1f) },
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            int w = Screen.width;
            int h = Screen.height;
            int titleH = 120;
            int bodyH = 80;
            int topPad = (h - titleH - bodyH - 40) / 2;
            GUI.Label(new Rect(0, topPad, w, titleH), "Color Mix: Alchemist", _title);
            GUI.Label(
                new Rect(0, topPad + titleH + 20, w, bodyH),
                "v1.0.0\nBuild pipeline OK\nGameplay scene: next round",
                _body);
        }
    }
}
