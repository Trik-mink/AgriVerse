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
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("AgriVerse/Build/macOS Checkpoint")]
        public static void BuildCheckpoint()
        {
            Build("AgriVerseCheckpoint.app");
        }

        [MenuItem("AgriVerse/Build/macOS Release")]
        public static void BuildRelease()
        {
            Build("AgriVerse.app");
        }

        private static void Build(string applicationName)
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
                    scenes = new[] { ScenePath },
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
