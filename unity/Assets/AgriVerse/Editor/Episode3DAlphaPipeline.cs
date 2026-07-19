#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AgriVerse.Client.Editor
{
    public static class Episode3DAlphaPipeline
    {
        private const string WorldScene =
            "Assets/Scenes/AnGiangWorldLab.unity";
        private const string AlphaScene =
            "Assets/Scenes/Episode3DAlpha.unity";
        private const string MaiPrefab =
            "Assets/AgriVerse/Art/Characters/Mai/Prefabs/Mai.prefab";
        private const string Root =
            "Assets/AgriVerse/Art/Environment/WorldLab/";
        private const string Materials = Root + "Materials/";
        private const string Meshes = Root + "Meshes/";
        private const string ConfigPath =
            Root + "Episode3DWorldConfig.asset";
        private const string AudioRoot =
            "Assets/AgriVerse/Art/Audio/SFX/Water/";

        [MenuItem("AgriVerse/Art/Build Episode 3D Alpha")]
        public static void BuildEpisode3DAlpha()
        {
            Scene scene = EditorSceneManager.OpenScene(
                WorldScene,
                OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, AlphaScene);

            FirstPersonWalker walker =
                FindInScene<FirstPersonWalker>(scene);
            Terrain terrain = FindInScene<Terrain>(scene);
            if (walker == null || terrain == null)
            {
                throw new InvalidOperationException(
                    "AnGiangWorldLab must provide a walker and terrain.");
            }
            Episode3DWorldConfig config =
                LoadOrCreateConfig();

            Material ringMaterial = TransparentUnlit(
                Materials + "SamplingRing_URP.mat",
                new Color(1f, .70f, .24f, .10f));
            Material vialMaterial = TransparentUnlit(
                Materials + "SampleVial_URP.mat",
                new Color(.83f, .95f, .91f, .48f));
            Material sampleMaterial = TransparentUnlit(
                Materials + "SampleWater_URP.mat",
                new Color(.17f, .48f, .47f, .82f));
            Material capMaterial = OpaqueLit(
                Materials + "SampleVialCap_URP.mat",
                new Color(.82f, .60f, .24f, 1f));

            Mesh ringMesh = CreateRingMesh(
                Meshes + "SamplingRing.asset");
            Mesh vialMesh = CreateCylinderMesh(
                Meshes + "SampleVial.asset",
                .026f,
                .18f,
                24);
            Mesh fillMesh = CreateCylinderMesh(
                Meshes + "SampleFill.asset",
                .021f,
                .105f,
                20);
            Mesh capMesh = CreateCylinderMesh(
                Meshes + "SampleCap.asset",
                .031f,
                .026f,
                20);

            WaterSampleHotspot hotspot = CreateHotspot(
                scene,
                ringMesh,
                ringMaterial,
                config.HotspotPosition);
            Animator maiAnimator = CreateMai(
                scene,
                terrain,
                walker,
                config.MaiPosition);
            CreateVial(
                walker.ViewCamera.transform,
                vialMesh,
                fillMesh,
                capMesh,
                vialMaterial,
                sampleMaterial,
                capMaterial,
                out GameObject vial,
                out GameObject fill);

            GameObject alphaRoot = new GameObject("Episode3DAlpha");
            SceneManager.MoveGameObjectToScene(alphaRoot, scene);
            InvestigationController investigation =
                alphaRoot.AddComponent<InvestigationController>();
            investigation.ConfigurePresentation(
                createUi: false,
                createMarkers: false);
            Episode3DAlphaController alpha =
                alphaRoot.AddComponent<Episode3DAlphaController>();

            Vector3 approach = GroundPosition(
                terrain,
                config.ApproachPosition);
            alpha.Configure(
                investigation,
                walker,
                hotspot,
                config.ArrivalSiteId,
                maiAnimator,
                vial,
                fill,
                approach,
                config.ApproachHeading,
                LoadClip("WaterScoop_01.wav"),
                LoadClip("VialHandle_01.wav"),
                LoadClip("VialCap_01.wav"));

            EditorSceneManager.SaveScene(scene, AlphaScene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "Episode3DAlpha built: Mai arrival, first-person field walk, " +
                "scenario-driven water prediction/sample, notebook, and CC0 SFX.");
        }

        private static WaterSampleHotspot CreateHotspot(
            Scene scene,
            Mesh mesh,
            Material material,
            Vector3 position)
        {
            GameObject root = new GameObject(
                "WaterSampleHotspot",
                typeof(SphereCollider),
                typeof(WaterSampleHotspot));
            SceneManager.MoveGameObjectToScene(root, scene);
            root.layer = 0;
            root.transform.position = position;
            SphereCollider collider =
                root.GetComponent<SphereCollider>();
            collider.center = new Vector3(0f, .65f, 0f);
            collider.radius = .92f;
            collider.isTrigger = true;

            GameObject ring = new GameObject(
                "WaterFocusRing",
                typeof(MeshFilter),
                typeof(MeshRenderer));
            ring.layer = 0;
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0f, .015f, 0f);
            ring.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = ring.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            WaterSampleHotspot hotspot =
                root.GetComponent<WaterSampleHotspot>();
            hotspot.Configure(renderer, 3.8f);
            return hotspot;
        }

        private static Animator CreateMai(
            Scene scene,
            Terrain terrain,
            FirstPersonWalker walker,
            Vector3 configuredPosition)
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(MaiPrefab);
            if (prefab == null)
            {
                throw new InvalidOperationException(
                    "The verified Mai prefab is unavailable.");
            }
            GameObject mai =
                PrefabUtility.InstantiatePrefab(prefab, scene)
                as GameObject;
            if (mai == null)
            {
                throw new InvalidOperationException(
                    "Mai could not be added to the alpha scene.");
            }
            mai.name = "Mai_FieldCoordinator";
            Vector3 position = GroundPosition(
                terrain,
                configuredPosition);
            mai.transform.position = position;
            Vector3 direction =
                walker.transform.position - position;
            direction.y = 0f;
            mai.transform.rotation =
                Quaternion.LookRotation(
                    direction.normalized,
                    Vector3.up);
            foreach (Transform child in
                     mai.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = 2;
            }
            Animator animator =
                mai.GetComponentInChildren<Animator>(true);
            MaiGuideController guide =
                mai.GetComponent<MaiGuideController>();
            if (animator == null || guide == null)
            {
                throw new InvalidOperationException(
                    "Mai must retain her verified Animator and guide behavior.");
            }
            guide.Configure(animator, walker.ViewCamera.transform);
            return animator;
        }

        private static void CreateVial(
            Transform camera,
            Mesh vialMesh,
            Mesh fillMesh,
            Mesh capMesh,
            Material vialMaterial,
            Material fillMaterial,
            Material capMaterial,
            out GameObject root,
            out GameObject fill)
        {
            root = new GameObject("FieldSampleVial");
            root.layer = 2;
            root.transform.SetParent(camera, false);
            root.transform.localPosition =
                new Vector3(.26f, -.19f, .52f);
            root.transform.localRotation =
                Quaternion.Euler(8f, -14f, -7f);

            MeshObject(
                root.transform,
                "Vial",
                vialMesh,
                vialMaterial,
                Vector3.zero);
            fill = MeshObject(
                root.transform,
                "CollectedWater",
                fillMesh,
                fillMaterial,
                new Vector3(0f, .008f, 0f));
            MeshObject(
                root.transform,
                "Cap",
                capMesh,
                capMaterial,
                new Vector3(0f, .18f, 0f));
            fill.SetActive(false);
            root.SetActive(false);
        }

        private static GameObject MeshObject(
            Transform parent,
            string name,
            Mesh mesh,
            Material material,
            Vector3 position)
        {
            GameObject item = new GameObject(
                name,
                typeof(MeshFilter),
                typeof(MeshRenderer));
            item.layer = 2;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer =
                item.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return item;
        }

        private static Mesh CreateRingMesh(string path)
        {
            const int segments = 64;
            const float innerRadius = .48f;
            const float outerRadius = .68f;
            var vertices = new Vector3[segments * 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 6];
            for (int index = 0; index < segments; index++)
            {
                float angle =
                    index / (float)segments * Mathf.PI * 2f;
                Vector3 direction =
                    new Vector3(
                        Mathf.Cos(angle),
                        0f,
                        Mathf.Sin(angle));
                vertices[index * 2] = direction * innerRadius;
                vertices[index * 2 + 1] = direction * outerRadius;
                normals[index * 2] = Vector3.up;
                normals[index * 2 + 1] = Vector3.up;
                uvs[index * 2] =
                    new Vector2(index / (float)segments, 0f);
                uvs[index * 2 + 1] =
                    new Vector2(index / (float)segments, 1f);
                int next = (index + 1) % segments;
                int start = index * 6;
                triangles[start] = index * 2;
                triangles[start + 1] = next * 2 + 1;
                triangles[start + 2] = index * 2 + 1;
                triangles[start + 3] = index * 2;
                triangles[start + 4] = next * 2;
                triangles[start + 5] = next * 2 + 1;
            }
            Mesh mesh = new Mesh
            {
                name = "SamplingRing",
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            return SaveMesh(path, mesh);
        }

        private static Mesh CreateCylinderMesh(
            string path,
            float radius,
            float height,
            int segments)
        {
            int sideVertices = (segments + 1) * 2;
            var vertices =
                new Vector3[sideVertices + segments * 2 + 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var triangles = new int[segments * 12];
            for (int index = 0; index <= segments; index++)
            {
                float amount = index / (float)segments;
                float angle = amount * Mathf.PI * 2f;
                Vector3 radial = new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle));
                vertices[index * 2] = radial * radius;
                vertices[index * 2 + 1] =
                    radial * radius + Vector3.up * height;
                normals[index * 2] = radial;
                normals[index * 2 + 1] = radial;
                uvs[index * 2] = new Vector2(amount, 0f);
                uvs[index * 2 + 1] = new Vector2(amount, 1f);
                if (index == segments) continue;
                int triangle = index * 12;
                int side = index * 2;
                triangles[triangle] = side;
                triangles[triangle + 1] = side + 1;
                triangles[triangle + 2] = side + 3;
                triangles[triangle + 3] = side;
                triangles[triangle + 4] = side + 3;
                triangles[triangle + 5] = side + 2;

                int bottom = sideVertices + index;
                int top = sideVertices + segments + index;
                vertices[bottom] = radial * radius;
                vertices[top] =
                    radial * radius + Vector3.up * height;
                normals[bottom] = Vector3.down;
                normals[top] = Vector3.up;
                int next = (index + 1) % segments;
                int bottomCenter =
                    sideVertices + segments * 2;
                int topCenter = bottomCenter + 1;
                triangles[triangle + 6] = bottomCenter;
                triangles[triangle + 7] =
                    sideVertices + next;
                triangles[triangle + 8] = bottom;
                triangles[triangle + 9] = topCenter;
                triangles[triangle + 10] = top;
                triangles[triangle + 11] =
                    sideVertices + segments + next;
            }
            vertices[vertices.Length - 2] = Vector3.zero;
            vertices[vertices.Length - 1] =
                Vector3.up * height;
            normals[normals.Length - 2] = Vector3.down;
            normals[normals.Length - 1] = Vector3.up;
            Mesh mesh = new Mesh
            {
                name = Path.GetFileNameWithoutExtension(path),
                vertices = vertices,
                normals = normals,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateBounds();
            return SaveMesh(path, mesh);
        }

        private static Mesh SaveMesh(string path, Mesh mesh)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Material TransparentUnlit(
            string path,
            Color color)
        {
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Unlit");
            Material material = LoadOrCreateMaterial(path, shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", 5f);
            material.SetFloat("_DstBlend", 10f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", 0f);
            material.renderQueue = 3000;
            material.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material OpaqueLit(
            string path,
            Color color)
        {
            Shader shader = Shader.Find(
                "Universal Render Pipeline/Lit");
            Material material = LoadOrCreateMaterial(path, shader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", .25f);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_ZWrite", 1f);
            material.renderQueue = 2000;
            material.DisableKeyword(
                "_SURFACE_TYPE_TRANSPARENT");
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreateMaterial(
            string path,
            Shader shader)
        {
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Required URP shader is unavailable.");
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
            material.name =
                Path.GetFileNameWithoutExtension(path);
            return material;
        }

        private static AudioClip LoadClip(string filename)
        {
            AudioClip clip =
                AssetDatabase.LoadAssetAtPath<AudioClip>(
                    AudioRoot + filename);
            if (clip == null)
            {
                throw new InvalidOperationException(
                    $"Missing sample audio {filename}");
            }
            return clip;
        }

        private static Episode3DWorldConfig LoadOrCreateConfig()
        {
            Episode3DWorldConfig config =
                AssetDatabase.LoadAssetAtPath<
                    Episode3DWorldConfig>(ConfigPath);
            if (config != null) return config;

            config =
                ScriptableObject.CreateInstance<
                    Episode3DWorldConfig>();
            config.Configure(
                "vietnam-mekong-salinity",
                "upstream",
                new Vector3(-.25f, 0f, -36.3f),
                new Vector3(-7.35f, .735f, -14f),
                new Vector3(-4.6f, 0f, -16.4f),
                300f);
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        private static Vector3 GroundPosition(
            Terrain terrain,
            Vector3 position)
        {
            position.y =
                terrain.SampleHeight(position) +
                terrain.transform.position.y +
                .03f;
            return position;
        }

        private static T FindInScene<T>(Scene scene)
            where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<T>(true))
                .FirstOrDefault();
    }
}
#endif
