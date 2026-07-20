using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class PremiumAssetIntegrationTests
    {
        private const string CharacterRoot =
            "Assets/AgriVerse/Art/Characters/";
        private const string EnvironmentRoot =
            "Assets/AgriVerse/Art/Environment/";

        [TestCase(
            CharacterRoot + "MrBa/Source/" +
            "tripo_convert_578bc8b3-30e7-4247-8cd4-930d77a0a59e.fbx",
            "c582e01411301fdcbb097930160bb337f1b62f44c1a54eafc5ef8c3ae40cb4db")]
        [TestCase(
            CharacterRoot + "DrLinh/Source/" +
            "tripo_convert_a035564f-f5de-4ea9-8c80-0a3ce475f7d8.fbx",
            "ad29e34b2389684593a5b34009d33759729acef05baed80be463b437d0f00930")]
        [TestCase(
            CharacterRoot + "MsHoa/Source/" +
            "tripo_convert_60a1cbf8-9a53-4f82-add8-8cf25eced8d5.fbx",
            "756dca2abe7cc39a3b6fb7f91309a74a2b4175ce0eb5fc6a4c852e9a6a7ff339")]
        public void StakeholderSourceFbxRemainsByteIdentical(
            string assetPath,
            string expectedSha256)
        {
            string fullPath = Path.Combine(
                Directory.GetParent(Application.dataPath).FullName,
                assetPath);

            Assert.That(File.Exists(fullPath), Is.True, assetPath);
            using (FileStream stream = File.OpenRead(fullPath))
            using (SHA256 hash = SHA256.Create())
            {
                string actual = BitConverter
                    .ToString(hash.ComputeHash(stream))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
                Assert.That(actual, Is.EqualTo(expectedSha256));
            }
        }

        [TestCase("MrBa")]
        [TestCase("DrLinh")]
        [TestCase("MsHoa")]
        public void StakeholderPrefabsUseValidHumanoidsAndExplicitUrpMaterials(
            string characterId)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CharacterRoot + characterId + "/Prefabs/" +
                characterId + ".prefab");

            Assert.That(prefab, Is.Not.Null, characterId);
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null, characterId);
            Assert.That(animator.avatar, Is.Not.Null, characterId);
            Assert.That(animator.avatar.isValid, Is.True, characterId);
            Assert.That(animator.avatar.isHuman, Is.True, characterId);
            Assert.That(animator.runtimeAnimatorController, Is.Not.Null);
            Assert.That(
                animator.runtimeAnimatorController.animationClips
                    .Select(clip => clip.name),
                Does.Contain("Mai_Idle"));
            Assert.That(
                animator.runtimeAnimatorController.animationClips
                    .Select(clip => clip.name),
                Does.Contain("Mai_Talk"));
            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, characterId);
            Assert.That(
                renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .All(material =>
                        material.shader.name ==
                        "Universal Render Pipeline/Lit"),
                Is.True,
                characterId);
        }

        [TestCase(
            EnvironmentRoot +
            "Structures/ResearchPost_A/Prefabs/ResearchPost_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Structures/DistrictOffice_A/Prefabs/DistrictOffice_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Structures/ReflectionPavilion_A/Prefabs/" +
            "ReflectionPavilion_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/ResearchWorkstation_A/Prefabs/" +
            "ResearchWorkstation_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/SamplingKit_A/Prefabs/SamplingKit_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/PlanningTable_A/Prefabs/PlanningTable_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/WovenBasket_A/Prefabs/WovenBasket_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/Hoe_A/Prefabs/Hoe_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/Shovel_A/Prefabs/Shovel_A.prefab")]
        public void EnvironmentPrefabsHaveUrpMaterialsLodsAndSimpleColliders(
            string path)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(
                prefab.GetComponentInChildren<LODGroup>(true),
                Is.Not.Null,
                path);
            Assert.That(
                prefab.GetComponentInChildren<Collider>(true),
                Is.Not.Null,
                path);
            Renderer[] renderers =
                prefab.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers, Is.Not.Empty, path);
            Assert.That(
                renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null)
                    .All(material =>
                        material.shader.name ==
                        "Universal Render Pipeline/Lit"),
                Is.True,
                path);
        }

        [Test]
        public void GlobeSourcesAndRuntimeAssetSetArePreserved()
        {
            Texture2D earth = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/AgriVerse/Art/Globe/Source/Textures/Earth/" +
                "Earth_Color_8K.jpg");
            Texture2D clouds = AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/AgriVerse/Art/Globe/Source/Textures/Clouds/" +
                "Earth_Clouds_Transparent_4K.png");
            ScriptableObject runtimeAssets =
                AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    "Assets/AgriVerse/Resources/GlobeLandingAssets.asset");
            FieldNetworkCatalogAsset fieldNetwork =
                AssetDatabase.LoadAssetAtPath<FieldNetworkCatalogAsset>(
                    "Assets/AgriVerse/Resources/FieldNetworkCatalog.asset");

            Assert.That(earth, Is.Not.Null);
            Assert.That(clouds, Is.Not.Null);
            Assert.That(runtimeAssets, Is.Not.Null);
            Assert.That(fieldNetwork, Is.Not.Null);
            Assert.That(fieldNetwork.FutureLocations.Count, Is.EqualTo(4));
        }

        [TestCase(
            CharacterRoot + "MrBa/Prefabs/MrBa.prefab")]
        [TestCase(
            CharacterRoot + "DrLinh/Prefabs/DrLinh.prefab")]
        [TestCase(
            CharacterRoot + "MsHoa/Prefabs/MsHoa.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Structures/ResearchPost_A/Prefabs/ResearchPost_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Structures/DistrictOffice_A/Prefabs/DistrictOffice_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Structures/ReflectionPavilion_A/Prefabs/" +
            "ReflectionPavilion_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/ResearchWorkstation_A/Prefabs/" +
            "ResearchWorkstation_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/SamplingKit_A/Prefabs/SamplingKit_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/PlanningTable_A/Prefabs/PlanningTable_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/WovenBasket_A/Prefabs/WovenBasket_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/Hoe_A/Prefabs/Hoe_A.prefab")]
        [TestCase(
            EnvironmentRoot +
            "Props/Shovel_A/Prefabs/Shovel_A.prefab")]
        public void DerivedPresentationPrefabsHaveStableGroundedWrapperRoots(
            string path)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            Assert.That(
                Quaternion.Angle(
                    prefab.transform.localRotation,
                    Quaternion.identity),
                Is.LessThan(.01f),
                path + " must expose an identity wrapper root.");
            Assert.That(
                Vector3.Distance(
                    prefab.transform.localScale,
                    Vector3.one),
                Is.LessThan(.0001f),
                path);
            Assert.That(
                prefab.GetComponentsInChildren<Rigidbody>(true),
                Is.Empty,
                path + " must remain static presentation art.");

            GameObject instance =
                PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            try
            {
                Renderer[] renderers =
                    instance.GetComponentsInChildren<Renderer>(true);
                Assert.That(renderers, Is.Not.Empty, path);
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers.Skip(1))
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                Assert.That(
                    Mathf.Abs(bounds.min.y - instance.transform.position.y),
                    Is.LessThan(.035f),
                    path + " must rest on its wrapper base.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }
    }
}
