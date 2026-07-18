using UnityEngine;

namespace AgriVerse.Client
{
    /// <summary>
    /// Compact procedural field presentation. It is deliberately data-neutral: the scenario
    /// still owns all learning content, while this component only creates non-interactive scenery.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MekongEnvironmentController : MonoBehaviour
    {
        private static readonly Color RiceDeep = new Color(.16f, .34f, .18f);
        private static readonly Color RiceGreen = new Color(.33f, .56f, .20f);
        private static readonly Color RiceSun = new Color(.58f, .72f, .27f);
        private static readonly Color WaterTeal = new Color(.08f, .45f, .47f);
        private static readonly Color BankClay = new Color(.53f, .25f, .15f);
        private static readonly Color PathSand = new Color(.76f, .57f, .32f);
        private static readonly Color Wood = new Color(.31f, .16f, .08f);
        private static readonly Color Roof = new Color(.60f, .22f, .12f);
        private static readonly Color Cream = new Color(.94f, .84f, .62f);
        private bool built;

        private void Start() => Build();

        public void BuildForTesting() => Build();

        private void Build()
        {
            if (built) return;
            built = true;
            ConfigureAtmosphere();
            Transform root = new GameObject("MekongFieldEnvironment").transform;
            root.SetParent(transform, false);

            Box(root, "FieldBase", new Vector3(0f, -.65f, 11f), new Vector3(34f, 1f, 44f), RiceDeep);
            Water(root, "WarmTealChannel", new Vector3(0f, -.08f, 6f), new Vector3(5f, .16f, 25f));
            Box(root, "CentralRaisedPath", new Vector3(0f, .15f, 7f), new Vector3(1.35f, .42f, 27f), PathSand);

            Paddy(root, "NorthWestPaddy", new Vector3(-6.4f, -.08f, 8f), new Vector2(9f, 8.4f), RiceGreen);
            Paddy(root, "NorthEastPaddy", new Vector3(6.4f, -.08f, 8f), new Vector2(9f, 8.4f), RiceSun);
            Paddy(root, "SouthWestPaddy", new Vector3(-6.4f, -.08f, -.8f), new Vector2(9f, 7f), new Color(.28f, .48f, .18f));
            Paddy(root, "SouthEastPaddy", new Vector3(6.4f, -.08f, -.8f), new Vector2(9f, 7f), new Color(.43f, .63f, .22f));

            Dock(root, new Vector3(-4.3f, .18f, -2.2f));
            Shelter(root, new Vector3(9.5f, .15f, 17f));
            Palm(root, new Vector3(-12f, .1f, 12f), 4.5f);
            Palm(root, new Vector3(13f, .1f, 8f), 5.5f);
            Palm(root, new Vector3(-14f, .1f, 20f), 4f);
            Horizon(root);
        }

        private static void ConfigureAtmosphere()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(.67f, .78f, .74f);
            RenderSettings.fogDensity = .006f;
            RenderSettings.ambientSkyColor = new Color(.52f, .68f, .69f);
            RenderSettings.ambientEquatorColor = new Color(.72f, .64f, .44f);
            RenderSettings.ambientGroundColor = new Color(.22f, .28f, .16f);
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                light.color = new Color(1f, .82f, .56f);
                light.intensity = 1.25f;
                light.shadows = LightShadows.Soft;
                light.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
            }
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.transform.position = new Vector3(0f, 4.4f, -13.5f);
                camera.transform.rotation = Quaternion.Euler(13f, 0f, 0f);
                camera.fieldOfView = 54f;
                camera.backgroundColor = new Color(.48f, .68f, .74f);
            }
        }

        private static void Paddy(Transform root, string name, Vector3 center, Vector2 size, Color riceColor)
        {
            Box(root, name, center, new Vector3(size.x, .18f, size.y), riceColor);
            Box(root, name + "BankNorth", center + new Vector3(0f, .18f, size.y * .5f), new Vector3(size.x + .35f, .34f, .38f), BankClay);
            Box(root, name + "BankSouth", center + new Vector3(0f, .18f, -size.y * .5f), new Vector3(size.x + .35f, .34f, .38f), BankClay);
            Box(root, name + "BankWest", center + new Vector3(-size.x * .5f, .18f, 0f), new Vector3(.38f, .34f, size.y), BankClay);
            Box(root, name + "BankEast", center + new Vector3(size.x * .5f, .18f, 0f), new Vector3(.38f, .34f, size.y), BankClay);
            for (int row = 0; row < 6; row++)
            {
                float z = center.z - size.y * .33f + row * (size.y * .13f);
                Box(root, name + "RiceRow" + row, new Vector3(center.x, .19f, z), new Vector3(size.x * .78f, .16f, .10f), row % 2 == 0 ? RiceSun : RiceGreen);
            }
        }

        private static void Water(Transform root, string name, Vector3 position, Vector3 scale)
        {
            Box(root, name, position, scale, WaterTeal, .72f);
            Transform rippleRoot = new GameObject(name + "Ripples").transform;
            rippleRoot.SetParent(root, false);
            for (int ripple = 0; ripple < 7; ripple++)
                Box(rippleRoot, "Ripple" + ripple,
                    position + new Vector3(-1.5f + (ripple % 3) * 1.35f, .1f, -8f + ripple * 2.8f),
                    new Vector3(.72f, .015f, .05f), Cream, .5f);
        }

        private static void Dock(Transform root, Vector3 origin)
        {
            Transform dock = new GameObject("SamplingDock").transform;
            dock.SetParent(root, false);
            for (int plank = 0; plank < 5; plank++)
                Box(dock, "DockPlank" + plank, origin + new Vector3(plank * .72f, .18f, 0f), new Vector3(.64f, .16f, 2.6f), Wood);
            for (int post = 0; post < 3; post++)
                Cylinder(dock, "DockPost" + post, origin + new Vector3(post * 1.25f, -.35f, post % 2 == 0 ? -1.05f : 1.05f), new Vector3(.16f, 1.15f, .16f), Wood);
        }

        private static void Shelter(Transform root, Vector3 origin)
        {
            Transform shelter = new GameObject("FieldShelter").transform;
            shelter.SetParent(root, false);
            Box(shelter, "ShelterPlatform", origin + new Vector3(0f, 1.15f, 0f), new Vector3(5f, .24f, 3.4f), Wood);
            for (int corner = 0; corner < 4; corner++)
            {
                float x = corner < 2 ? -2f : 2f;
                float z = corner % 2 == 0 ? -1.2f : 1.2f;
                Cylinder(shelter, "ShelterPost" + corner, origin + new Vector3(x, 1.35f, z), new Vector3(.22f, 2.7f, .22f), Wood);
            }
            GameObject roof = Box(shelter, "ShelterRoof", origin + new Vector3(0f, 3.1f, 0f), new Vector3(5.9f, .36f, 4.2f), Roof);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            Box(shelter, "ShelterWall", origin + new Vector3(-1.95f, 2f, 0f), new Vector3(.16f, 1.5f, 2.6f), Cream);
        }

        private static void Palm(Transform root, Vector3 origin, float height)
        {
            Transform palm = new GameObject("CoconutPalm").transform;
            palm.SetParent(root, false);
            Cylinder(palm, "PalmTrunk", origin + new Vector3(0f, height * .5f, 0f), new Vector3(.38f, height, .38f), Wood);
            for (int leaf = 0; leaf < 5; leaf++)
            {
                GameObject frond = Box(palm, "PalmFrond" + leaf, origin + new Vector3(0f, height + .1f, 0f), new Vector3(.42f, .09f, 3.5f), leaf % 2 == 0 ? RiceDeep : RiceGreen);
                frond.transform.localRotation = Quaternion.Euler(18f, leaf * 72f, 0f);
            }
        }

        private static void Horizon(Transform root)
        {
            for (int layer = 0; layer < 3; layer++)
            {
                Color color = Color.Lerp(new Color(.47f, .63f, .59f), new Color(.68f, .76f, .70f), layer * .34f);
                Box(root, "HorizonLayer" + layer, new Vector3(0f, 2.2f + layer * .45f, 29f + layer * 6f), new Vector3(38f - layer * 3f, 3.2f, .45f), color);
            }
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Color color, float smoothness = .16f)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            ApplySurface(obj, color, smoothness);
            return obj;
        }

        private static void Cylinder(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = scale;
            ApplySurface(obj, color, .12f);
        }

        private static void ApplySurface(GameObject obj, Color color, float smoothness)
        {
            Collider collider = obj.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = MekongEnvironmentMaterial.Create(color, smoothness);
        }
    }
}
