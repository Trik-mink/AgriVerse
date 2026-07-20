using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class ReleaseConfigurationTests
    {
        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new DirectoryNotFoundException(
                "Unity project root was not found.");

        [Test]
        public void CommittedPlayerSettingsUseTheReleaseIdentity()
        {
            string settings = File.ReadAllText(
                Path.Combine(
                    ProjectRoot,
                    "ProjectSettings",
                    "ProjectSettings.asset"));

            StringAssert.Contains("companyName: AgriVerse", settings);
            StringAssert.Contains("productName: AgriVerse", settings);
            StringAssert.Contains(
                "Standalone: org.agriverse.episode1",
                settings);
            StringAssert.Contains("Standalone: 2", settings);
        }

        [Test]
        public void EditorBuildSettingsStartOnlyTheEpisodeReleaseScene()
        {
            string settings = File.ReadAllText(
                Path.Combine(
                    ProjectRoot,
                    "ProjectSettings",
                    "EditorBuildSettings.asset"));

            StringAssert.Contains(
                "path: Assets/Scenes/Episode3DAlpha.unity",
                settings);
            StringAssert.DoesNotContain(
                "path: Assets/Scenes/SampleScene.unity",
                settings);
        }

        [Test]
        public void ReleaseBuilderUsesAFreshNonDevelopmentDestination()
        {
            string source = File.ReadAllText(
                Path.Combine(
                    Application.dataPath,
                    "AgriVerse",
                    "Editor",
                    "AgriVerseMacBuild.cs"));

            StringAssert.Contains(
                "Builds/Release/AgriVerse.app",
                source);
            StringAssert.Contains(
                "Directory.Exists(outputPath)",
                source);
            StringAssert.Contains(
                "BuildOptions.None",
                source);
            StringAssert.Contains(
                "AlphaScenePath",
                source);
            StringAssert.DoesNotContain(
                "Build(\"AgriVerse.app\", SampleScenePath)",
                source);
        }
    }
}
