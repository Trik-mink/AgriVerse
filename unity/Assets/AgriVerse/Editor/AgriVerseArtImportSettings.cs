#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace AgriVerse.Client.Editor
{
    /// <summary>
    /// Deterministic import rules for the licensed Episode 1 art bundle. Raw files remain
    /// byte-identical on disk; these settings control Unity's imported representation.
    /// </summary>
    public sealed class AgriVerseArtImportSettings : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/AgriVerse/Art/";
        private const string MaiModelPath =
            "Assets/AgriVerse/Art/Characters/Mai/Source/" +
            "tripo_convert_a9305ef4-e460-4d27-97e6-e56330fc8896.fbx";
        private static readonly string[] MaiMotionNames =
        {
            "Mai_HatAdjust",
            "Mai_Idle",
            "Mai_Wave",
            "Mai_Talk",
            "Mai_Walk"
        };

        public override uint GetVersion() => 4;

        private bool IsEpisodeArt =>
            assetPath.StartsWith(ArtRoot, StringComparison.Ordinal);

        private void OnPreprocessModel()
        {
            if (!IsEpisodeArt ||
                (!assetPath.Contains("/Source/") &&
                 !assetPath.Contains("/Derived/")))
            {
                return;
            }
            if (!(assetImporter is ModelImporter importer)) return;

            bool isMai = assetPath.Contains(
                "/Characters/Mai/",
                StringComparison.Ordinal);
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.materialImportMode =
                ModelImporterMaterialImportMode.None;
            importer.globalScale = assetPath.Contains("/Source/")
                ? SourceModelScale(assetPath)
                : 1f;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.isReadable = false;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
            importer.importBlendShapes = isMai;
            importer.importAnimation = isMai;
            importer.animationType = isMai
                ? ModelImporterAnimationType.Human
                : ModelImporterAnimationType.None;
            if (isMai)
            {
                importer.avatarSetup =
                    ModelImporterAvatarSetup.CreateFromThisModel;
                ConfigureMaiClips(importer);
            }
        }

        private void OnPostprocessModel(GameObject importedRoot)
        {
            if (!IsEpisodeArt || !assetPath.Contains("/Source/")) return;
            Transform defaultCube = FindChild(importedRoot.transform, "Cube");
            if (defaultCube != null)
            {
                UnityEngine.Object.DestroyImmediate(defaultCube.gameObject);
            }
        }

        private void OnPreprocessTexture()
        {
            if (!IsEpisodeArt) return;
            if (!(assetImporter is TextureImporter importer)) return;

            string filename = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool isNormal =
                filename.Contains("normal", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith("_0_3", StringComparison.OrdinalIgnoreCase);
            bool isColor =
                filename.Contains("basecolor", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith("_0_0", StringComparison.OrdinalIgnoreCase) ||
                filename.EndsWith("_Card", StringComparison.OrdinalIgnoreCase);
            bool isCard = filename.EndsWith(
                "_Card",
                StringComparison.OrdinalIgnoreCase);

            importer.textureType = isNormal
                ? TextureImporterType.NormalMap
                : TextureImporterType.Default;
            importer.sRGBTexture = isColor && !isNormal;
            importer.mipmapEnabled = true;
            importer.wrapMode = isCard
                ? TextureWrapMode.Clamp
                : TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = isColor ? 4 : 2;
            importer.maxTextureSize = 2048;
            importer.textureCompression =
                TextureImporterCompression.CompressedHQ;
            if (isCard)
            {
                importer.alphaSource =
                    TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = true;
            }
        }

        private void OnPreprocessAudio()
        {
            if (!IsEpisodeArt) return;
            if (!(assetImporter is AudioImporter importer)) return;

            bool isAmbience = assetPath.Contains(
                "/Audio/Ambience/",
                StringComparison.Ordinal);
            AudioImporterSampleSettings settings =
                importer.defaultSampleSettings;
            settings.loadType = isAmbience
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = isAmbience
                ? AudioCompressionFormat.Vorbis
                : AudioCompressionFormat.ADPCM;
            settings.quality = isAmbience ? .58f : 1f;
            settings.sampleRateSetting =
                AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = !isAmbience;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = isAmbience;
        }

        private static float SourceModelScale(string path)
        {
            if (path.Contains("/Characters/Mai/", StringComparison.Ordinal))
                return 1.65f;
            if (path.Contains("/Trees/Banana_A/", StringComparison.Ordinal))
                return 5f;
            if (path.Contains("/Trees/CoconutPalm_A/", StringComparison.Ordinal))
                return 10f;
            if (path.Contains("/Trees/FanPalm_A/", StringComparison.Ordinal))
                return 9f;
            if (path.Contains("/Trees/Broadleaf_A/", StringComparison.Ordinal))
                return 8f;
            if (path.Contains("/Structures/Shelter_A/", StringComparison.Ordinal))
                return 5f;
            if (path.Contains("/Structures/Dock_A/", StringComparison.Ordinal))
                return 6f;
            if (path.Contains("/Props/Boat_A/", StringComparison.Ordinal))
                return 6f;
            if (path.Contains("/Banks/Reed_A/", StringComparison.Ordinal))
                return 1.4f;
            if (path.Contains("/Banks/Grass_A/", StringComparison.Ordinal))
                return .8f;
            return 1f;
        }

        private static void ConfigureMaiClips(ModelImporter importer)
        {
            ModelImporterClipAnimation[] defaults =
                importer.defaultClipAnimations;
            if (defaults == null || defaults.Length == 0) return;

            var selected =
                new System.Collections.Generic.List<ModelImporterClipAnimation>();
            foreach (ModelImporterClipAnimation clip in defaults)
            {
                string sourceName =
                    string.IsNullOrWhiteSpace(clip.takeName)
                        ? clip.name
                        : clip.takeName;
                if (!sourceName.StartsWith(
                        "Armature|NlaTrack",
                        StringComparison.Ordinal))
                {
                    continue;
                }
                if (selected.Count >= MaiMotionNames.Length)
                {
                    break;
                }

                clip.name = MaiMotionNames[selected.Count];
                clip.loopTime =
                    clip.name == "Mai_Idle" ||
                    clip.name == "Mai_Talk" ||
                    clip.name == "Mai_Walk";
                selected.Add(clip);
            }

            if (selected.Count == 5)
            {
                importer.clipAnimations = selected.ToArray();
            }
        }

        [MenuItem("AgriVerse/Art/Normalize Imported Sources")]
        public static void NormalizeImportedSources()
        {
            ModelImporter importer =
                AssetImporter.GetAtPath(MaiModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    "The Mai source FBX has not been imported.");
            }

            ConfigureMaiClips(importer);
            if (importer.clipAnimations == null ||
                importer.clipAnimations.Length != 5)
            {
                throw new InvalidOperationException(
                    "Mai must expose exactly five preferred source motion takes.");
            }
            importer.SaveAndReimport();
            Debug.Log(
                "AgriVerse art sources normalized: Mai has one five-clip Humanoid motion set.");
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindChild(root.GetChild(index), name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
#endif
