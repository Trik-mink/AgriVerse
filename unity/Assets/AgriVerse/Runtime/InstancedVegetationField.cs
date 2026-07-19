using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AgriVerse.Client
{
    /// <summary>
    /// Deterministic GPU-instanced paddy/bank vegetation with distance-selected
    /// mesh, alpha-card, and camera-facing billboard LODs.
    /// </summary>
    public sealed class InstancedVegetationField : MonoBehaviour
    {
        [Serializable]
        private struct Plant
        {
            public Vector3 Position;
            public float Scale;
            public float Yaw;
            public float Phase;
        }

        [SerializeField] private Mesh[] lodMeshes = new Mesh[3];
        [SerializeField] private Material[] lodMaterials =
            new Material[3];
        [SerializeField] private Vector3[] lodLocalPositions =
            new Vector3[3];
        [SerializeField] private Quaternion[] lodLocalRotations =
        {
            Quaternion.identity,
            Quaternion.identity,
            Quaternion.identity
        };
        [SerializeField] private Vector3[] lodLocalScales =
        {
            Vector3.one,
            Vector3.one,
            Vector3.one
        };
        [SerializeField] private Terrain terrain;
        [SerializeField] private Vector2 areaSize =
            new Vector2(10f, 10f);
        [SerializeField] private float spacing = .62f;
        [SerializeField] private float jitter = .16f;
        [SerializeField] private float minimumScale = .88f;
        [SerializeField] private float maximumScale = 1.12f;
        [SerializeField] private int seed = 73;
        [SerializeField] private float lod0Distance = 23f;
        [SerializeField] private float lod1Distance = 52f;
        [SerializeField, Range(0f, .3f)] private float patchGap = .07f;

        private Plant[] plants = Array.Empty<Plant>();
        private readonly List<Matrix4x4>[] matrices =
        {
            new List<Matrix4x4>(1024),
            new List<Matrix4x4>(1024),
            new List<Matrix4x4>(1024)
        };

        public int InstanceCount => plants.Length;
        public IReadOnlyList<Mesh> LodMeshes => lodMeshes;
        public IReadOnlyList<Material> LodMaterials => lodMaterials;
        public IReadOnlyList<Vector3> LodLocalScales =>
            lodLocalScales;

        public void Rebuild()
        {
            BuildPlants();
        }

        public void Configure(
            GameObject lodPrefab,
            Terrain sourceTerrain,
            Vector2 configuredAreaSize,
            float configuredSpacing,
            int configuredSeed,
            float configuredMinimumScale = .88f,
            float configuredMaximumScale = 1.12f,
            float configuredPatchGap = .07f)
        {
            if (lodPrefab == null)
            {
                throw new ArgumentNullException(nameof(lodPrefab));
            }
            LODGroup group = lodPrefab.GetComponent<LODGroup>();
            if (group == null || group.GetLODs().Length != 3)
            {
                throw new InvalidOperationException(
                    "Vegetation field requires a three-level LOD prefab.");
            }

            LOD[] lods = group.GetLODs();
            for (int index = 0; index < 3; index++)
            {
                Renderer renderer = lods[index].renderers[0];
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                lodMeshes[index] = filter.sharedMesh;
                lodMaterials[index] = renderer.sharedMaterial;
                lodLocalPositions[index] =
                    renderer.transform.localPosition;
                lodLocalRotations[index] =
                    renderer.transform.localRotation;
                lodLocalScales[index] =
                    renderer.transform.localScale;
            }
            terrain = sourceTerrain;
            areaSize = configuredAreaSize;
            spacing = configuredSpacing;
            seed = configuredSeed;
            minimumScale = configuredMinimumScale;
            maximumScale = configuredMaximumScale;
            patchGap = configuredPatchGap;
            BuildPlants();
        }

        private void OnEnable()
        {
            BuildPlants();
        }

        private void BuildPlants()
        {
            if (lodMeshes == null ||
                lodMeshes.Length != 3 ||
                lodMeshes[0] == null ||
                spacing <= .05f)
            {
                plants = Array.Empty<Plant>();
                return;
            }

            var random = new System.Random(seed);
            int columns = Mathf.Max(1, Mathf.FloorToInt(
                areaSize.x / spacing));
            int rows = Mathf.Max(1, Mathf.FloorToInt(
                areaSize.y / spacing));
            var generated = new List<Plant>(columns * rows);
            Vector3 center = transform.position;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    if (random.NextDouble() < patchGap) continue;
                    float x =
                        center.x - areaSize.x * .5f +
                        (column + .5f) * spacing +
                        RandomRange(random, -jitter, jitter);
                    float z =
                        center.z - areaSize.y * .5f +
                        (row + .5f) * spacing +
                        RandomRange(random, -jitter, jitter);
                    float y = terrain == null
                        ? center.y
                        : terrain.SampleHeight(new Vector3(x, 0f, z)) +
                          terrain.transform.position.y + .015f;
                    generated.Add(
                        new Plant
                        {
                            Position = new Vector3(x, y, z),
                            Scale = RandomRange(
                                random,
                                minimumScale,
                                maximumScale),
                            Yaw = RandomRange(random, 0f, 360f),
                            Phase = RandomRange(
                                random,
                                0f,
                                Mathf.PI * 2f)
                        });
                }
            }
            plants = generated.ToArray();
        }

        private void LateUpdate()
        {
            Camera camera = Camera.main;
            if (camera == null || plants.Length == 0) return;
            for (int index = 0; index < matrices.Length; index++)
            {
                matrices[index].Clear();
            }

            Vector3 cameraPosition = camera.transform.position;
            float nearSquared = lod0Distance * lod0Distance;
            float middleSquared = lod1Distance * lod1Distance;
            float time = Time.time;
            foreach (Plant plant in plants)
            {
                float distanceSquared =
                    (plant.Position - cameraPosition).sqrMagnitude;
                int lod = distanceSquared < nearSquared
                    ? 0
                    : distanceSquared < middleSquared
                        ? 1
                        : 2;
                float wind = Mathf.Sin(
                    time * .74f +
                    plant.Phase +
                    Vector2.Dot(
                        new Vector2(
                            plant.Position.x,
                            plant.Position.z),
                        WindSway.SharedDirection) * .045f) * 1.1f;
                Quaternion rotation;
                if (lod == 2)
                {
                    Vector3 facing =
                        plant.Position - cameraPosition;
                    facing.y = 0f;
                    rotation = facing.sqrMagnitude < .001f
                        ? Quaternion.identity
                        : Quaternion.LookRotation(
                            facing.normalized,
                            Vector3.up);
                }
                else
                {
                    rotation = Quaternion.Euler(
                        0f,
                        plant.Yaw,
                        wind);
                }
                Matrix4x4 plantMatrix =
                    Matrix4x4.TRS(
                        plant.Position,
                        rotation,
                        Vector3.one * plant.Scale);
                Matrix4x4 sourceTransform =
                    Matrix4x4.TRS(
                        lodLocalPositions[lod],
                        lodLocalRotations[lod],
                        lodLocalScales[lod]);
                matrices[lod].Add(
                    plantMatrix * sourceTransform);
            }

            for (int lod = 0; lod < 3; lod++)
            {
                DrawBatches(
                    lodMeshes[lod],
                    lodMaterials[lod],
                    matrices[lod],
                    lod < 2);
            }
        }

        private static void DrawBatches(
            Mesh mesh,
            Material material,
            List<Matrix4x4> source,
            bool shadows)
        {
            if (mesh == null || material == null ||
                source.Count == 0)
            {
                return;
            }
            const int maximum = 1023;
            var batch = new Matrix4x4[
                Mathf.Min(maximum, source.Count)];
            for (int offset = 0;
                 offset < source.Count;
                 offset += maximum)
            {
                int count = Mathf.Min(
                    maximum,
                    source.Count - offset);
                if (batch.Length < count)
                {
                    batch = new Matrix4x4[count];
                }
                source.CopyTo(offset, batch, 0, count);
                Graphics.DrawMeshInstanced(
                    mesh,
                    0,
                    material,
                    batch,
                    count,
                    null,
                    shadows
                        ? ShadowCastingMode.On
                        : ShadowCastingMode.Off,
                    true,
                    2,
                    null,
                    LightProbeUsage.Off,
                    null);
            }
        }

        private static float RandomRange(
            System.Random random,
            float minimum,
            float maximum) =>
            Mathf.Lerp(
                minimum,
                maximum,
                (float)random.NextDouble());
    }
}
