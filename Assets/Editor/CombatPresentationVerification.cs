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
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class CombatPresentationVerification
{
    [Serializable] sealed class PoseMetric { public string character,action; public float idleReach,peakReach,maxReach,peakTime; }
    [Serializable] sealed class Report { public bool passed; public string date,failure; public List<string> checks=new List<string>(); public List<PoseMetric> poses=new List<PoseMetric>(); }
    static Report report;
    static IEnumerator runner;
    static readonly Stack<IEnumerator> steps=new Stack<IEnumerator>();
    static Keyboard keyboard;
    static Gamepad gamepad;
    static InputSettings oldSettings, testSettings;
    static double deadline;
    static int runtimeErrors;
    const string Output="Docs/validation/presentation/";
    static CombatPresentationVerification() { EditorApplication.playModeStateChanged+=State; }
    [MenuItem("Tools/UnDFight/Presentation/Verify Motion And Menus")]
    public static void Start()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Start outside Play Mode");
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Scene has unsaved changes");
        SessionState.SetBool("UnDFight.PresentationVerification",true);
        EditorSceneManager.OpenScene("Assets/Scenes/FightingPrototype.unity",OpenSceneMode.Single); EditorApplication.EnterPlaymode();
    }
    static void State(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("UnDFight.PresentationVerification",false)) return;
        report=new Report {date=DateTime.UtcNow.ToString("O")}; runtimeErrors=0; deadline=EditorApplication.timeSinceStartup+300;
        Directory.CreateDirectory(Output); Application.logMessageReceived+=Log;
        steps.Clear(); runner=Run(); steps.Push(runner); EditorApplication.update+=Tick;
    }
    static void Log(string message,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) { runtimeErrors++; if(report.failure==null) report.failure=message; } }
    static void Tick()
    {
        try {
            if(EditorApplication.timeSinceStartup>deadline) throw new TimeoutException("Presentation verification timeout");
            if(steps.Count==0) { Finish(true); return; }
            var step=steps.Peek(); if(!step.MoveNext()) { steps.Pop(); return; }
            if(step.Current is IEnumerator nested) steps.Push(nested);
        } catch(Exception ex) { report.failure=ex.ToString(); Finish(false); }
    }
    static void Check(bool value,string label) { if(!value) throw new InvalidOperationException(label); report.checks.Add(label); }
    static IEnumerator Frames(int n) { int end=Time.frameCount+n; while(Time.frameCount<end) yield return null; }
    static IEnumerator Seconds(float s) { float end=Time.unscaledTime+s; while(Time.unscaledTime<end) yield return null; }
    static IEnumerator Key(Key key) { InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); yield return Frames(3); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return Frames(3); }
    static IEnumerator Run()
    {
        yield return Frames(15);
        oldSettings=InputSystem.settings; testSettings=UnityEngine.Object.Instantiate(oldSettings);
        testSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus; InputSystem.settings=testSettings;
        keyboard=InputSystem.AddDevice<Keyboard>(); gamepad=InputSystem.AddDevice<Gamepad>();
        var flow=GameFlowController.Instance; Check(flow!=null,"Flow ready");
        foreach(string name in new[]{"HomeBackground","ModeBackground","CharacterBackground","StageBackground","UnDFightLogo"}) Check(Resources.Load<Texture2D>("UI/Brand/"+name)!=null,"Brand asset "+name);
        var menu=UnityEngine.Object.FindAnyObjectByType<ModernMenuPresenter>().GetComponent<UIDocument>();
        foreach(var resolution in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080)}) yield return CaptureUI(menu,resolution,"home");
        yield return Key(UnityEngine.InputSystem.Key.Enter); Check(flow.CurrentMenuScreen=="Mode","Home keyboard enters modes");
        foreach(var resolution in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080)}) yield return CaptureUI(menu,resolution,"modes");
        InputSystem.QueueStateEvent(gamepad,new GamepadState().WithButton(GamepadButton.South)); yield return Frames(3);
        InputSystem.QueueStateEvent(gamepad,new GamepadState()); yield return Frames(3);
        Check(flow.IsSelecting && !flow.ChoosingOpponent,"Mode gamepad enters selection without double confirm");
        flow.OpenMainMenu(); yield return Frames(3);
        for(int index=0;index<flow.CharacterPrefabs.Length;index++)
        {
            flow.Stages.UseLegacy(); GameFlowController.StartDirectFightForTesting(index,(index+1)%flow.CharacterPrefabs.Length,false); yield return Seconds(.35f);
            var fighter=flow.CurrentPlayer; var movement=fighter.Movement; var animator=fighter.Animator;
            string name=GameFlowController.CharacterDisplayNames[index];
            Check(animator.isHuman && animator.avatar.isValid && animator.runtimeAnimatorController.name=="AuthoredFighter",name+": authored humanoid controller");
            CaptureCamera(name+"-guard");
            var position=fighter.transform.position;
            var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot); float standingFoot=foot.position.y;
            movement.InputConfig.SimulatedMovement=new Vector2(1,-1); yield return Seconds(.65f);
            Check(movement.IsCrouching && Mathf.Abs(fighter.transform.position.x-position.x)<.015f,name+": crouch locks horizontal movement");
            Check(Mathf.Abs(foot.position.y-standingFoot)<.1f,name+": crouch foot planted");
            CaptureCamera(name+"-crouch");
            movement.InputConfig.ResetSimulation(); yield return Seconds(.4f); flow.Rematch(); yield return Frames(3);
            foreach(int direction in new[]{0,1,-1})
            {
                position=fighter.transform.position;
                movement.InputConfig.SimulatedMovement=new Vector2(direction,1); yield return Frames(4);
                movement.InputConfig.SimulatedMovement=new Vector2(direction,0);
                float until=Time.realtimeSinceStartup+3,maxHeight=fighter.transform.position.y;
                while((!movement.IsGrounded || Time.unscaledTime<.05f) && Time.realtimeSinceStartup<until)
                {
                    maxHeight=Mathf.Max(maxHeight,fighter.transform.position.y);
                    if(index==0 && maxHeight-fighter.transform.position.y<.02f && maxHeight-position.y>1.1f) CaptureCamera("jump-"+direction+"-apex");
                    yield return Frames(1);
                }
                movement.InputConfig.ResetSimulation();
                Check(movement.IsGrounded && maxHeight-position.y>.7f,name+": jump "+direction+" physical arc and landing");
                if(direction!=0) Check((fighter.transform.position.x-position.x)*direction>.1f,name+": jump "+direction+" travel");
                flow.Rematch(); yield return Frames(5);
            }
            yield return Attack(fighter,FighterAttackType.Punch,name);
            yield return Attack(fighter,FighterAttackType.Attack2,name);
            yield return PoseReach(fighter,name,"Attack",HumanBodyBones.RightHand,.43f);
            yield return PoseReach(fighter,name,"Attack2",HumanBodyBones.RightFoot,.5f);
            flow.Rematch(); yield return Frames(3);
        }
        Check(runtimeErrors==0,"No runtime errors"); flow.OpenMainMenu();
    }
    static IEnumerator Attack(FighterController fighter,FighterAttackType type,string name)
    {
        var flow=GameFlowController.Instance; flow.Rematch(); yield return Frames(4);
        fighter.TriggerAttack(type); bool opened=false; float maximum=Time.realtimeSinceStartup+5;
        while(fighter.CurrentState is AttackState && Time.realtimeSinceStartup<maximum)
        {
            if(fighter.TryGetCurrentAttackProgress(out float phase))
            {
                bool active=fighter.GetComponentsInChildren<Hitbox>().Any(h=>h.IsActive);
                if(active) { opened=true; var timing=fighter.CurrentAttackTiming; Check(phase>=timing.activeStartNormalized-.025f && phase<=timing.activeEndNormalized+.04f,name+": "+type+" contact phase "+phase.ToString("F3")); }
            }
            yield return Frames(1);
        }
        Check(opened && fighter.CurrentState is NeutralState,name+": "+type+" active window and full recovery");
        Check(!fighter.GetComponentsInChildren<Hitbox>().Any(h=>h.IsActive),name+": "+type+" no damage during recovery");
    }
    static IEnumerator PoseReach(FighterController fighter,string name,string state,HumanBodyBones limb,float peak)
    {
        fighter.enabled=false; fighter.Movement.enabled=false; var animator=fighter.Animator; animator.speed=0;
        var bone=animator.GetBoneTransform(limb);
        animator.Play("Idle",0,0); animator.Update(0); float idle=Vector3.Dot(bone.position-fighter.transform.position,fighter.transform.forward);
        float max=float.NegativeInfinity,at=0,peakReach=0;
        for(int i=0;i<=20;i++)
        {
            float t=i/20f; animator.Play(state,0,t); animator.Update(0); float reach=Vector3.Dot(bone.position-fighter.transform.position,fighter.transform.forward);
            if(reach>max) {max=reach;at=t;}
        }
        animator.Play(state,0,peak); animator.Update(0); peakReach=Vector3.Dot(bone.position-fighter.transform.position,fighter.transform.forward);
        CaptureCamera(name+"-"+state+"-peak");
        report.poses.Add(new PoseMetric {character=name,action=state,idleReach=idle,peakReach=peakReach,maxReach=max,peakTime=at});
        animator.speed=1; fighter.enabled=true; fighter.Movement.enabled=true; yield return Frames(1);
    }
    static IEnumerator CaptureUI(UIDocument document,Vector2Int size,string name)
    {
        var target=new RenderTexture(size.x,size.y,24); target.Create(); document.panelSettings.targetTexture=target; yield return Frames(12);
        Rect root=document.rootVisualElement.worldBound;
        foreach(var button in document.rootVisualElement.Query<Button>().ToList()) { var r=button.worldBound; Check(r.xMin>=root.xMin-1 && r.xMax<=root.xMax+1,name+" "+size+": "+button.text+" fits width"); }
        WriteImage(target,Output+name+"-"+size.x+"x"+size.y+".png"); document.panelSettings.targetTexture=null; target.Release(); UnityEngine.Object.DestroyImmediate(target);
    }
    static void CaptureCamera(string name)
    {
        var camera=Camera.main; var target=RenderTexture.GetTemporary(1280,720,24); var old=camera.targetTexture;
        try { camera.targetTexture=target; camera.Render(); WriteImage(target,Output+name+".png"); }
        finally { camera.targetTexture=old; RenderTexture.ReleaseTemporary(target); }
    }
    static void WriteImage(RenderTexture target,string path)
    {
        var old=RenderTexture.active; RenderTexture.active=target; var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); RenderTexture.active=old;
    }
    static void Finish(bool passed)
    {
        EditorApplication.update-=Tick; Application.logMessageReceived-=Log; SessionState.SetBool("UnDFight.PresentationVerification",false);
        if(keyboard!=null && keyboard.added) InputSystem.RemoveDevice(keyboard); if(gamepad!=null && gamepad.added) InputSystem.RemoveDevice(gamepad);
        if(oldSettings!=null) InputSystem.settings=oldSettings; if(testSettings!=null) UnityEngine.Object.DestroyImmediate(testSettings);
        report.passed=passed && runtimeErrors==0; File.WriteAllText(Output+"results.json",JsonUtility.ToJson(report,true));
        if(report.passed) Debug.Log("PRESENTATION VERIFY PASS"); else Debug.LogWarning("PRESENTATION VERIFY FAILED "+report.failure);
        if(Application.isBatchMode) EditorApplication.Exit(report.passed?0:1); else EditorApplication.ExitPlaymode();
    }
}
