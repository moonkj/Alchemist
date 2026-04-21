#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Alchemist.Bootstrap;

namespace Alchemist.EditorTools
{
    /// <summary>
    /// 헤드리스 iOS 빌드: Unity -batchmode -executeMethod Alchemist.EditorTools.BuildScript.BuildIOS 로 호출.
    /// Scene 은 매 빌드마다 프로그래매틱 구성 → 수동 Scene 작업 불필요.
    /// </summary>
    public static class BuildScript
    {
        private const string ScenePath = "Assets/_Project/Scenes/GameScene.unity";
        private const string BuildOutput = "Builds/iOS";

        [MenuItem("Alchemist/Build iOS")]
        public static void BuildIOS()
        {
            EnsureScene();
            Directory.CreateDirectory(BuildOutput);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildOutput,
                target = BuildTarget.iOS,
                options = BuildOptions.None,
            };
            PlayerSettings.applicationIdentifier = "com.moonkj.colormixalchemist";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, (int)AppleMobileArchitecture.ARM64);
            PlayerSettings.iOS.appleDeveloperTeamID = "QN975MTM7H";
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void EnsureScene()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var canvasGo = new GameObject("UI_Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(390f, 844f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var titleGo = new GameObject("TitleLabel");
            titleGo.transform.SetParent(canvasGo.transform, false);
            var rt = titleGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(16f, 16f);
            rt.offsetMax = new Vector2(-16f, -16f);
            var tmp = titleGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "<size=48>컬러 믹스: 연금술사</size>\n<size=24>v1.0.0 · 빌드 파이프라인 동작 확인</size>\n<size=16>Scene/Prefab 수동 제작은 다음 라운드</size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var bootstrapGo = new GameObject("AppBootstrap");
            bootstrapGo.AddComponent<AppBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            var settings = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = new[] { settings };
        }
    }
}
#endif
