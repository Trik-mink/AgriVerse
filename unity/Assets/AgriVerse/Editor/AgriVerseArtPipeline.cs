#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace AgriVerse.Client.Editor
{
    public static class AgriVerseArtPipeline
    {
        private const string MaiSource =
            "Assets/AgriVerse/Art/Characters/Mai/Source/" +
            "tripo_convert_a9305ef4-e460-4d27-97e6-e56330fc8896.fbx";
        private const string MaiBaseColor =
            "Assets/AgriVerse/Art/Characters/Mai/Source/" +
            "tripo_convert_a9305ef4-e460-4d27-97e6-e56330fc8896.fbm/" +
            "adventure_explorer_character_3d_model_basecolor.JPEG";
        private const string MaiNormal =
            "Assets/AgriVerse/Art/Characters/Mai/Source/" +
            "tripo_convert_a9305ef4-e460-4d27-97e6-e56330fc8896.fbm/" +
            "adventure_explorer_character_3d_model_normal.JPEG";
        private const string MaiMaterial =
            "Assets/AgriVerse/Art/Characters/Mai/Materials/Mai_URP.mat";
        private const string LabFloorMaterial =
            "Assets/AgriVerse/Art/Characters/Mai/Materials/" +
            "CharacterLabFloor_URP.mat";
        private const string MaiController =
            "Assets/AgriVerse/Art/Characters/Mai/Controllers/" +
            "MaiLab.controller";
        private const string MaiPrefab =
            "Assets/AgriVerse/Art/Characters/Mai/Prefabs/Mai.prefab";
        private const string CharacterLabScene =
            "Assets/Scenes/CharacterLab.unity";

        [MenuItem("AgriVerse/Art/Build Character Lab")]
        public static void BuildCharacterLab()
        {
            EnsureFolder(Path.GetDirectoryName(MaiMaterial));
            EnsureFolder(Path.GetDirectoryName(MaiController));
            EnsureFolder(Path.GetDirectoryName(MaiPrefab));

            Material maiMaterial = CreateOrUpdateLitMaterial(
                MaiMaterial,
                AssetDatabase.LoadAssetAtPath<Texture2D>(MaiBaseColor),
                AssetDatabase.LoadAssetAtPath<Texture2D>(MaiNormal),
                new Color(1f, 1f, 1f, 1f),
                .26f);
            Material floorMaterial = CreateOrUpdateLitMaterial(
                LabFloorMaterial,
                null,
                null,
                new Color(.075f, .10f, .105f, 1f),
                .18f);
            AnimatorController controller = CreateOrUpdateController();
            GameObject prefab =
                CreateOrUpdateMaiPrefab(maiMaterial, controller);
            CreateCharacterLabScene(prefab, floorMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "AgriVerse CharacterLab built with Mai URP material, " +
                "five-motion controller, prefab, look-at behavior, and studio scene.");
        }

        private static Material CreateOrUpdateLitMaterial(
            string path,
            Texture2D baseColor,
            Texture2D normal,
            Color tint,
            float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "URP Lit shader is unavailable.");
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.name = Path.GetFileNameWithoutExtension(path);
            material.SetColor("_BaseColor", tint);
            material.SetTexture("_BaseMap", baseColor);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", normal == null ? 0f : .82f);
            if (normal == null)
                material.DisableKeyword("_NORMALMAP");
            else
                material.EnableKeyword("_NORMALMAP");
            material.enableInstancing = true;
            material.doubleSidedGI = false;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static AnimatorController CreateOrUpdateController()
        {
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(
                    MaiController);
            if (controller == null)
            {
                controller =
                    AnimatorController.CreateAnimatorControllerAtPath(
                        MaiController);
            }

            AnimatorStateMachine machine =
                controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
            {
                machine.RemoveState(child.state);
            }

            AnimationClip[] motions = AssetDatabase
                .LoadAllAssetsAtPath(MaiSource)
                .OfType<AnimationClip>()
                .Where(clip => clip.name.StartsWith(
                    "Mai_",
                    StringComparison.Ordinal))
                .ToArray();
            if (motions.Length != 5)
            {
                throw new InvalidOperationException(
                    $"Mai requires five normalized motions; found {motions.Length}.");
            }

            AnimatorState first = null;
            foreach (AnimationClip motion in motions)
            {
                AnimatorState state =
                    machine.AddState(motion.name);
                state.motion = motion;
                state.writeDefaultValues = true;
                if (first == null) first = state;
            }
            machine.defaultState = machine.states
                .Select(child => child.state)
                .FirstOrDefault(state =>
                    state.name == "Mai_Idle") ?? first;
            AnimatorControllerLayer[] layers = controller.layers;
            layers[0].iKPass = true;
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static GameObject CreateOrUpdateMaiPrefab(
            Material material,
            RuntimeAnimatorController controller)
        {
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(MaiSource);
            if (source == null)
            {
                throw new InvalidOperationException(
                    "Mai source model is unavailable.");
            }

            GameObject instance =
                PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException(
                    "Mai source model could not be instantiated.");
            }
            instance.name = "Mai";
            try
            {
                foreach (Renderer renderer in
                         instance.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                Animator animator =
                    instance.GetComponentInChildren<Animator>(true);
                if (animator == null ||
                    animator.avatar == null ||
                    !animator.avatar.isValid ||
                    !animator.avatar.isHuman)
                {
                    throw new InvalidOperationException(
                        "Mai did not produce a valid Humanoid Animator.");
                }
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode =
                    AnimatorCullingMode.CullUpdateTransforms;
                MaiGuideController guide =
                    instance.GetComponent<MaiGuideController>();
                if (guide == null)
                {
                    guide = instance.AddComponent<MaiGuideController>();
                }
                guide.Configure(animator, null);
                return PrefabUtility.SaveAsPrefabAsset(instance, MaiPrefab);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void CreateCharacterLabScene(
            GameObject maiPrefab,
            Material floorMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                NewSceneMode.Single);
            scene.name = "CharacterLab";

            GameObject root = new GameObject("CharacterLab");
            CharacterLabController lab =
                root.AddComponent<CharacterLabController>();

            GameObject mai =
                PrefabUtility.InstantiatePrefab(maiPrefab, scene) as GameObject;
            mai.transform.SetParent(root.transform, false);
            mai.transform.position = Vector3.zero;
            mai.transform.rotation = Quaternion.identity;
            Animator animator = mai.GetComponentInChildren<Animator>(true);
            lab.Configure(animator);

            GameObject floor = GameObject.CreatePrimitive(
                PrimitiveType.Plane);
            floor.name = "NeutralStudioFloor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.position = new Vector3(0f, -.01f, 0f);
            floor.transform.localScale = new Vector3(1.4f, 1f, 1.4f);
            floor.GetComponent<MeshRenderer>().sharedMaterial =
                floorMaterial;

            GameObject cameraObject = new GameObject(
                "CharacterLabCamera",
                typeof(Camera),
                typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position =
                new Vector3(2.25f, 1.34f, -3.35f);
            cameraObject.transform.rotation = LookAt(
                cameraObject.transform.position,
                new Vector3(0f, .82f, 0f));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(.028f, .045f, .05f, 1f);
            camera.nearClipPlane = .05f;

            MaiGuideController guide =
                mai.GetComponent<MaiGuideController>();
            guide.Configure(animator, cameraObject.transform);

            CreateLight(
                root.transform,
                "CharacterLabKey",
                LightType.Directional,
                new Color(1f, .82f, .66f),
                1.25f,
                new Vector3(42f, -32f, 0f));
            Light fill = CreateLight(
                root.transform,
                "CharacterLabFill",
                LightType.Point,
                new Color(.55f, .78f, .86f),
                4.2f,
                Vector3.zero);
            fill.transform.position = new Vector3(-1.5f, 1.7f, -1.1f);
            fill.range = 5f;
            fill.shadows = LightShadows.None;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight =
                new Color(.18f, .22f, .23f, 1f);
            RenderSettings.fog = false;
            EditorSceneManager.SaveScene(scene, CharacterLabScene);
        }

        private static Light CreateLight(
            Transform parent,
            string name,
            LightType type,
            Color color,
            float intensity,
            Vector3 euler)
        {
            Light light = new GameObject(name, typeof(Light))
                .GetComponent<Light>();
            light.transform.SetParent(parent, false);
            light.type = type;
            light.color = color;
            light.intensity = intensity;
            light.transform.rotation = Quaternion.Euler(euler);
            light.shadows = type == LightType.Directional
                ? LightShadows.Soft
                : LightShadows.None;
            return light;
        }

        private static Quaternion LookAt(Vector3 from, Vector3 to) =>
            Quaternion.LookRotation((to - from).normalized, Vector3.up);

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            Directory.CreateDirectory(
                Path.Combine(
                    Directory.GetParent(Application.dataPath)?.FullName ??
                    throw new InvalidOperationException(
                        "Unity project root is unavailable."),
                    path));
            AssetDatabase.Refresh();
        }
    }
}
#endif
