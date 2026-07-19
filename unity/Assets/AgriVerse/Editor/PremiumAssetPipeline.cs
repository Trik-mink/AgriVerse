#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AgriVerse.Client.Editor
{
    public static class PremiumAssetPipeline
    {
        private const string MaiController =
            "Assets/AgriVerse/Art/Characters/Mai/Controllers/" +
            "MaiLab.controller";

        private sealed class AssetSpec
        {
            public string Id;
            public string Source;
            public string BaseColor;
            public string Normal;
            public string Material;
            public string Prefab;
            public float Smoothness;
        }

        private static readonly AssetSpec[] Characters =
        {
            Character(
                "MrBa",
                "tripo_convert_578bc8b3-30e7-4247-8cd4-930d77a0a59e",
                "villager_farmer_3d_model"),
            Character(
                "DrLinh",
                "tripo_convert_a035564f-f5de-4ea9-8c80-0a3ce475f7d8",
                "woman_explorer_3d_model"),
            Character(
                "MsHoa",
                "tripo_convert_60a1cbf8-9a53-4f82-add8-8cf25eced8d5",
                "human_figure_3d_model")
        };

        private static readonly AssetSpec[] EnvironmentAssets =
        {
            Structure(
                "ResearchPost_A",
                "tripo_convert_6f87ff0b-f13e-402f-9a35-a929a33bfa12",
                "rural_hut_3d_model"),
            Structure(
                "DistrictOffice_A",
                "tripo_convert_12db1f50-ae83-4c2a-bb1f-e6a8df87405f",
                "rural_service_office_3d_model"),
            Prop(
                "ResearchWorkstation_A",
                "tripo_convert_fd044cd7-5b4f-4ad8-be62-f17339c6d83a",
                "laboratory_bench_3d_model"),
            Structure(
                "ReflectionPavilion_A",
                "tripo_convert_bc2f6a30-2573-405c-8e61-49a7b1cc68bf",
                "waterfront_pavilion_3d_model"),
            Prop(
                "SamplingKit_A",
                "tripo_convert_dab18c70-90e3-4a63-bab4-e339ea4aa3c0",
                "water_sampling_kit_3d_model"),
            Prop(
                "PlanningTable_A",
                "tripo_convert_78d8f9d0-8d13-4c01-a9ad-00d7cd015ab9",
                "rural_planning_table_3d_model"),
            Prop(
                "WovenBasket_A",
                "tripo_convert_219e2e4e-aad6-46a5-94f2-5e8e8726a95e",
                "giỏ_đan_tre_3d_model"),
            Prop(
                "Hoe_A",
                "tripo_convert_6432e476-66fe-47a7-b274-df80facd397b",
                "agricultural_hoe_3d_model"),
            Prop(
                "Shovel_A",
                "tripo_convert_3766d4bb-c042-41b9-a17f-8db3f950763a",
                "shovel_3d_model")
        };

        [MenuItem("AgriVerse/Art/Build Premium Asset Library")]
        public static void BuildPremiumAssetLibrary()
        {
            AssetDatabase.Refresh();
            RuntimeAnimatorController controller =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    MaiController);
            if (controller == null)
            {
                throw new InvalidOperationException(
                    "The verified Mai Humanoid controller is unavailable.");
            }

            foreach (AssetSpec spec in Characters)
            {
                AssetDatabase.ImportAsset(
                    spec.Source,
                    ImportAssetOptions.ForceUpdate);
                Material material = CreateOrUpdateLitMaterial(spec);
                CreateCharacterPrefab(spec, material, controller);
            }

            foreach (AssetSpec spec in EnvironmentAssets)
            {
                AssetDatabase.ImportAsset(
                    spec.Source,
                    ImportAssetOptions.ForceUpdate);
                Material material = CreateOrUpdateLitMaterial(spec);
                CreateEnvironmentPrefab(spec, material);
            }

            CreateGlobeAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Premium asset library built: three Humanoid stakeholders, " +
                "nine fallback-safe environment prefabs, and licensed globe assets.");
        }

        private static AssetSpec Character(
            string id,
            string sourceStem,
            string textureStem)
        {
            string root =
                "Assets/AgriVerse/Art/Characters/" + id + "/";
            return Spec(
                id,
                root,
                sourceStem,
                textureStem,
                .28f);
        }

        private static AssetSpec Structure(
            string id,
            string sourceStem,
            string textureStem)
        {
            string root =
                "Assets/AgriVerse/Art/Environment/Structures/" +
                id + "/";
            return Spec(
                id,
                root,
                sourceStem,
                textureStem,
                .22f);
        }

        private static AssetSpec Prop(
            string id,
            string sourceStem,
            string textureStem)
        {
            string root =
                "Assets/AgriVerse/Art/Environment/Props/" + id + "/";
            return Spec(
                id,
                root,
                sourceStem,
                textureStem,
                .25f);
        }

        private static AssetSpec Spec(
            string id,
            string root,
            string sourceStem,
            string textureStem,
            float smoothness)
        {
            string sourceRoot = root + "Source/" + sourceStem;
            return new AssetSpec
            {
                Id = id,
                Source = sourceRoot + ".fbx",
                BaseColor =
                    sourceRoot + ".fbm/" +
                    textureStem + "_basecolor.JPEG",
                Normal =
                    sourceRoot + ".fbm/" +
                    textureStem + "_normal.JPEG",
                Material =
                    root + "Materials/" + id + "_URP.mat",
                Prefab =
                    root + "Prefabs/" + id + ".prefab",
                Smoothness = smoothness
            };
        }

        private static Material CreateOrUpdateLitMaterial(
            AssetSpec spec)
        {
            EnsureFolder(Path.GetDirectoryName(spec.Material));
            Shader shader =
                Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Lit shader is unavailable.");
            }

            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(spec.Material);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, spec.Material);
            }
            else
            {
                material.shader = shader;
            }

            Texture2D baseColor =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    spec.BaseColor);
            Texture2D normal =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    spec.Normal);
            if (baseColor == null || normal == null)
            {
                throw new InvalidOperationException(
                    spec.Id + " is missing its supplied PBR maps.");
            }

            material.name = spec.Id + "_URP";
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", baseColor);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", spec.Smoothness);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", .78f);
            material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateCharacterPrefab(
            AssetSpec spec,
            Material material,
            RuntimeAnimatorController controller)
        {
            EnsureFolder(Path.GetDirectoryName(spec.Prefab));
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(spec.Source);
            GameObject instance =
                source == null
                    ? null
                    : PrefabUtility.InstantiatePrefab(source)
                        as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    spec.Id + " could not be instantiated.");
            }

            instance.name = spec.Id;
            try
            {
                Renderer[] renderers =
                    instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        spec.Id + " has no renderer.");
                }
                ConfigureRenderers(renderers, material);

                Animator animator =
                    instance.GetComponentInChildren<Animator>(true);
                if (animator == null ||
                    animator.avatar == null ||
                    !animator.avatar.isValid ||
                    !animator.avatar.isHuman)
                {
                    throw new InvalidOperationException(
                        spec.Id +
                        " did not produce a valid Unity Humanoid Avatar.");
                }
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode =
                    AnimatorCullingMode.CullUpdateTransforms;

                AddCullingLod(instance, renderers, .025f);
                AddCapsuleCollider(instance, renderers);
                SetLayerRecursively(instance, 2);
                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    spec.Prefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void CreateEnvironmentPrefab(
            AssetSpec spec,
            Material material)
        {
            EnsureFolder(Path.GetDirectoryName(spec.Prefab));
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(spec.Source);
            GameObject instance =
                source == null
                    ? null
                    : PrefabUtility.InstantiatePrefab(source)
                        as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    spec.Id + " could not be instantiated.");
            }

            instance.name = spec.Id;
            try
            {
                Renderer[] renderers =
                    instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0)
                {
                    throw new InvalidOperationException(
                        spec.Id + " has no renderer.");
                }
                ConfigureRenderers(renderers, material);
                AddCullingLod(instance, renderers, .018f);
                AddBoxCollider(instance, renderers);
                SetLayerRecursively(instance, 2);
                PrefabUtility.SaveAsPrefabAsset(
                    instance,
                    spec.Prefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void ConfigureRenderers(
            Renderer[] renderers,
            Material material)
        {
            foreach (Renderer renderer in renderers)
            {
                Material[] materials =
                    renderer.sharedMaterials.Length == 0
                        ? new[] { material }
                        : renderer.sharedMaterials
                            .Select(_ => material)
                            .ToArray();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode =
                    ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage =
                    LightProbeUsage.BlendProbes;
            }
        }

        private static void AddCullingLod(
            GameObject root,
            Renderer[] renderers,
            float cullThreshold)
        {
            LODGroup group = root.GetComponent<LODGroup>();
            if (group == null)
            {
                group = root.AddComponent<LODGroup>();
            }
            group.SetLODs(
                new[]
                {
                    new LOD(cullThreshold, renderers)
                });
            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            group.RecalculateBounds();
        }

        private static void AddCapsuleCollider(
            GameObject root,
            Renderer[] renderers)
        {
            Bounds bounds = CombinedBounds(renderers);
            CapsuleCollider collider =
                root.GetComponent<CapsuleCollider>();
            if (collider == null)
            {
                collider = root.AddComponent<CapsuleCollider>();
            }
            Vector3 localCenter =
                root.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize =
                Divide(bounds.size, root.transform.lossyScale);
            collider.center = localCenter;
            collider.direction = 1;
            collider.height = Mathf.Max(.2f, localSize.y);
            collider.radius = Mathf.Max(
                .08f,
                Mathf.Min(localSize.x, localSize.z) * .32f);
        }

        private static void AddBoxCollider(
            GameObject root,
            Renderer[] renderers)
        {
            Bounds bounds = CombinedBounds(renderers);
            BoxCollider collider = root.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = root.AddComponent<BoxCollider>();
            }
            collider.center =
                root.transform.InverseTransformPoint(bounds.center);
            collider.size =
                Divide(bounds.size, root.transform.lossyScale);
        }

        private static Bounds CombinedBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
            {
                bounds.Encapsulate(renderers[index].bounds);
            }
            return bounds;
        }

        private static Vector3 Divide(Vector3 value, Vector3 divisor)
        {
            return new Vector3(
                divisor.x == 0f ? value.x : value.x / divisor.x,
                divisor.y == 0f ? value.y : value.y / divisor.y,
                divisor.z == 0f ? value.z : value.z / divisor.z);
        }

        private static void CreateGlobeAssets()
        {
            const string globeRoot =
                "Assets/AgriVerse/Art/Globe/";
            const string earthTexture =
                globeRoot +
                "Source/Textures/Earth/Earth_Color_8K.jpg";
            const string normalTexture =
                globeRoot +
                "Source/Textures/Earth/Earth_NormalGL_4K.png";
            const string cloudTexture =
                globeRoot +
                "Source/Textures/Clouds/" +
                "Earth_Clouds_Transparent_4K.png";
            const string starTexture =
                globeRoot +
                "Source/HDRI/Space_StarMap_8K.exr";
            const string earthPath =
                globeRoot + "Materials/Earth_URP.mat";
            const string cloudPath =
                globeRoot + "Materials/Clouds_URP.mat";
            const string spacePath =
                globeRoot + "Materials/Space_URP.mat";
            const string assetsPath =
                "Assets/AgriVerse/Resources/" +
                "GlobeLandingAssets.asset";

            EnsureFolder(globeRoot + "Materials");
            EnsureFolder("Assets/AgriVerse/Resources");
            Material earth = CreateGlobeMaterial(
                earthPath,
                "Universal Render Pipeline/Lit",
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    earthTexture),
                false,
                false);
            Texture2D normal =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    normalTexture);
            earth.SetTexture("_BumpMap", normal);
            earth.SetFloat("_BumpScale", .34f);
            earth.SetFloat("_Smoothness", .34f);
            earth.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(earth);

            Material clouds = CreateGlobeMaterial(
                cloudPath,
                "Universal Render Pipeline/Unlit",
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    cloudTexture),
                true,
                false);
            Material space = CreateGlobeMaterial(
                spacePath,
                "Universal Render Pipeline/Unlit",
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    starTexture),
                false,
                true);

            GlobeLandingAssets assets =
                AssetDatabase.LoadAssetAtPath<GlobeLandingAssets>(
                    assetsPath);
            if (assets == null)
            {
                assets =
                    ScriptableObject.CreateInstance<
                        GlobeLandingAssets>();
                AssetDatabase.CreateAsset(assets, assetsPath);
            }
            assets.Configure(earth, clouds, space);
            EditorUtility.SetDirty(assets);
        }

        private static Material CreateGlobeMaterial(
            string path,
            string shaderName,
            Texture texture,
            bool transparent,
            bool renderInside)
        {
            if (texture == null)
            {
                throw new InvalidOperationException(
                    path + " is missing its licensed source texture.");
            }
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    shaderName + " is unavailable.");
            }
            Material material =
                AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Cull", renderInside ? 1f : 2f);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", 5f);
                material.SetFloat("_DstBlend", 10f);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword(
                    "_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag(
                    "RenderType",
                    "Transparent");
                material.renderQueue = 3000;
            }
            else
            {
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword(
                    "_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag(
                    "RenderType",
                    "Opaque");
                material.renderQueue = -1;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetLayerRecursively(
            GameObject root,
            int layer)
        {
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) ||
                AssetDatabase.IsValidFolder(path))
            {
                return;
            }
            string parent = Path.GetDirectoryName(path)
                ?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(
                parent,
                Path.GetFileName(path));
        }
    }
}
#endif
