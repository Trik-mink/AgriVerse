using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Scenario-neutral meeting point. It carries only a sanitized stakeholder ID and a
    /// restrained focus ring; the interview controller remains the state and API owner.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class StakeholderHotspot : MonoBehaviour
    {
        [SerializeField] private Renderer focusRenderer;
        [SerializeField]
        private StakeholderCharacterController character;
        [SerializeField] private float interactionRange = 3.8f;
        [SerializeField] private Color idleColor =
            new Color(.94f, .72f, .30f, .04f);
        [SerializeField] private Color focusColor =
            new Color(1f, .72f, .24f, .76f);

        private MaterialPropertyBlock properties;
        private bool focused;

        public string StakeholderId { get; private set; } =
            string.Empty;
        public float InteractionRange => interactionRange;
        public bool IsFocused => focused;
        public StakeholderCharacterController Character => character;

        public void Configure(
            Renderer sourceRenderer,
            float range = 3.8f)
        {
            Configure(sourceRenderer, null, range);
        }

        public void Configure(
            Renderer sourceRenderer,
            StakeholderCharacterController sourceCharacter,
            float range = 3.8f)
        {
            focusRenderer = sourceRenderer;
            character = sourceCharacter;
            interactionRange = Mathf.Max(.5f, range);
            RefreshVisual();
        }

        public void Bind(string stakeholderId)
        {
            StakeholderId = stakeholderId ?? string.Empty;
        }

        public void SetFocused(bool value)
        {
            if (focused == value) return;
            focused = value;
            character?.SetFocused(value);
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
                .78f + Mathf.Sin(Time.time * 2.6f) * .14f;
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
