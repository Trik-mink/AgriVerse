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
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string CharacterLabScenePath =
            "Assets/Scenes/CharacterLab.unity";
        private const string WorldLabScenePath =
            "Assets/Scenes/AnGiangWorldLab.unity";
        private const string AlphaScenePath =
            "Assets/Scenes/Episode3DAlpha.unity";

        [MenuItem("AgriVerse/Build/macOS Checkpoint")]
        public static void BuildCheckpoint()
        {
            Build("AgriVerseCheckpoint.app", SampleScenePath);
        }

        [MenuItem("AgriVerse/Build/macOS Release")]
        public static void BuildRelease()
        {
            Build("AgriVerse.app", AlphaScenePath);
        }

        [MenuItem("AgriVerse/Build/macOS Character Lab")]
        public static void BuildCharacterLab()
        {
            Build("CharacterLab.app", CharacterLabScenePath);
        }

        [MenuItem("AgriVerse/Build/macOS An Giang World Lab")]
        public static void BuildWorldLab()
        {
            Build("AnGiangWorldLab.app", WorldLabScenePath);
        }

        [MenuItem("AgriVerse/Build/macOS Episode 3D Alpha")]
        public static void BuildVerticalSlice()
        {
            Build("AgriVerse3DAlpha.app", AlphaScenePath);
        }

        private static void Build(string applicationName, string scenePath)
        {
            string projectRoot =
                Directory.GetParent(Application.dataPath)?.FullName ??
                throw new BuildFailedException("Unity project root was not found.");
            string outputPath = Path.Combine(
                projectRoot,
                "Builds",
                "macOS",
                applicationName);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            BuildReport report = BuildPipeline.BuildPlayer(
                new BuildPlayerOptions
                {
                    scenes = new[] { scenePath },
                    locationPathName = outputPath,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.None
                });
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
