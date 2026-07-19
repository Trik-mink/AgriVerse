using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class ArtImportSettingsTests
    {
        private const string MaiModel =
            "Assets/AgriVerse/Art/Characters/Mai/Source/" +
            "tripo_convert_a9305ef4-e460-4d27-97e6-e56330fc8896.fbx";
        private const string RiceModel =
            "Assets/AgriVerse/Art/Environment/Vegetation/Rice/" +
            "RiceClump_A/Source/" +
            "tripo_convert_e8561bbf-fc38-435d-8d07-e588426c24cb.fbx";
        private const string RiceNormal =
            "Assets/AgriVerse/Art/Environment/Vegetation/Rice/" +
            "RiceClump_A/Source/" +
            "tripo_convert_e8561bbf-fc38-435d-8d07-e588426c24cb.fbm/" +
            "tripo_image_e8561bbf-fc38-435d-8d07-e588426c24cb_0_3.jpg";
        private const string ClayBaseColor =
            "Assets/AgriVerse/Art/Environment/Materials/Source/Clay/" +
            "red_dirt_mud_01_BaseColor_2k.jpg";
        private const string ClayRoughness =
            "Assets/AgriVerse/Art/Environment/Materials/Source/Clay/" +
            "red_dirt_mud_01_Roughness_2k.png";
        private const string CanalLoop =
            "Assets/AgriVerse/Art/Audio/Ambience/Canal_Loop.ogg";
        private const string WaterScoop =
            "Assets/AgriVerse/Art/Audio/SFX/Water/WaterScoop_01.wav";

        [Test]
        public void MaiImportsAsHumanoidWithoutSourceSceneCamerasOrLights()
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(MaiModel) as ModelImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(
                importer.animationType,
                Is.EqualTo(ModelImporterAnimationType.Human));
            Assert.That(importer.importAnimation, Is.True);
            Assert.That(importer.importCameras, Is.False);
            Assert.That(importer.importLights, Is.False);
            Assert.That(importer.globalScale, Is.EqualTo(1.65f).Within(.001f));
        }

        [Test]
        public void MaiProducesAValidHumanoidAvatarAndFiveMotionClips()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(MaiModel);
            Animator animator = model.GetComponentInChildren<Animator>(true);
            AnimationClip[] clips = AssetDatabase
                .LoadAllAssetsAtPath(MaiModel)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                .ToArray();

            Assert.That(model.transform.Find("Cube"), Is.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.avatar, Is.Not.Null);
            Assert.That(animator.avatar.isValid, Is.True);
            Assert.That(animator.avatar.isHuman, Is.True);
            Assert.That(clips, Has.Length.EqualTo(5));
            Assert.That(
                clips.Select(clip => clip.name),
                Is.EquivalentTo(new[]
                {
                    "Mai_HatAdjust",
                    "Mai_Idle",
                    "Mai_Wave",
                    "Mai_Talk",
                    "Mai_Walk"
                }));
            Assert.That(clips.All(clip => clip.length > 0f), Is.True);
        }

        [Test]
        public void StaticEnvironmentModelsDoNotImportAnimationOrSourceSceneObjects()
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(RiceModel) as ModelImporter;

            Assert.That(importer, Is.Not.Null);
            Assert.That(importer.animationType, Is.EqualTo(ModelImporterAnimationType.None));
            Assert.That(importer.importAnimation, Is.False);
            Assert.That(importer.importCameras, Is.False);
            Assert.That(importer.importLights, Is.False);
            Assert.That(importer.globalScale, Is.EqualTo(1f).Within(.001f));
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(RiceModel);
            Assert.That(model.transform.Find("Cube"), Is.Null);
        }

        [Test]
        public void PbrDataMapsStayLinearAndNormalMapsUseTheNormalImporter()
        {
            TextureImporter normal =
                AssetImporter.GetAtPath(RiceNormal) as TextureImporter;
            TextureImporter baseColor =
                AssetImporter.GetAtPath(ClayBaseColor) as TextureImporter;
            TextureImporter roughness =
                AssetImporter.GetAtPath(ClayRoughness) as TextureImporter;

            Assert.That(normal.textureType, Is.EqualTo(TextureImporterType.NormalMap));
            Assert.That(normal.sRGBTexture, Is.False);
            Assert.That(baseColor.sRGBTexture, Is.True);
            Assert.That(roughness.sRGBTexture, Is.False);
            Assert.That(baseColor.maxTextureSize, Is.EqualTo(2048));
        }

        [Test]
        public void LongAmbienceStreamsAndShortInteractionEffectsDecompressOnLoad()
        {
            AudioImporter ambience =
                AssetImporter.GetAtPath(CanalLoop) as AudioImporter;
            AudioImporter effect =
                AssetImporter.GetAtPath(WaterScoop) as AudioImporter;

            Assert.That(
                ambience.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.Streaming));
            Assert.That(ambience.loadInBackground, Is.True);
            Assert.That(
                ambience.defaultSampleSettings.preloadAudioData,
                Is.False);
            Assert.That(
                effect.defaultSampleSettings.loadType,
                Is.EqualTo(AudioClipLoadType.DecompressOnLoad));
            Assert.That(effect.defaultSampleSettings.preloadAudioData, Is.True);
        }
    }
}
