using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Authoring only. Generates editable prefab stages and explicit scene bindings.</summary>
public static partial class StageTools
{
    const string Folder = "Assets/Stages";
    static readonly string[] Groups = { "GameplaySurface", "BackgroundArchitecture", "MidgroundEnvironment", "ForegroundDetails", "Lighting", "Atmosphere", "AmbientAnimation", "Audio", "SpawnPoints", "CameraBoundaries", "Optimization" };
    static GameObject root;
    static string id;
    static Material stone, metal, wood, glow, glass, paint;
    static Mesh panelMesh;
    static Transform Group(string name) => root.transform.Find(name);
    static T Asset<T>(T value,string path) where T : UnityEngine.Object
    {
        var old = AssetDatabase.LoadAssetAtPath<T>(path);
        if(old == null) { AssetDatabase.CreateAsset(value,path); return value; }
        EditorUtility.CopySerialized(value,old); UnityEngine.Object.DestroyImmediate(value); return old;
    }
    [MenuItem("Tools/UnDFight/Stages/Rebuild Neon District")]
    public static void BuildNeon() { EnsureFolders(); Build(0); BindScenes(); AssetDatabase.SaveAssets(); }
    [MenuItem("Tools/UnDFight/Stages/Build Remaining Arenas")]
    public static void BuildRemaining() { EnsureFolders(); for(int i=1;i<5;i++) Build(i); BindScenes(); AssetDatabase.SaveAssets(); }
    [MenuItem("Tools/UnDFight/Stages/Build All And Test Arena")]
    public static void BuildAll() { EnsureFolders(); ExportExisting(); for(int i=0;i<5;i++) Build(i); BindScenes(); AssetDatabase.SaveAssets(); }
    static void EnsureFolders() { Directory.CreateDirectory(Folder+"/Art"); Directory.CreateDirectory("Docs/validation"); AssetDatabase.Refresh(); }
    static StageRegistry Registry()
    {
        var path = Folder+"/StageRegistry.asset"; var registry = AssetDatabase.LoadAssetAtPath<StageRegistry>(path);
        if(registry == null) { registry = ScriptableObject.CreateInstance<StageRegistry>(); AssetDatabase.CreateAsset(registry,path); }
        return registry;
    }
    static Bounds Contract()
    {
        var prefab = AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"}).Select(g=>AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).First(p=>p.GetComponent<FighterMovement>() != null);
        return prefab.GetComponent<FighterMovement>().StageBounds;
    }
    static StageEnvironmentController ContractRoot()
    {
        var environment = root.AddComponent<StageEnvironmentController>(); environment.combatBounds = Contract();
        environment.playerSpawn = new GameObject("Player1").transform; environment.playerSpawn.SetParent(Group("SpawnPoints")); environment.playerSpawn.SetPositionAndRotation(new Vector3(-2.5f,0,environment.combatBounds.center.z),Quaternion.Euler(0,90,0));
        environment.opponentSpawn = new GameObject("Player2").transform; environment.opponentSpawn.SetParent(Group("SpawnPoints")); environment.opponentSpawn.SetPositionAndRotation(new Vector3(2.5f,0,environment.combatBounds.center.z),Quaternion.Euler(0,-90,0));
        environment.effectsRoot = Group("Atmosphere").gameObject;
        return environment;
    }
    static void Begin(string stableId)
    {
        panelMesh = null;
        id = stableId; root = new GameObject(id);
        foreach(string group in Groups) new GameObject(group).transform.SetParent(root.transform);
    }
    static void Build(int kind)
    {
        string[] ids = {"neon-district","skyline-rooftop","underground","ancient-temple","ironworks"};
        string[] names = {"Neon District","Skyline Rooftop","Underground","Ancient Temple","Ironworks"};
        string[] descriptions = {"Comércio noturno, asfalto molhado e letreiros sob a chuva.","O último sol sobre a cobertura técnica da metrópole.","A plataforma esquecida. A cidade continua acima de nós.","Madeira, pedra e vento em um antigo pátio de treinamento.","Aço, vapor e o brilho dos fornos da siderúrgica."};
        Begin(ids[kind]);
        stone = Material("Concrete",new Color(.26f,.28f,.3f),0,.25f,0);
        metal = Material("Steel",new Color(.18f,.22f,.26f),.8f,.5f,0);
        wood = Material("Cedar",new Color(.24f,.10f,.045f),0,.2f,1);
        glass = Material("Glass",new Color(.035f,.1f,.13f),.6f,.9f,0);
        paint = Material("Safety",new Color(.83f,.61f,.12f),.15f,.35f,0);
        glow = Material("Signal",kind == 0 ? new Color(.05f,.7f,.9f) : new Color(1,.23f,.035f),.1f,.6f,0,3);
        var env = ContractRoot();
        var floor = Box("FightSurface",new Vector3(0,-.25f,0),new Vector3(28,.5f,12),kind == 0 ? Material("WetAsphalt",new Color(.065f,.075f,.09f),.25f,.86f,0) : stone,"GameplaySurface");
        env.gameplaySurface = floor.AddComponent<BoxCollider>();
        Box("ForegroundContinuation",new Vector3(0,-.25f,-10),new Vector3(36,.5f,8),stone,"ForegroundDetails");
        switch(kind) { case 0: Neon(); break; case 1: Rooftop(); break; case 2: Underground(); break; case 3: Temple(); break; case 4: Ironworks(); break; }
        Light key = new GameObject("KeyLight").AddComponent<Light>(); key.transform.SetParent(Group("Lighting")); key.type = LightType.Directional; key.transform.rotation = Quaternion.Euler(45,-35,0); key.intensity = kind == 0 ? .6f : 1.5f; key.color = kind == 0 ? new Color(.55f,.7f,1) : kind == 1 || kind == 3 ? new Color(1,.81f,.57f) : new Color(.67f,.79f,1); key.shadows = LightShadows.Soft;
        var definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(Folder+"/"+id+".asset");
        if(definition == null) { definition = ScriptableObject.CreateInstance<StageDefinition>(); AssetDatabase.CreateAsset(definition,Folder+"/"+id+".asset"); }
        definition.stableId = id; definition.displayName = names[kind]; definition.description = descriptions[kind]; definition.available = true; definition.sortOrder = kind;
        definition.ambient = kind == 0 ? new Color(.15f,.18f,.25f) : kind == 2 ? new Color(.23f,.28f,.31f) : new Color(.38f,.39f,.42f);
        definition.fog = true; definition.fogDensity = kind == 2 ? .018f : .009f; definition.fogColor = kind == 0 ? new Color(.02f,.035f,.08f) : kind == 1 ? new Color(.5f,.44f,.39f) : new Color(.21f,.24f,.26f);
        definition.ambience = CreateAmbience(kind);
        CombineStatic();
        if(!env.Validate(out var error)) throw new InvalidOperationException(error);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root,Folder+"/"+id+".prefab"); definition.environmentPrefab = prefab.GetComponent<StageEnvironmentController>();
        var registry = Registry(); if(!registry.stages.Contains(definition)) registry.stages.Add(definition); EditorUtility.SetDirty(registry); EditorUtility.SetDirty(definition);
        UnityEngine.Object.DestroyImmediate(root); CapturePreview(definition); AssetDatabase.SaveAssets(); Debug.Log("STAGE BUILD PASS: "+definition.stableId);
    }
    static Material Material(string name,Color color,float metallic,float smoothness,int grain,float emission = 0)
    {
        var texture = new Texture2D(128,128,TextureFormat.RGBA32,false); var pixels = new Color[128*128];
        for(int y=0;y<128;y++) for(int x=0;x<128;x++) { float noise = Mathf.PerlinNoise(x*(grain == 1 ? .5f : .18f),y*(grain == 1 ? .015f : .18f)); float v = .78f+noise*.3f; if(grain == 2 && (x%32<2 || y%32<2)) v *= .5f; pixels[y*128+x] = new Color(v,v,v,1); }
        texture.SetPixels(pixels); texture.Apply(); texture = Asset(texture,Folder+"/Art/"+id+"-"+name+"-texture.asset");
        var material = new Material(Shader.Find(emission > 0 ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); material.name = id+"-"+name; material.SetColor("_BaseColor",color); material.SetTexture("_BaseMap",texture);
        if(emission == 0) { material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness); }
        if(emission > 0) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*emission); }
        return Asset(material,Folder+"/Art/"+id+"-"+name+".mat");
    }
    static GameObject Box(string name,Vector3 position,Vector3 size,Material material,string group = "BackgroundArchitecture")
    {
        if(panelMesh == null) panelMesh = BeveledPanel();
        var go = new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.GetComponent<MeshFilter>().sharedMesh=panelMesh; go.transform.SetParent(Group(group)); go.transform.position = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true; return go;
    }
    static Mesh BeveledPanel()
    {
        var vertices=new System.Collections.Generic.List<Vector3>(); var triangles=new System.Collections.Generic.List<int>();
        var ring=new[]{new Vector2(-.46f,-.5f),new Vector2(.46f,-.5f),new Vector2(.5f,-.46f),new Vector2(.5f,.46f),new Vector2(.46f,.5f),new Vector2(-.46f,.5f),new Vector2(-.5f,.46f),new Vector2(-.5f,-.46f)};
        for(int level=0;level<4;level++) foreach(var p in ring) { float y=level==0?-.5f:level==1?-.48f:level==2?.48f:.5f; float scale=level==0 || level==3?.96f:1; vertices.Add(new Vector3(p.x*scale,y,p.y*scale)); }
        for(int level=0;level<3;level++) for(int j=0;j<8;j++) { int a=level*8+j,b=level*8+(j+1)%8,c=a+8,d=b+8; triangles.AddRange(new[]{a,c,b,b,c,d}); }
        for(int j=1;j<7;j++) { triangles.AddRange(new[]{0,j,j+1,24,24+j+1,24+j}); }
        var mesh=new Mesh { name="ChamferedArchitecturalPanel" }; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals();
        var uv=vertices.Select(v=>new Vector2(v.x+v.z,v.y+v.z)).ToArray(); mesh.uv=uv;
        return Asset(mesh,Folder+"/Art/"+id+"-panel.asset");
    }
    static void CombineStatic()
    {
        foreach(string groupName in Groups)
        {
            if(groupName == "AmbientAnimation" || groupName == "Atmosphere") continue;
            var group = Group(groupName);
            var filters = group.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<MeshRenderer>() != null && f.GetComponent<TextMesh>() == null && f.sharedMesh != null).ToArray();
            foreach(var set in filters.GroupBy(f => f.GetComponent<MeshRenderer>().sharedMaterial))
            {
                if(set.Key == null) continue;
                var meshes = set.ToArray(); if(meshes.Length < 2) continue;
                var combine = meshes.Select(f => new CombineInstance { mesh=f.sharedMesh,transform=group.worldToLocalMatrix*f.transform.localToWorldMatrix }).ToArray();
                var merged = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                merged.CombineMeshes(combine,true,true); merged.RecalculateBounds();
                string safeName = set.Key.name.Replace('/','-'); merged=Asset(merged,Folder+"/Art/"+id+"-"+groupName+"-"+safeName+".asset");
                var go = new GameObject("Batched_"+safeName,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(group,false); go.GetComponent<MeshFilter>().sharedMesh=merged; go.GetComponent<Renderer>().sharedMaterial=set.Key; go.isStatic=true;
                foreach(var filter in meshes) { UnityEngine.Object.DestroyImmediate(filter.GetComponent<MeshRenderer>()); UnityEngine.Object.DestroyImmediate(filter); }
            }
        }
    }
    static AudioClip CreateAmbience(int kind)
    {
        const int rate = 22050, count = rate*10;
        string path=Folder+"/Art/"+id+"-ambience.wav";
        var random = new System.Random(47+kind); float previous = 0;
        using(var stream=new BinaryWriter(File.Create(path)))
        {
            stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); stream.Write(36+count*2); stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); stream.Write(16); stream.Write((short)1); stream.Write((short)1); stream.Write(rate); stream.Write(rate*2); stream.Write((short)2); stream.Write((short)16); stream.Write(System.Text.Encoding.ASCII.GetBytes("data")); stream.Write(count*2);
            for(int i=0;i<count;i++) { float noise=(float)(random.NextDouble()*2-1); previous=Mathf.Lerp(previous,noise,kind==0?.3f:.025f); float hum=(kind==2 || kind==4)?Mathf.Sin(i*2*Mathf.PI*50/rate)*.05f:0; float envelope=Mathf.Min(1,Mathf.Min(i/(rate*.3f),(count-1-i)/(rate*.3f))); stream.Write((short)(Mathf.Clamp(previous*.2f+hum,-1,1)*envelope*short.MaxValue)); }
        }
        AssetDatabase.ImportAsset(path); return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    static void Pipe(Vector3 a,Vector3 b,float radius,Material material,string group = "BackgroundArchitecture")
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(Group(group)); go.transform.position = (a+b)*.5f; go.transform.up = b-a; go.transform.localScale = new Vector3(radius*2,(b-a).magnitude*.5f,radius*2); go.GetComponent<Renderer>().sharedMaterial = material; go.isStatic = true;
    }
    static void Sign(string text,Vector3 position,float width,Material material)
    {
        Box("SignFrame",position,new Vector3(width,.7f,.18f),metal);
        Box("SignLight",position+Vector3.back*.1f,new Vector3(width-.1f,.58f,.04f),material);
        var label = new GameObject("SignTypography").AddComponent<TextMesh>(); label.transform.SetParent(Group("BackgroundArchitecture")); label.transform.position = position+Vector3.back*.14f; label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center; label.text = text; label.fontSize = 64; label.characterSize = .065f; label.color = Color.white;
    }
    static void Window(float x,float y,float z,float width=1)
    {
        Box("WindowFrame",new Vector3(x,y,z),new Vector3(width+.12f,1.3f,.12f),metal);
        Box("WindowGlass",new Vector3(x,y,z-.08f),new Vector3(width,1.16f,.04f),glass);
        Box("Mullion",new Vector3(x,y,z-.12f),new Vector3(.045f,1.2f,.05f),metal);
    }
    static void Rail(float x,float z,float length)
    {
        for(float i=-length/2;i<=length/2;i+=1.25f) Pipe(new Vector3(x+i,0,z),new Vector3(x+i,1.15f,z),.035f,metal);
        Pipe(new Vector3(x-length/2,1.15f,z),new Vector3(x+length/2,1.15f,z),.045f,metal);
        Pipe(new Vector3(x-length/2,.55f,z),new Vector3(x+length/2,.55f,z),.025f,metal);
    }
    static void Point(Vector3 position,Color color,float intensity,float range)
    {
        var light = new GameObject("PracticalLight").AddComponent<Light>(); light.transform.SetParent(Group("Lighting")); light.transform.position = position; light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
    }
    static void BindScenes()
    {
        foreach(string path in new[]{"Assets/Scenes/FightingPrototype.unity","Assets/Scenes/RuaUrbana.unity"})
        {
            var scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            foreach(var flow in UnityEngine.Object.FindObjectsByType<GameFlowController>(FindObjectsSortMode.None)) { var so = new SerializedObject(flow); so.FindProperty("stageRegistry").objectReferenceValue = Registry(); so.FindProperty("menuBackground").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/UI/ArenaBackdrop.png"); so.FindProperty("menuMusic").objectReferenceValue=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/eltema.mp3"); so.ApplyModifiedPropertiesWithoutUndo(); }
            var binding = UnityEngine.Object.FindAnyObjectByType<StageSceneBinding>(); if(binding == null) binding = new GameObject("StageBinding").AddComponent<StageSceneBinding>();
            binding.legacyEnvironment = scene.GetRootGameObjects().Where(g => g != binding.gameObject && g.GetComponentInChildren<GameFlowController>() == null && g.GetComponentInChildren<Camera>() == null && g.GetComponentInChildren<FighterController>() == null).ToArray();
            EditorSceneManager.SaveScene(scene);
        }
    }
    static void ExportExisting()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/RuaUrbana.unity",OpenSceneMode.Single); Begin("rua-urbana-test");
        foreach(var original in scene.GetRootGameObjects()) if(original != root && new[]{"Gameplay_Arena","Background","Midground","Scene_Props_Decor","Lighting_Atmosphere"}.Contains(original.name)) { var copy = UnityEngine.Object.Instantiate(original,root.transform); copy.name = original.name; }
        var env = ContractRoot();
        foreach(var collider in root.GetComponentsInChildren<Collider>(true)) { if(collider.name == "ArenaCollider" && collider is BoxCollider box) env.gameplaySurface = box; else collider.enabled = false; }
        if(env.gameplaySurface == null) { var floor = Box("TestSurface",new Vector3(0,-.25f,0),new Vector3(28,.5f,10),AssetDatabase.LoadAssetAtPath<Material>("Assets/Arena_Floor_Mat.mat"),"GameplaySurface"); env.gameplaySurface = floor.AddComponent<BoxCollider>(); }
        var prefab = PrefabUtility.SaveAsPrefabAsset(root,Folder+"/rua-urbana-test.prefab");
        var definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(Folder+"/rua-urbana-test.asset"); if(definition == null) { definition = ScriptableObject.CreateInstance<StageDefinition>(); AssetDatabase.CreateAsset(definition,Folder+"/rua-urbana-test.asset"); }
        definition.stableId = "rua-urbana-test"; definition.displayName = "Rua Urbana / Teste"; definition.description = "Arena original exportada sem câmeras ou lutadores."; definition.sortOrder = 999; definition.environmentPrefab = prefab.GetComponent<StageEnvironmentController>();
        var registry = Registry(); if(!registry.stages.Contains(definition)) registry.stages.Add(definition); EditorUtility.SetDirty(registry); EditorUtility.SetDirty(definition); UnityEngine.Object.DestroyImmediate(root); CapturePreview(definition);
    }
    public static void CapturePreview(StageDefinition stage)
    {
        var scene = EditorSceneManager.NewPreviewScene(); var instance = UnityEngine.Object.Instantiate(stage.environmentPrefab); SceneManager.MoveGameObjectToScene(instance.gameObject,scene);
        var camera = new GameObject("PreviewCamera").AddComponent<Camera>(); SceneManager.MoveGameObjectToScene(camera.gameObject,scene); camera.scene = scene; camera.transform.position = new Vector3(0,4.5f,-16); camera.transform.LookAt(new Vector3(0,2.4f,7)); camera.fieldOfView = 45; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = stage.fogColor;
        var ambient = RenderSettings.ambientLight; var mode = RenderSettings.ambientMode; var fog = RenderSettings.fog;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = stage.ambient; RenderSettings.fog = false;
        var target = RenderTexture.GetTemporary(960,540,24); var previous = RenderTexture.active; camera.targetTexture = target;
        try { camera.Render(); RenderTexture.active = target; var image = new Texture2D(960,540,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,960,540),0,0); image.Apply(); string path = Folder+"/"+stage.stableId+"-preview.png"; File.WriteAllBytes(path,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); AssetDatabase.ImportAsset(path); stage.preview = AssetDatabase.LoadAssetAtPath<Texture2D>(path); EditorUtility.SetDirty(stage); }
        finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); RenderSettings.ambientLight = ambient; RenderSettings.ambientMode = mode; RenderSettings.fog = fog; EditorSceneManager.ClosePreviewScene(scene); }
    }
}
