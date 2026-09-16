using UnityEngine;

/// <summary>
/// Constrói em runtime uma arena demonstrativa de palafita tropical usando somente
/// geometrias nativas. O cenário é independente da lógica de combate e pode ser
/// trocado por um mapa autoral sem alterar os lutadores.
/// </summary>
[DisallowMultipleComponent]
public sealed class ArenaEnvironmentBuilder : MonoBehaviour
{
    private const string RootName = "RainforestDockArena";

    private Material wood;
    private Material darkWood;
    private Material leaf;
    private Material vine;
    private Material water;
    private Material sky;
    private Material sand;

    public void Build()
    {
        if (GameObject.Find(RootName) != null) return;
        DisableLegacyArena();
        CreateMaterials();

        Transform root = new GameObject(RootName).transform;
        CreateSky(root);
        CreateWater(root);
        CreateDock(root);
        CreateCentralTree(root);
        CreateHut(root, new Vector3(-8.2f, 1.35f, 4.8f), 1.15f, false);
        CreateHut(root, new Vector3(8.2f, 1.35f, 5.1f), 1.0f, true);
        CreateTree(root, new Vector3(-10.4f, 0f, 6.5f), 1.45f);
        CreateTree(root, new Vector3(10.6f, 0f, 6.8f), 1.3f);
        CreateTree(root, new Vector3(-6.2f, 0f, 8.2f), .85f);
        CreateTree(root, new Vector3(6.8f, 0f, 8.6f), .9f);
        CreatePlants(root);
        CreateImportedLandmarks(root);
        CreateRopeRails(root);
        CreateLighting(root);
    }

    private static void DisableLegacyArena()
    {
        string[] legacyNames = { "Arena_Floor", "Arena_Decorations", "Pillar_Red", "Pillar_Blue", "Pillar_Green", "Pillar_Yellow", "Ring_Center_Marker", "Spectators" };
        foreach (string item in legacyNames)
        {
            GameObject legacy = GameObject.Find(item);
            if (legacy != null) legacy.SetActive(false);
        }
    }

    private void CreateMaterials()
    {
        wood = MaterialOf(new Color(.35f, .17f, .07f), .18f, .08f);
        darkWood = MaterialOf(new Color(.13f, .055f, .018f), .08f, .12f);
        leaf = MaterialOf(new Color(.035f, .24f, .08f), .05f, .2f);
        vine = MaterialOf(new Color(.08f, .34f, .08f), .05f, .25f);
        water = MaterialOf(new Color(.02f, .31f, .48f), .72f, .0f);
        sky = UnlitMaterial(new Color(.12f, .52f, .82f));
        sand = MaterialOf(new Color(.52f, .37f, .17f), .35f, .15f);
    }

    private static Material MaterialOf(Color color, float smoothness, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        return material;
    }

    private static Material UnlitMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    private void CreateSky(Transform root)
    {
        GameObject background = Primitive(PrimitiveType.Quad, "TropicalSky", new Vector3(0f, 7f, 18f), new Vector3(28f, 15f, 1f), sky, root);
        background.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        GameObject cloudA = Primitive(PrimitiveType.Sphere, "Cloud", new Vector3(-5f, 8.7f, 17.7f), new Vector3(4f, .7f, .3f), MaterialOf(new Color(.94f, .98f, 1f), .15f, 0f), root);
        GameObject cloudB = Primitive(PrimitiveType.Sphere, "Cloud", new Vector3(4.5f, 7.8f, 17.7f), new Vector3(3.2f, .55f, .3f), MaterialOf(new Color(.94f, .98f, 1f), .15f, 0f), root);
        cloudA.transform.rotation = Quaternion.Euler(0, 0, -8f);
        cloudB.transform.rotation = Quaternion.Euler(0, 0, 5f);
    }

    private void CreateWater(Transform root)
    {
        GameObject plane = Primitive(PrimitiveType.Plane, "River", new Vector3(0f, -.06f, 6.3f), new Vector3(3.5f, 1f, 2f), water, root);
        plane.transform.localScale = new Vector3(3.8f, 1f, 1.65f);
        for (int i = -9; i <= 9; i += 3)
            Primitive(PrimitiveType.Cube, "WaterHighlight", new Vector3(i + .7f, .01f, 4.5f + Mathf.Abs(i % 2)), new Vector3(1.4f, .012f, .06f), MaterialOf(new Color(.38f, .85f, .9f), .9f, 0f), root);
    }

    private void CreateDock(Transform root)
    {
        Primitive(PrimitiveType.Cube, "DockFoundation", new Vector3(0f, -.05f, -1.2f), new Vector3(23f, .22f, 12f), darkWood, root);
        for (int z = -6; z <= 3; z++)
        {
            for (int x = -10; x <= 10; x++)
            {
                float stagger = z % 2 == 0 ? .06f : -.06f;
                GameObject board = Primitive(PrimitiveType.Cube, "DockBoard", new Vector3(x + stagger, .16f, z * .78f), new Vector3(.94f, .18f, .72f), wood, root);
                board.transform.localRotation = Quaternion.Euler(0f, (x * 7 + z * 3) % 4, 0f);
            }
        }
        for (int x = -10; x <= 10; x += 4)
        {
            CreatePost(root, new Vector3(x, -.85f, -2.9f));
            CreatePost(root, new Vector3(x, -.85f, 2.9f));
        }
    }

    private void CreateCentralTree(Transform root)
    {
        // A árvore monumental fica fora do eixo de combate para nunca esconder os lutadores.
        Vector3 at = new Vector3(-6.3f, 0f, 12.4f);
        GameObject trunk = Primitive(PrimitiveType.Cylinder, "AncientCeiba", at + Vector3.up * 3.5f, new Vector3(1.18f, 3.5f, 1.18f), darkWood, root);
        trunk.transform.rotation = Quaternion.Euler(0f, 0f, -2f);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f;
            Vector3 branchAt = at + Vector3.up * (4.1f + (i % 2) * .5f) + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.2f;
            GameObject branch = Primitive(PrimitiveType.Cylinder, "CeibaBranch", branchAt, new Vector3(.22f, 1.75f, .22f), darkWood, root);
            branch.transform.rotation = Quaternion.Euler(42f, angle, 0f);
            CreateCanopy(root, branchAt + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.2f + Vector3.up * .65f, 2f);
        }
        CreateCanopy(root, at + Vector3.up * 7.8f, 2.8f);
        for (int x = -2; x <= 2; x += 2)
            CreateVine(root, at + new Vector3(x, 7.3f, -.35f), 4.2f + Mathf.Abs(x));
    }

    private void CreateTree(Transform root, Vector3 at, float scale)
    {
        Primitive(PrimitiveType.Cylinder, "PalmTrunk", at + Vector3.up * (2.3f * scale), new Vector3(.28f * scale, 2.3f * scale, .28f * scale), darkWood, root);
        Vector3 crown = at + Vector3.up * (4.75f * scale);
        for (int i = 0; i < 7; i++)
        {
            float angle = i * 51f;
            GameObject frond = Primitive(PrimitiveType.Capsule, "PalmFrond", crown + Quaternion.Euler(0, angle, 0) * Vector3.forward * (1.15f * scale), new Vector3(.27f * scale, 1.7f * scale, .7f * scale), leaf, root);
            frond.transform.rotation = Quaternion.Euler(68f, angle, 0f);
        }
    }

    private void CreateCanopy(Transform root, Vector3 at, float size)
    {
        Primitive(PrimitiveType.Sphere, "CeibaLeaves", at, new Vector3(size * 1.25f, size * .68f, size), leaf, root);
        Primitive(PrimitiveType.Sphere, "CeibaLeaves", at + new Vector3(size * .55f, .25f, .15f), new Vector3(size, size * .55f, size * .85f), leaf, root);
    }

    private void CreateVine(Transform root, Vector3 at, float length)
    {
        GameObject vinePiece = Primitive(PrimitiveType.Cylinder, "HangingVine", at - Vector3.up * length * .5f, new Vector3(.055f, length * .5f, .055f), vine, root);
        vinePiece.transform.rotation = Quaternion.Euler(4f, 0f, 5f);
    }

    private void CreateHut(Transform root, Vector3 at, float scale, bool flip)
    {
        Primitive(PrimitiveType.Cube, "HutWall", at, new Vector3(3.1f * scale, 2.3f * scale, .36f), wood, root);
        Primitive(PrimitiveType.Cube, "HutSide", at + new Vector3(flip ? -1.45f : 1.45f, 0f, .78f), new Vector3(.36f, 2.3f * scale, 1.9f * scale), wood, root);
        GameObject roof = Primitive(PrimitiveType.Cube, "ThatchedRoof", at + Vector3.up * (1.55f * scale), new Vector3(3.8f * scale, .45f, 2.6f * scale), sand, root);
        roof.transform.rotation = Quaternion.Euler(0f, 0f, flip ? 8f : -8f);
        for (int i = -1; i <= 1; i++)
            CreatePost(root, at + new Vector3(i * scale, -1.45f * scale, -.85f * scale));
    }

    private void CreatePlants(Transform root)
    {
        for (int i = 0; i < 32; i++)
        {
            float x = Mathf.Sin(i * 2.41f) * 10.3f;
            float z = 3.7f + (i % 6) * .86f;
            float size = .22f + (i % 4) * .1f;
            Primitive(PrimitiveType.Sphere, "RainforestPlant", new Vector3(x, size * .55f, z), new Vector3(size * 2.4f, size, size * 1.8f), leaf, root);
        }
    }

    private static void CreateImportedLandmarks(Transform root)
    {
        // Assets autorais entram atras do plano de luta: refinam a composicao sem
        // ocupar o espaco fisico nem esconder os dois personagens.
        GameObject building = Resources.Load<GameObject>("Arena/DistanceBuilding");
        if (building != null)
        {
            GameObject instance = Instantiate(building, new Vector3(7.4f, .02f, 13.4f), Quaternion.Euler(0f, 205f, 0f), root);
            instance.name = "DistanceBuilding_Imported";
            instance.transform.localScale = Vector3.one * 2.15f;
            DisableColliders(instance);
        }
        GameObject tree = Resources.Load<GameObject>("Arena/FeaturedTree");
        if (tree != null)
        {
            GameObject instance = Instantiate(tree, new Vector3(-8.7f, .02f, 10.8f), Quaternion.Euler(0f, 165f, 0f), root);
            instance.name = "FeaturedTree_Imported";
            instance.transform.localScale = Vector3.one * 2.7f;
            DisableColliders(instance);
        }
    }

    private static void DisableColliders(GameObject item)
    {
        foreach (Collider collider in item.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
    }

    private void CreateRopeRails(Transform root)
    {
        const float backSide = 3.05f;
        for (int x = -8; x < 9; x += 4)
            if (x != 0) CreatePost(root, new Vector3(x, .5f, backSide));
        for (int x = -6; x <= 6; x += 4)
        {
            GameObject rope = Primitive(PrimitiveType.Cylinder, "DockRope", new Vector3(x, 1.25f, backSide), new Vector3(.045f, 2.02f, .045f), sand, root);
            rope.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }
    }

    private void CreatePost(Transform root, Vector3 position)
    {
        Primitive(PrimitiveType.Cylinder, "DockPost", position + Vector3.up * 1.5f, new Vector3(.16f, 1.5f, .16f), darkWood, root);
    }

    private static void CreateLighting(Transform root)
    {
        GameObject glow = new GameObject("WarmDockLight");
        glow.transform.SetParent(root);
        glow.transform.position = new Vector3(0f, 5.5f, 2.2f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, .72f, .4f);
        light.range = 13f;
        light.intensity = 3.2f;
    }

    private static GameObject Primitive(PrimitiveType type, string objectName, Vector3 position, Vector3 scale, Material material, Transform parent)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return item;
    }
}
