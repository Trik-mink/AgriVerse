using UnityEngine;
using UnityEngine.UI;

namespace AgriVerse.Client
{
    /// <summary>
    /// Presentation-only 3D globe. Missing licensed assets leave the caller's
    /// procedural texture untouched, so the episode can always begin.
    /// </summary>
    public sealed class GlobeLandingRenderer : MonoBehaviour
    {
        private const int GlobeLayer = 30;
        private RawImage output;
        private RenderTexture target;
        private Transform globeRoot;
        private Transform cloudShell;
        private Camera globeCamera;
        private float elapsed;

        public bool UsingLicensedAssets { get; private set; }

        public void Configure(RawImage targetImage)
        {
            output = targetImage;
            GlobeLandingAssets assets =
                Resources.Load<GlobeLandingAssets>(
                    "GlobeLandingAssets");
            if (output == null || assets == null || !assets.IsUsable)
            {
                return;
            }

            BuildGlobe(assets);
            UsingLicensedAssets = true;
        }

        private void BuildGlobe(GlobeLandingAssets assets)
        {
            target = new RenderTexture(
                640,
                640,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = "AgriVerseGlobeLanding",
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear,
                useMipMap = false
            };
            target.Create();
            output.texture = target;

            GameObject stage = new GameObject("LicensedGlobeStage");
            stage.layer = GlobeLayer;
            stage.transform.SetParent(transform, false);

            globeRoot = new GameObject("EarthSystem").transform;
            globeRoot.gameObject.layer = GlobeLayer;
            globeRoot.SetParent(stage.transform, false);
            globeRoot.localRotation =
                Quaternion.Euler(-8f, 104f, -6f);

            CreateSphere(
                globeRoot,
                "Earth",
                Vector3.one * 1.9f,
                assets.EarthMaterial);
            cloudShell = CreateSphere(
                globeRoot,
                "Clouds",
                Vector3.one * 1.925f,
                assets.CloudMaterial).transform;
            CreateSphere(
                stage.transform,
                "StarField",
                Vector3.one * 18f,
                assets.SpaceMaterial);

            GameObject cameraObject = new GameObject(
                "GlobeCamera",
                typeof(Camera));
            cameraObject.layer = GlobeLayer;
            cameraObject.transform.SetParent(stage.transform, false);
            cameraObject.transform.localPosition =
                new Vector3(0f, 0f, -4.15f);
            cameraObject.transform.localRotation = Quaternion.identity;
            globeCamera = cameraObject.GetComponent<Camera>();
            globeCamera.targetTexture = target;
            globeCamera.cullingMask = 1 << GlobeLayer;
            globeCamera.clearFlags = CameraClearFlags.SolidColor;
            globeCamera.backgroundColor =
                new Color(.004f, .012f, .018f, 1f);
            globeCamera.fieldOfView = 42f;
            globeCamera.nearClipPlane = .05f;
            globeCamera.farClipPlane = 30f;
            globeCamera.allowHDR = true;
            globeCamera.allowMSAA = true;

            GameObject lightObject = new GameObject(
                "GlobeSun",
                typeof(Light));
            lightObject.layer = GlobeLayer;
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation =
                Quaternion.Euler(18f, 145f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .84f, .67f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << GlobeLayer;
        }

        private void Update()
        {
            if (!UsingLicensedAssets || globeRoot == null) return;
            elapsed += Time.unscaledDeltaTime;
            float arrival = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / 1.8f));
            if (globeCamera != null)
            {
                globeCamera.transform.localPosition =
                    new Vector3(
                        0f,
                        0f,
                        Mathf.Lerp(-4.15f, -3.38f, arrival));
            }
            globeRoot.Rotate(
                0f,
                Time.unscaledDeltaTime * 1.8f,
                0f,
                Space.Self);
            if (cloudShell != null)
            {
                cloudShell.Rotate(
                    0f,
                    Time.unscaledDeltaTime * .8f,
                    0f,
                    Space.Self);
            }
        }

        private static GameObject CreateSphere(
            Transform parent,
            string objectName,
            Vector3 scale,
            Material material)
        {
            GameObject sphere =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = objectName;
            sphere.layer = GlobeLayer;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localScale = scale;
            Collider collider = sphere.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyRuntimeObject(collider);
            }
            sphere.GetComponent<MeshRenderer>().sharedMaterial =
                material;
            return sphere;
        }

        private void OnDestroy()
        {
            if (target == null) return;
            if (output != null && output.texture == target)
            {
                output.texture = null;
            }
            target.Release();
            DestroyRuntimeObject(target);
        }

        private static void DestroyRuntimeObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }
    }
}
