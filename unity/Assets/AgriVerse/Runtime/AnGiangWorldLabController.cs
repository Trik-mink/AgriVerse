using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Isolated world-quality harness. It contains no scenario, marker, or
    /// learning-loop state and is safe to abandon without changing SampleScene.
    /// </summary>
    public sealed class AnGiangWorldLabController : MonoBehaviour
    {
        [Serializable]
        public struct Viewpoint
        {
            public string Name;
            public Vector3 Position;
            public float Heading;
            public float Pitch;
        }

        [SerializeField] private FirstPersonWalker walker;
        [SerializeField] private Viewpoint[] evidenceViewpoints =
            Array.Empty<Viewpoint>();
        private int sampledFrames;
        private float sampledTime;
        private bool performanceReportingEnabled;
        private bool performanceReported;

        public IReadOnlyList<Viewpoint> EvidenceViewpoints =>
            evidenceViewpoints;

        public void Configure(
            FirstPersonWalker sourceWalker,
            Viewpoint[] viewpoints)
        {
            walker = sourceWalker;
            evidenceViewpoints = viewpoints ?? Array.Empty<Viewpoint>();
        }

        private void Start()
        {
            string[] args = Environment.GetCommandLineArgs();
            performanceReportingEnabled =
                PerformanceReportingEnabledFromArguments(args);
            string directory = CaptureDirectoryFromArguments(args);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                StartCoroutine(CaptureEvidence(directory));
            }
        }

        private void Update()
        {
            if (!performanceReportingEnabled || performanceReported)
            {
                return;
            }

            sampledFrames++;
            sampledTime += Time.unscaledDeltaTime;
            if (sampledFrames >= 300 && sampledTime > 0f)
            {
                Debug.Log(
                    $"AnGiangWorldLab average FPS: " +
                    $"{sampledFrames / sampledTime:F1}");
                performanceReported = true;
            }
        }

        private IEnumerator CaptureEvidence(string directory)
        {
            Directory.CreateDirectory(directory);
            yield return new WaitForSecondsRealtime(2f);
            for (int index = 0;
                 index < evidenceViewpoints.Length;
                 index++)
            {
                Viewpoint viewpoint = evidenceViewpoints[index];
                walker.Teleport(
                    viewpoint.Position,
                    viewpoint.Heading,
                    viewpoint.Pitch);
                yield return new WaitForSecondsRealtime(.8f);
                yield return new WaitForEndOfFrame();
                string safeName = string.IsNullOrWhiteSpace(
                    viewpoint.Name)
                    ? $"view_{index + 1:00}"
                    : viewpoint.Name;
                ScreenCapture.CaptureScreenshot(
                    Path.Combine(
                        directory,
                        $"{index + 1:00}_{safeName}.png"));
                yield return new WaitForSecondsRealtime(.35f);
            }
            yield return new WaitForSecondsRealtime(.5f);
            Application.Quit(0);
        }

        internal static string CaptureDirectoryFromArguments(
            string[] args)
        {
            if (args == null) return null;
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(
                        args[index],
                        "-agriverse-world-capture-dir",
                        StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }
            return null;
        }

        internal static bool PerformanceReportingEnabledFromArguments(
            string[] args)
        {
            if (args == null)
            {
                return false;
            }

            foreach (string argument in args)
            {
                if (string.Equals(
                        argument,
                        "-agriverse-performance-report",
                        StringComparison.Ordinal) ||
                    string.Equals(
                        argument,
                        "-agriverse-world-capture-dir",
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
