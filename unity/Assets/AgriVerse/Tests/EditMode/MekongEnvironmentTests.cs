using NUnit.Framework;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class MekongEnvironmentTests
    {
        [Test]
        public void BuildsUrpLitSceneryWithoutRaycastBlockingColliders()
        {
            GameObject root = new GameObject("EnvironmentTest");
            MekongEnvironmentController environment = root.AddComponent<MekongEnvironmentController>();
            environment.BuildForTesting();

            MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.GreaterThan(40));
            foreach (MeshRenderer renderer in renderers)
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null);
                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Universal Render Pipeline/Lit"));
            }
            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
                Assert.That(collider.enabled, Is.False, "Environment geometry must never intercept marker raycasts.");

            Object.DestroyImmediate(root);
        }

        [Test]
        public void EnvironmentLeavesAVisibleTestSiteMarkerAsTheFirstRaycastHit()
        {
            GameObject environmentRoot = new GameObject("EnvironmentTest");
            environmentRoot.AddComponent<MekongEnvironmentController>().BuildForTesting();
            GameObject cameraObject = new GameObject("EnvironmentTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.transform.SetPositionAndRotation(
                new Vector3(0f, 4.4f, -13.5f),
                Quaternion.Euler(13f, 0f, 0f));
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "TestSiteMarker";
            marker.transform.position = new Vector3(0f, .5f, 0f);

            Physics.SyncTransforms();
            Vector3 markerScreenPoint = camera.WorldToScreenPoint(marker.transform.position);
            Ray markerRay = camera.ScreenPointToRay(markerScreenPoint);

            Assert.That(Physics.Raycast(markerRay, out RaycastHit hit), Is.True);
            Assert.That(hit.collider.gameObject, Is.EqualTo(marker));

            Object.DestroyImmediate(marker);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(environmentRoot);
        }

        [Test]
        public void WaterRipplesRemainWithinTheWorldSpaceChannelBounds()
        {
            GameObject root = new GameObject("EnvironmentTest");
            root.AddComponent<MekongEnvironmentController>().BuildForTesting();
            Renderer channel = FindRenderer(root.transform, "WarmTealChannel");

            foreach (Renderer ripple in root.GetComponentsInChildren<Renderer>())
            {
                if (!ripple.name.StartsWith("Ripple")) continue;
                Assert.That(ripple.bounds.min.x, Is.GreaterThanOrEqualTo(channel.bounds.min.x - .02f));
                Assert.That(ripple.bounds.max.x, Is.LessThanOrEqualTo(channel.bounds.max.x + .02f));
                Assert.That(ripple.bounds.min.z, Is.GreaterThanOrEqualTo(channel.bounds.min.z - .02f));
                Assert.That(ripple.bounds.max.z, Is.LessThanOrEqualTo(channel.bounds.max.z + .02f));
            }

            Object.DestroyImmediate(root);
        }

        private static Renderer FindRenderer(Transform root, string name)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
                if (renderer.name == name) return renderer;
            Assert.Fail($"Could not find renderer named {name}.");
            return null;
        }
    }
}
