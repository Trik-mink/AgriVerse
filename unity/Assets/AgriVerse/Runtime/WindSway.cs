using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Restrained shared-direction sway for authored tree and reed meshes.
    /// Dense rice and grass use the same field in InstancedVegetationField.
    /// </summary>
    public sealed class WindSway : MonoBehaviour
    {
        public static readonly Vector2 SharedDirection =
            new Vector2(.82f, .57f).normalized;

        [SerializeField] private float maximumDegrees = 1.35f;
        [SerializeField] private float speed = .72f;
        private Quaternion restRotation;
        private float phase;

        public void Configure(float degrees, float configuredSpeed)
        {
            maximumDegrees = degrees;
            speed = configuredSpeed;
        }

        private void Awake()
        {
            restRotation = transform.localRotation;
            Vector3 position = transform.position;
            phase =
                position.x * .071f +
                position.z * .053f;
        }

        private void Update()
        {
            float amount = Mathf.Sin(
                Time.time * speed + phase) * maximumDegrees;
            Vector3 axis = new Vector3(
                SharedDirection.y,
                0f,
                -SharedDirection.x);
            transform.localRotation =
                Quaternion.AngleAxis(amount, axis) * restRotation;
        }
    }
}
