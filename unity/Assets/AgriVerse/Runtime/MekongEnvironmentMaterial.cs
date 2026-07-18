using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>Clones the serialized URP Lit environment surface retained through Resources.</summary>
    public static class MekongEnvironmentMaterial
    {
        private const string ResourcePath = "MekongEnvironmentSurface";
        private static Material template;

        public static Material Create(Color color, float smoothness = .2f)
        {
            Material source = Template;
            if (source == null)
            {
                Debug.LogError("The serialized MekongEnvironmentSurface URP material is missing from Resources.");
                return null;
            }
            Material material = new Material(source);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        public static Material Template => template ??= Resources.Load<Material>(ResourcePath);
    }
}
