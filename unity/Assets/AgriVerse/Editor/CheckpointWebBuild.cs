#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AgriVerse.Client.Editor
{
    public static class CheckpointWebBuild
    {
        private const string TemporaryScenePath =
            "Assets/AgriVerse/Editor/CheckpointVerification.unity";

        [MenuItem("AgriVerse/Build/Checkpoint Web")]
        public static void Build()
        {
            string originalScenePath = SceneManager.GetActiveScene().path;
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new BuildFailedException("Unity project root was not found.");
            string outputPath = Path.Combine(projectRoot, "Builds", "WebGL");
            bool temporarySceneCreated = false;

            try
            {
                if (EditorUserBuildSettings.activeBuildTarget !=
                    BuildTarget.WebGL)
                {
                    throw new BuildFailedException(
                        "Switch the active build profile to Web first. " +
                        "In batch mode, launch Unity with -buildTarget WebGL.");
                }

                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(
                        TemporaryScenePath) != null)
                {
                    throw new BuildFailedException(
                        $"Refusing to replace existing scene {TemporaryScenePath}.");
                }

                CreateVerificationScene();
                temporarySceneCreated = true;
                Directory.CreateDirectory(outputPath);

                BuildReport report = BuildPipeline.BuildPlayer(
                    new BuildPlayerOptions
                    {
                        scenes = new[] { TemporaryScenePath },
                        locationPathName = outputPath,
                        target = BuildTarget.WebGL,
                        options = BuildOptions.None,
                    });

                if (report.summary.result != BuildResult.Succeeded)
                {
                    throw new BuildFailedException(
                        $"Web build failed with {report.summary.totalErrors} errors.");
                }

                Debug.Log(
                    $"Checkpoint Web build succeeded: {report.summary.totalSize} bytes.");
            }
            finally
            {
                if (temporarySceneCreated)
                {
                    AssetDatabase.DeleteAsset(TemporaryScenePath);
                }

                if (!Application.isBatchMode &&
                    !string.IsNullOrEmpty(originalScenePath) &&
                    File.Exists(originalScenePath))
                {
                    EditorSceneManager.OpenScene(originalScenePath);
                }
            }
        }

        private static void CreateVerificationScene()
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);

            GameObject cameraObject = new GameObject(
                "Main Camera",
                typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.1f);

            GameObject canvasObject = new GameObject(
                "Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);

            GameObject titleObject = new GameObject(
                "ScenarioTitleText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            titleObject.transform.SetParent(canvasObject.transform, false);

            Text title = titleObject.GetComponent<Text>();
            title.text = "Loading scenario…";
            title.font = Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf");
            title.fontSize = 46;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;

            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.1f, 0.35f);
            titleRect.anchorMax = new Vector2(0.9f, 0.65f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            GameObject presenterObject = new GameObject(
                "ScenarioBootstrap",
                typeof(ScenarioTitlePresenter));
            ScenarioTitlePresenter presenter =
                presenterObject.GetComponent<ScenarioTitlePresenter>();
            SerializedObject serializedPresenter =
                new SerializedObject(presenter);
            serializedPresenter.FindProperty("scenarioTitleText").objectReferenceValue =
                title;
            serializedPresenter.FindProperty("editorApiBaseUrl").stringValue =
                "http://localhost:8787";
            serializedPresenter.FindProperty("webApiBaseUrl").stringValue =
                "http://localhost:8787";
            serializedPresenter.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, TemporaryScenePath);
        }
    }
}
#endif
