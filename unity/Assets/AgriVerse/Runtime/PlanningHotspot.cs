using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Scenario-neutral physical handoff into the existing proposal controller.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlanningHotspot : MonoBehaviour
    {
        [SerializeField] private Renderer focusRenderer;
        [SerializeField] private float interactionRange = 4.2f;
        [SerializeField] private Color idleColor =
            new Color(.94f, .72f, .30f, .03f);
        [SerializeField] private Color focusColor =
            new Color(1f, .72f, .24f, .82f);

        private MaterialPropertyBlock properties;

        public float InteractionRange => interactionRange;
        public bool IsFocused { get; private set; }

        public void Configure(
            Renderer sourceRenderer,
            float range = 4.2f)
        {
            focusRenderer = sourceRenderer;
            interactionRange = Mathf.Max(.5f, range);
            ApplyColor(idleColor);
        }

        public void SetFocused(bool value)
        {
            if (IsFocused == value) return;
            IsFocused = value;
            ApplyColor(value ? focusColor : idleColor);
        }

        private void Update()
        {
            if (!IsFocused || focusRenderer == null) return;
            float pulse =
                .82f + Mathf.Sin(Time.time * 2.3f) * .12f;
            ApplyColor(new Color(
                focusColor.r,
                focusColor.g,
                focusColor.b,
                focusColor.a * pulse));
        }

        private void ApplyColor(Color value)
        {
            if (focusRenderer == null) return;
            properties =
                properties ?? new MaterialPropertyBlock();
            focusRenderer.GetPropertyBlock(properties);
            properties.SetColor("_BaseColor", value);
            properties.SetColor(
                "_EmissionColor",
                new Color(
                    value.r * value.a,
                    value.g * value.a,
                    value.b * value.a,
                    1f));
            focusRenderer.SetPropertyBlock(properties);
        }
    }
}
