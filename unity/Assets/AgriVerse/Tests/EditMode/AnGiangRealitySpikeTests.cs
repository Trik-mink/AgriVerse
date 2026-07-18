using NUnit.Framework;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class AnGiangRealitySpikeTests
    {
        [Test]
        public void LoadedBackdropHidesFallbackAndLeavesMarkersAsFirstRaycastHit()
        {
            GameObject cameraObject = CreateCamera();
            GameObject environmentRoot = new GameObject("Environment");
            environmentRoot.AddComponent<MekongEnvironmentController>().BuildForTesting();
            GameObject spikeRoot = new GameObject("RealitySpike");
            AnGiangRealitySpikeController spike = spikeRoot.AddComponent<AnGiangRealitySpikeController>();
            spike.ActivateForTesting(new Texture2D(16, 9));
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.transform.position = new Vector3(0f, .5f, 0f);

            Physics.SyncTransforms();
            Camera camera = cameraObject.GetComponent<Camera>();
            Ray ray = camera.ScreenPointToRay(camera.WorldToScreenPoint(marker.transform.position));

            Assert.That(spike.PhotoPresentationActive, Is.True);
            Assert.That(spike.ProceduralFallbackActive, Is.False);
            Assert.That(Physics.Raycast(ray, out RaycastHit hit), Is.True);
            Assert.That(hit.collider.gameObject, Is.EqualTo(marker));
            foreach (Collider collider in spikeRoot.GetComponentsInChildren<Collider>(true))
                Assert.That(collider.enabled, Is.False);

            Object.DestroyImmediate(marker);
            Object.DestroyImmediate(spikeRoot);
            Object.DestroyImmediate(environmentRoot);
            Object.DestroyImmediate(cameraObject);
        }

        [Test]
        public void MissingBackdropLeavesTheProceduralEnvironmentVisible()
        {
            GameObject cameraObject = CreateCamera();
            GameObject environmentRoot = new GameObject("Environment");
            environmentRoot.AddComponent<MekongEnvironmentController>().BuildForTesting();
            GameObject spikeRoot = new GameObject("RealitySpike");
            AnGiangRealitySpikeController spike = spikeRoot.AddComponent<AnGiangRealitySpikeController>();

            spike.ActivateForTesting(null);

            Assert.That(spike.PhotoPresentationActive, Is.False);
            Assert.That(spike.ProceduralFallbackActive, Is.True);

            Object.DestroyImmediate(spikeRoot);
            Object.DestroyImmediate(environmentRoot);
            Object.DestroyImmediate(cameraObject);
        }

        private static GameObject CreateCamera()
        {
            GameObject cameraObject = new GameObject("RealitySpikeCamera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(new Vector3(0f, 4.4f, -13.5f), Quaternion.Euler(13f, 0f, 0f));
            camera.fieldOfView = 54f;
            return cameraObject;
        }
    }
}
