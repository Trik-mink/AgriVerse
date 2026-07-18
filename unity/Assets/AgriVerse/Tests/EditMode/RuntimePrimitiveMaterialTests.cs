using NUnit.Framework;
using UnityEngine;

namespace AgriVerse.Client.Tests
{
    public sealed class RuntimePrimitiveMaterialTests
    {
        [Test]
        public void AppliesTheSerializedUrpSurfaceAndPreservesTheRequestedGray()
        {
            Material template = RuntimePrimitiveMaterial.Template;
            Assert.That(template, Is.Not.Null, "The Resources material must be present so its URP shader is retained in standalone builds.");
            Assert.That(template.shader.name, Is.EqualTo("Universal Render Pipeline/Unlit"));

            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
            Color expected = new Color(.58f, .58f, .58f);
            RuntimePrimitiveMaterial.Apply(renderer, expected);

            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.shader, Is.EqualTo(template.shader));
            Assert.That(renderer.sharedMaterial.GetColor("_BaseColor"), Is.EqualTo(expected));
            Assert.That(primitive.GetComponent<Collider>(), Is.Not.Null, "Material assignment must not disturb primitive click colliders.");
            Object.DestroyImmediate(primitive);
        }
    }
}
