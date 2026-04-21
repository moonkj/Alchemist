#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Alchemist.Bootstrap;

namespace Alchemist.EditorTools
{
    /// <summary>
    /// 헤드리스 iOS 빌드 엔트리포인트.
    /// CLI: Unity -batchmode -quit -executeMethod Alchemist.EditorTools.BuildScript.BuildIOS
    /// Scene 은 매 빌드마다 in-memory 로 구성한 뒤 저장 → 수동 Scene 편집 불필요.
    /// </summary>
    public static class BuildScript
    {
        private const string SceneDir = "Assets/_Project/Scenes";
        private const string ScenePath = SceneDir + "/GameScene.unity";
        private const string BuildOutput = "Builds/iOS";
        private const string BundleId = "com.moonkj.colormixalchemist";
        private const string TeamId = "QN975MTM7H";

        [MenuItem("Alchemist/Build iOS")]
        public static void BuildIOS()
        {
            EnsureScene();
            ConfigurePlayerSettings();

            Directory.CreateDirectory(BuildOutput);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildOutput,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(opts);
            bool ok = report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded;
            Debug.Log("[BuildScript] iOS build " + (ok ? "SUCCEEDED" : "FAILED"));
            if (!ok) EditorApplication.Exit(1);
        }

        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.applicationIdentifier = BundleId;
            PlayerSettings.productName = "Alchemist";
            PlayerSettings.companyName = "moonkj";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.iOS.buildNumber = "1";
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.sdkVersion = iOSSdkVersion.DeviceSDK;
            PlayerSettings.iOS.appleDeveloperTeamID = TeamId;
            PlayerSettings.iOS.appleEnableAutomaticSigning = true;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            // 1 = ARM64. 정수 상수로 지정하여 Unity 버전 간 enum 명칭 차이 회피.
            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.statusBarHidden = true;
        }

        private static void EnsureScene()
        {
            Directory.CreateDirectory(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.08f, 0.10f, 1f);
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

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
            tmp.text = "<size=48>컬러 믹스: 연금술사</size>\n\n<size=24>v1.0.0</size>\n<size=18>빌드 파이프라인 동작 확인</size>\n\n<size=14>게임 플레이 Scene 은 다음 빌드에서</size>";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var bootstrapGo = new GameObject("AppBootstrap");
            bootstrapGo.AddComponent<AppBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Debug.Log("[BuildScript] Scene written: " + ScenePath);
        }
    }
}
#endif
