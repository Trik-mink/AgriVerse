using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// One-shot standalone performance sample for release verification. It has no
    /// gameplay or UI effect.
    /// </summary>
    public sealed class PlayerPerformanceReporter : MonoBehaviour
    {
        [SerializeField] private float warmupSeconds = 2f;
        [SerializeField] private float sampleSeconds = 5f;

        private float sampleStart;
        private int sampledFrames;
        private bool reporting;
        private bool reported;

        private void Update()
        {
            if (reported) return;
            if (!reporting)
            {
                if (Time.unscaledTime < warmupSeconds) return;
                sampleStart = Time.unscaledTime;
                sampledFrames = 0;
                reporting = true;
            }

            sampledFrames++;
            float elapsed = Time.unscaledTime - sampleStart;
            if (elapsed < sampleSeconds) return;
            float framesPerSecond =
                elapsed <= 0f ? 0f : sampledFrames / elapsed;
            Debug.Log(
                "AGRIVERSE_PERF average_fps=" +
                framesPerSecond.ToString("0.0") +
                " frames=" + sampledFrames +
                " seconds=" + elapsed.ToString("0.00"));
            reported = true;
        }
    }
}
