using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Unity.Profiling;

/// <summary>Real Editor play-mode verification. No runtime or build dependency.</summary>
[InitializeOnLoad]
public static class StageVerification
{
    [Serializable] sealed class Measurement
    {
        public string stage;
        public int renderers, materials, triangles, lights, shadowLights, particleLimit;
        public double loadMilliseconds, idempotentPrepareMilliseconds, meanMainThreadMilliseconds, meanGCBytes;
        public long textureMemoryBytes;
        public string gpu = "Unavailable in batch Editor; no GPU/FPS claim";
    }
    [Serializable] sealed class Report
    {
        public bool passed;
        public string unity, date, failure;
        public List<string> checks = new List<string>();
        public List<Measurement> measurements = new List<Measurement>();
    }
    static readonly Stack<IEnumerator> work = new Stack<IEnumerator>();
    static Report report;
    static Keyboard keyboard;
    static Gamepad gamepad;
    static InputSettings originalSettings, testSettings;
    static RenderTexture uiTarget;
    static int errors;
    static double deadline;
    const string ResultPath = "Docs/validation/stage-results.json";
    static StageVerification() { EditorApplication.playModeStateChanged += OnPlay; }
    [MenuItem("Tools/UnDFight/Stages/Verify Flow And UI")]
    public static void Start()
    {
        SessionState.SetBool("UnDFight.StageVerification",true);
        EditorSceneManager.OpenScene("Assets/Scenes/FightingPrototype.unity",OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    static void OnPlay(PlayModeStateChange state)
    {
        if(state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("UnDFight.StageVerification",false)) return;
        report=new Report { unity=Application.unityVersion,date=DateTime.UtcNow.ToString("O") }; errors=0;
        Application.logMessageReceived += Log;
        deadline=EditorApplication.timeSinceStartup+240;
        work.Clear(); work.Push(Run()); EditorApplication.update += Tick;
    }
    static void Log(string message,string trace,LogType type) { if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) { errors++; if(report.failure==null) report.failure=message; } }
    static void Tick()
    {
        try
        {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Verification exceeded 240 seconds");
            if(work.Count==0) { Finish(true); return; }
            var current=work.Peek();
            if(!current.MoveNext()) { work.Pop(); return; }
            if(current.Current is IEnumerator nested) work.Push(nested);
        }
        catch(Exception ex) { report.failure=ex.ToString(); Finish(false); }
    }
    static void Check(bool condition,string label) { if(!condition) throw new InvalidOperationException(label); report.checks.Add(label); }
    static IEnumerator Frames(int count) { while(count-->0) yield return null; }
    static IEnumerator Seconds(float seconds) { float end=Time.unscaledTime+seconds; while(Time.unscaledTime<end) yield return null; }
    static IEnumerator Key(Key key)
    {
        InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); yield return Frames(3);
        InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Frames(3);
    }
    static IEnumerator Pad(GamepadButton button)
    {
        InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(button)); yield return Frames(3);
        InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return Frames(3);
    }
    static IEnumerator Run()
    {
        yield return Frames(20);
        originalSettings=InputSystem.settings; testSettings=UnityEngine.Object.Instantiate(originalSettings);
        testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings=testSettings;
        keyboard=InputSystem.AddDevice<Keyboard>(); gamepad=InputSystem.AddDevice<Gamepad>();
        var flow=GameFlowController.Instance;
        Check(flow!=null,"GameFlow initialized");
        var registry=flow.Stages.Registry;
        Check(registry!=null && registry.Validate(out _),"Registry and contracts valid");
        var arenas=registry.Available.Where(s=>s.stableId!="rua-urbana-test").ToArray();
        Check(arenas.Length==5 && registry.Available.All(s=>s.preview!=null),"Five arenas and real previews registered");
        Check(flow.MenuBackground!=null,"Original menu background retained");
        flow.BeginLocalSelection(); yield return Frames(20);
        var view=UnityEngine.Object.FindAnyObjectByType<SelectionPresenter>();
        Check(view!=null && view.IsReady,"UI Toolkit character presenter ready");
        Check(flow.PlayerPreview!=null && flow.CardPreview(0)!=null,"Existing RenderTextures reused");
        int previous=flow.FocusedCharacter; yield return Key(UnityEngine.InputSystem.Key.RightArrow);
        Check(flow.FocusedCharacter!=previous,"Keyboard character navigation");
        previous=flow.FocusedCharacter; yield return Pad(GamepadButton.DpadDown);
        Check(flow.FocusedCharacter!=previous,"Gamepad character navigation");
        flow.FocusCharacter(3);
        foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(2560,1440),new Vector2Int(2560,1080)}) yield return CaptureUI(view,resolution,"characters");
        yield return Key(UnityEngine.InputSystem.Key.Enter); Check(flow.ChoosingOpponent,"Keyboard confirms P1");
        yield return Pad(GamepadButton.South); yield return Frames(8); Check(flow.IsStageSelecting,"P2 confirms into arena selection");
        int selectedP2=flow.FocusedCharacter;
        flow.BackSelection(); Check(flow.IsSelecting && flow.ChoosingOpponent && flow.FocusedCharacter==selectedP2,"Back preserves character choices");
        flow.ConfirmCharacter(); yield return Frames(8);
        previous=view.FocusedStageIndex; yield return Key(UnityEngine.InputSystem.Key.DownArrow);
        Check(view.FocusedStageIndex!=previous,"Keyboard stage navigation");
        previous=view.FocusedStageIndex; yield return Pad(GamepadButton.DpadDown);
        Check(view.FocusedStageIndex!=previous,"Gamepad stage navigation");
        previous=view.FocusedStageIndex; yield return Key(UnityEngine.InputSystem.Key.R);
        Check(view.FocusedStageIndex!=previous,"Random stage changes focus");
        InputSystem.RemoveDevice(gamepad); gamepad=null; yield return Frames(5);
        previous=view.FocusedStageIndex; yield return Key(UnityEngine.InputSystem.Key.UpArrow);
        Check(view.FocusedStageIndex!=previous,"Navigation survives gamepad disconnect");
        view.FocusStage(arenas[0].stableId);
        foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1280,720),new Vector2Int(2560,1440),new Vector2Int(2560,1080)}) yield return CaptureUI(view,resolution,"arenas");
        Check(!flow.ConfirmStage("missing-id"),"Unknown stage rejected");
        int textures=Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.width==512 && t.height==512);
        foreach(var arena in arenas)
        {
            if(!flow.IsStageSelecting) { flow.ReturnToCharacterSelect(); yield return Frames(8); flow.FocusCharacter(0); flow.ConfirmCharacter(); flow.FocusCharacter(1); flow.ConfirmCharacter(); yield return Frames(8); }
            Check(flow.ConfirmStage(arena.stableId),arena.stableId+": accepted");
            Check(!flow.ConfirmStage(arena.stableId),arena.stableId+": duplicate confirm rejected");
            float until=Time.unscaledTime+10; while(!flow.IsFightScreen && Time.unscaledTime<until) yield return null;
            Check(flow.IsFightScreen && flow.Stages.Loaded!=null && flow.Stages.Selected==arena,arena.stableId+": correct stage loaded");
            yield return Seconds(.5f);
            var env=flow.Stages.Loaded;
            Check(UnityEngine.Object.FindObjectsByType<StageEnvironmentController>(FindObjectsSortMode.None).Length==1,arena.stableId+": one instance");
            Check(flow.CurrentPlayer.Movement.CharacterController.isGrounded && flow.CurrentOpponent.Movement.CharacterController.isGrounded,arena.stableId+": grounded fighters");
            Check(Mathf.Abs(flow.CurrentPlayer.transform.position.x-env.playerSpawn.position.x)<.02f && Mathf.Abs(flow.CurrentOpponent.transform.position.x-env.opponentSpawn.position.x)<.02f,arena.stableId+": spawn positions preserved");
            Check(UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1,arena.stableId+": one audio listener");
            Check(env.GetComponentsInChildren<AudioSource>().Count(s=>s.isPlaying)==1,arena.stableId+": one ambience source");
            var same=env; Check(flow.Stages.Prepare(env.gameObject.scene,flow.CurrentPlayer.Movement,out _ ) && flow.Stages.Loaded==same,arena.stableId+": idempotent prepare");
            flow.SetFightPaused(true); Check(!GameFlowController.AllowsCombat,arena.stableId+": pause gates combat"); flow.SetFightPaused(false);
            var model=new FightHudState(); model.Bind(flow.CurrentPlayer,flow.CurrentOpponent); int hits=0; model.Damaged += _=>hits++;
            flow.CurrentOpponent.HealthSystem.TakeDamage(new DamageData(10,0,Vector3.zero,flow.CurrentPlayer,0));
            Check(hits==1 && model.OpponentRatio<1,arena.stableId+": HUD event data updates"); model.Dispose();
            flow.Rematch(); yield return Frames(5); Check(flow.Stages.Loaded==same && flow.CurrentOpponent.HealthSystem.HealthNormalized==1,arena.stableId+": rematch preserves arena and resets health");
            yield return Measure(env,arena);
            CaptureFight(arena.stableId);
            flow.CurrentOpponent.HealthSystem.TakeDamage(new DamageData(10000,0,Vector3.zero,flow.CurrentPlayer,0));
            until=Time.unscaledTime+8; while(!flow.IsVictoryScreen && Time.unscaledTime<until) yield return null;
            Check(flow.IsVictoryScreen,arena.stableId+": KO reaches victory");
            flow.ReturnToCharacterSelect(); yield return Frames(12);
            Check(UnityEngine.Object.FindObjectsByType<StageEnvironmentController>(FindObjectsSortMode.None).Length==0,arena.stableId+": unload cleans stage");
            Check(Resources.FindObjectsOfTypeAll<RenderTexture>().Count(t=>t.width==512 && t.height==512)==textures,arena.stableId+": preview texture count stable");
            flow.ConfirmCharacter(); flow.ConfirmCharacter(); yield return Frames(5);
        }
        flow.OpenMainMenu(); yield return Frames(15);
        Check(UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(s=>s.name.EndsWith("(Clone)"))==0,"No stage audio survives unload");
        Check(errors==0,"No runtime errors/assertions");
        Check(textures>0,"RenderTexture resource audit sampled");
    }
    static IEnumerator CaptureUI(SelectionPresenter view,Vector2Int size,string label)
    {
        var doc=view.GetComponent<UIDocument>();
        if(uiTarget!=null) { doc.panelSettings.targetTexture=null; uiTarget.Release(); UnityEngine.Object.DestroyImmediate(uiTarget); }
        uiTarget=new RenderTexture(size.x,size.y,24); uiTarget.Create(); doc.panelSettings.targetTexture=uiTarget;
        yield return Frames(16);
        var root=doc.rootVisualElement.Q("selection"); Rect bounds=root.worldBound;
        foreach(var item in new[]{root.Q("hero"),root.Q("heading"),root.Q("confirm"),root.Q("name")})
        {
            Rect r=item.worldBound; Check(r.width>1 && r.height>1 && r.xMin>=bounds.xMin-1 && r.xMax<=bounds.xMax+1 && r.yMin>=bounds.yMin-1 && r.yMax<=bounds.yMax+1,label+" "+size+": "+item.name+" within screen");
        }
        WriteImage(uiTarget,"Docs/validation/"+label+"-"+size.x+"x"+size.y+".png");
    }
    static void WriteImage(RenderTexture target,string path)
    {
        var previous=RenderTexture.active; RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllBytes(path,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); RenderTexture.active=previous;
    }
    static void CaptureFight(string id)
    {
        var camera=Camera.main; var target=RenderTexture.GetTemporary(1280,720,24); var old=camera.targetTexture;
        try { camera.targetTexture=target; camera.Render(); WriteImage(target,"Docs/validation/fight-"+id+".png"); }
        finally { camera.targetTexture=old; RenderTexture.ReleaseTemporary(target); }
    }
    static IEnumerator Measure(StageEnvironmentController env,StageDefinition stage)
    {
        var renderers=env.GetComponentsInChildren<Renderer>(); var lights=env.GetComponentsInChildren<Light>();
        var measure=new Measurement { stage=stage.stableId,renderers=renderers.Length,materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count(),triangles=env.GetComponentsInChildren<MeshFilter>().Where(f=>f.sharedMesh!=null).Sum(f=>f.sharedMesh.triangles.Length/3),lights=lights.Length,shadowLights=lights.Count(l=>l.shadows!=LightShadows.None),particleLimit=env.GetComponentsInChildren<ParticleSystem>().Sum(p=>p.main.maxParticles),textureMemoryBytes=(long)Texture.currentTextureMemory,loadMilliseconds=GameFlowController.Instance.Stages.LastLoadMilliseconds };
        var watch=System.Diagnostics.Stopwatch.StartNew(); GameFlowController.Instance.Stages.Prepare(env.gameObject.scene,GameFlowController.Instance.CurrentPlayer.Movement,out _); watch.Stop(); measure.idempotentPrepareMilliseconds=watch.Elapsed.TotalMilliseconds;
        using(var cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
        using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame"))
        { double sum=0,allocated=0; for(int i=0;i<30;i++) { yield return null; sum+=cpu.LastValue; allocated+=gc.LastValue; } measure.meanMainThreadMilliseconds=cpu.Valid?sum/30/1000000:-1; measure.meanGCBytes=gc.Valid?allocated/30:-1; }
        report.measurements.Add(measure);
    }
    static void Finish(bool passed)
    {
        EditorApplication.update-=Tick; Application.logMessageReceived-=Log; SessionState.SetBool("UnDFight.StageVerification",false);
        if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard); if(gamepad!=null && gamepad.added) InputSystem.RemoveDevice(gamepad);
        if(originalSettings!=null) InputSystem.settings=originalSettings; if(testSettings!=null) UnityEngine.Object.DestroyImmediate(testSettings);
        if(uiTarget!=null) { uiTarget.Release(); UnityEngine.Object.DestroyImmediate(uiTarget); }
        report.passed=passed && errors==0; Directory.CreateDirectory("Docs/validation"); File.WriteAllText(ResultPath,JsonUtility.ToJson(report,true));
        if(report.passed) Debug.Log("STAGE VERIFICATION PASS"); else Debug.LogWarning("STAGE VERIFICATION FAILED: "+report.failure);
        if(Application.isBatchMode) EditorApplication.Exit(report.passed?0:1); else EditorApplication.ExitPlaymode();
    }
}
