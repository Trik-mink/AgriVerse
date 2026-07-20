using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AgriVerse.Client
{
    /// <summary>
    /// Direct-to-screen orbital field-network presentation. The licensed globe temporarily
    /// owns the scene camera, avoiding both a framed texture and fragile URP camera stacking.
    /// </summary>
    public sealed class GlobeLandingRenderer :
        MonoBehaviour,
        IPointerDownHandler,
        IDragHandler,
        IPointerUpHandler,
        IScrollHandler
    {
        private const int GlobeLayer = 30;
        private const float EarthDiameter = 3f;
        private const float MinimumDistance = 5.25f;
        private const float MaximumDistance = 7.1f;
        private const float IdleResumeDelay = 1.6f;
        // The licensed equirectangular Earth map's prime meridian is 75 degrees
        // west of Unity's built-in sphere UV seam.
        private const float TextureLongitudeOffsetDegrees = 75f;

        private readonly List<FieldNetworkPinView> pins =
            new List<FieldNetworkPinView>();
        private RectTransform pinLayer;
        private Action<FieldNetworkLocation> selectLocation;
        private GameObject stage;
        private Transform globeRoot;
        private Transform cloudShell;
        private Transform atmosphereRim;
        private Camera globeCamera;
        private CanvasGroup wakeFade;
        private Material atmosphereMaterial;
        private Quaternion targetRotation;
        private float targetDistance = 6f;
        private float interactionEndsAt;
        private bool dragging;
        private bool rotatingToSelection;
        private bool ownsCamera;
        private bool cameraConfigured;
        private int previousCullingMask;
        private CameraClearFlags previousClearFlags;
        private Color previousBackgroundColor;
        private float previousFieldOfView;
        private float previousNearClip;
        private float previousFarClip;
        private string selectedId = string.Empty;
        private int keyboardFocusIndex = -1;

        public bool UsingLicensedAssets { get; private set; }
        public bool HasDirectScreenCamera =>
            globeCamera != null && globeCamera.targetTexture == null;
        public bool UsesRectangularRenderTarget => false;
        public int PinCount => pins.Count;
        internal string KeyboardFocusedLocationIdForTesting =>
            keyboardFocusIndex >= 0 &&
            keyboardFocusIndex < pins.Count
                ? pins[keyboardFocusIndex].Location.Id
                : string.Empty;
        internal bool IsFramingKeyboardFocusForTesting =>
            keyboardFocusIndex >= 0;

        public void Initialize(
            RectTransform targetPinLayer,
            Action<FieldNetworkLocation> onSelectLocation)
        {
            pinLayer = targetPinLayer;
            selectLocation = onSelectLocation;
            if (stage == null)
            {
                BuildStage(
                    Resources.Load<GlobeLandingAssets>(
                        "GlobeLandingAssets"));
            }
        }

        public void SetCatalog(FieldNetworkCatalog catalog)
        {
            ClearPins();
            selectedId = string.Empty;
            keyboardFocusIndex = -1;
            if (catalog == null || pinLayer == null) return;
            foreach (FieldNetworkLocation location in catalog.Locations)
            {
                FieldNetworkPinView pin = new GameObject(
                    "FieldPin_" + location.Id,
                    typeof(RectTransform),
                    typeof(FieldNetworkPinView))
                    .GetComponent<FieldNetworkPinView>();
                pin.Build(
                    pinLayer,
                    location,
                    SelectFromPin);
                pins.Add(pin);
            }
        }

        public void FocusLocation(FieldNetworkLocation location)
        {
            selectedId = location?.Id ?? string.Empty;
            keyboardFocusIndex = -1;
            for (int index = 0; index < pins.Count; index++)
            {
                FieldNetworkPinView pin = pins[index];
                bool matches =
                    location != null &&
                    string.Equals(
                        pin.Location.Id,
                        location.Id,
                        StringComparison.Ordinal);
                pin.SetSelected(
                    matches);
                pin.SetKeyboardFocused(matches);
                if (matches)
                {
                    keyboardFocusIndex = index;
                }
            }
            FrameLocation(location);
        }

        public void ClearSelection()
        {
            selectedId = string.Empty;
            rotatingToSelection = false;
            keyboardFocusIndex = -1;
            foreach (FieldNetworkPinView pin in pins)
            {
                pin.SetSelected(false);
                pin.SetKeyboardFocused(false);
            }
        }

        public void FocusNextPin(int direction)
        {
            if (pins.Count == 0) return;
            int step = direction < 0 ? -1 : 1;
            int currentIndex = keyboardFocusIndex;
            if (currentIndex < 0 &&
                EventSystem.current != null)
            {
                GameObject current =
                    EventSystem.current.currentSelectedGameObject;
                currentIndex = pins.FindIndex(
                    pin =>
                        pin != null &&
                        pin.gameObject == current);
            }
            keyboardFocusIndex = currentIndex < 0
                ? (step > 0 ? 0 : pins.Count - 1)
                : (currentIndex + step + pins.Count) %
                  pins.Count;

            for (int index = 0; index < pins.Count; index++)
            {
                pins[index]?.SetKeyboardFocused(
                    index == keyboardFocusIndex);
            }
            FieldNetworkPinView focused =
                pins[keyboardFocusIndex];
            FrameLocation(focused.Location);
            focused.Focus();
        }

        public bool SelectKeyboardFocusedPin()
        {
            if (keyboardFocusIndex < 0 ||
                keyboardFocusIndex >= pins.Count)
            {
                return false;
            }

            SelectFromPin(pins[keyboardFocusIndex].Location);
            return true;
        }

        internal void SetAllPinsVisibleForTesting(bool visible)
        {
            foreach (FieldNetworkPinView pin in pins)
            {
                pin?.SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            if (stage != null)
            {
                stage.SetActive(visible);
            }
            if (visible)
            {
                ConfigureCamera();
                SetWakeFadeSuppressed(true);
            }
            else
            {
                SetWakeFadeSuppressed(false);
                RestoreCamera();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            dragging = true;
            rotatingToSelection = false;
            interactionEndsAt =
                Time.unscaledTime + IdleResumeDelay;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || globeRoot == null) return;
            Vector2 delta = eventData.delta;
            Quaternion yaw = Quaternion.AngleAxis(
                -delta.x * .20f,
                Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(
                delta.y * .15f,
                Vector3.right);
            globeRoot.localRotation =
                pitch * yaw * globeRoot.localRotation;
            interactionEndsAt =
                Time.unscaledTime + IdleResumeDelay;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            dragging = false;
            interactionEndsAt =
                Time.unscaledTime + IdleResumeDelay;
        }

        public void OnScroll(PointerEventData eventData)
        {
            targetDistance = Mathf.Clamp(
                targetDistance + eventData.scrollDelta.y * -.12f,
                MinimumDistance,
                MaximumDistance);
            interactionEndsAt =
                Time.unscaledTime + IdleResumeDelay;
        }

        private void Update()
        {
            if (globeRoot == null || globeCamera == null) return;
            float delta = Time.unscaledDeltaTime;
            if (rotatingToSelection)
            {
                globeRoot.localRotation = Quaternion.Slerp(
                    globeRoot.localRotation,
                    targetRotation,
                    1f - Mathf.Exp(-delta * 3.5f));
                if (Quaternion.Angle(
                        globeRoot.localRotation,
                        targetRotation) < .18f)
                {
                    globeRoot.localRotation = targetRotation;
                    rotatingToSelection = false;
                }
            }
            else if (!dragging &&
                     !EpisodeAccessibility.ReducedMotion &&
                     Time.unscaledTime >= interactionEndsAt)
            {
                globeRoot.Rotate(
                    0f,
                    delta * 1.25f,
                    0f,
                    Space.Self);
            }

            if (cloudShell != null &&
                !EpisodeAccessibility.ReducedMotion)
            {
                cloudShell.Rotate(
                    0f,
                    delta * .65f,
                    0f,
                    Space.Self);
            }

            float z = Mathf.Lerp(
                globeRoot.localPosition.z,
                targetDistance,
                1f - Mathf.Exp(-delta * 6f));
            globeRoot.localPosition =
                new Vector3(0f, 0f, z);
            if (atmosphereRim != null)
            {
                atmosphereRim.localPosition =
                    new Vector3(0f, 0f, z);
            }
        }

        private void LateUpdate()
        {
            if (globeRoot == null ||
                globeCamera == null ||
                pinLayer == null)
            {
                return;
            }
            float pulse = EpisodeAccessibility.ReducedMotion
                ? 1.12f
                : 1.12f +
                  Mathf.Sin(Time.unscaledTime * 3.2f) * .09f;
            for (int index = 0; index < pins.Count; index++)
            {
                FieldNetworkPinView pin = pins[index];
                Vector3 local =
                    GeographicDirection(pin.Location) *
                    (EarthDiameter * .515f);
                Vector3 world =
                    globeRoot.TransformPoint(local);
                Vector3 outward =
                    (world - globeRoot.position).normalized;
                Vector3 towardCamera =
                    (globeCamera.transform.position - world)
                        .normalized;
                bool front =
                    Vector3.Dot(outward, towardCamera) > .04f;
                Vector3 screen =
                    globeCamera.WorldToScreenPoint(world);
                bool visible =
                    front &&
                    screen.z > 0f &&
                    screen.x > -30f &&
                    screen.x < Screen.width + 30f &&
                    screen.y > -30f &&
                    screen.y < Screen.height + 30f;
                pin.SetVisible(visible);
                if (!visible) continue;
                if (RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        pinLayer,
                        screen,
                        null,
                        out Vector2 localPoint))
                {
                    pin.RectTransform.anchoredPosition = localPoint;
                }
                pin.SetPulse(
                    string.Equals(
                        pin.Location.Id,
                        selectedId,
                        StringComparison.Ordinal)
                        ? pulse
                        : 1f);
                if (index == keyboardFocusIndex)
                {
                    pin.Focus();
                }
            }
        }

        private void FrameLocation(
            FieldNetworkLocation location)
        {
            if (location == null || globeRoot == null) return;

            Vector3 direction = GeographicDirection(location);
            targetRotation = Quaternion.FromToRotation(
                direction,
                Vector3.back);
            rotatingToSelection =
                !EpisodeAccessibility.ReducedMotion;
            if (!rotatingToSelection)
            {
                globeRoot.localRotation = targetRotation;
            }
            targetDistance =
                location.IsPlayable ? 5.45f : 5.8f;
            interactionEndsAt =
                Time.unscaledTime + IdleResumeDelay;
        }

        private void BuildStage(GlobeLandingAssets assets)
        {
            globeCamera = Camera.main;
            if (globeCamera == null)
            {
                GameObject cameraObject = new GameObject(
                    "OrbitalGlobeCamera",
                    typeof(Camera));
                globeCamera = cameraObject.GetComponent<Camera>();
                ownsCamera = true;
            }
            ConfigureCamera();
            SetWakeFadeSuppressed(true);

            stage = new GameObject("OrbitalFieldNetworkStage");
            stage.layer = GlobeLayer;
            stage.transform.SetParent(
                globeCamera.transform,
                false);
            globeRoot = new GameObject("EarthSystem").transform;
            globeRoot.gameObject.layer = GlobeLayer;
            globeRoot.SetParent(stage.transform, false);
            globeRoot.localPosition =
                new Vector3(0f, 0f, targetDistance);
            globeRoot.localRotation =
                Quaternion.Euler(-7f, 78f, -4f);
            targetRotation = globeRoot.localRotation;

            Material earth = assets?.EarthMaterial;
            Material clouds = assets?.CloudMaterial;
            Material space = assets?.SpaceMaterial;
            UsingLicensedAssets =
                assets != null && assets.IsUsable;
            if (earth == null)
            {
                earth = RuntimePrimitiveMaterial.Template;
            }

            CreateSphere(
                globeRoot,
                "Earth",
                Vector3.one * EarthDiameter,
                earth);
            if (clouds != null)
            {
                cloudShell = CreateSphere(
                    globeRoot,
                    "Clouds",
                    Vector3.one * (EarthDiameter * 1.012f),
                    clouds).transform;
            }
            if (space != null)
            {
                CreateSphere(
                    stage.transform,
                    "StarField",
                    Vector3.one * 18f,
                    space);
            }

            GameObject lightObject = new GameObject(
                "OrbitalSun",
                typeof(Light));
            lightObject.layer = GlobeLayer;
            lightObject.transform.SetParent(stage.transform, false);
            lightObject.transform.localRotation =
                Quaternion.Euler(18f, -24f, 0f);
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .84f, .68f);
            light.intensity = 1.28f;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << GlobeLayer;

            if (clouds != null)
            {
                CreateAtmosphere(clouds);
            }
        }

        private void CreateAtmosphere(Material source)
        {
            atmosphereMaterial = new Material(source)
            {
                name = "OrbitalAtmosphere_Runtime"
            };
            atmosphereMaterial.SetTexture(
                "_BaseMap",
                Texture2D.whiteTexture);
            atmosphereMaterial.SetColor(
                "_BaseColor",
                new Color(.28f, .76f, .79f, .42f));
            atmosphereMaterial.renderQueue = 3100;

            LineRenderer rim = new GameObject(
                "AtmosphericRim",
                typeof(LineRenderer))
                .GetComponent<LineRenderer>();
            rim.gameObject.layer = GlobeLayer;
            rim.transform.SetParent(stage.transform, false);
            atmosphereRim = rim.transform;
            atmosphereRim.localPosition =
                new Vector3(0f, 0f, targetDistance);
            rim.useWorldSpace = false;
            rim.loop = true;
            rim.positionCount = 128;
            rim.widthMultiplier = .026f;
            rim.sharedMaterial = atmosphereMaterial;
            rim.numCornerVertices = 3;
            float radius = EarthDiameter * .512f;
            for (int index = 0; index < rim.positionCount; index++)
            {
                float angle =
                    index / (float)rim.positionCount *
                    Mathf.PI * 2f;
                rim.SetPosition(
                    index,
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        Mathf.Sin(angle) * radius,
                        -.04f));
            }
        }

        private void SelectFromPin(FieldNetworkLocation location)
        {
            FocusLocation(location);
            selectLocation?.Invoke(location);
        }

        private static Vector3 GeographicDirection(
            FieldNetworkLocation location)
        {
            float latitude =
                (float)location.Latitude * Mathf.Deg2Rad;
            float longitude =
                ((float)location.Longitude +
                 TextureLongitudeOffsetDegrees) *
                Mathf.Deg2Rad;
            float horizontal = Mathf.Cos(latitude);
            return new Vector3(
                horizontal * Mathf.Sin(longitude),
                Mathf.Sin(latitude),
                -horizontal * Mathf.Cos(longitude));
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

        private void ClearPins()
        {
            foreach (FieldNetworkPinView pin in pins)
            {
                if (pin != null)
                {
                    DestroyRuntimeObject(pin.gameObject);
                }
            }
            pins.Clear();
        }

        private void OnDestroy()
        {
            SetWakeFadeSuppressed(false);
            RestoreCamera();
            ClearPins();
            if (stage != null)
            {
                DestroyRuntimeObject(stage);
            }
            if (ownsCamera && globeCamera != null)
            {
                DestroyRuntimeObject(globeCamera.gameObject);
            }
            if (atmosphereMaterial != null)
            {
                DestroyRuntimeObject(atmosphereMaterial);
            }
        }

        private void ConfigureCamera()
        {
            if (globeCamera == null || cameraConfigured) return;
            previousCullingMask = globeCamera.cullingMask;
            previousClearFlags = globeCamera.clearFlags;
            previousBackgroundColor = globeCamera.backgroundColor;
            previousFieldOfView = globeCamera.fieldOfView;
            previousNearClip = globeCamera.nearClipPlane;
            previousFarClip = globeCamera.farClipPlane;
            globeCamera.targetTexture = null;
            globeCamera.cullingMask = 1 << GlobeLayer;
            globeCamera.clearFlags = CameraClearFlags.SolidColor;
            globeCamera.backgroundColor =
                new Color(.002f, .006f, .014f, 1f);
            globeCamera.fieldOfView = 42f;
            globeCamera.nearClipPlane = .05f;
            globeCamera.farClipPlane = 30f;
            globeCamera.allowHDR = true;
            globeCamera.allowMSAA = true;
            globeCamera.enabled = true;
            cameraConfigured = true;
        }

        private void RestoreCamera()
        {
            if (globeCamera == null || !cameraConfigured) return;
            globeCamera.cullingMask = previousCullingMask;
            globeCamera.clearFlags = previousClearFlags;
            globeCamera.backgroundColor = previousBackgroundColor;
            globeCamera.fieldOfView = previousFieldOfView;
            globeCamera.nearClipPlane = previousNearClip;
            globeCamera.farClipPlane = previousFarClip;
            if (ownsCamera)
            {
                globeCamera.enabled = false;
            }
            cameraConfigured = false;
        }

        private void SetWakeFadeSuppressed(bool suppressed)
        {
            if (wakeFade == null)
            {
                foreach (CanvasGroup candidate in
                         FindObjectsByType<CanvasGroup>(
                             FindObjectsInactive.Include,
                             FindObjectsSortMode.None))
                {
                    if (candidate.gameObject.name == "WakeFade")
                    {
                        wakeFade = candidate;
                        break;
                    }
                }
            }
            if (wakeFade == null) return;
            wakeFade.alpha = suppressed ? 0f : 1f;
            wakeFade.blocksRaycasts = !suppressed;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
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
