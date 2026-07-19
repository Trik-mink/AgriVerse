#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace AgriVerse.Client.Editor
{
    public static class AnGiangWorldLabPipeline
    {
        private const string Root =
            "Assets/AgriVerse/Art/Environment/";
        private const string WorldRoot = Root + "WorldLab/";
        private const string WorldMaterials =
            WorldRoot + "Materials/";
        private const string WorldMeshes = WorldRoot + "Meshes/";
        private const string WorldTerrain = WorldRoot + "Terrain/";
        private const string WorldTextures = WorldRoot + "Textures/";
        private const string ScenePath =
            "Assets/Scenes/AnGiangWorldLab.unity";

        private const string RiceRoot =
            Root + "Vegetation/Rice/RiceClump_A/";
        private const string RiceModel =
            RiceRoot + "Derived/RiceClump_A_LOD0.fbx";
        private const string RiceCard =
            RiceRoot + "Derived/RiceClump_A_Card.png";
        private const string RiceBase =
            RiceRoot + "Source/" +
            "tripo_convert_e8561bbf-fc38-435d-8d07-e588426c24cb.fbm/" +
            "tripo_image_e8561bbf-fc38-435d-8d07-e588426c24cb_0_0.jpg";
        private const string RiceNormal =
            RiceRoot + "Source/" +
            "tripo_convert_e8561bbf-fc38-435d-8d07-e588426c24cb.fbm/" +
            "tripo_image_e8561bbf-fc38-435d-8d07-e588426c24cb_0_3.jpg";
        private const string RicePrefab =
            RiceRoot + "Prefabs/RiceClump_A.prefab";

        private const string GrassRoot =
            Root + "Vegetation/Banks/Grass_A/";
        private const string GrassModel =
            GrassRoot + "Derived/Grass_A_LOD0.fbx";
        private const string GrassCard =
            GrassRoot + "Derived/Grass_A_Card.png";
        private const string GrassPrefab =
            GrassRoot + "Prefabs/Grass_A.prefab";

        private const string ReedModel =
            Root + "Vegetation/Banks/Reed_A/Source/" +
            "tripo_convert_a30fbd75-5eb7-49fe-9a58-74c5fb6a994c.fbx";
        private const string ReedBase =
            Root + "Vegetation/Banks/Reed_A/Source/" +
            "tripo_convert_a30fbd75-5eb7-49fe-9a58-74c5fb6a994c.fbm/" +
            "wetland_reed_cluster_3d_model_basecolor.JPEG";
        private const string ReedNormal =
            Root + "Vegetation/Banks/Reed_A/Source/" +
            "tripo_convert_a30fbd75-5eb7-49fe-9a58-74c5fb6a994c.fbm/" +
            "wetland_reed_cluster_3d_model_normal.JPEG";

        private const string BananaModel =
            Root + "Vegetation/Trees/Banana_A/Source/" +
            "tripo_convert_be610465-fd18-4e21-8898-03245253a103.fbx";
        private const string BananaBase =
            Root + "Vegetation/Trees/Banana_A/Source/" +
            "tripo_convert_be610465-fd18-4e21-8898-03245253a103.fbm/" +
            "banana_basecolor.JPEG";
        private const string BananaNormal =
            Root + "Vegetation/Trees/Banana_A/Source/" +
            "tripo_convert_be610465-fd18-4e21-8898-03245253a103.fbm/" +
            "banana_normal.JPEG";

        private const string CoconutModel =
            Root + "Vegetation/Trees/CoconutPalm_A/Source/" +
            "tripo_convert_ac22fd44-e612-48bb-9f06-ad3b71eff1be.fbx";
        private const string CoconutBase =
            Root + "Vegetation/Trees/CoconutPalm_A/Source/" +
            "tripo_convert_ac22fd44-e612-48bb-9f06-ad3b71eff1be.fbm/" +
            "coconut_palm_tree_3d_model_basecolor.JPEG";
        private const string CoconutNormal =
            Root + "Vegetation/Trees/CoconutPalm_A/Source/" +
            "tripo_convert_ac22fd44-e612-48bb-9f06-ad3b71eff1be.fbm/" +
            "coconut_palm_tree_3d_model_normal.JPEG";

        private const string FanPalmModel =
            Root + "Vegetation/Trees/FanPalm_A/Derived/" +
            "FanPalm_A_Optimized.fbx";
        private const string FanPalmBase =
            Root + "Vegetation/Trees/FanPalm_A/Source/" +
            "tripo_convert_586e9138-0554-46ad-882a-61ff00cffd3c.fbm/" +
            "palm_tree_3d_model_basecolor.JPEG";
        private const string FanPalmNormal =
            Root + "Vegetation/Trees/FanPalm_A/Source/" +
            "tripo_convert_586e9138-0554-46ad-882a-61ff00cffd3c.fbm/" +
            "palm_tree_3d_model_normal.JPEG";

        private const string BroadleafModel =
            Root + "Vegetation/Trees/Broadleaf_A/Source/" +
            "tripo_convert_d9d333ac-8f88-45a6-974d-a30b87e4e9d2.fbx";
        private const string BroadleafBase =
            Root + "Vegetation/Trees/Broadleaf_A/Source/" +
            "tripo_convert_d9d333ac-8f88-45a6-974d-a30b87e4e9d2.fbm/" +
            "tropical_broadleaf_tree_3d_model_basecolor.JPEG";
        private const string BroadleafNormal =
            Root + "Vegetation/Trees/Broadleaf_A/Source/" +
            "tripo_convert_d9d333ac-8f88-45a6-974d-a30b87e4e9d2.fbm/" +
            "tropical_broadleaf_tree_3d_model_normal.JPEG";

        private const string ShelterModel =
            Root + "Structures/Shelter_A/Source/" +
            "tripo_convert_0955bcea-5113-4ad7-a5b8-8a1c399870ce.fbx";
        private const string ShelterBase =
            Root + "Structures/Shelter_A/Source/" +
            "tripo_convert_0955bcea-5113-4ad7-a5b8-8a1c399870ce.fbm/" +
            "rustic_wooden_shelter_3d_model_basecolor.JPEG";
        private const string ShelterNormal =
            Root + "Structures/Shelter_A/Source/" +
            "tripo_convert_0955bcea-5113-4ad7-a5b8-8a1c399870ce.fbm/" +
            "rustic_wooden_shelter_3d_model_normal.JPEG";

        private const string DockModel =
            Root + "Structures/Dock_A/Source/" +
            "tripo_convert_1985cb40-e31c-4158-b03f-2d1d0114e595.fbx";
        private const string DockBase =
            Root + "Structures/Dock_A/Source/" +
            "tripo_convert_1985cb40-e31c-4158-b03f-2d1d0114e595.fbm/" +
            "wooden_sampling_dock_3d_model_basecolor.JPEG";
        private const string DockNormal =
            Root + "Structures/Dock_A/Source/" +
            "tripo_convert_1985cb40-e31c-4158-b03f-2d1d0114e595.fbm/" +
            "wooden_sampling_dock_3d_model_normal.JPEG";

        private const string BoatModel =
            Root + "Props/Boat_A/Derived/Boat_A_Optimized.fbx";
        private const string BoatBase =
            Root + "Props/Boat_A/Source/" +
            "tripo_convert_445684dd-d127-4ca1-aa99-0c51df883e29.fbm/" +
            "traditional_wooden_boat_3d_model_basecolor.JPEG";
        private const string BoatNormal =
            Root + "Props/Boat_A/Source/" +
            "tripo_convert_445684dd-d127-4ca1-aa99-0c51df883e29.fbm/" +
            "traditional_wooden_boat_3d_model_normal.JPEG";

        private const string ClayBase =
            Root + "Materials/Source/Clay/" +
            "red_dirt_mud_01_BaseColor_2k.jpg";
        private const string ClayNormal =
            Root + "Materials/Source/Clay/" +
            "red_dirt_mud_01_NormalGL_2k.png";
        private const string MudBase =
            Root + "Materials/Source/Mud/" +
            "muddy_tracks_BaseColor_2k.jpg";
        private const string MudNormal =
            Root + "Materials/Source/Mud/" +
            "muddy_tracks_NormalGL_2k.png";
        private const string PathBase =
            Root + "Materials/Source/Grass/" +
            "grass_path_2_BaseColor_2k.jpg";
        private const string PathNormal =
            Root + "Materials/Source/Grass/" +
            "grass_path_2_NormalGL_2k.png";
        private const string TimberBase =
            Root + "Materials/Source/Timber/" +
            "weathered_planks_BaseColor_2k.jpg";
        private const string TimberNormal =
            Root + "Materials/Source/Timber/" +
            "weathered_planks_NormalGL_2k.png";

        private const string TerrainDataPath =
            WorldTerrain + "AnGiangTerrain.asset";
        private const string GradeProfilePath =
            WorldRoot + "WorldLab_Grade.asset";
        private const string WaterNormalPath =
            WorldTextures + "CanalRipples_Normal.png";

        [MenuItem("AgriVerse/Art/Build An Giang World Lab")]
        public static void BuildAnGiangWorldLab()
        {
            EnsureFolders();
            CreateWaterNormal();

            Material clay = Lit(
                WorldMaterials + "Clay_URP.mat",
                ClayBase,
                ClayNormal,
                Color.white,
                .13f);
            Material mud = Lit(
                WorldMaterials + "Mud_URP.mat",
                MudBase,
                MudNormal,
                new Color(.76f, .72f, .64f, 1f),
                .24f);
            Material path = Lit(
                WorldMaterials + "GrassPath_URP.mat",
                PathBase,
                PathNormal,
                new Color(.78f, .83f, .68f, 1f),
                .10f);
            Material timber = Lit(
                WorldMaterials + "Timber_URP.mat",
                TimberBase,
                TimberNormal,
                Color.white,
                .18f);
            Material terrainMaterial = TerrainMaterial();
            Material water = WaterMaterial();

            GameObject ricePrefab = CreateVegetationPrefab(
                RicePrefab,
                RiceModel,
                RiceCard,
                RiceRoot + "Materials/Rice_LOD0_URP.mat",
                RiceRoot + "Materials/Rice_Card_URP.mat",
                RiceBase,
                RiceNormal,
                new Color(.50f, .72f, .28f, 1f),
                1.0f);
            GameObject grassPrefab = CreateVegetationPrefab(
                GrassPrefab,
                GrassModel,
                GrassCard,
                GrassRoot + "Materials/Grass_LOD0_URP.mat",
                GrassRoot + "Materials/Grass_Card_URP.mat",
                null,
                null,
                new Color(.38f, .58f, .20f, 1f),
                .72f);

            Material reed = Lit(
                WorldMaterials + "Reed_URP.mat",
                ReedBase,
                ReedNormal,
                new Color(.76f, .86f, .58f, 1f),
                .12f,
                false,
                false,
                true);
            Material banana = Lit(
                WorldMaterials + "Banana_URP.mat",
                BananaBase,
                BananaNormal,
                Color.white,
                .16f,
                false,
                false,
                true);
            Material coconut = Lit(
                WorldMaterials + "Coconut_URP.mat",
                CoconutBase,
                CoconutNormal,
                Color.white,
                .14f,
                false,
                false,
                true);
            Material fanPalm = Lit(
                WorldMaterials + "FanPalm_URP.mat",
                FanPalmBase,
                FanPalmNormal,
                Color.white,
                .14f,
                false,
                false,
                true);
            Material broadleaf = Lit(
                WorldMaterials + "Broadleaf_URP.mat",
                BroadleafBase,
                BroadleafNormal,
                new Color(.82f, .88f, .72f, 1f),
                .13f,
                false,
                false,
                true);
            Material shelter = Lit(
                WorldMaterials + "Shelter_URP.mat",
                ShelterBase,
                ShelterNormal,
                Color.white,
                .18f);
            Material dock = Lit(
                WorldMaterials + "Dock_URP.mat",
                DockBase,
                DockNormal,
                Color.white,
                .2f);
            Material boat = Lit(
                WorldMaterials + "Boat_URP.mat",
                BoatBase,
                BoatNormal,
                Color.white,
                .2f);

            TerrainData terrainData =
                CreateTerrainData();
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "AnGiangWorldLab";
            CreateScene(
                scene,
                terrainData,
                terrainMaterial,
                water,
                clay,
                mud,
                path,
                timber,
                ricePrefab,
                grassPrefab,
                reed,
                banana,
                coconut,
                fanPalm,
                broadleaf,
                shelter,
                dock,
                boat);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "AnGiangWorldLab built: true-3D terrain, canal, " +
                "instanced vegetation LODs, authored structures/props, " +
                "first-person movement, atmosphere, and layered CC0 audio.");
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                WorldRoot,
                WorldMaterials,
                WorldMeshes,
                WorldTerrain,
                WorldTextures,
                RiceRoot + "Materials/",
                RiceRoot + "Prefabs/",
                GrassRoot + "Materials/",
                GrassRoot + "Prefabs/"
            };
            foreach (string folder in folders)
            {
                Directory.CreateDirectory(
                    Path.Combine(ProjectRoot(), folder));
            }
            AssetDatabase.Refresh();
        }

        private static Material Lit(
            string path,
            string baseTexturePath,
            string normalPath,
            Color tint,
            float smoothness,
            bool alphaClip = false,
            bool transparent = false,
            bool doubleSided = false)
        {
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Lit shader is unavailable.");
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

            Texture2D baseTexture = string.IsNullOrWhiteSpace(
                baseTexturePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>(
                    baseTexturePath);
            Texture2D normal = string.IsNullOrWhiteSpace(normalPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            material.name = Path.GetFileNameWithoutExtension(path);
            material.SetTexture("_BaseMap", baseTexture);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", normal == null ? 0f : .82f);
            SetKeyword(material, "_NORMALMAP", normal != null);
            material.SetFloat("_AlphaClip", alphaClip ? 1f : 0f);
            material.SetFloat("_Cutoff", alphaClip ? .32f : .5f);
            SetKeyword(material, "_ALPHATEST_ON", alphaClip);
            material.SetFloat("_Cull", doubleSided ? 0f : 2f);
            material.doubleSidedGI = doubleSided;
            material.SetFloat("_Surface", transparent ? 1f : 0f);
            if (transparent)
            {
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_SrcBlend", 5f);
                material.SetFloat("_DstBlend", 10f);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = 3000;
                SetKeyword(
                    material,
                    "_SURFACE_TYPE_TRANSPARENT",
                    true);
            }
            else
            {
                material.SetFloat("_SrcBlend", 1f);
                material.SetFloat("_DstBlend", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.renderQueue = alphaClip ? 2450 : 2000;
                SetKeyword(
                    material,
                    "_SURFACE_TYPE_TRANSPARENT",
                    false);
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material TerrainMaterial()
        {
            string path = WorldMaterials + "Terrain_URP.mat";
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Terrain/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Terrain Lit shader is unavailable.");
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
            material.name = "Terrain_URP";
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material WaterMaterial()
        {
            Material material = Lit(
                WorldMaterials + "CanalWater_URP.mat",
                null,
                WaterNormalPath,
                new Color(.13f, .42f, .43f, .86f),
                .91f,
                false,
                true,
                true);
            material.SetTextureScale(
                "_BumpMap",
                new Vector2(8f, 24f));
            material.SetFloat("_BumpScale", .28f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetKeyword(
            Material material,
            string keyword,
            bool enabled)
        {
            if (enabled) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }

        private static GameObject CreateVegetationPrefab(
            string prefabPath,
            string lod0ModelPath,
            string cardTexturePath,
            string lod0MaterialPath,
            string cardMaterialPath,
            string lod0BasePath,
            string lod0NormalPath,
            Color tint,
            float height)
        {
            Material cardMaterial = Lit(
                cardMaterialPath,
                cardTexturePath,
                null,
                tint,
                .08f,
                true,
                false,
                true);
            Material lod0Material = string.IsNullOrWhiteSpace(
                lod0BasePath)
                ? cardMaterial
                : Lit(
                    lod0MaterialPath,
                    lod0BasePath,
                    lod0NormalPath,
                    tint,
                    .11f,
                    false,
                    false,
                    true);
            Mesh lod0 = LoadMesh(lod0ModelPath);
            GameObject importedModel =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    lod0ModelPath);
            MeshFilter importedFilter =
                importedModel.GetComponentInChildren<MeshFilter>(
                    true);
            Mesh lod1 = CreateCardMesh(
                cardMaterialPath.Replace(
                    "Materials/" +
                    Path.GetFileName(cardMaterialPath),
                    "Derived/" +
                    Path.GetFileNameWithoutExtension(
                        cardMaterialPath) + "_LOD1.asset"),
                2,
                height);
            Mesh lod2 = CreateCardMesh(
                cardMaterialPath.Replace(
                    "Materials/" +
                    Path.GetFileName(cardMaterialPath),
                    "Derived/" +
                    Path.GetFileNameWithoutExtension(
                        cardMaterialPath) + "_LOD2.asset"),
                1,
                height);

            GameObject root = new GameObject(
                Path.GetFileNameWithoutExtension(prefabPath));
            root.layer = 2;
            try
            {
                LODGroup group = root.AddComponent<LODGroup>();
                Renderer near = MeshChild(
                    root.transform,
                    "LOD0_Mesh",
                    lod0,
                    lod0Material,
                    true);
                near.transform.localPosition =
                    importedFilter.transform.localPosition;
                near.transform.localRotation =
                    importedFilter.transform.localRotation;
                near.transform.localScale =
                    importedFilter.transform.localScale;
                Renderer middle = MeshChild(
                    root.transform,
                    "LOD1_AlphaCards",
                    lod1,
                    cardMaterial,
                    false);
                Renderer far = MeshChild(
                    root.transform,
                    "LOD2_Billboard",
                    lod2,
                    cardMaterial,
                    false);
                far.gameObject.AddComponent<BillboardFacingCamera>();
                group.SetLODs(
                    new[]
                    {
                        new LOD(.34f, new[] { near }),
                        new LOD(.12f, new[] { middle }),
                        new LOD(.025f, new[] { far })
                    });
                group.fadeMode = LODFadeMode.CrossFade;
                group.animateCrossFading = true;
                group.RecalculateBounds();
                return PrefabUtility.SaveAsPrefabAsset(
                    root,
                    prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static Renderer MeshChild(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            bool shadows)
        {
            GameObject child = new GameObject(
                name,
                typeof(MeshFilter),
                typeof(MeshRenderer));
            child.layer = 2;
            child.transform.SetParent(parent, false);
            child.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer =
                child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows
                ? ShadowCastingMode.On
                : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static Mesh CreateCardMesh(
            string path,
            int planes,
            float height)
        {
            float width = height;
            var vertices = new List<Vector3>(planes * 4);
            var normals = new List<Vector3>(planes * 4);
            var uvs = new List<Vector2>(planes * 4);
            var triangles = new List<int>(planes * 6);
            for (int plane = 0; plane < planes; plane++)
            {
                float angle = Mathf.PI * plane / planes;
                Vector3 half = new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)) * width * .5f;
                int start = vertices.Count;
                vertices.Add(-half);
                vertices.Add(half);
                vertices.Add(half + Vector3.up * height);
                vertices.Add(-half + Vector3.up * height);
                Vector3 normal = Vector3.Cross(
                    Vector3.up,
                    half.normalized);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(0f, 1f));
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
            Mesh generated = new Mesh
            {
                name = Path.GetFileNameWithoutExtension(path)
            };
            generated.SetVertices(vertices);
            generated.SetNormals(normals);
            generated.SetUVs(0, uvs);
            generated.SetTriangles(triangles, 0);
            generated.RecalculateBounds();
            generated.UploadMeshData(false);
            return SaveMesh(generated, path);
        }

        private static Mesh SaveMesh(Mesh generated, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, path);
                return generated;
            }
            EditorUtility.CopySerialized(generated, existing);
            UnityEngine.Object.DestroyImmediate(generated);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Mesh LoadMesh(string path)
        {
            Mesh mesh = AssetDatabase
                .LoadAllAssetsAtPath(path)
                .OfType<Mesh>()
                .FirstOrDefault();
            if (mesh == null)
            {
                throw new InvalidOperationException(
                    $"No mesh imported at {path}");
            }
            return mesh;
        }

        private static TerrainData CreateTerrainData()
        {
            TerrainData data =
                AssetDatabase.LoadAssetAtPath<TerrainData>(
                    TerrainDataPath);
            if (data == null)
            {
                data = new TerrainData();
                AssetDatabase.CreateAsset(data, TerrainDataPath);
            }
            data.heightmapResolution = 129;
            data.size = new Vector3(120f, 7f, 120f);
            float[,] heights = new float[129, 129];
            for (int z = 0; z < 129; z++)
            {
                for (int x = 0; x < 129; x++)
                {
                    float worldX = Mathf.Lerp(-60f, 60f, x / 128f);
                    float worldZ = Mathf.Lerp(-60f, 60f, z / 128f);
                    heights[z, x] =
                        TerrainHeight(worldX, worldZ) / data.size.y;
                }
            }
            data.SetHeights(0, 0, heights);

            TerrainLayer[] layers =
            {
                TerrainLayer(
                    WorldTerrain + "GrassPath.terrainlayer",
                    PathBase,
                    PathNormal,
                    new Vector2(7f, 7f),
                    new Color(.36f, .40f, .29f, 1f)),
                TerrainLayer(
                    WorldTerrain + "Clay.terrainlayer",
                    ClayBase,
                    ClayNormal,
                    new Vector2(5f, 5f),
                    new Color(.50f, .38f, .28f, 1f)),
                TerrainLayer(
                    WorldTerrain + "Mud.terrainlayer",
                    MudBase,
                    MudNormal,
                    new Vector2(4f, 4f),
                    new Color(.39f, .32f, .26f, 1f))
            };
            data.terrainLayers = layers;
            data.alphamapResolution = 128;
            float[,,] maps = new float[128, 128, 3];
            for (int z = 0; z < 128; z++)
            {
                for (int x = 0; x < 128; x++)
                {
                    float worldX =
                        Mathf.Lerp(-60f, 60f, x / 127f);
                    float worldZ =
                        Mathf.Lerp(-60f, 60f, z / 127f);
                    float canal = Mathf.Abs(
                        worldX - CanalCenter(worldZ));
                    float dike = DikeProximity(worldX, worldZ);
                    float mudWeight = Mathf.Clamp01(
                        1f - (canal - 4.8f) / 5f);
                    float clayWeight = Mathf.Clamp01(
                        1f - dike / 2.2f) *
                        (1f - mudWeight * .55f);
                    float grassWeight = Mathf.Max(
                        .05f,
                        1f - mudWeight - clayWeight * .82f);
                    float total =
                        grassWeight + clayWeight + mudWeight;
                    maps[z, x, 0] = grassWeight / total;
                    maps[z, x, 1] = clayWeight / total;
                    maps[z, x, 2] = mudWeight / total;
                }
            }
            data.SetAlphamaps(0, 0, maps);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static TerrainLayer TerrainLayer(
            string path,
            string diffusePath,
            string normalPath,
            Vector2 tileSize,
            Color remapMaximum)
        {
            TerrainLayer layer =
                AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, path);
            }
            layer.diffuseTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    diffusePath);
            layer.normalMapTexture =
                AssetDatabase.LoadAssetAtPath<Texture2D>(
                    normalPath);
            layer.tileSize = tileSize;
            layer.normalScale = .82f;
            layer.smoothness = .035f;
            layer.smoothnessSource =
                TerrainLayerSmoothnessSource.ConstantOnly;
            layer.metallic = 0f;
            layer.diffuseRemapMin = Vector4.zero;
            layer.diffuseRemapMax = remapMaximum;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static float TerrainHeight(
            float x,
            float z)
        {
            float center = CanalCenter(z);
            float canal = Mathf.Abs(x - center);
            float subtle =
                Mathf.Sin(x * .16f + z * .08f) * .035f +
                Mathf.Sin(z * .31f) * .018f;
            float height;
            if (canal < 4.7f)
            {
                height = .16f + canal / 4.7f * .24f;
            }
            else if (canal < 8.4f)
            {
                float t = (canal - 4.7f) / 3.7f;
                height = Mathf.Lerp(.40f, 1.33f, t * t);
            }
            else
            {
                height = 1.12f + subtle;
            }
            float dike = DikeProximity(x, z);
            height += Mathf.Clamp01(1f - dike / 1.45f) * .25f;
            float edge = Mathf.Max(
                Mathf.Abs(x),
                Mathf.Abs(z));
            height += Mathf.Clamp01((edge - 54f) / 6f) * .7f;
            return height;
        }

        private static float CanalCenter(float z) =>
            -12f + Mathf.Sin((z + 8f) * .045f) * 2.8f;

        private static float DikeProximity(float x, float z)
        {
            float eastBank = Mathf.Abs(
                x - (CanalCenter(z) + 8.9f));
            float westBank = Mathf.Abs(
                x - (CanalCenter(z) - 8.9f));
            float gridX = Mathf.Min(
                DistanceToAny(x, 4f, 22f, 40f, 56f, -24f, -42f),
                Mathf.Min(eastBank, westBank));
            float gridZ = DistanceToAny(
                z,
                -48f,
                -24f,
                0f,
                24f,
                48f);
            return Mathf.Min(gridX, gridZ);
        }

        private static float DistanceToAny(
            float value,
            params float[] marks)
        {
            float minimum = float.MaxValue;
            foreach (float mark in marks)
            {
                minimum = Mathf.Min(
                    minimum,
                    Mathf.Abs(value - mark));
            }
            return minimum;
        }

        private static void CreateScene(
            Scene scene,
            TerrainData terrainData,
            Material terrainMaterial,
            Material waterMaterial,
            Material clay,
            Material mud,
            Material path,
            Material timber,
            GameObject ricePrefab,
            GameObject grassPrefab,
            Material reedMaterial,
            Material bananaMaterial,
            Material coconutMaterial,
            Material fanPalmMaterial,
            Material broadleafMaterial,
            Material shelterMaterial,
            Material dockMaterial,
            Material boatMaterial)
        {
            GameObject world = new GameObject("AnGiangWorldLab");
            AnGiangWorldLabController lab =
                world.AddComponent<AnGiangWorldLabController>();

            GameObject terrainObject =
                Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "DeltaTerrain";
            terrainObject.layer = 2;
            terrainObject.transform.position =
                new Vector3(-60f, 0f, -60f);
            terrainObject.transform.SetParent(world.transform, true);
            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.materialTemplate = terrainMaterial;
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 4f;
            terrain.basemapDistance = 110f;
            terrain.shadowCastingMode = ShadowCastingMode.On;

            CreateCanal(world.transform, waterMaterial);
            CreateWorldBounds(world.transform);
            CreateVegetation(
                world.transform,
                terrain,
                ricePrefab,
                grassPrefab,
                reedMaterial,
                bananaMaterial,
                coconutMaterial,
                fanPalmMaterial,
                broadleafMaterial);
            CreateStructures(
                world.transform,
                shelterMaterial,
                dockMaterial,
                boatMaterial);
            FirstPersonWalker walker =
                CreatePlayer(world.transform);
            CreateLighting(world.transform, walker.ViewCamera);
            CreateAudio(world.transform);

            var viewpoints =
                new[]
                {
                    View(
                        "spawn",
                        -2.2f,
                        -42f,
                        -12f,
                        -3f),
                    View(
                        "canal",
                        -1.6f,
                        -20f,
                        325f,
                        -5f),
                    View(
                        "rice",
                        21f,
                        12f,
                        48f,
                        -4f),
                    View(
                        "shelter",
                        5f,
                        -12f,
                        128f,
                        -2f)
                };
            lab.Configure(walker, viewpoints);
            walker.Teleport(
                viewpoints[0].Position,
                viewpoints[0].Heading,
                viewpoints[0].Pitch);
        }

        private static AnGiangWorldLabController.Viewpoint View(
            string name,
            float x,
            float z,
            float heading,
            float pitch) =>
            new AnGiangWorldLabController.Viewpoint
            {
                Name = name,
                Position = new Vector3(
                    x,
                    TerrainHeight(x, z) + .04f,
                    z),
                Heading = heading,
                Pitch = pitch
            };

        private static void CreateCanal(
            Transform parent,
            Material material)
        {
            Mesh mesh = CreateCanalMesh();
            GameObject canal = new GameObject(
                "IrrigationCanal",
                typeof(MeshFilter),
                typeof(MeshRenderer),
                typeof(CanalWaterSurface));
            canal.layer = 2;
            canal.transform.SetParent(parent, false);
            canal.transform.position = new Vector3(0f, .69f, 0f);
            canal.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer =
                canal.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
        }

        private static Mesh CreateCanalMesh()
        {
            const int segments = 80;
            const float halfWidth = 5.25f;
            var vertices = new Vector3[(segments + 1) * 2];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (int index = 0; index <= segments; index++)
            {
                float t = index / (float)segments;
                float z = Mathf.Lerp(-60f, 60f, t);
                float center = CanalCenter(z);
                float next = CanalCenter(
                    Mathf.Min(60f, z + .2f));
                Vector2 tangent = new Vector2(
                    next - center,
                    .2f).normalized;
                Vector2 side = new Vector2(
                    tangent.y,
                    -tangent.x);
                Vector3 left = new Vector3(
                    center - side.x * halfWidth,
                    0f,
                    z - side.y * halfWidth);
                Vector3 right = new Vector3(
                    center + side.x * halfWidth,
                    0f,
                    z + side.y * halfWidth);
                vertices[index * 2] = left;
                vertices[index * 2 + 1] = right;
                uvs[index * 2] = new Vector2(0f, t * 18f);
                uvs[index * 2 + 1] = new Vector2(1f, t * 18f);
                if (index == segments) continue;
                int start = index * 2;
                int triangle = index * 6;
                triangles[triangle] = start;
                triangles[triangle + 1] = start + 2;
                triangles[triangle + 2] = start + 1;
                triangles[triangle + 3] = start + 1;
                triangles[triangle + 4] = start + 2;
                triangles[triangle + 5] = start + 3;
            }
            Mesh generated = new Mesh
            {
                name = "CanalWaterMesh"
            };
            generated.vertices = vertices;
            generated.uv = uvs;
            generated.triangles = triangles;
            generated.RecalculateNormals();
            generated.RecalculateBounds();
            return SaveMesh(
                generated,
                WorldMeshes + "CanalWater.asset");
        }

        private static void CreateWorldBounds(Transform parent)
        {
            GameObject bounds = new GameObject("NaturalWorldBounds");
            bounds.layer = 2;
            bounds.transform.SetParent(parent, false);
            BoxBoundary(
                bounds.transform,
                new Vector3(0f, 2f, -61f),
                new Vector3(124f, 5f, 2f));
            BoxBoundary(
                bounds.transform,
                new Vector3(0f, 2f, 61f),
                new Vector3(124f, 5f, 2f));
            BoxBoundary(
                bounds.transform,
                new Vector3(-61f, 2f, 0f),
                new Vector3(2f, 5f, 124f));
            BoxBoundary(
                bounds.transform,
                new Vector3(61f, 2f, 0f),
                new Vector3(2f, 5f, 124f));
        }

        private static void BoxBoundary(
            Transform parent,
            Vector3 position,
            Vector3 size)
        {
            GameObject boundary = new GameObject(
                "VegetatedBoundary",
                typeof(BoxCollider));
            boundary.layer = 2;
            boundary.transform.SetParent(parent, false);
            boundary.transform.position = position;
            boundary.GetComponent<BoxCollider>().size = size;
        }

        private static void CreateVegetation(
            Transform parent,
            Terrain terrain,
            GameObject ricePrefab,
            GameObject grassPrefab,
            Material reedMaterial,
            Material bananaMaterial,
            Material coconutMaterial,
            Material fanPalmMaterial,
            Material broadleafMaterial)
        {
            GameObject root = new GameObject("Vegetation");
            root.layer = 2;
            root.transform.SetParent(parent, false);

            Vector3[] riceCenters =
            {
                new Vector3(13f, 0f, -36f),
                new Vector3(32f, 0f, -36f),
                new Vector3(13f, 0f, -12f),
                new Vector3(32f, 0f, -12f),
                new Vector3(13f, 0f, 12f),
                new Vector3(32f, 0f, 12f),
                new Vector3(13f, 0f, 36f),
                new Vector3(32f, 0f, 36f),
                new Vector3(-32f, 0f, -34f),
                new Vector3(-48f, 0f, -34f),
                new Vector3(-32f, 0f, 14f),
                new Vector3(-48f, 0f, 14f)
            };
            for (int index = 0;
                 index < riceCenters.Length;
                 index++)
            {
                GameObject field = new GameObject(
                    $"Paddy_{index + 1:00}");
                field.layer = 2;
                field.transform.SetParent(root.transform, false);
                field.transform.position = riceCenters[index];
                InstancedVegetationField instancing =
                    field.AddComponent<InstancedVegetationField>();
                Vector2 size = index < 8
                    ? new Vector2(15f, 20f)
                    : new Vector2(12f, 20f);
                instancing.Configure(
                    ricePrefab,
                    terrain,
                    size,
                    .62f,
                    140 + index * 29,
                    .82f,
                    1.08f,
                    index % 3 == 0 ? .12f : .065f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject bank = new GameObject(
                    side < 0 ? "WestBankGrass" : "EastBankGrass");
                bank.layer = 2;
                bank.transform.SetParent(root.transform, false);
                bank.transform.position = new Vector3(
                    CanalCenter(0f) + side * 7.4f,
                    0f,
                    0f);
                InstancedVegetationField instancing =
                    bank.AddComponent<InstancedVegetationField>();
                instancing.Configure(
                    grassPrefab,
                    terrain,
                    new Vector2(2.6f, 112f),
                    .52f,
                    side < 0 ? 811 : 977,
                    .68f,
                    1.14f,
                    .14f);
            }

            for (int index = 0; index < 36; index++)
            {
                float z = -52f + index * 3.0f;
                int side = index % 2 == 0 ? -1 : 1;
                float x = CanalCenter(z) + side * 6.4f;
                PlaceAuthoredModel(
                    ReedModel,
                    $"Reeds_{index + 1:00}",
                    new Vector3(x, TerrainHeight(x, z), z),
                    new Vector3(
                        0f,
                        (index * 47) % 360,
                        0f),
                    reedMaterial,
                    root.transform,
                    false,
                    true,
                    .78f);
            }

            PlaceTreeGroup(
                BananaModel,
                bananaMaterial,
                root.transform,
                "Banana",
                new[]
                {
                    new Vector3(24f, 0f, -29f),
                    new Vector3(28f, 0f, -31f),
                    new Vector3(25f, 0f, -34f),
                    new Vector3(46f, 0f, 47f)
                },
                1.6f);
            PlaceTreeGroup(
                FanPalmModel,
                fanPalmMaterial,
                root.transform,
                "Palmyra",
                new[]
                {
                    new Vector3(-48f, 0f, -49f),
                    new Vector3(-39f, 0f, 43f),
                    new Vector3(51f, 0f, -44f),
                    new Vector3(47f, 0f, 42f),
                    new Vector3(-53f, 0f, 20f)
                },
                .95f);
            PlaceTreeGroup(
                CoconutModel,
                coconutMaterial,
                root.transform,
                "Coconut",
                new[]
                {
                    new Vector3(31f, 0f, -45f),
                    new Vector3(-44f, 0f, 50f)
                },
                .8f);
            PlaceTreeGroup(
                BroadleafModel,
                broadleafMaterial,
                root.transform,
                "Broadleaf",
                new[]
                {
                    new Vector3(55f, 0f, -8f),
                    new Vector3(-55f, 0f, -8f),
                    new Vector3(54f, 0f, 22f),
                    new Vector3(-52f, 0f, -25f)
                },
                .88f);
        }

        private static void PlaceTreeGroup(
            string model,
            Material material,
            Transform parent,
            string prefix,
            IEnumerable<Vector3> positions,
            float scale)
        {
            int index = 0;
            foreach (Vector3 raw in positions)
            {
                index++;
                Vector3 position = new Vector3(
                    raw.x,
                    TerrainHeight(raw.x, raw.z),
                    raw.z);
                PlaceAuthoredModel(
                    model,
                    $"{prefix}_{index:00}",
                    position,
                    new Vector3(
                        0f,
                        (index * 83) % 360,
                        0f),
                    material,
                    parent,
                    true,
                    true,
                    scale);
            }
        }

        private static void CreateStructures(
            Transform parent,
            Material shelterMaterial,
            Material dockMaterial,
            Material boatMaterial)
        {
            GameObject root = new GameObject("RuralStructures");
            root.layer = 2;
            root.transform.SetParent(parent, false);
            float shelterX = 21f;
            float shelterZ = -25f;
            PlaceAuthoredModel(
                ShelterModel,
                "FieldShelter",
                new Vector3(
                    shelterX,
                    TerrainHeight(shelterX, shelterZ),
                    shelterZ),
                new Vector3(0f, -18f, 0f),
                shelterMaterial,
                root.transform,
                true,
                false,
                1f);

            float dockX = -4.6f;
            float dockZ = -14f;
            PlaceAuthoredModel(
                DockModel,
                "SamplingDock",
                new Vector3(
                    dockX,
                    .68f,
                    dockZ),
                new Vector3(0f, 88f, 0f),
                dockMaterial,
                root.transform,
                true,
                false,
                .88f);

            float boatZ = -7f;
            PlaceAuthoredModel(
                BoatModel,
                "LocalWoodenBoat",
                new Vector3(
                    CanalCenter(boatZ),
                    .73f,
                    boatZ),
                new Vector3(0f, 8f, -1.5f),
                boatMaterial,
                root.transform,
                false,
                false,
                1f);
        }

        private static GameObject PlaceAuthoredModel(
            string modelPath,
            string name,
            Vector3 position,
            Vector3 euler,
            Material material,
            Transform parent,
            bool collider,
            bool wind,
            float scale)
        {
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (source == null)
            {
                throw new InvalidOperationException(
                    $"Missing authored model {modelPath}");
            }

            GameObject anchor = new GameObject(name);
            anchor.layer = 2;
            anchor.transform.SetParent(parent, false);
            anchor.transform.position = position;
            anchor.transform.rotation = Quaternion.Euler(euler);
            anchor.transform.localScale = Vector3.one * scale;

            GameObject instance =
                PrefabUtility.InstantiatePrefab(
                    source,
                    anchor.transform) as GameObject;
            if (instance == null)
            {
                UnityEngine.Object.DestroyImmediate(anchor);
                throw new InvalidOperationException(
                    $"Could not instantiate authored model {modelPath}");
            }
            instance.name = "Visual";
            instance.layer = 2;
            foreach (Transform child in
                     instance.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 2;
            }
            Renderer[] renderers =
                instance.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int index = 1; index < renderers.Length; index++)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }
                float desiredBase = position.y;
                anchor.transform.position +=
                    Vector3.up * (desiredBase - bounds.min.y);
                if (collider)
                {
                    BoxCollider box =
                        anchor.AddComponent<BoxCollider>();
                    Bounds localBounds = CalculateLocalBounds(
                        anchor.transform,
                        renderers);
                    box.center = localBounds.center;
                    box.size = localBounds.size;
                }
            }
            if (wind)
            {
                WindSway sway = anchor.AddComponent<WindSway>();
                sway.Configure(
                    name.StartsWith("Reeds", StringComparison.Ordinal)
                        ? 2.2f
                        : 1.05f,
                    name.StartsWith("Reeds", StringComparison.Ordinal)
                        ? .86f
                        : .58f);
            }
            return anchor;
        }

        private static Bounds CalculateLocalBounds(
            Transform anchor,
            IEnumerable<Renderer> renderers)
        {
            bool initialized = false;
            Bounds localBounds = default;
            foreach (Renderer renderer in renderers)
            {
                Bounds source = renderer.localBounds;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 rendererPoint =
                                source.center +
                                Vector3.Scale(
                                    source.extents,
                                    new Vector3(x, y, z));
                            Vector3 worldPoint =
                                renderer.transform.TransformPoint(
                                    rendererPoint);
                            Vector3 anchorPoint =
                                anchor.InverseTransformPoint(worldPoint);
                            if (!initialized)
                            {
                                localBounds = new Bounds(
                                    anchorPoint,
                                    Vector3.zero);
                                initialized = true;
                            }
                            else
                            {
                                localBounds.Encapsulate(anchorPoint);
                            }
                        }
                    }
                }
            }
            return localBounds;
        }

        private static FirstPersonWalker CreatePlayer(Transform parent)
        {
            GameObject player = new GameObject(
                "FirstPersonPlayer",
                typeof(CharacterController),
                typeof(FirstPersonWalker));
            player.transform.SetParent(parent, false);
            CharacterController controller =
                player.GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .31f;
            controller.center = new Vector3(0f, .9f, 0f);
            controller.stepOffset = .32f;
            controller.slopeLimit = 46f;
            controller.skinWidth = .035f;

            GameObject cameraObject = new GameObject(
                "PlayerCamera",
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(
                player.transform,
                false);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 68f;
            camera.nearClipPlane = .05f;
            camera.farClipPlane = 240f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            UniversalAdditionalCameraData cameraData =
                cameraObject.AddComponent<
                    UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing =
                AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            FirstPersonWalker walker =
                player.GetComponent<FirstPersonWalker>();
            walker.Configure(camera, 1.65f);
            return walker;
        }

        private static void CreateLighting(
            Transform parent,
            Camera camera)
        {
            Light sun = new GameObject(
                    "WarmMorningSun",
                    typeof(Light))
                .GetComponent<Light>();
            sun.transform.SetParent(parent, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1f, .78f, .58f);
            sun.intensity = .98f;
            sun.transform.rotation =
                Quaternion.Euler(31f, -38f, 0f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .84f;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor =
                new Color(.46f, .56f, .59f);
            RenderSettings.ambientEquatorColor =
                new Color(.34f, .39f, .34f);
            RenderSettings.ambientGroundColor =
                new Color(.17f, .15f, .12f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor =
                new Color(.66f, .72f, .69f);
            RenderSettings.fogDensity = .0082f;
            RenderSettings.skybox = SkyboxMaterial();

            VolumeProfile profile = GradeProfile();
            Volume volume = new GameObject(
                    "HumidMorningGrade",
                    typeof(Volume))
                .GetComponent<Volume>();
            volume.transform.SetParent(parent, false);
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        private static Material SkyboxMaterial()
        {
            string path = WorldMaterials + "MorningSkybox.mat";
            Shader shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Procedural skybox shader is unavailable.");
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
            material.SetColor(
                "_SkyTint",
                new Color(.42f, .55f, .61f));
            material.SetColor(
                "_GroundColor",
                new Color(.43f, .40f, .33f));
            material.SetFloat("_AtmosphereThickness", .72f);
            material.SetFloat("_SunSize", .035f);
            material.SetFloat("_SunSizeConvergence", 5f);
            material.SetFloat("_Exposure", 1.08f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static VolumeProfile GradeProfile()
        {
            VolumeProfile profile =
                AssetDatabase.LoadAssetAtPath<VolumeProfile>(
                    GradeProfilePath);
            if (profile == null)
            {
                profile =
                    ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(
                    profile,
                    GradeProfilePath);
            }
            if (!profile.TryGet(out ColorAdjustments color))
            {
                color = profile.Add<ColorAdjustments>(true);
            }
            color.postExposure.Override(-.16f);
            color.contrast.Override(7f);
            color.saturation.Override(-5f);
            color.colorFilter.Override(
                new Color(1f, .97f, .90f, 1f));
            if (!profile.TryGet(out WhiteBalance balance))
            {
                balance = profile.Add<WhiteBalance>(true);
            }
            balance.temperature.Override(8f);
            balance.tint.Override(-2f);
            if (!profile.TryGet(out Vignette vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.intensity.Override(.11f);
            vignette.smoothness.Override(.42f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void CreateAudio(Transform parent)
        {
            CreateLoop(
                parent,
                "CanalAmbience",
                "Assets/AgriVerse/Art/Audio/Ambience/" +
                "Canal_Loop.ogg",
                .15f);
            CreateLoop(
                parent,
                "WindAmbience",
                "Assets/AgriVerse/Art/Audio/Ambience/" +
                "Wind_Loop.ogg",
                .065f);
            CreateLoop(
                parent,
                "BirdAmbience",
                "Assets/AgriVerse/Art/Audio/Ambience/" +
                "Birds_Loop.ogg",
                .045f);
            CreateLoop(
                parent,
                "InsectAmbience",
                "Assets/AgriVerse/Art/Audio/Ambience/" +
                "Insects_Loop.ogg",
                .035f);
        }

        private static void CreateLoop(
            Transform parent,
            string name,
            string clipPath,
            float volume)
        {
            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Missing ambience {clipPath}");
            }
            AudioSource source = new GameObject(
                    name,
                    typeof(AudioSource))
                .GetComponent<AudioSource>();
            source.transform.SetParent(parent, false);
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = volume;
            source.spatialBlend = 0f;
            source.priority = 160;
        }

        private static void CreateWaterNormal()
        {
            const int size = 256;
            var texture = new Texture2D(
                size,
                size,
                TextureFormat.RGBA32,
                false,
                true);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u =
                        x / (float)size * Mathf.PI * 2f;
                    float v =
                        y / (float)size * Mathf.PI * 2f;
                    float nx =
                        Mathf.Sin(u * 3f + v * 2f) * .16f +
                        Mathf.Sin(u * 5f - v * 3f) * .08f;
                    float ny =
                        Mathf.Cos(v * 4f + u * 2f) * .14f +
                        Mathf.Cos(u * 4f - v * 2f) * .07f;
                    Vector3 normal =
                        new Vector3(-nx, -ny, 1f).normalized;
                    pixels[y * size + x] = new Color(
                        normal.x * .5f + .5f,
                        normal.y * .5f + .5f,
                        normal.z * .5f + .5f,
                        1f);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            File.WriteAllBytes(
                Path.Combine(ProjectRoot(), WaterNormalPath),
                texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(
                WaterNormalPath,
                ImportAssetOptions.ForceUpdate);
        }

        private static string ProjectRoot() =>
            Directory.GetParent(Application.dataPath)?.FullName ??
            throw new InvalidOperationException(
                "Unity project root is unavailable.");
    }
}
#endif
