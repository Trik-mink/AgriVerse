using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    public sealed class EpisodeAccessibleText : MonoBehaviour
    {
        [SerializeField] private int baseFontSize;
        [SerializeField] private Color baseColor;
        [SerializeField] private bool configured;

        public void Apply(float scale, bool highContrast)
        {
            Text text = GetComponent<Text>();
            if (text == null) return;
            if (!configured)
            {
                baseFontSize = text.fontSize;
                baseColor = text.color;
                configured = true;
            }
            text.fontSize = Mathf.Max(
                10,
                Mathf.RoundToInt(baseFontSize * scale));
            if (!highContrast)
            {
                text.color = baseColor;
                return;
            }
            bool accent =
                baseColor.r > .78f && baseColor.g < .86f;
            text.color = accent
                ? new Color(1f, .78f, .32f, 1f)
                : new Color(.99f, .97f, .89f, 1f);
        }
    }

    public sealed class EpisodeAccessibleImage : MonoBehaviour
    {
        [SerializeField] private Color baseColor;
        [SerializeField] private bool configured;

        public void Apply(bool highContrast)
        {
            Image image = GetComponent<Image>();
            if (image == null) return;
            if (!configured)
            {
                baseColor = image.color;
                configured = true;
            }
            image.color = highContrast
                ? new Color(
                    baseColor.r,
                    baseColor.g,
                    baseColor.b,
                    Mathf.Max(.96f, baseColor.a))
                : baseColor;
        }
    }

    /// <summary>
    /// Session-local presentation preferences. They never enter scored requests or
    /// scenario state.
    /// </summary>
    public static class EpisodeAccessibility
    {
        public static float TextScale { get; private set; } = 1f;
        public static bool HighContrast { get; private set; }
        public static bool ReducedMotion { get; private set; }

        public static void SetTextScale(float value)
        {
            TextScale = Mathf.Clamp(value, .9f, 1.4f);
            ApplyAll();
        }

        public static void ChangeTextScale(float delta) =>
            SetTextScale(TextScale + delta);

        public static void ToggleHighContrast()
        {
            HighContrast = !HighContrast;
            ApplyAll();
        }

        public static void ToggleReducedMotion()
        {
            ReducedMotion = !ReducedMotion;
        }

        public static void ApplyAll()
        {
            foreach (Text text in
                     Object.FindObjectsByType<Text>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                EpisodeAccessibleText accessible =
                    text.GetComponent<EpisodeAccessibleText>();
                if (accessible == null)
                {
                    accessible =
                        text.gameObject.AddComponent<
                            EpisodeAccessibleText>();
                }
                accessible.Apply(TextScale, HighContrast);
            }
            foreach (Image image in
                     Object.FindObjectsByType<Image>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                EpisodeAccessibleImage accessible =
                    image.GetComponent<EpisodeAccessibleImage>();
                if (accessible == null)
                {
                    accessible =
                        image.gameObject.AddComponent<
                            EpisodeAccessibleImage>();
                }
                accessible.Apply(HighContrast);
            }
        }

        public static void ResetForTesting()
        {
            TextScale = 1f;
            HighContrast = false;
            ReducedMotion = false;
        }
    }
}
