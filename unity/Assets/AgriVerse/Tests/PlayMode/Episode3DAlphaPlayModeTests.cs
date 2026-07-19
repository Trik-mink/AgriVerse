using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AgriVerse.Client.Tests
{
    public sealed class Episode3DAlphaPlayModeTests
    {
        [UnitySetUp]
        public IEnumerator ResetSessions()
        {
            DestroyAll<EvidenceNotebookSession>();
            DestroyAll<EpisodeSession>();
            DestroyAll<RuntimePanelManager>();
            yield return null;
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator LiveFirstSitePredictionAndSampleReachTheNotebook()
        {
            GameObject hotspotObject = new GameObject(
                "AlphaTestHotspot",
                typeof(SphereCollider),
                typeof(WaterSampleHotspot));
            WaterSampleHotspot hotspot =
                hotspotObject.GetComponent<WaterSampleHotspot>();

            GameObject root = new GameObject("AlphaTest");
            InvestigationController investigation =
                root.AddComponent<InvestigationController>();
            investigation.ConfigureEndpointsForTesting(
                "http://localhost:8787",
                "http://localhost:8787");
            investigation.ConfigurePresentation(
                createUi: false,
                createMarkers: false);
            Episode3DAlphaController alpha =
                root.AddComponent<Episode3DAlphaController>();
            alpha.Configure(
                investigation,
                null,
                hotspot,
                "upstream",
                null,
                null,
                null,
                Vector3.zero,
                0f,
                null,
                null,
                null);

            for (int interval = 0;
                 interval < 160 &&
                 alpha.State == Episode3DAlphaState.Loading;
                 interval++)
            {
                yield return new WaitForSecondsRealtime(.05f);
            }

            Assert.That(alpha.State, Is.EqualTo(Episode3DAlphaState.Intro));
            Assert.That(investigation.LoadState, Is.EqualTo(
                InvestigationLoadState.Ready));
            Assert.That(investigation.MarkerCount, Is.EqualTo(0));
            Assert.That(alpha.ActiveSite.id, Is.EqualTo("upstream"));

            alpha.AdvanceIntro();
            alpha.AdvanceIntro();
            Assert.That(alpha.State, Is.EqualTo(
                Episode3DAlphaState.Exploring));
            Assert.That(alpha.BeginSiteInteraction(), Is.True);
            Assert.That(alpha.State, Is.EqualTo(
                Episode3DAlphaState.Predicting));
            Assert.That(alpha.ChoosePrediction(0), Is.True);
            Assert.That(alpha.State, Is.EqualTo(
                Episode3DAlphaState.ReadyToCollect));
            Assert.That(alpha.BeginCollection(), Is.True);

            for (int interval = 0;
                 interval < 160 &&
                 alpha.State == Episode3DAlphaState.Sampling;
                 interval++)
            {
                yield return new WaitForSecondsRealtime(.05f);
            }

            Assert.That(alpha.State, Is.EqualTo(
                Episode3DAlphaState.Reading));
            Assert.That(alpha.SampleRecorded, Is.True);
            Assert.That(investigation.RecordedReadingCount, Is.EqualTo(1));
            Assert.That(
                alpha.ReadingTextForTesting,
                Does.Contain(alpha.ActiveSite.label));
            Assert.That(
                alpha.ReadingTextForTesting,
                Does.Contain("SOURCE IDs"));
            alpha.ToggleNotebook();
            Assert.That(alpha.NotebookOpenForTesting, Is.True);
            Assert.That(alpha.ReadingPanelVisibleForTesting, Is.False);
            Assert.That(alpha.DialogueVisibleForTesting, Is.False);
            Assert.That(
                alpha.ObjectiveTextForTesting,
                Does.Contain("Press N to close"));

            Object.Destroy(root);
            Object.Destroy(hotspotObject);
            DestroyAll<EvidenceNotebookSession>();
            DestroyAll<EpisodeSession>();
            yield return null;
        }

        private static void DestroyAll<T>()
            where T : Component
        {
            foreach (T item in Object.FindObjectsByType<T>(
                         FindObjectsSortMode.None))
            {
                Object.Destroy(item.gameObject);
            }
        }
    }
}
