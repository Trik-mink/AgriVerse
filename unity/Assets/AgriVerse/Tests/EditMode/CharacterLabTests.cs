using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgriVerse.Client.Tests
{
    public sealed class CharacterLabTests
    {
        private const string MaterialPath =
            "Assets/AgriVerse/Art/Characters/Mai/Materials/Mai_URP.mat";
        private const string ControllerPath =
            "Assets/AgriVerse/Art/Characters/Mai/Controllers/MaiLab.controller";
        private const string PrefabPath =
            "Assets/AgriVerse/Art/Characters/Mai/Prefabs/Mai.prefab";
        private const string ScenePath = "Assets/Scenes/CharacterLab.unity";

        [Test]
        public void MaiLabAssetsUseExplicitUrpMaterialAndAllFiveMotions()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Assert.That(material, Is.Not.Null);
            Assert.That(
                material.shader.name,
                Is.EqualTo("Universal Render Pipeline/Lit"));
            Assert.That(material.mainTexture, Is.Not.Null);
            Assert.That(controller, Is.Not.Null);
            Assert.That(
                controller.animationClips.Distinct().Count(),
                Is.EqualTo(5));
            Assert.That(
                controller.animationClips.Select(clip => clip.name),
                Is.EquivalentTo(new[]
                {
                    "Mai_HatAdjust",
                    "Mai_Idle",
                    "Mai_Wave",
                    "Mai_Talk",
                    "Mai_Walk"
                }));
            Assert.That(prefab, Is.Not.Null);
            SkinnedMeshRenderer renderer =
                prefab.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            Assert.That(renderer, Is.Not.Null);
            Assert.That(renderer.sharedMaterial, Is.SameAs(material));
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar.isValid, Is.True);
            Assert.That(animator.runtimeAnimatorController, Is.SameAs(controller));
            Assert.That(prefab.GetComponent("MaiGuideController"), Is.Not.Null);
        }

        [Test]
        public void CharacterLabIsSeparateFromSampleSceneAndContainsNoLearningLoop()
        {
            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                Assert.That(
                    roots.Any(root =>
                        root.GetComponent("CharacterLabController") != null),
                    Is.True);
                Assert.That(
                    roots.SelectMany(root =>
                            root.GetComponentsInChildren<InvestigationController>(true))
                        .Any(),
                    Is.False);
                Assert.That(
                    roots.SelectMany(root =>
                            root.GetComponentsInChildren<Camera>(true))
                        .Count(camera => camera.CompareTag("MainCamera")),
                    Is.EqualTo(1));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }
}
