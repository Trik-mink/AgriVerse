using UnityEngine;
using UnityEngine.InputSystem;

namespace AgriVerse.Client
{
    /// <summary>
    /// Walking-only first-person controller used by Episode 1 and its world lab.
    /// It intentionally has no sprint, jump, combat, inventory, or player body.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonWalker : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;
        [SerializeField] private float eyeHeight = 1.65f;
        [SerializeField] private float walkSpeed = 3.3f;
        [SerializeField] private float lookSensitivity = .075f;
        [SerializeField] private float gravity = 18f;
        [SerializeField] private bool movementEnabled = true;

        private CharacterController character;
        private float yaw;
        private float pitch;
        private float verticalSpeed;

        public Camera ViewCamera => viewCamera;
        public float EyeHeight => eyeHeight;
        public bool MovementEnabled => movementEnabled;
        public bool CursorIsCaptured =>
            Cursor.lockState == CursorLockMode.Locked;
        public float ViewHeading => yaw;
        public float ViewPitch => pitch;

        public void Configure(
            Camera camera,
            float configuredEyeHeight = 1.65f)
        {
            viewCamera = camera;
            eyeHeight = configuredEyeHeight;
            if (viewCamera != null)
            {
                viewCamera.transform.localPosition =
                    new Vector3(0f, eyeHeight, 0f);
            }
        }

        public void Teleport(
            Vector3 groundPosition,
            float heading,
            float verticalLook = 0f)
        {
            if (character == null)
            {
                character = GetComponent<CharacterController>();
            }
            bool wasEnabled = character.enabled;
            character.enabled = false;
            transform.position = groundPosition;
            yaw = heading;
            pitch = Mathf.Clamp(verticalLook, -82f, 82f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (viewCamera != null)
            {
                viewCamera.transform.localRotation =
                    Quaternion.Euler(pitch, 0f, 0f);
            }
            character.enabled = wasEnabled;
            verticalSpeed = 0f;
        }

        public void SetViewAngles(float heading, float verticalLook)
        {
            yaw = heading;
            pitch = Mathf.Clamp(verticalLook, -82f, 82f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (viewCamera != null)
            {
                viewCamera.transform.localRotation =
                    Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        public void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void SetMovementEnabled(bool enabled)
        {
            movementEnabled = enabled;
            if (!enabled)
            {
                verticalSpeed = 0f;
                ReleaseCursor();
            }
        }

        public void Move(Vector2 input, float deltaTime)
        {
            if (character == null)
            {
                character = GetComponent<CharacterController>();
            }
            input = Vector2.ClampMagnitude(input, 1f);
            if (character.isGrounded && verticalSpeed < 0f)
            {
                verticalSpeed = -2f;
            }
            verticalSpeed -= gravity * Mathf.Max(0f, deltaTime);
            Vector3 planar =
                (transform.forward * input.y +
                 transform.right * input.x) * walkSpeed;
            character.Move(
                (planar + Vector3.up * verticalSpeed) *
                Mathf.Max(0f, deltaTime));
        }

        private void Awake()
        {
            Application.runInBackground = true;
            character = GetComponent<CharacterController>();
            if (viewCamera == null)
            {
                viewCamera = GetComponentInChildren<Camera>(true);
            }
            if (viewCamera != null)
            {
                viewCamera.transform.localPosition =
                    new Vector3(0f, eyeHeight, 0f);
            }
            yaw = transform.eulerAngles.y;
            pitch = viewCamera == null
                ? 0f
                : NormalizeAngle(viewCamera.transform.localEulerAngles.x);
        }

        private void Start()
        {
            CaptureCursor();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null &&
                keyboard.escapeKey.wasPressedThisFrame)
            {
                ReleaseCursor();
            }
            else if (movementEnabled &&
                     !CursorIsCaptured &&
                     mouse != null &&
                     mouse.leftButton.wasPressedThisFrame)
            {
                CaptureCursor();
            }

            if (!movementEnabled)
            {
                return;
            }

            if (CursorIsCaptured && mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * lookSensitivity;
                pitch = Mathf.Clamp(
                    pitch - delta.y * lookSensitivity,
                    -82f,
                    82f);
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                if (viewCamera != null)
                {
                    viewCamera.transform.localRotation =
                        Quaternion.Euler(pitch, 0f, 0f);
                }
            }

            Vector2 input = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed) input.x -= 1f;
            }
            Move(input, Time.deltaTime);
        }

        private void OnDisable()
        {
            ReleaseCursor();
        }

        private static float NormalizeAngle(float angle) =>
            angle > 180f ? angle - 360f : angle;
    }
}
