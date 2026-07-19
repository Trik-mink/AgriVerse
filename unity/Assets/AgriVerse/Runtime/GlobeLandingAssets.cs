using UnityEngine;

namespace AgriVerse.Client
{
    [CreateAssetMenu(
        fileName = "GlobeLandingAssets",
        menuName = "AgriVerse/Globe Landing Assets")]
    public sealed class GlobeLandingAssets : ScriptableObject
    {
        [SerializeField] private Material earthMaterial;
        [SerializeField] private Material cloudMaterial;
        [SerializeField] private Material spaceMaterial;

        public Material EarthMaterial => earthMaterial;
        public Material CloudMaterial => cloudMaterial;
        public Material SpaceMaterial => spaceMaterial;
        public bool IsUsable =>
            earthMaterial != null &&
            cloudMaterial != null &&
            spaceMaterial != null;

        public void Configure(
            Material earth,
            Material clouds,
            Material space)
        {
            earthMaterial = earth;
            cloudMaterial = clouds;
            spaceMaterial = space;
        }
    }
}
