using UnityEngine;

namespace AgriVerse.Client
{
    public sealed class BillboardFacingCamera : MonoBehaviour
    {
        [SerializeField] private bool yawOnly = true;

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 direction =
                transform.position - camera.transform.position;
            if (yawOnly) direction.y = 0f;
            if (direction.sqrMagnitude < .0001f) return;
            transform.rotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);
        }
    }
}
