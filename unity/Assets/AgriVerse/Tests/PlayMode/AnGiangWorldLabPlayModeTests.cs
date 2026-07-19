using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AgriVerse.Client.Tests
{
    public sealed class AnGiangWorldLabPlayModeTests
    {
        [UnityTest]
        public IEnumerator WalkerMovesCollidesAndSupportsCursorRelease()
        {
            GameObject ground = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            ground.name = "WalkTestGround";
            ground.transform.position = new Vector3(0f, -.1f, 0f);
            ground.transform.localScale = new Vector3(12f, .2f, 12f);

            GameObject wall = GameObject.CreatePrimitive(
                PrimitiveType.Cube);
            wall.name = "WalkTestWall";
            wall.transform.position = new Vector3(0f, 1f, 2f);
            wall.transform.localScale = new Vector3(3f, 2f, .25f);

            GameObject player = new GameObject(
                "WalkTestPlayer",
                typeof(CharacterController),
                typeof(FirstPersonWalker));
            CharacterController character =
                player.GetComponent<CharacterController>();
            character.height = 1.8f;
            character.radius = .31f;
            character.center = new Vector3(0f, .9f, 0f);
            character.stepOffset = .32f;

            GameObject cameraObject = new GameObject(
                "WalkTestCamera",
                typeof(Camera));
            cameraObject.transform.SetParent(player.transform, false);
            FirstPersonWalker walker =
                player.GetComponent<FirstPersonWalker>();
            walker.Configure(cameraObject.GetComponent<Camera>(), 1.65f);
            walker.Teleport(Vector3.zero, 0f);

            Physics.SyncTransforms();
            yield return null;
            Assert.That(
                cameraObject.transform.localPosition.y,
                Is.EqualTo(1.65f).Within(.001f));

            for (int frame = 0; frame < 60; frame++)
            {
                walker.Move(Vector2.up, .02f);
                yield return null;
            }
            yield return null;

            Assert.That(
                player.transform.position.z,
                Is.GreaterThan(.5f),
                "Forward input must move the walking controller.");
            Assert.That(
                player.transform.position.z,
                Is.LessThan(1.7f),
                "The CharacterController must stop at world collision.");

            walker.ReleaseCursor();
            Assert.That(walker.CursorIsCaptured, Is.False);
            walker.CaptureCursor();
            if (!Application.isBatchMode)
            {
                Assert.That(walker.CursorIsCaptured, Is.True);
            }
            walker.ReleaseCursor();

            Object.Destroy(player);
            Object.Destroy(ground);
            Object.Destroy(wall);
            yield return null;
        }
    }
}
