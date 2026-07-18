using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// A photo-led upstream arrival view. The procedural field remains intact and is only hidden
    /// after the licensed An Giang backdrop and its small foreground layer are ready to render.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnGiangRealitySpikeController : MonoBehaviour
    {
        private const string BackdropResourcePath = "AnGiang/AnGiangCanalBackdrop";
        private const float BackdropDistance = 40f;
        private readonly List<Transform> waterGlints = new List<Transform>();
        private Transform spikeRoot;
        private Transform backdrop;
        private GameObject proceduralRoot;
        private Camera sceneCamera;

        public bool PhotoPresentationActive { get; private set; }
        public bool ProceduralFallbackActive => proceduralRoot == null || proceduralRoot.activeSelf;

        /// <summary>Interview UI replaces interaction remnants with the unobstructed arrival view.</summary>
        public void SetCinematicInterviewActive(bool active)
        {
            foreach (Transform glint in waterGlints)
            {
                if (glint == null) continue;
                MeshRenderer renderer = glint.GetComponent<MeshRenderer>();
                if (renderer != null) renderer.enabled = !active;
            }
        }

        private IEnumerator Start()
        {
            // Let the procedural controller establish its dependable scene before deciding whether
            // the licensed backdrop can replace it.
            yield return null;
            Activate(Resources.Load<Texture2D>(BackdropResourcePath));
        }

        private void Update()
        {
            if (!PhotoPresentationActive || sceneCamera == null || backdrop == null) return;

            float time = Time.time;
            backdrop.position = sceneCamera.transform.position + sceneCamera.transform.forward * BackdropDistance +
                                sceneCamera.transform.right * Mathf.Sin(time * .08f) * .32f;
            backdrop.rotation = Quaternion.LookRotation(sceneCamera.transform.forward, sceneCamera.transform.up);
            for (int index = 0; index < waterGlints.Count; index++)
                waterGlints[index].localPosition += Vector3.right * Mathf.Sin(time * .8f + index) * .0008f;
        }

        public void ActivateForTesting(Texture2D photoTexture) => Activate(photoTexture);

        private void Activate(Texture2D photoTexture)
        {
            proceduralRoot = FindProceduralRoot();
            if (photoTexture == null || Camera.main == null || MekongEnvironmentMaterial.Template == null)
            {
                RestoreProceduralFallback();
                return;
            }

            sceneCamera = Camera.main;
            spikeRoot = new GameObject("AnGiangRealitySpike").transform;
            spikeRoot.SetParent(transform, false);
            try
            {
                BuildBackdrop(photoTexture);
                BuildForeground();
                if (proceduralRoot != null) proceduralRoot.SetActive(false);
                PhotoPresentationActive = true;
            }
            catch (System.Exception error)
            {
                Debug.LogWarning($"An Giang photo presentation unavailable; keeping the procedural fallback. {error.Message}", this);
                RestoreProceduralFallback();
            }
        }

        private void BuildBackdrop(Texture2D texture)
        {
            GameObject photo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            photo.name = "AnGiangCanalPhotoBackdrop";
            photo.transform.SetParent(spikeRoot, false);
            backdrop = photo.transform;
            float height = 2f * BackdropDistance * Mathf.Tan(sceneCamera.fieldOfView * Mathf.Deg2Rad * .5f);
            backdrop.localScale = new Vector3(height * texture.width / texture.height, height, 1f);
            Material material = new Material(MekongEnvironmentMaterial.Template);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(1.05f, .96f, .84f, 1f));
            material.SetFloat("_Smoothness", .08f);
            photo.GetComponent<MeshRenderer>().sharedMaterial = material;
            DisableCollider(photo);
            RefreshBackdropTransform();
        }

        private void BuildForeground()
        {
            for (int index = 0; index < 5; index++)
            {
                Transform glint = Box("WaterGlint" + index,
                    new Vector3(-4.5f + index * 2.1f, -.075f, .2f + index * 1.25f),
                    new Vector3(.75f, .012f, .045f), new Color(.73f, .78f, .61f), .4f).transform;
                waterGlints.Add(glint);
            }
        }

        private GameObject Box(string name, Vector3 position, Vector3 scale, Color color, float smoothness = .14f)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(spikeRoot, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            obj.GetComponent<MeshRenderer>().sharedMaterial = MekongEnvironmentMaterial.Create(color, smoothness);
            DisableCollider(obj);
            return obj;
        }

        private static void DisableCollider(GameObject obj)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
        }

        private void RefreshBackdropTransform()
        {
            backdrop.position = sceneCamera.transform.position + sceneCamera.transform.forward * BackdropDistance;
            backdrop.rotation = Quaternion.LookRotation(sceneCamera.transform.forward, sceneCamera.transform.up);
        }

        private GameObject FindProceduralRoot()
        {
            MekongEnvironmentController environment = FindAnyObjectByType<MekongEnvironmentController>();
            if (environment == null) return null;
            Transform root = environment.transform.Find("MekongFieldEnvironment");
            return root == null ? null : root.gameObject;
        }

        private void RestoreProceduralFallback()
        {
            if (spikeRoot != null) Destroy(spikeRoot.gameObject);
            if (proceduralRoot != null) proceduralRoot.SetActive(true);
            PhotoPresentationActive = false;
        }
    }
}
