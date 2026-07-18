using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Applies the serialized Resources material that keeps the narrow URP surface shader
    /// reachable in standalone builds. Each primitive gets its own colored material instance.
    /// </summary>
    public static class RuntimePrimitiveMaterial
    {
        private const string ResourcePath = "AgriVerseRuntimePrimitive";
        private static Material template;

        public static void Apply(MeshRenderer renderer, Color color)
        {
            if (renderer == null) return;
            Material surfaceTemplate = Template;
            if (surfaceTemplate == null)
            {
                Debug.LogError("The serialized AgriVerseRuntimePrimitive URP material is missing from Resources.");
                return;
            }

            Material surface = new Material(surfaceTemplate) { name = "RuntimePrimitiveSurface" };
            if (surface.HasProperty("_BaseColor")) surface.SetColor("_BaseColor", color);
            else surface.color = color;
            renderer.sharedMaterial = surface;
        }

        public static Material Template => template ??= Resources.Load<Material>(ResourcePath);
    }
}
