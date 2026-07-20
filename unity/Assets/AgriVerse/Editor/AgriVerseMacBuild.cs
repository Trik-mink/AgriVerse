#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AgriVerse.Client.Editor
{
    public static class AgriVerseMacBuild
    {
        public const string ReleaseProductName = "AgriVerse";
        public const string ReleaseCompanyName = "AgriVerse";
        public const string ReleaseApplicationIdentifier =
            "org.agriverse.episode1";
        public const string ReleaseOutputRelativePath =
            "Builds/Release/AgriVerse.app";
        public const string ReleaseScenePath =
            "Assets/Scenes/Episode3DAlpha.unity";

        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string CharacterLabScenePath =
            "Assets/Scenes/CharacterLab.unity";
        private const string WorldLabScenePath =
            "Assets/Scenes/AnGiangWorldLab.unity";
        private const string AlphaScenePath =
            ReleaseScenePath;

        [MenuItem("AgriVerse/Build/macOS Checkpoint")]
        public static void BuildCheckpoint()
        {
            Build(
                Path.Combine(
                    "Builds",
                    "macOS",
                    "AgriVerseCheckpoint.app"),
                SampleScenePath);
        }

        [MenuItem("AgriVerse/Build/macOS Release")]
        public static void BuildRelease()
        {
            Build(
                ReleaseOutputRelativePath,
                AlphaScenePath,
                requireFreshDestination: true);
        }

        [MenuItem("AgriVerse/Build/macOS Character Lab")]
        public static void BuildCharacterLab()
        {
            Build(
                Path.Combine("Builds", "macOS", "CharacterLab.app"),
                CharacterLabScenePath);
        }

        [MenuItem("AgriVerse/Build/macOS An Giang World Lab")]
        public static void BuildWorldLab()
        {
            Build(
                Path.Combine("Builds", "macOS", "AnGiangWorldLab.app"),
                WorldLabScenePath);
        }

        [MenuItem("AgriVerse/Build/macOS Episode 3D Alpha")]
        public static void BuildVerticalSlice()
        {
            Build(
                Path.Combine(
                    "Builds",
                    "macOS",
                    "AgriVerse3DAlpha.app"),
                AlphaScenePath);
        }

        private static void Build(
            string relativeOutputPath,
            string scenePath,
            bool requireFreshDestination = false)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new BuildFailedException("Unity project root was not found.");
            string outputPath = Path.Combine(projectRoot, relativeOutputPath);
            if (requireFreshDestination && Directory.Exists(outputPath))
            {
                throw new BuildFailedException(
                    "Release destination already exists. Remove the generated " +
                    $"bundle before building: {outputPath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            FullScreenMode previousFullScreenMode =
                PlayerSettings.fullScreenMode;
            bool previousNativeResolution =
                PlayerSettings.defaultIsNativeResolution;
            string previousProductName =
                PlayerSettings.productName;
            string previousCompanyName =
                PlayerSettings.companyName;
            string previousApplicationIdentifier =
                PlayerSettings.GetApplicationIdentifier(
                    NamedBuildTarget.Standalone);
            int previousArchitecture =
                PlayerSettings.GetArchitecture(
                    NamedBuildTarget.Standalone);
            BuildReport report;
            try
            {
                PlayerSettings.productName =
                    ReleaseProductName;
                PlayerSettings.companyName =
                    ReleaseCompanyName;
                PlayerSettings.SetApplicationIdentifier(
                    NamedBuildTarget.Standalone,
                    ReleaseApplicationIdentifier);
                PlayerSettings.SetArchitecture(
                    NamedBuildTarget.Standalone,
                    2);
                PlayerSettings.fullScreenMode =
                    FullScreenMode.FullScreenWindow;
                PlayerSettings.defaultIsNativeResolution = true;
                report = BuildPipeline.BuildPlayer(
                    new BuildPlayerOptions
                    {
                        scenes = new[] { scenePath },
                        locationPathName = outputPath,
                        target = BuildTarget.StandaloneOSX,
                        options = BuildOptions.None
                    });
            }
            finally
            {
                PlayerSettings.fullScreenMode =
                    previousFullScreenMode;
                PlayerSettings.defaultIsNativeResolution =
                    previousNativeResolution;
                PlayerSettings.productName =
                    previousProductName;
                PlayerSettings.companyName =
                    previousCompanyName;
                PlayerSettings.SetApplicationIdentifier(
                    NamedBuildTarget.Standalone,
                    previousApplicationIdentifier);
                PlayerSettings.SetArchitecture(
                    NamedBuildTarget.Standalone,
                    previousArchitecture);
            }
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"macOS build failed with {report.summary.totalErrors} errors.");
            }

            Debug.Log(
                $"AgriVerse macOS build succeeded: {report.summary.totalSize} bytes at {outputPath}");
        }
    }
}
#endif
