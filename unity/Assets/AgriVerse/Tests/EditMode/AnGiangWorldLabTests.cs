using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriVerse.Client.Tests
{
    public sealed class AnGiangWorldLabTests
    {
        private const string RiceLod0 =
            "Assets/AgriVerse/Art/Environment/Vegetation/Rice/" +
            "RiceClump_A/Derived/RiceClump_A_LOD0.fbx";
        private const string GrassLod0 =
            "Assets/AgriVerse/Art/Environment/Vegetation/Banks/Grass_A/" +
            "Derived/Grass_A_LOD0.fbx";
        private const string FanPalmOptimized =
            "Assets/AgriVerse/Art/Environment/Vegetation/Trees/FanPalm_A/" +
            "Derived/FanPalm_A_Optimized.fbx";
        private const string BoatOptimized =
            "Assets/AgriVerse/Art/Environment/Props/Boat_A/" +
            "Derived/Boat_A_Optimized.fbx";
        private const string RicePrefab =
            "Assets/AgriVerse/Art/Environment/Vegetation/Rice/" +
            "RiceClump_A/Prefabs/RiceClump_A.prefab";
        private const string GrassPrefab =
            "Assets/AgriVerse/Art/Environment/Vegetation/Banks/Grass_A/" +
            "Prefabs/Grass_A.prefab";
        private const string ScenePath = "Assets/Scenes/AnGiangWorldLab.unity";

        [TestCase(RiceLod0, 1500)]
        [TestCase(GrassLod0, 2000)]
        [TestCase(FanPalmOptimized, 12000)]
        [TestCase(BoatOptimized, 20000)]
        public void DerivedMeshesRespectTheirTriangleBudgets(
            string path,
            int maximumTriangles)
        {
            Mesh[] meshes = AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .ToArray();

            Assert.That(meshes, Is.Not.Empty, path);
            Assert.That(
                meshes.Sum(mesh => (int)mesh.GetIndexCount(0) / 3),
                Is.LessThanOrEqualTo(maximumTriangles),
                path);
        }

        [TestCase(RicePrefab)]
        [TestCase(GrassPrefab)]
        public void VegetationPrefabsProvideThreeInstancedLods(string path)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            LODGroup group = prefab.GetComponent<LODGroup>();
            Assert.That(group, Is.Not.Null);
            Assert.That(group.GetLODs(), Has.Length.EqualTo(3));
            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Has.Length.GreaterThanOrEqualTo(3));
            Assert.That(
                renderers.All(renderer =>
                    renderer.sharedMaterial != null &&
                    renderer.sharedMaterial.enableInstancing),
                Is.True);
        }

        [Test]
        public void VegetationPhysicalScaleAndWorldDensityArePlausible()
        {
            Assert.That(
                TransformedLod0Height(RicePrefab),
                Is.InRange(.65f, 1.25f));
            Assert.That(
                TransformedLod0Height(GrassPrefab),
                Is.InRange(.4f, .9f));

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            try
            {
                InstancedVegetationField[] fields = scene
                    .GetRootGameObjects()
                    .SelectMany(root =>
                        root.GetComponentsInChildren<
                            InstancedVegetationField>(true))
                    .ToArray();
                Assert.That(fields, Has.Length.GreaterThanOrEqualTo(14));
                foreach (InstancedVegetationField field in fields)
                {
                    field.Rebuild();
                }
                Assert.That(
                    fields.All(field =>
                        field.LodLocalScales.Count == 3 &&
                        field.LodLocalScales[0].x > 50f),
                    Is.True,
                    "GPU instances must retain the imported FBX LOD0 transform.");
                Assert.That(
                    fields.Sum(field => field.InstanceCount),
                    Is.GreaterThan(7000));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static float TransformedLod0Height(string prefabPath)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            MeshFilter filter = prefab
                .GetComponentsInChildren<MeshFilter>(true)
                .First(item => item.name == "LOD0_Mesh");
            Bounds bounds = filter.sharedMesh.bounds;
            float minimum = float.MaxValue;
            float maximum = float.MinValue;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 local =
                            bounds.center +
                            Vector3.Scale(
                                bounds.extents,
                                new Vector3(x, y, z));
                        float worldY = filter.transform
                            .localToWorldMatrix
                            .MultiplyPoint3x4(local).y;
                        minimum = Mathf.Min(minimum, worldY);
                        maximum = Mathf.Max(maximum, worldY);
                    }
                }
            }
            return maximum - minimum;
        }

        [Test]
        public void WorldLabIsSeparateAndContainsTheRequired3DSystems()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                Assert.That(
                    CountComponentsNamed(roots, "FirstPersonWalker"),
                    Is.EqualTo(1));
                Assert.That(
                    CountComponentsNamed(
                        roots,
                        "InstancedVegetationField"),
                    Is.GreaterThanOrEqualTo(2));
                Assert.That(
                    roots.SelectMany(root =>
                            root.GetComponentsInChildren<Terrain>(true))
                        .Any(),
                    Is.True);
                Assert.That(
                    CountComponentsNamed(roots, "CanalWaterSurface"),
                    Is.GreaterThanOrEqualTo(1));
                Assert.That(
                    CountComponentsNamed(
                        roots,
                        nameof(InvestigationController)),
                    Is.EqualTo(0));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void TerrainUsesMatteConstantSmoothness()
        {
            string[] layerPaths =
            {
                "Assets/AgriVerse/Art/Environment/WorldLab/Terrain/" +
                "GrassPath.terrainlayer",
                "Assets/AgriVerse/Art/Environment/WorldLab/Terrain/" +
                "Clay.terrainlayer",
                "Assets/AgriVerse/Art/Environment/WorldLab/Terrain/" +
                "Mud.terrainlayer"
            };
            foreach (string path in layerPaths)
            {
                TerrainLayer layer =
                    AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                Assert.That(layer, Is.Not.Null, path);
                Assert.That(
                    layer.smoothnessSource,
                    Is.EqualTo(
                        TerrainLayerSmoothnessSource.ConstantOnly),
                    path);
                Assert.That(layer.smoothness, Is.LessThanOrEqualTo(.08f));
            }
        }

        [Test]
        public void AuthoredLandmarksRemainUprightAndGrounded()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            try
            {
                AssertHorizontalLandmark(scene, "SamplingDock", 1.8f);
                AssertHorizontalLandmark(scene, "LocalWoodenBoat", 2.5f);
                AssertGroundedLandmark(
                    scene,
                    "FieldShelter",
                    2.5f,
                    6.5f);
                AssertUprightLandmark(scene, "Banana_01", .85f);
                AssertUprightLandmark(scene, "Palmyra_01", .85f);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void AssertHorizontalLandmark(
            Scene scene,
            string name,
            float minimumRatio)
        {
            Bounds bounds = RendererBounds(scene, name);
            Assert.That(
                Mathf.Max(bounds.size.x, bounds.size.z) /
                bounds.size.y,
                Is.GreaterThan(minimumRatio),
                $"{name} should remain horizontal after scene placement.");
        }

        private static void AssertUprightLandmark(
            Scene scene,
            string name,
            float minimumRatio)
        {
            Bounds bounds = RendererBounds(scene, name);
            Assert.That(
                bounds.size.y /
                Mathf.Max(bounds.size.x, bounds.size.z),
                Is.GreaterThan(minimumRatio),
                $"{name} should remain upright after scene placement.");
        }

        private static void AssertGroundedLandmark(
            Scene scene,
            string name,
            float minimumHeight,
            float maximumHeight)
        {
            Transform target = FindTransform(scene, name);
            Bounds bounds = RendererBounds(target);
            Assert.That(
                bounds.size.y,
                Is.InRange(minimumHeight, maximumHeight),
                $"{name} should retain a plausible physical height.");
            Assert.That(
                Mathf.Abs(bounds.min.y - target.position.y),
                Is.LessThan(.04f),
                $"{name} should sit on its placement surface.");
        }

        private static Bounds RendererBounds(Scene scene, string name)
        {
            Transform target = FindTransform(scene, name);
            return RendererBounds(target);
        }

        private static Transform FindTransform(Scene scene, string name)
        {
            Transform target = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(item => item.name == name);
            Assert.That(target, Is.Not.Null, name);
            return target;
        }

        private static Bounds RendererBounds(Transform target)
        {
            Renderer[] renderers =
                target.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, target.name);
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static int CountComponentsNamed(
            GameObject[] roots,
            string typeName) =>
            roots.SelectMany(root =>
                    root.GetComponentsInChildren<Component>(true))
                .Count(component =>
                    component != null &&
                    component.GetType().Name == typeName);
    }
}
