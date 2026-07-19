using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Scenario-neutral physical sampling point. The 3D presentation binds a sanitized
    /// site ID after the scenario loads; the visible ring is never the data source.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class WaterSampleHotspot : MonoBehaviour
    {
        [SerializeField] private Renderer focusRenderer;
        [SerializeField] private float interactionRange = 3.4f;
        [SerializeField] private Color idleColor =
            new Color(.94f, .72f, .30f, .08f);
        [SerializeField] private Color focusColor =
            new Color(1f, .72f, .24f, .82f);

        private MaterialPropertyBlock properties;
        private bool focused;

        public string SiteId { get; private set; } = string.Empty;
        public float InteractionRange => interactionRange;
        public bool IsFocused => focused;

        public void Configure(
            Renderer sourceRenderer,
            float range = 3.4f)
        {
            focusRenderer = sourceRenderer;
            interactionRange = Mathf.Max(.5f, range);
            RefreshVisual();
        }

        public void Bind(string siteId)
        {
            SiteId = siteId ?? string.Empty;
        }

        public void SetFocused(bool value)
        {
            if (focused == value) return;
            focused = value;
            RefreshVisual();
        }

        private void Awake()
        {
            if (focusRenderer == null)
            {
                focusRenderer =
                    GetComponentInChildren<Renderer>(true);
            }
            RefreshVisual();
        }

        private void Update()
        {
            if (!focused || focusRenderer == null) return;
            float pulse =
                .82f + Mathf.Sin(Time.time * 3.2f) * .12f;
            ApplyColor(new Color(
                focusColor.r,
                focusColor.g,
                focusColor.b,
                focusColor.a * pulse));
        }

        private void RefreshVisual()
        {
            ApplyColor(focused ? focusColor : idleColor);
        }

        private void ApplyColor(Color color)
        {
            if (focusRenderer == null) return;
            if (properties == null)
            {
                properties = new MaterialPropertyBlock();
            }
            focusRenderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", color);
            properties.SetColor(
                "_EmissionColor",
                new Color(
                    color.r * color.a,
                    color.g * color.a,
                    color.b * color.a,
                    1f));
            focusRenderer.SetPropertyBlock(properties);
        }
    }
}
