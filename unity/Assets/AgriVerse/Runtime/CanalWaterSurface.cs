using UnityEngine;

namespace AgriVerse.Client
{
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class CanalWaterSurface : MonoBehaviour
    {
        [SerializeField] private Vector2 normalSpeed =
            new Vector2(.018f, .011f);
        [SerializeField] private float verticalRipple = .012f;
        [SerializeField] private float rippleSpeed = .55f;
        private Material runtimeMaterial;
        private float baseHeight;

        public Bounds WorldBounds
        {
            get
            {
                MeshRenderer renderer = GetComponent<MeshRenderer>();
                return renderer == null
                    ? new Bounds(transform.position, Vector3.zero)
                    : renderer.bounds;
            }
        }

        private void Awake()
        {
            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer.sharedMaterial != null)
            {
                runtimeMaterial = new Material(renderer.sharedMaterial)
                {
                    name = renderer.sharedMaterial.name + " (Runtime)"
                };
                renderer.sharedMaterial = runtimeMaterial;
            }
            baseHeight = transform.position.y;
        }

        private void Update()
        {
            float time = Time.time;
            if (runtimeMaterial != null &&
                runtimeMaterial.HasProperty("_BumpMap"))
            {
                runtimeMaterial.SetTextureOffset(
                    "_BumpMap",
                    normalSpeed * time);
            }
            Vector3 position = transform.position;
            position.y = baseHeight +
                         Mathf.Sin(time * rippleSpeed) *
                         verticalRipple;
            transform.position = position;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }
    }
}
