#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Construtor automatizado da cena "Rua Urbana" para o UnDFight.
/// Cria uma arena 3D temática brasileira com profundidade em camadas
/// (Background, Midground, Gameplay_Arena, Props, Iluminação e Câmera),
/// compatível com TekkenCamera, FighterMovement e GameFlowController.
/// </summary>
public static class RuaUrbanaSceneBuilder
{
    public const string ScenePath = "Assets/Scenes/RuaUrbana.unity";
    private const string MaterialsFolder = "Assets/Materials/Stages/RuaUrbana";
    private const string PrefabFolder = "Assets/Prefabs/Characters";
    private const string URPProfilePath = "Assets/Settings/SampleSceneProfile.asset";

    private static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();

    [MenuItem("Tools/UnDFight/Build Rua Urbana Scene")]
    public static void BuildSceneMenu()
    {
        BuildScene();
    }

    public static void BuildScene()
    {
        Debug.Log("[RuaUrbanaSceneBuilder] Iniciando construção da cena Rua Urbana...");

        // 1. Garante pastas necessárias
        EnsureFoldersExist();
        EnsureEnvironmentLayer();

        // 2. Prepara os materiais URP Lit
        InitializeMaterials();

        // 3. Cria uma nova cena limpa
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 4. Constrói as camadas do cenário
        Transform rootAtmosphere = CreateGroup("Lighting_Atmosphere");
        Transform rootGameplay = CreateGroup("Gameplay_Arena");
        Transform rootMidground = CreateGroup("Midground");
        Transform rootProps = CreateGroup("Scene_Props_Decor");
        Transform rootBackground = CreateGroup("Background");
        Transform rootManagers = CreateGroup("Cameras_And_Managers");

        // 5. Popula cada camada
        SetupLightingAndAtmosphere(rootAtmosphere);
        SetupGameplayArena(rootGameplay);
        SetupMidgroundFacades(rootMidground);
        SetupInfrastructure(rootMidground);
        SetupTropicalVegetation(rootMidground);
        SetupSceneProps(rootProps);
        SetupBackgroundSkyline(rootBackground);

        // 6. Configura Spawns, Câmera e Fluxo de Jogo
        SetupGameplayEntities(rootManagers);

        // 7. Salva a cena
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[RuaUrbanaSceneBuilder] Cena salva em: {ScenePath}");

        // 8. Registra no EditorBuildSettings
        RegisterInBuildSettings(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RuaUrbanaSceneBuilder] Construção da cena Rua Urbana finalizada com sucesso!");
    }

    #region 1. Pasta e Materiais

    private static void EnsureFoldersExist()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Stages"))
            AssetDatabase.CreateFolder("Assets/Materials", "Stages");
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            AssetDatabase.CreateFolder("Assets/Materials/Stages", "RuaUrbana");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    private static void InitializeMaterials()
    {
        MatCache.Clear();
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null) urpLit = Shader.Find("Standard");

        // Paleta Urbana Brasileira
        GetOrCreateMaterial("Mat_Asphalt", new Color(0.20f, 0.21f, 0.23f), 0.05f, 0.2f, urpLit);
        GetOrCreateMaterial("Mat_Cobblestone_Sidewalk", new Color(0.60f, 0.58f, 0.55f), 0.0f, 0.15f, urpLit);
        GetOrCreateMaterial("Mat_Curb", new Color(0.48f, 0.49f, 0.50f), 0.0f, 0.2f, urpLit);
        GetOrCreateMaterial("Mat_Crosswalk_White", new Color(0.92f, 0.91f, 0.88f), 0.0f, 0.35f, urpLit);
        GetOrCreateMaterial("Mat_Yellow_Curbs", new Color(0.92f, 0.72f, 0.12f), 0.0f, 0.3f, urpLit);

        // Fachadas e Casas
        GetOrCreateMaterial("Mat_Boteco_Yellow", new Color(0.95f, 0.74f, 0.18f), 0.0f, 0.25f, urpLit);
        GetOrCreateMaterial("Mat_Boteco_Blue_Tile", new Color(0.11f, 0.32f, 0.62f), 0.1f, 0.75f, urpLit);
        GetOrCreateMaterial("Mat_Terracotta", new Color(0.72f, 0.34f, 0.21f), 0.0f, 0.15f, urpLit);
        GetOrCreateMaterial("Mat_Barbearia_Turquoise", new Color(0.22f, 0.62f, 0.58f), 0.0f, 0.3f, urpLit);
        GetOrCreateMaterial("Mat_Casa_Mint", new Color(0.45f, 0.74f, 0.61f), 0.0f, 0.2f, urpLit);
        GetOrCreateMaterial("Mat_Exposed_Brick", new Color(0.58f, 0.28f, 0.18f), 0.0f, 0.1f, urpLit);
        GetOrCreateMaterial("Mat_Plaster_White", new Color(0.86f, 0.84f, 0.80f), 0.0f, 0.2f, urpLit);
        GetOrCreateMaterial("Mat_Metal_Shutter", new Color(0.38f, 0.40f, 0.42f), 0.6f, 0.45f, urpLit);
        GetOrCreateMaterial("Mat_Caixa_D_Agua_Blue", new Color(0.02f, 0.38f, 0.72f), 0.05f, 0.65f, urpLit);

        // Toldos e Bandeirinhas
        GetOrCreateMaterial("Mat_Awning_Red", new Color(0.78f, 0.16f, 0.16f), 0.0f, 0.3f, urpLit);
        GetOrCreateMaterial("Mat_Awning_White", new Color(0.95f, 0.95f, 0.95f), 0.0f, 0.25f, urpLit);
        GetOrCreateMaterial("Mat_Awning_Yellow", new Color(0.96f, 0.76f, 0.14f), 0.0f, 0.3f, urpLit);
        GetOrCreateMaterial("Mat_Bunting_Green", new Color(0.18f, 0.62f, 0.28f), 0.0f, 0.3f, urpLit);
        GetOrCreateMaterial("Mat_Bunting_Blue", new Color(0.15f, 0.45f, 0.85f), 0.0f, 0.3f, urpLit);

        // Adereços urbanos / Bar
        GetOrCreateMaterial("Mat_Plastic_Chair_Yellow", new Color(0.98f, 0.76f, 0.08f), 0.0f, 0.55f, urpLit);
        GetOrCreateMaterial("Mat_Wood_Crate", new Color(0.65f, 0.50f, 0.32f), 0.0f, 0.15f, urpLit);
        GetOrCreateMaterial("Mat_Fruit_Orange", new Color(0.95f, 0.44f, 0.05f), 0.0f, 0.35f, urpLit);
        GetOrCreateMaterial("Mat_Fruit_Green", new Color(0.28f, 0.58f, 0.22f), 0.0f, 0.4f, urpLit);
        GetOrCreateMaterial("Mat_Trash_Bin_Green", new Color(0.20f, 0.48f, 0.25f), 0.1f, 0.4f, urpLit);

        // Veículo Batido
        GetOrCreateMaterial("Mat_Car_Body_Teal", new Color(0.16f, 0.46f, 0.44f), 0.55f, 0.7f, urpLit);
        GetOrCreateMaterial("Mat_Car_Tire_Rubber", new Color(0.11f, 0.11f, 0.11f), 0.0f, 0.12f, urpLit);
        GetOrCreateMaterial("Mat_Car_Wheel_Rim", new Color(0.72f, 0.72f, 0.72f), 0.85f, 0.75f, urpLit);
        GetOrCreateMaterial("Mat_Car_Glass", new Color(0.22f, 0.34f, 0.38f), 0.2f, 0.95f, urpLit);

        // Vegetação e Infraestrutura
        GetOrCreateMaterial("Mat_Palm_Trunk", new Color(0.42f, 0.31f, 0.21f), 0.0f, 0.15f, urpLit);
        GetOrCreateMaterial("Mat_Palm_Leaves", new Color(0.18f, 0.48f, 0.18f), 0.0f, 0.35f, urpLit);
        GetOrCreateMaterial("Mat_Concrete_Pole", new Color(0.48f, 0.49f, 0.50f), 0.0f, 0.2f, urpLit);
        GetOrCreateMaterial("Mat_Metal_Dark", new Color(0.14f, 0.14f, 0.15f), 0.7f, 0.45f, urpLit);

        // Silhuetas do Skyline distante
        GetOrCreateMaterial("Mat_Skyline_Close", new Color(0.34f, 0.42f, 0.51f), 0.0f, 0.1f, urpLit);
        GetOrCreateMaterial("Mat_Skyline_Far", new Color(0.55f, 0.64f, 0.74f), 0.0f, 0.08f, urpLit);
    }

    private static Material GetOrCreateMaterial(string name, Color color, float metallic, float smoothness, Shader shader)
    {
        string path = $"{MaterialsFolder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color", color);
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        MatCache[name] = mat;
        return mat;
    }

    private static Material GetMat(string name)
    {
        if (MatCache.TryGetValue(name, out Material mat) && mat != null) return mat;
        string path = $"{MaterialsFolder}/{name}.mat";
        mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) MatCache[name] = mat;
        return mat;
    }

    #endregion

    #region 2. Iluminação e Atmosfera

    private static void SetupLightingAndAtmosphere(Transform parent)
    {
        // Luz solar quente de tarde tropical brasileira
        GameObject sun = new GameObject("Directional Light - Sol Tropical");
        sun.transform.SetParent(parent);
        sun.transform.position = new Vector3(0f, 15f, 0f);
        sun.transform.rotation = Quaternion.Euler(34f, -40f, 0f);

        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1.0f, 0.92f, 0.78f); // Luz dourada
        light.intensity = 1.35f;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.85f;

        UniversalAdditionalLightData uLight = sun.AddComponent<UniversalAdditionalLightData>();

        // Luz de preenchimento suave vinda do céu
        GameObject fill = new GameObject("Fill Light - Ambient Sky");
        fill.transform.SetParent(parent);
        fill.transform.rotation = Quaternion.Euler(60f, 135f, 0f);
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color(0.70f, 0.82f, 0.95f);
        fillLight.intensity = 0.40f;
        fillLight.shadows = LightShadows.None;
        fill.AddComponent<UniversalAdditionalLightData>();

        // Volume pós-processamento URP
        GameObject volumeObj = new GameObject("Global Volume");
        volumeObj.transform.SetParent(parent);
        Volume volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(URPProfilePath);
        if (profile != null)
        {
            volume.profile = profile;
        }
    }

    #endregion

    #region 3. Chão de Luta (Gameplay Arena)

    private static void SetupGameplayArena(Transform parent)
    {
        // [ASSET SWAP POINT: Ground Collider & Floor]
        // O ArenaCollider precisa estar exatamente na Layer "Environment" (isTrigger = false)
        // e sua face superior em Y = 0 para bater com o CharacterController do FighterMovement.
        int envLayer = EnsureEnvironmentLayer();

        GameObject arenaColliderGo = new GameObject("ArenaCollider");
        arenaColliderGo.transform.SetParent(parent);
        arenaColliderGo.layer = envLayer;
        arenaColliderGo.transform.position = new Vector3(0f, -0.5f, 0f);

        BoxCollider arenaBox = arenaColliderGo.AddComponent<BoxCollider>();
        arenaBox.isTrigger = false;
        arenaBox.size = new Vector3(28f, 1.0f, 10.0f);

        // Paredes laterais invisíveis para prevenir quedas acidentais fora da arena (X = +/- 12)
        CreateWallCollider(parent, "Boundary_Wall_Left", new Vector3(-12.5f, 3f, 0f), new Vector3(1f, 8f, 10f), envLayer);
        CreateWallCollider(parent, "Boundary_Wall_Right", new Vector3(12.5f, 3f, 0f), new Vector3(1f, 8f, 10f), envLayer);

        // --- Visual: Asfalto da Rua ---
        // Pista da rua no centro (onde acontece a luta)
        CreateBox("Street_Asphalt", parent,
            new Vector3(0f, -0.015f, 0f),
            new Vector3(28f, 0.03f, 3.2f),
            GetMat("Mat_Asphalt"));

        // --- Visual: Faixa de Pedestres (Crosswalk) centralizada para perspectiva 3D ---
        GameObject crosswalkRoot = new GameObject("Crosswalk_Pedestrian");
        crosswalkRoot.transform.SetParent(parent);
        crosswalkRoot.transform.position = new Vector3(0f, 0.005f, 0f);

        Material crossMat = GetMat("Mat_Crosswalk_White");
        float[] stripePositions = { -3.0f, -1.8f, -0.6f, 0.6f, 1.8f, 3.0f };
        for (int i = 0; i < stripePositions.Length; i++)
        {
            CreateBox($"Stripe_{i + 1}", crosswalkRoot.transform,
                new Vector3(stripePositions[i], 0f, 0f),
                new Vector3(0.55f, 0.01f, 2.6f),
                crossMat);
        }

        // --- Visual: Guias da Calçada (Curbs) ---
        // Guia Norte (atrás dos lutadores, separando a rua da calçada)
        CreateBox("Curb_North", parent,
            new Vector3(0f, 0.05f, 1.65f),
            new Vector3(28f, 0.12f, 0.18f),
            GetMat("Mat_Curb"));

        // Pintura amarela de trânsito em trechos da guia
        CreateBox("Yellow_Curb_Stripe_Left", parent,
            new Vector3(-5.5f, 0.052f, 1.65f),
            new Vector3(5f, 0.125f, 0.19f),
            GetMat("Mat_Yellow_Curbs"));
        CreateBox("Yellow_Curb_Stripe_Right", parent,
            new Vector3(5.5f, 0.052f, 1.65f),
            new Vector3(5f, 0.125f, 0.19f),
            GetMat("Mat_Yellow_Curbs"));

        // --- Visual: Calçada Norte (Pedras portuguesas / cimento onde ficam as fachadas) ---
        CreateBox("Sidewalk_North", parent,
            new Vector3(0f, 0.045f, 3.4f),
            new Vector3(28f, 0.1f, 3.3f),
            GetMat("Mat_Cobblestone_Sidewalk"));

        // --- Visual: Calçada Sul (Primeiro plano, abaixo do campo da câmera) ---
        CreateBox("Sidewalk_South", parent,
            new Vector3(0f, -0.05f, -2.2f),
            new Vector3(28f, 0.1f, 1.2f),
            GetMat("Mat_Cobblestone_Sidewalk"));

        // Marcadores discretos de limite de arena (-9m e +9m)
        CreateBox("Drain_Grate_Left", parent,
            new Vector3(-9.0f, 0.005f, 0f),
            new Vector3(0.6f, 0.01f, 0.6f),
            GetMat("Mat_Metal_Dark"));
        CreateBox("Drain_Grate_Right", parent,
            new Vector3(9.0f, 0.005f, 0f),
            new Vector3(0.6f, 0.01f, 0.6f),
            GetMat("Mat_Metal_Dark"));
    }

    private static void CreateWallCollider(Transform parent, string name, Vector3 pos, Vector3 size, int layer)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.layer = layer;
        BoxCollider box = wall.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.size = size;
    }

    #endregion

    #region 4. Fachadas do Plano Intermediário (Midground)

    private static void SetupMidgroundFacades(Transform parent)
    {
        // [ASSET SWAP POINT: Facades and Buildings]
        // As fachadas criam o corredor urbano emoldurando os lados da arena.
        // O CENTRO (X = -1.8 a +1.8) fica ABERTO como uma rua transversal / viela,
        // permitindo que o céu e os arranha-céus do background sejam vistos nitidamente!

        Transform facadesRoot = new GameObject("Facades_Street_Corridor").transform;
        facadesRoot.SetParent(parent);

        // 1. O Clássico Boteco Brasileiro (Amarelo & Azulejos Azuis) [X: -5.0 a -1.8, Z: 5.0]
        CreateBotecoBuilding(facadesRoot, new Vector3(-3.5f, 0f, 5.0f));

        // 2. Sobrado Terracota Tradicional de 2 Andares [X: -9.5 a -5.5, Z: 5.5f]
        CreateTerracottaHouse(facadesRoot, new Vector3(-7.5f, 0f, 5.5f));

        // 3. Barbearia Tradicional / Loja Turquesa [X: +1.8 a +5.0, Z: 5.0f]
        CreateBarberShopBuilding(facadesRoot, new Vector3(3.5f, 0f, 5.0f));

        // 4. Casa de Alvenaria com Laje e Caixa D'Água Azul [X: +5.5 a +9.5, Z: 5.5f]
        CreateLajeHouseWithWaterTank(facadesRoot, new Vector3(7.5f, 0f, 5.5f));

        // 5. Paredes de Fechamento Lateral (Wings) nos cantos distantes
        CreateSideFramingBuilding(facadesRoot, "Building_Wing_West", new Vector3(-13.5f, 0f, 4.2f), new Vector3(5.5f, 7.5f, 6.0f), GetMat("Mat_Casa_Mint"));
        CreateSideFramingBuilding(facadesRoot, "Building_Wing_East", new Vector3(13.5f, 0f, 4.2f), new Vector3(5.5f, 7.5f, 6.0f), GetMat("Mat_Plaster_White"));

        // 6. Rua Transversal / Beco ao Centro (Z = 10m a 16m) dando profundidade urbana
        CreateCrossStreetVista(facadesRoot);
    }

    private static void CreateBotecoBuilding(Transform parent, Vector3 rootPos)
    {
        GameObject boteco = new GameObject("Building_Boteco_Do_Ze");
        boteco.transform.SetParent(parent);
        boteco.transform.position = rootPos;

        // Parede principal de 1 andar (amarela)
        CreateBox("Wall_Main", boteco.transform, new Vector3(0f, 2.0f, 0f), new Vector3(3.4f, 4.0f, 1.6f), GetMat("Mat_Boteco_Yellow"));

        // Azulejos azuis na metade inferior (típico boteco brasileiro)
        CreateBox("Tiles_Lower", boteco.transform, new Vector3(0f, 0.65f, -0.81f), new Vector3(3.42f, 1.3f, 0.05f), GetMat("Mat_Boteco_Blue_Tile"));

        // Porta comercial de aço de enrolar
        CreateBox("Steel_Shutter_Door", boteco.transform, new Vector3(0f, 1.15f, -0.82f), new Vector3(2.3f, 2.2f, 0.05f), GetMat("Mat_Metal_Shutter"));

        // Letreiro do Boteco
        CreateBox("Signboard_Boteco", boteco.transform, new Vector3(0f, 2.65f, -0.84f), new Vector3(2.5f, 0.45f, 0.08f), GetMat("Mat_Boteco_Blue_Tile"));

        // Toldo listrado vermelho e amarelo projetado sobre a calçada
        CreateAwning(boteco.transform, new Vector3(0f, 2.35f, -1.25f), 3.0f, 0.85f, GetMat("Mat_Awning_Red"), GetMat("Mat_Awning_Yellow"));
    }

    private static void CreateTerracottaHouse(Transform parent, Vector3 rootPos)
    {
        GameObject house = new GameObject("Building_Sobrado_Terracota");
        house.transform.SetParent(parent);
        house.transform.position = rootPos;

        // Bloco principal de 2 andares
        CreateBox("Main_Body", house.transform, new Vector3(0f, 3.2f, 0f), new Vector3(3.8f, 6.4f, 2.0f), GetMat("Mat_Terracotta"));

        // Platibanda / moldura no topo
        CreateBox("Roof_Trim", house.transform, new Vector3(0f, 6.45f, -0.05f), new Vector3(4.0f, 0.3f, 2.1f), GetMat("Mat_Plaster_White"));

        // Porta de entrada
        CreateBox("Front_Door", house.transform, new Vector3(-0.85f, 1.05f, -1.01f), new Vector3(0.9f, 2.1f, 0.05f), GetMat("Mat_Metal_Dark"));

        // Janela do 1º andar
        CreateBox("Window_Ground", house.transform, new Vector3(0.85f, 1.25f, -1.01f), new Vector3(1.0f, 1.1f, 0.05f), GetMat("Mat_Plaster_White"));

        // Sacada / Balcão no 2º andar
        CreateBox("Balcony_Slab", house.transform, new Vector3(0f, 3.3f, -1.35f), new Vector3(2.6f, 0.15f, 0.7f), GetMat("Mat_Plaster_White"));
        CreateBox("Balcony_Railing", house.transform, new Vector3(0f, 3.8f, -1.68f), new Vector3(2.6f, 0.85f, 0.04f), GetMat("Mat_Metal_Dark"));

        // Janelas duplas do 2º andar
        CreateBox("Balcony_Door_1", house.transform, new Vector3(-0.6f, 4.35f, -1.01f), new Vector3(0.8f, 1.7f, 0.05f), GetMat("Mat_Plaster_White"));
        CreateBox("Balcony_Door_2", house.transform, new Vector3(0.6f, 4.35f, -1.01f), new Vector3(0.8f, 1.7f, 0.05f), GetMat("Mat_Plaster_White"));
    }

    private static void CreateBarberShopBuilding(Transform parent, Vector3 rootPos)
    {
        GameObject barber = new GameObject("Building_Barbearia_Turquesa");
        barber.transform.SetParent(parent);
        barber.transform.position = rootPos;

        // Bloco principal turquesa de 1 andar alto
        CreateBox("Main_Body", barber.transform, new Vector3(0f, 2.0f, 0f), new Vector3(3.4f, 4.0f, 1.6f), GetMat("Mat_Barbearia_Turquoise"));

        // Vitrine de vidro
        CreateBox("Storefront_Glass", barber.transform, new Vector3(-0.55f, 1.2f, -0.81f), new Vector3(1.7f, 1.5f, 0.05f), GetMat("Mat_Car_Glass"));

        // Porta
        CreateBox("Shop_Door", barber.transform, new Vector3(0.95f, 1.05f, -0.81f), new Vector3(0.85f, 2.0f, 0.05f), GetMat("Mat_Plaster_White"));

        // Toldo listrado azul e branco
        CreateAwning(barber.transform, new Vector3(0f, 2.35f, -1.25f), 3.0f, 0.85f, GetMat("Mat_Bunting_Blue"), GetMat("Mat_Awning_White"));

        // Poste de barbeiro clássico
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Barber_Pole";
        pole.transform.SetParent(barber.transform);
        pole.transform.localPosition = new Vector3(1.5f, 1.8f, -0.9f);
        pole.transform.localScale = new Vector3(0.16f, 0.4f, 0.16f);
        SetRendererMaterial(pole, GetMat("Mat_Awning_Red"));
        RemoveCollider(pole);
    }

    private static void CreateLajeHouseWithWaterTank(Transform parent, Vector3 rootPos)
    {
        GameObject house = new GameObject("Building_Casa_Com_Caixa_D_Agua");
        house.transform.SetParent(parent);
        house.transform.position = rootPos;

        // Casa de alvenaria / tijolo aparente
        CreateBox("Main_Body", house.transform, new Vector3(0f, 2.7f, 0f), new Vector3(3.8f, 5.4f, 2.2f), GetMat("Mat_Exposed_Brick"));

        // Pilares e vigas de concreto
        CreateBox("Pillar_Left", house.transform, new Vector3(-1.8f, 2.7f, -1.11f), new Vector3(0.25f, 5.4f, 0.08f), GetMat("Mat_Curb"));
        CreateBox("Pillar_Right", house.transform, new Vector3(1.8f, 2.7f, -1.11f), new Vector3(0.25f, 5.4f, 0.08f), GetMat("Mat_Curb"));
        CreateBox("Beam_Middle", house.transform, new Vector3(0f, 2.65f, -1.11f), new Vector3(3.8f, 0.22f, 0.08f), GetMat("Mat_Curb"));

        // Laje superior de concreto
        CreateBox("Roof_Slab", house.transform, new Vector3(0f, 5.45f, 0f), new Vector3(4.0f, 0.18f, 2.4f), GetMat("Mat_Curb"));

        // Mureta da laje
        CreateBox("Roof_Wall_Front", house.transform, new Vector3(0f, 5.95f, -1.1f), new Vector3(4.0f, 0.8f, 0.12f), GetMat("Mat_Exposed_Brick"));

        // Caixa d'Água Azul Cilíndrica (ícone clássico da arquitetura urbana brasileira)
        GameObject waterTank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        waterTank.name = "Caixa_D_Agua_Azul";
        waterTank.transform.SetParent(house.transform);
        waterTank.transform.localPosition = new Vector3(-0.85f, 6.25f, -0.2f);
        waterTank.transform.localScale = new Vector3(1.3f, 0.65f, 1.3f);
        SetRendererMaterial(waterTank, GetMat("Mat_Caixa_D_Agua_Blue"));
        RemoveCollider(waterTank);

        // Tampa da caixa d'água
        GameObject tankLid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tankLid.name = "Tampa_Caixa";
        tankLid.transform.SetParent(house.transform);
        tankLid.transform.localPosition = new Vector3(-0.85f, 6.92f, -0.2f);
        tankLid.transform.localScale = new Vector3(1.36f, 0.05f, 1.36f);
        SetRendererMaterial(tankLid, GetMat("Mat_Caixa_D_Agua_Blue"));
        RemoveCollider(tankLid);
    }

    private static void CreateSideFramingBuilding(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject wing = new GameObject(name);
        wing.transform.SetParent(parent);
        wing.transform.position = pos;

        CreateBox("Wall_Mass", wing.transform, new Vector3(0f, size.y * 0.5f, 0f), size, mat);
        CreateBox("Wall_Stripe", wing.transform, new Vector3(0f, size.y * 0.45f, -size.z * 0.51f), new Vector3(size.x, 0.35f, 0.04f), GetMat("Mat_Plaster_White"));
    }

    private static void CreateCrossStreetVista(Transform parent)
    {
        // Rua transversal ao centro que cria profundidade visual em direção ao horizonte
        GameObject vista = new GameObject("Central_Street_Vista");
        vista.transform.SetParent(parent);

        // Pista da rua transversal estendendo-se no eixo Z
        CreateBox("Alley_Asphalt", vista.transform, new Vector3(0f, 0.01f, 10.5f), new Vector3(3.4f, 0.02f, 11f), GetMat("Mat_Asphalt"));

        // Muros laterais do beco/travessa
        CreateBox("Alley_Wall_Left", vista.transform, new Vector3(-1.85f, 1.5f, 9.5f), new Vector3(0.3f, 3.0f, 9.0f), GetMat("Mat_Exposed_Brick"));
        CreateBox("Alley_Wall_Right", vista.transform, new Vector3(1.85f, 1.5f, 9.5f), new Vector3(0.3f, 3.0f, 9.0f), GetMat("Mat_Plaster_White"));

        // Mureta baixa ao fundo da travessa
        CreateBox("Alley_Back_Wall", vista.transform, new Vector3(0f, 0.5f, 15.0f), new Vector3(3.6f, 1.0f, 0.3f), GetMat("Mat_Curb"));
    }

    private static void CreateAwning(Transform parent, Vector3 localPos, float width, float depth, Material mat1, Material mat2)
    {
        GameObject awning = new GameObject("Awning_Striped");
        awning.transform.SetParent(parent, false);
        awning.transform.localPosition = localPos;
        awning.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

        int stripes = 6;
        float stripeWidth = width / stripes;
        float startX = -width * 0.5f + stripeWidth * 0.5f;

        for (int i = 0; i < stripes; i++)
        {
            Material stripeMat = (i % 2 == 0) ? mat1 : mat2;
            CreateBox($"Stripe_{i + 1}", awning.transform,
                new Vector3(startX + i * stripeWidth, 0f, depth * 0.5f),
                new Vector3(stripeWidth * 0.98f, 0.04f, depth),
                stripeMat);
        }
    }

    #endregion

    #region 5. Infraestrutura e Postes de Luz

    private static void SetupInfrastructure(Transform parent)
    {
        // [ASSET SWAP POINT: Utility Poles & Overhead Wires]
        // Postes de concreto brasileiros com cruzetas, transformador e fios cruzando a rua.

        Transform infraRoot = new GameObject("Infrastructure_Utility_Poles").transform;
        infraRoot.SetParent(parent);

        CreateBrazilianUtilityPole(infraRoot, "Concrete_Pole_West", new Vector3(-7.5f, 0f, 2.0f));
        CreateBrazilianUtilityPole(infraRoot, "Concrete_Pole_East", new Vector3(7.5f, 0f, 2.0f));

        // Cabos elétricos aéreos cruzando de um poste ao outro (Y ~ 4.4f a 4.9f)
        CreatePowerCables(infraRoot, new Vector3(-7.5f, 4.6f, 2.0f), new Vector3(7.5f, 4.6f, 2.0f));

        // Bandeirinhas coloridas penduradas atravessando o cenário
        CreateBuntingString(infraRoot, new Vector3(-7.2f, 4.1f, 1.8f), new Vector3(7.2f, 4.1f, 1.8f));
    }

    private static void CreateBrazilianUtilityPole(Transform parent, string name, Vector3 pos)
    {
        GameObject poleGo = new GameObject(name);
        poleGo.transform.SetParent(parent);
        poleGo.transform.position = pos;

        // Tronco de concreto armado retangular cônico / clássico
        CreateBox("Pole_Shaft", poleGo.transform, new Vector3(0f, 2.9f, 0f), new Vector3(0.32f, 5.8f, 0.32f), GetMat("Mat_Concrete_Pole"));

        // Cruzeta horizontal no topo
        CreateBox("Crossarm_Top", poleGo.transform, new Vector3(0f, 5.2f, 0f), new Vector3(1.7f, 0.14f, 0.14f), GetMat("Mat_Concrete_Pole"));
        CreateBox("Crossarm_Mid", poleGo.transform, new Vector3(0f, 4.7f, 0f), new Vector3(1.3f, 0.12f, 0.12f), GetMat("Mat_Concrete_Pole"));

        // Isoladores de porcelana
        Material darkMetal = GetMat("Mat_Metal_Dark");
        float[] insOffsets = { -0.65f, -0.25f, 0.25f, 0.65f };
        for (int i = 0; i < insOffsets.Length; i++)
        {
            GameObject ins = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ins.name = $"Insulator_{i + 1}";
            ins.transform.SetParent(poleGo.transform, false);
            ins.transform.localPosition = new Vector3(insOffsets[i], 5.34f, 0f);
            ins.transform.localScale = new Vector3(0.08f, 0.08f, 0.08f);
            SetRendererMaterial(ins, darkMetal);
            RemoveCollider(ins);
        }

        // Braço com luminária de iluminação pública estendendo-se sobre a rua
        CreateBox("Lamp_Arm", poleGo.transform, new Vector3(0f, 4.3f, -0.55f), new Vector3(0.07f, 0.07f, 1.1f), darkMetal);
        CreateBox("Street_Lamp_Head", poleGo.transform, new Vector3(0f, 4.22f, -1.15f), new Vector3(0.24f, 0.14f, 0.42f), GetMat("Mat_Plaster_White"));

        // Transformador cilíndrico no poste leste
        if (pos.x > 0)
        {
            GameObject transf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            transf.name = "Transformer_Canister";
            transf.transform.SetParent(poleGo.transform, false);
            transf.transform.localPosition = new Vector3(0.32f, 4.0f, 0f);
            transf.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            SetRendererMaterial(transf, GetMat("Mat_Curb"));
            RemoveCollider(transf);
        }
    }

    private static void CreatePowerCables(Transform parent, Vector3 start, Vector3 end)
    {
        GameObject cables = new GameObject("Overhead_Power_Cables");
        cables.transform.SetParent(parent);

        Material wireMat = GetMat("Mat_Metal_Dark");
        float[] yOffsets = { 0f, 0.3f, 0.6f };
        float length = Vector3.Distance(start, end);
        Vector3 mid = (start + end) * 0.5f;

        for (int i = 0; i < yOffsets.Length; i++)
        {
            GameObject wire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wire.name = $"Wire_{i + 1}";
            wire.transform.SetParent(cables.transform);
            wire.transform.position = mid + new Vector3(0f, yOffsets[i] - 0.1f, (i - 1) * 0.1f);
            wire.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            wire.transform.localScale = new Vector3(0.02f, length * 0.5f, 0.02f);
            SetRendererMaterial(wire, wireMat);
            RemoveCollider(wire);
        }
    }

    private static void CreateBuntingString(Transform parent, Vector3 start, Vector3 end)
    {
        GameObject buntingRoot = new GameObject("Bunting_Bandeirinhas");
        buntingRoot.transform.SetParent(parent);

        Material[] colors = {
            GetMat("Mat_Awning_Red"),
            GetMat("Mat_Awning_Yellow"),
            GetMat("Mat_Bunting_Green"),
            GetMat("Mat_Bunting_Blue")
        };

        int count = 22;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);
            // Sag parabólico natural do cordão
            float sag = Mathf.Sin(t * Mathf.PI) * 0.45f;
            pos.y -= sag;

            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = $"Flag_{i + 1}";
            flag.transform.SetParent(buntingRoot.transform);
            flag.transform.position = pos + new Vector3(0f, -0.12f, 0f);
            flag.transform.rotation = Quaternion.Euler(14f, UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(-8f, 8f));
            flag.transform.localScale = new Vector3(0.20f, 0.28f, 0.02f);
            SetRendererMaterial(flag, colors[i % colors.Length]);
            RemoveCollider(flag);
        }
    }

    #endregion

    #region 6. Vegetação Tropical

    private static void SetupTropicalVegetation(Transform parent)
    {
        // [ASSET SWAP POINT: Tropical Palm Trees]
        // Palmeiras tropicais surgindo por trás dos muros e calçadas.

        Transform vegRoot = new GameObject("Tropical_Vegetation").transform;
        vegRoot.SetParent(parent);

        CreateStylizedPalmTree(vegRoot, "Palm_Tree_West", new Vector3(-5.8f, 0f, 6.8f), 6.5f);
        CreateStylizedPalmTree(vegRoot, "Palm_Tree_East", new Vector3(5.8f, 0f, 7.0f), 7.0f);
    }

    private static void CreateStylizedPalmTree(Transform parent, string name, Vector3 basePos, float height)
    {
        GameObject palm = new GameObject(name);
        palm.transform.SetParent(parent);
        palm.transform.position = basePos;

        Material trunkMat = GetMat("Mat_Palm_Trunk");
        Material leavesMat = GetMat("Mat_Palm_Leaves");

        int segments = 6;
        float segHeight = height / segments;
        Vector3 currPos = Vector3.zero;

        for (int i = 0; i < segments; i++)
        {
            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            seg.name = $"Trunk_Seg_{i + 1}";
            seg.transform.SetParent(palm.transform, false);
            float radius = Mathf.Lerp(0.32f, 0.20f, (float)i / segments);
            currPos += new Vector3(0f, segHeight * 0.5f, 0f);
            seg.transform.localPosition = currPos;
            seg.transform.localScale = new Vector3(radius, segHeight * 0.5f, radius);
            currPos += new Vector3(0f, segHeight * 0.5f, 0f);
            SetRendererMaterial(seg, trunkMat);
            RemoveCollider(seg);
        }

        // Copa de palmas no topo
        int frondCount = 8;
        for (int i = 0; i < frondCount; i++)
        {
            float angle = (360f / frondCount) * i;
            GameObject frond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frond.name = $"Frond_{i + 1}";
            frond.transform.SetParent(palm.transform, false);
            frond.transform.localPosition = currPos;
            frond.transform.localRotation = Quaternion.Euler(26f, angle, 0f);
            frond.transform.localPosition += frond.transform.forward * 1.3f - Vector3.up * 0.2f;
            frond.transform.localScale = new Vector3(0.40f, 0.04f, 2.3f);
            SetRendererMaterial(frond, leavesMat);
            RemoveCollider(frond);
        }
    }

    #endregion

    #region 7. Elementos Decorativos Fora da Arena (Scene Props)

    private static void SetupSceneProps(Transform parent)
    {
        // [ASSET SWAP POINT: Vehicle, Market Crates and Boteco Props]
        // Todos os props ficam na calçada norte ou nas laterais fora do corredor central da luta (Z = 0)
        // para dar sensação de rua viva sem interferir na movimentação.

        // 1. Veículo Batido na lateral direita (X = 8.8f, Z = 1.6f) - meio na calçada, meio na rua
        CreateCrashedCar(parent, new Vector3(8.8f, 0f, 1.55f));

        // 2. Barraca de Feira Livre com Caixotes de Frutas na calçada esquerda (X = -7.8f, Z = 2.5f)
        CreateFeiraMarketStall(parent, new Vector3(-7.8f, 0.05f, 2.5f));

        // 3. Jogo de Mesa e Cadeiras Amarelas de Boteco na calçada em frente ao boteco (X = -5.6f, Z = 2.6f)
        CreateBotecoTableSet(parent, new Vector3(-5.6f, 0.05f, 2.6f));

        // 4. Lixeira Urbana de Calçada e Hidrante
        CreateStreetTrashBin(parent, new Vector3(6.6f, 0.05f, 2.1f));
        CreateFireHydrant(parent, new Vector3(-2.2f, 0.05f, 1.85f));
    }

    private static void CreateCrashedCar(Transform parent, Vector3 rootPos)
    {
        GameObject car = new GameObject("Prop_Crashed_Car");
        car.transform.SetParent(parent);
        car.transform.position = rootPos;
        // Inclinado contra o meio-fio
        car.transform.rotation = Quaternion.Euler(3f, -25f, 8f);

        Material bodyMat = GetMat("Mat_Car_Body_Teal");
        Material tireMat = GetMat("Mat_Car_Tire_Rubber");
        Material rimMat = GetMat("Mat_Car_Wheel_Rim");
        Material glassMat = GetMat("Mat_Car_Glass");

        // Chassi inferior
        CreateBox("Car_Chassis", car.transform, new Vector3(0f, 0.42f, 0f), new Vector3(1.8f, 0.42f, 3.6f), bodyMat);

        // Cabine com vidros
        CreateBox("Car_Cabin", car.transform, new Vector3(0f, 0.98f, -0.2f), new Vector3(1.55f, 0.7f, 2.0f), glassMat);

        // Teto da cabine
        CreateBox("Car_Roof", car.transform, new Vector3(0f, 1.35f, -0.2f), new Vector3(1.52f, 0.08f, 1.95f), bodyMat);

        // Capô amassado
        GameObject hood = CreateBox("Car_Hood_Crumpled", car.transform, new Vector3(0f, 0.70f, 1.25f), new Vector3(1.65f, 0.22f, 1.0f), bodyMat);
        hood.transform.localRotation = Quaternion.Euler(-16f, 0f, 0f);

        // 4 Rodas
        Vector3[] wheelOffsets = {
            new Vector3(-0.92f, 0.32f, 1.1f),
            new Vector3(0.92f, 0.32f, 1.1f),
            new Vector3(-0.92f, 0.32f, -1.1f),
            new Vector3(0.92f, 0.32f, -1.1f)
        };

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = $"Wheel_{i + 1}";
            wheel.transform.SetParent(car.transform, false);
            wheel.transform.localPosition = wheelOffsets[i];
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localScale = new Vector3(0.64f, 0.20f, 0.64f);
            SetRendererMaterial(wheel, tireMat);
            RemoveCollider(wheel);

            // Calota / aro
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(wheel.transform, false);
            rim.transform.localPosition = new Vector3(0f, (wheelOffsets[i].x > 0 ? 0.55f : -0.55f), 0f);
            rim.transform.localScale = new Vector3(0.55f, 0.08f, 0.55f);
            SetRendererMaterial(rim, rimMat);
            RemoveCollider(rim);
        }

        // Pára-choque solto no chão
        GameObject bumper = CreateBox("Loose_Bumper", car.transform, new Vector3(0.15f, 0.1f, 2.05f), new Vector3(1.7f, 0.18f, 0.18f), GetMat("Mat_Curb"));
        bumper.transform.localRotation = Quaternion.Euler(0f, 16f, -6f);
    }

    private static void CreateFeiraMarketStall(Transform parent, Vector3 rootPos)
    {
        GameObject stall = new GameObject("Prop_Feira_Livre_Stall");
        stall.transform.SetParent(parent);
        stall.transform.position = rootPos;
        stall.transform.rotation = Quaternion.Euler(0f, 18f, 0f);

        Material woodMat = GetMat("Mat_Wood_Crate");

        // Bancada de madeira da feira
        CreateBox("Stall_Table", stall.transform, new Vector3(0f, 0.70f, 0f), new Vector3(2.2f, 0.08f, 1.1f), woodMat);
        CreateBox("Table_Leg_1", stall.transform, new Vector3(-1.0f, 0.35f, -0.45f), new Vector3(0.08f, 0.7f, 0.08f), woodMat);
        CreateBox("Table_Leg_2", stall.transform, new Vector3(1.0f, 0.35f, -0.45f), new Vector3(0.08f, 0.7f, 0.08f), woodMat);
        CreateBox("Table_Leg_3", stall.transform, new Vector3(-1.0f, 0.35f, 0.45f), new Vector3(0.08f, 0.7f, 0.08f), woodMat);
        CreateBox("Table_Leg_4", stall.transform, new Vector3(1.0f, 0.35f, 0.45f), new Vector3(0.08f, 0.7f, 0.08f), woodMat);

        // Cobertura de lona da feira (verde e branca)
        CreateAwning(stall.transform, new Vector3(0f, 1.95f, -0.6f), 2.4f, 1.2f, GetMat("Mat_Bunting_Green"), GetMat("Mat_Awning_White"));

        // Caixotes de feira de madeira empilhados
        CreateCrateWithFruit(stall.transform, new Vector3(-0.6f, 0.88f, 0f), "Orange", GetMat("Mat_Fruit_Orange"));
        CreateCrateWithFruit(stall.transform, new Vector3(0.6f, 0.88f, 0f), "Green", GetMat("Mat_Fruit_Green"));
        CreateCrateWithFruit(stall.transform, new Vector3(1.3f, 0.18f, -0.15f), "Floor_Crate", GetMat("Mat_Fruit_Orange"));
    }

    private static void CreateCrateWithFruit(Transform parent, Vector3 localPos, string fruitType, Material fruitMat)
    {
        GameObject crateGo = new GameObject($"Crate_{fruitType}");
        crateGo.transform.SetParent(parent, false);
        crateGo.transform.localPosition = localPos;

        Material woodMat = GetMat("Mat_Wood_Crate");

        // Fundo do caixote
        CreateBox("Crate_Bottom", crateGo.transform, Vector3.zero, new Vector3(0.55f, 0.04f, 0.40f), woodMat);
        // Laterais
        CreateBox("Side_F", crateGo.transform, new Vector3(0f, 0.12f, 0.19f), new Vector3(0.55f, 0.22f, 0.03f), woodMat);
        CreateBox("Side_B", crateGo.transform, new Vector3(0f, 0.12f, -0.19f), new Vector3(0.55f, 0.22f, 0.03f), woodMat);
        CreateBox("Side_L", crateGo.transform, new Vector3(-0.26f, 0.12f, 0f), new Vector3(0.03f, 0.22f, 0.40f), woodMat);
        CreateBox("Side_R", crateGo.transform, new Vector3(0.26f, 0.12f, 0f), new Vector3(0.03f, 0.22f, 0.40f), woodMat);

        // Frutas dentro do caixote
        int cols = 3;
        int rows = 2;
        for (int x = 0; x < cols; x++)
        {
            for (int z = 0; z < rows; z++)
            {
                GameObject fruit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                fruit.name = $"Fruit_{x}_{z}";
                fruit.transform.SetParent(crateGo.transform, false);
                fruit.transform.localPosition = new Vector3(-0.15f + x * 0.15f, 0.15f, -0.08f + z * 0.16f);
                fruit.transform.localScale = Vector3.one * 0.12f;
                SetRendererMaterial(fruit, fruitMat);
                RemoveCollider(fruit);
            }
        }
    }

    private static void CreateBotecoTableSet(Transform parent, Vector3 pos)
    {
        GameObject tableSet = new GameObject("Prop_Boteco_Table_Set");
        tableSet.transform.SetParent(parent);
        tableSet.transform.position = pos;

        Material chairMat = GetMat("Mat_Plastic_Chair_Yellow");

        // Mesa plástica amarela
        CreateBox("Table_Top", tableSet.transform, new Vector3(0f, 0.70f, 0f), new Vector3(0.80f, 0.04f, 0.80f), chairMat);
        CreateBox("Table_Central_Leg", tableSet.transform, new Vector3(0f, 0.35f, 0f), new Vector3(0.10f, 0.70f, 0.10f), chairMat);
        CreateBox("Table_Base", tableSet.transform, new Vector3(0f, 0.03f, 0f), new Vector3(0.60f, 0.05f, 0.60f), chairMat);

        // Duas cadeiras plásticas amarelas clássicas
        CreatePlasticChair(tableSet.transform, new Vector3(-0.60f, 0f, 0f), 90f, chairMat);
        CreatePlasticChair(tableSet.transform, new Vector3(0.60f, 0f, 0f), -90f, chairMat);
    }

    private static void CreatePlasticChair(Transform parent, Vector3 localPos, float rotY, Material mat)
    {
        GameObject chair = new GameObject("Plastic_Chair");
        chair.transform.SetParent(parent, false);
        chair.transform.localPosition = localPos;
        chair.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);

        // Assento
        CreateBox("Seat", chair.transform, new Vector3(0f, 0.40f, 0f), new Vector3(0.42f, 0.04f, 0.42f), mat);
        // Encosto
        CreateBox("Backrest", chair.transform, new Vector3(0f, 0.68f, -0.19f), new Vector3(0.42f, 0.52f, 0.04f), mat);
        // Pernas
        CreateBox("Leg_FL", chair.transform, new Vector3(-0.18f, 0.20f, 0.18f), new Vector3(0.04f, 0.40f, 0.04f), mat);
        CreateBox("Leg_FR", chair.transform, new Vector3(0.18f, 0.20f, 0.18f), new Vector3(0.04f, 0.40f, 0.04f), mat);
        CreateBox("Leg_BL", chair.transform, new Vector3(-0.18f, 0.20f, -0.18f), new Vector3(0.04f, 0.40f, 0.04f), mat);
        CreateBox("Leg_BR", chair.transform, new Vector3(0.18f, 0.20f, -0.18f), new Vector3(0.04f, 0.40f, 0.04f), mat);
    }

    private static void CreateStreetTrashBin(Transform parent, Vector3 pos)
    {
        GameObject bin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bin.name = "Prop_Street_Trash_Bin";
        bin.transform.SetParent(parent);
        bin.transform.position = pos + new Vector3(0f, 0.45f, 0f);
        bin.transform.localScale = new Vector3(0.40f, 0.45f, 0.40f);
        SetRendererMaterial(bin, GetMat("Mat_Trash_Bin_Green"));
        RemoveCollider(bin);
    }

    private static void CreateFireHydrant(Transform parent, Vector3 pos)
    {
        GameObject hydrant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hydrant.name = "Prop_Fire_Hydrant";
        hydrant.transform.SetParent(parent);
        hydrant.transform.position = pos + new Vector3(0f, 0.35f, 0f);
        hydrant.transform.localScale = new Vector3(0.24f, 0.35f, 0.24f);
        SetRendererMaterial(hydrant, GetMat("Mat_Yellow_Curbs"));
        RemoveCollider(hydrant);
    }

    #endregion

    #region 8. Silhuetas do Plano de Fundo Distante (Background)

    private static void SetupBackgroundSkyline(Transform parent)
    {
        // [ASSET SWAP POINT: Skyline & Distant City Backdrop]
        // Silhuetas de arranha-céus distribuídas em duas camadas de profundidade
        // (Z = 22m a 42m) com tons atmosféricos atenuados para dar escala de metrópole.

        Transform bgRoot = new GameObject("Skyline_Skyscrapers").transform;
        bgRoot.SetParent(parent);

        Material matClose = GetMat("Mat_Skyline_Close");
        Material matFar = GetMat("Mat_Skyline_Far");

        // Camada 1: Prédios Intermediários (Z = 22f a 28f)
        float[] xPositionsClose = { -18f, -13f, -8.5f, -4.8f, 4.8f, 8.5f, 13f, 18f };
        float[] heightsClose = { 16f, 22f, 15f, 18f, 18f, 22f, 15f, 19f };
        float[] widthsClose = { 4.5f, 5.2f, 4.0f, 4.0f, 4.0f, 4.0f, 4.5f, 4.8f };

        for (int i = 0; i < xPositionsClose.Length; i++)
        {
            float h = heightsClose[i];
            float w = widthsClose[i];
            float z = 23f + (i % 3) * 1.5f;

            CreateBox($"Tower_Mid_{i + 1}", bgRoot,
                new Vector3(xPositionsClose[i], h * 0.5f, z),
                new Vector3(w, h, 5.0f),
                matClose);

            // Antenas / caixas de máquinas
            if (i == 1 || i == 5)
            {
                CreateBox($"Elevator_Shaft_{i + 1}", bgRoot,
                    new Vector3(xPositionsClose[i], h + 1.0f, z),
                    new Vector3(w * 0.35f, 2.0f, 2.0f),
                    matClose);
            }
            else if (i == 3)
            {
                // Spire de transmissão
                GameObject antenna = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                antenna.name = "Transmission_Spire";
                antenna.transform.SetParent(bgRoot);
                antenna.transform.position = new Vector3(xPositionsClose[i], h + 2.5f, z);
                antenna.transform.localScale = new Vector3(0.12f, 2.5f, 0.12f);
                SetRendererMaterial(antenna, GetMat("Mat_Metal_Dark"));
                RemoveCollider(antenna);
            }
        }

        // Camada 2: Prédios Distantes no Horizonte (Z = 34f a 44f)
        float[] xPositionsFar = { -22f, -16f, -11f, -5.5f, 5.5f, 11f, 16f, 22f };
        float[] heightsFar = { 24f, 30f, 26f, 30f, 30f, 28f, 25f, 27f };
        float[] widthsFar = { 5.5f, 6.5f, 5.0f, 5.5f, 5.5f, 5.8f, 5.5f, 6.0f };

        for (int i = 0; i < xPositionsFar.Length; i++)
        {
            float h = heightsFar[i];
            float w = widthsFar[i];
            float z = 36f + (i % 2) * 3f;

            CreateBox($"Tower_Far_{i + 1}", bgRoot,
                new Vector3(xPositionsFar[i], h * 0.5f, z),
                new Vector3(w, h, 7.0f),
                matFar);
        }

        // Morro / Silhueta de colina ao fundo (paisagem típica brasileira)
        GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hill.name = "Distant_Hill_Silhouette";
        hill.transform.SetParent(bgRoot);
        hill.transform.position = new Vector3(4f, -1f, 46f);
        hill.transform.localScale = new Vector3(48f, 18f, 12f);
        SetRendererMaterial(hill, matFar);
        RemoveCollider(hill);
    }

    #endregion

    #region 9. Câmera, Spawns e Game Flow

    private static void SetupGameplayEntities(Transform parent)
    {
        // Spawns dos lutadores
        GameObject spawnP1 = new GameObject("Spawn_P1");
        spawnP1.transform.SetParent(parent);
        spawnP1.transform.position = new Vector3(-2.2f, 0f, 0f);

        GameObject spawnP2 = new GameObject("Spawn_P2");
        spawnP2.transform.SetParent(parent);
        spawnP2.transform.position = new Vector3(2.2f, 0f, 0f);

        // Main Camera com TekkenCamera perfeitamente enquadrada
        GameObject camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(parent);
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 1.4f, -5.3f);
        camGo.transform.rotation = Quaternion.Euler(3.5f, 0f, 0f);

        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 120f;
        cam.orthographic = true;
        cam.orthographicSize = 3.65f;

        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<UniversalAdditionalCameraData>();

        TekkenCamera tekkenCam = camGo.AddComponent<TekkenCamera>();
        SerializedObject camSo = new SerializedObject(tekkenCam);
        camSo.FindProperty("baseDistance").floatValue = 5.2f;
        camSo.FindProperty("height").floatValue = 1.4f;
        camSo.FindProperty("lookAtHeight").floatValue = 1.05f;
        camSo.FindProperty("zoomFactor").floatValue = 0.35f;
        camSo.ApplyModifiedPropertiesWithoutUndo();

        // Instancia os lutadores padrão para teste imediato na cena
        Dictionary<string, GameObject> charPrefabs = LoadCharacterPrefabs();
        GameObject playerGo = null;
        GameObject cpuGo = null;

        if (charPrefabs.ContainsKey("CasualSmilingMan") && charPrefabs["CasualSmilingMan"] != null)
        {
            playerGo = PrefabUtility.InstantiatePrefab(charPrefabs["CasualSmilingMan"]) as GameObject;
            playerGo.name = "Player1_Tiago";
            playerGo.transform.position = spawnP1.transform.position;
            playerGo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

            FighterMovement pMov = playerGo.GetComponent<FighterMovement>();
            if (pMov != null) pMov.IsPlayerControlled = true;

            FighterController pCtrl = playerGo.GetComponent<FighterController>();
            if (pCtrl != null) SetPrivateBool(pCtrl, "showOnScreenControls", true);
        }

        if (charPrefabs.ContainsKey("EmeraldStrength") && charPrefabs["EmeraldStrength"] != null)
        {
            cpuGo = PrefabUtility.InstantiatePrefab(charPrefabs["EmeraldStrength"]) as GameObject;
            cpuGo.name = "CPU_Isaac";
            cpuGo.transform.position = spawnP2.transform.position;
            cpuGo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

            FighterMovement cpuMov = cpuGo.GetComponent<FighterMovement>();
            if (cpuMov != null) cpuMov.IsPlayerControlled = false;

            FighterSparringAI ai = cpuGo.GetComponent<FighterSparringAI>();
            if (ai == null) ai = cpuGo.AddComponent<FighterSparringAI>();
            ai.Difficulty = AIDifficulty.Easy;

            FighterController cpuCtrl = cpuGo.GetComponent<FighterController>();
            if (cpuCtrl != null) SetPrivateBool(cpuCtrl, "showOnScreenControls", false);
        }

        // Conecta as referências cruzadas de movimento
        if (playerGo != null && cpuGo != null)
        {
            FighterMovement pMov = playerGo.GetComponent<FighterMovement>();
            FighterMovement cMov = cpuGo.GetComponent<FighterMovement>();
            if (pMov != null) pMov.Opponent = cpuGo.transform;
            if (cMov != null) cMov.Opponent = playerGo.transform;

            tekkenCam.Fighter1 = playerGo.transform;
            tekkenCam.Fighter2 = cpuGo.transform;
        }

        // GameFlowController
        GameObject flowGo = new GameObject("GameFlow");
        flowGo.transform.SetParent(parent);
        GameFlowController flow = flowGo.AddComponent<GameFlowController>();

        SerializedObject flowSo = new SerializedObject(flow);
        SerializedProperty prefabsProp = flowSo.FindProperty("characterPrefabs");
        string[] charNames = { "CasualSmilingMan", "EmeraldStrength", "ManInBlack", "ModernGentleman", "ShadowSentinel", "StudioRocker", "VascoJacket" };
        prefabsProp.arraySize = charNames.Length;
        for (int i = 0; i < charNames.Length; i++)
        {
            if (charPrefabs.TryGetValue(charNames[i], out GameObject prefab))
                prefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = prefab;
        }

        SerializedProperty victoryProp = flowSo.FindProperty("victoryClips");
        string[] victoryFiles = { "tigas.fbx", "isaac.fbx", "lusca.fbx", "enomoto.fbx", "jompis.fbx", "gabutas.fbx", "ricardin.fbx" };
        victoryProp.arraySize = victoryFiles.Length;
        for (int i = 0; i < victoryFiles.Length; i++)
        {
            AnimationClip clip = LoadClip("Assets/Animations/Mixamo/" + victoryFiles[i]);
            victoryProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
        }

        flowSo.FindProperty("defeatedClip").objectReferenceValue = LoadClip("Assets/Animations/Mixamo/X Bot@Dying.fbx");
        flowSo.FindProperty("fightSceneName").stringValue = "RuaUrbana";
        flowSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Dictionary<string, GameObject> LoadCharacterPrefabs()
    {
        var dict = new Dictionary<string, GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) dict[prefab.name] = prefab;
        }
        return dict;
    }

    private static AnimationClip LoadClip(string assetPath)
    {
        foreach (UnityEngine.Object obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__")) return clip;
        }
        return null;
    }

    private static void SetPrivateBool(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null) prop.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    #endregion

    #region 10. Utilitários de Criação e Configuração

    /// <summary>
    /// Mantém o collider de chão em uma camada explícita de gameplay.
    /// Sem isso, LayerMask.NameToLayer retornaria -1 e a arena cairia em Default.
    /// </summary>
    private static int EnsureEnvironmentLayer()
    {
        int existingLayer = LayerMask.NameToLayer("Environment");
        if (existingLayer >= 0) return existingLayer;

        UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset").FirstOrDefault();
        if (tagManager == null)
            throw new InvalidOperationException("Não foi possível abrir ProjectSettings/TagManager.asset para criar a Layer Environment.");

        SerializedObject serializedTagManager = new SerializedObject(tagManager);
        SerializedProperty layers = serializedTagManager.FindProperty("layers");
        for (int layerIndex = 8; layerIndex < layers.arraySize; layerIndex++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(layerIndex);
            if (!string.IsNullOrEmpty(layer.stringValue)) continue;

            layer.stringValue = "Environment";
            serializedTagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return layerIndex;
        }

        throw new InvalidOperationException("Não há uma User Layer livre para criar a Layer Environment.");
    }

    private static Transform CreateGroup(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        return go.transform;
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 size, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPos;
        cube.transform.localScale = size;
        SetRendererMaterial(cube, material);
        RemoveCollider(cube); // Elementos puramente visuais não devem interferir na física do combate
        return cube;
    }

    private static void SetRendererMaterial(GameObject go, Material mat)
    {
        if (mat == null) return;
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider col = go.GetComponent<Collider>();
        if (col != null) UnityEngine.Object.DestroyImmediate(col);
    }

    private static void RegisterInBuildSettings(string scenePath)
    {
        var existing = EditorBuildSettings.scenes.ToList();
        if (!existing.Any(s => s.path == scenePath))
        {
            existing.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            Debug.Log($"[RuaUrbanaSceneBuilder] Cena '{scenePath}' adicionada ao EditorBuildSettings.");
        }
        else
        {
            Debug.Log($"[RuaUrbanaSceneBuilder] Cena '{scenePath}' já consta no EditorBuildSettings.");
        }
    }

    #endregion
}
#endif
