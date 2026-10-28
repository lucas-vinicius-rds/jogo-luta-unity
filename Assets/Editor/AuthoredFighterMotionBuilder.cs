using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;

/// <summary>Deterministic authored muscle curves; originals remain untouched.</summary>
public static class AuthoredFighterMotionBuilder
{
    const string Folder = "Assets/Animations/Authored";
    static float[] trustedMuscles;
    static float kickHipTarget=-.7f, punchFrontTarget=-.7f, punchUpTarget=-.05f;
    [MenuItem("UnDFight/Animation/Generate Authored Motions")]
    public static void Generate()
    {
        trustedMuscles=LoadTrustedMuscles();
        DiagnoseReachCandidates();
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Animations", "Authored");
        var guard = Clip("Guard", 1.6f, true, (t,m) => Guard(m));
        var crouch = Clip("CrouchHold", 1f, true, (t,m) => { Guard(m); Crouch(m,1f); });
        var down = Clip("CrouchDown", .28f, false, (t,m) => { Guard(m); Crouch(m, Mathf.SmoothStep(0,1,t)); });
        var up = Clip("CrouchUp", .32f, false, (t,m) => { Guard(m); Crouch(m, 1-Mathf.SmoothStep(0,1,t)); });
        var punch = Clip("Cross", .92f, false, PunchPose);
        var kick = Clip("FrontKick", 1.18f, false, KickPose);
        var jump = Clip("Airborne", 1f, false, (t,m) =>
        {
            Guard(m);
            float tuck = Mathf.Sin(Mathf.PI*t);
            Add(m,"Left Upper Leg Front-Back", -.28f*tuck);
            Add(m,"Right Upper Leg Front-Back", -.24f*tuck);
            Toward(m,"Left Lower Leg Stretch",.20f,tuck);
            Toward(m,"Right Lower Leg Stretch",.25f,tuck);
        });
        const string path = Folder + "/AuthoredFighter.controller";
        if (!AssetDatabase.LoadAssetAtPath<AnimatorController>(path))
            AssetDatabase.CopyAsset("Assets/Animator/Characters/BaseFighter.controller", path);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (!controller) throw new InvalidOperationException("BaseFighter controller missing");
        controller.name="AuthoredFighter";
        if (!controller.parameters.Any(p=>p.name=="Grounded")) controller.AddParameter("Grounded",AnimatorControllerParameterType.Bool);
        if (!controller.parameters.Any(p=>p.name=="FlightRate")) controller.AddParameter("FlightRate",AnimatorControllerParameterType.Float);
        var machine = controller.layers[0].stateMachine;
        var idle = machine.states.First(s=>s.state.name=="Idle").state;
        foreach(var child in machine.states)
        {
            var s = child.state;
            switch(s.name)
            {
                case "Idle": s.motion=guard; break;
                case "Attack": s.motion=punch; s.transitions=Array.Empty<AnimatorStateTransition>(); break;
                case "Attack2": s.motion=kick; s.transitions=Array.Empty<AnimatorStateTransition>(); break;
                case "CrouchIdle": case "CrouchForward": case "CrouchBack": s.motion=crouch; break;
                case "StandingToCrouched": s.motion=down; break;
                case "CrouchedToStanding": s.motion=up; break;
                case "Walk": s.motion=LoadClip("Assets/Animations/Mixamo/X Bot@Walking.fbx"); break;
                case "WalkBack": s.motion=LoadClip("Assets/Animations/Mixamo/X Bot@Walk Backwards.fbx"); break;
                case "Jumping": case "JumpBack": case "JumpForward":
                    s.motion=jump; s.speedParameter="FlightRate"; s.speedParameterActive=true;
                    s.transitions=Array.Empty<AnimatorStateTransition>();
                    var land=s.AddTransition(idle); land.hasExitTime=false; land.duration=.12f;
                    land.AddCondition(AnimatorConditionMode.If,0,"Grounded");
                    break;
                default: continue;
            }
            s.speed=1f;
            if(s.name=="Walk")
                foreach(var transition in s.transitions.Where(x=>x.destinationState==idle))
                    transition.duration=.14f;
        }
        // Avoid Idle -> Walk winning over the backward condition.
        foreach(var transition in idle.transitions.Where(x=>x.destinationState && x.destinationState.name=="Walk"))
            if(!transition.conditions.Any(c=>c.parameter=="MoveDirection")) transition.AddCondition(AnimatorConditionMode.Greater,0,"MoveDirection");
        EditorUtility.SetDirty(controller);
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"}))
        {
            string prefabPath=AssetDatabase.GUIDToAssetPath(guid);
            var root=PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var animator=root.GetComponent<Animator>();
                var fighter=root.GetComponent<FighterController>();
                if(!animator || !fighter) continue;
                animator.runtimeAnimatorController=controller; animator.applyRootMotion=false;
                foreach(var hitbox in root.GetComponentsInChildren<Hitbox>(true)) hitbox.AnchorToBoneContact=true;
                if(!root.GetComponent<AuthoredFighterGrounding>()) root.AddComponent<AuthoredFighterGrounding>();
                var data=new SerializedObject(fighter);
                Configure(data.FindProperty("primaryAttackTiming"),.39f,.43f,HitboxLimb.RightHand);
                Configure(data.FindProperty("secondaryAttackTiming"),.46f,.50f,HitboxLimb.RightFoot);
                data.FindProperty("attackDuration").floatValue=.92f;
                data.FindProperty("secondaryAttackDuration").floatValue=1.18f;
                data.ApplyModifiedPropertiesWithoutUndo();
                var movement=new SerializedObject(root.GetComponent<FighterMovement>());
                movement.FindProperty("forwardSpeed").floatValue=1.55f;
                movement.FindProperty("backwardSpeed").floatValue=1.2f;
                // Preserve the original clearance over a standing opponent. A lower
                // jump plus walking cadence could turn opponents into an air barrier.
                movement.FindProperty("jumpHeight").floatValue=2.2f;
                movement.FindProperty("airControl").floatValue=.9f;
                movement.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var layers=controller.layers; layers[0].iKPass=true; controller.layers=layers;
        AssetDatabase.SaveAssets();
        Debug.Log("Authored motions generated. Review all seven avatars before acceptance; originals retained.");
    }
    [MenuItem("UnDFight/Animation/Diagnose Authored Support")]
    public static void DiagnoseSupport()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var trusted=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Mixamo/Fighter_Idle.anim");
            var guard=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Guard.anim");
            var crouch=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/CrouchHold.anim");
            if(!trusted || !guard || !crouch) throw new InvalidOperationException("Generate authored clips before diagnosis");
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
                try
                {
                    root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    var animator=root.GetComponent<Animator>();
                    var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                    var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
                    trusted.SampleAnimation(root,0f);
                    float idleFloor=Mathf.Min(left.position.y,right.position.y);
                    guard.SampleAnimation(root,0f);
                    float leftGuard=left.position.y,rightGuard=right.position.y;
                    crouch.SampleAnimation(root,.5f);
                    float leftDelta=left.position.y-leftGuard,rightDelta=right.position.y-rightGuard;
                    float guardOffset=Mathf.Min(leftGuard,rightGuard)-idleFloor;
                    Debug.Log($"{path}: trustedFoot={idleFloor:F4}, guardFeet=({leftGuard:F4},{rightGuard:F4}), crouchFeet=({left.position.y:F4},{right.position.y:F4}), crouchDelta=({leftDelta:F4},{rightDelta:F4}), guardVsTrusted={guardOffset:F4}. Samples exclude runtime IK.");
                    if(Mathf.Abs(guardOffset)>.1f || Mathf.Abs(leftDelta)>.1f || Mathf.Abs(rightDelta)>.1f)
                        throw new InvalidOperationException("Authored support height failed on "+path+"; review body curves before Play QA");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
    static AnimationClip LoadClip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__"));
    static void Configure(SerializedProperty p,float start,float end,HitboxLimb limb)
    {
        p.FindPropertyRelative("activeStartNormalized").floatValue=start;
        p.FindPropertyRelative("activeEndNormalized").floatValue=end;
        p.FindPropertyRelative("recoveryEndNormalized").floatValue=1f;
        p.FindPropertyRelative("playbackSpeed").floatValue=1f;
        p.FindPropertyRelative("hitboxLimb").enumValueIndex=(int)limb;
    }
    static void PunchPose(float t,float[] m)
    {
        Guard(m);
        float prepare=Pulse(t,0,.22f,.36f),extend=Pulse(t,.22f,.43f,.80f);
        Add(m,"Spine Twist Left-Right",-.13f*prepare+.24f*extend);
        Add(m,"Chest Twist Left-Right",-.12f*prepare+.28f*extend);
        Toward(m,"Right Arm Front-Back",punchFrontTarget,extend);
        Toward(m,"Right Arm Down-Up",punchUpTarget,extend);
        Toward(m,"Right Forearm Stretch",.98f,extend);
        Add(m,"Right Shoulder Front-Back",.25f*extend);
    }
    static void KickPose(float t,float[] m)
    {
        Guard(m);
        float chamber=t<.34f ? Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.08f,.34f,t)) :
            t<=.55f ? 1f : 1f-Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.55f,.85f,t));
        float extend=Pulse(t,.34f,.50f,.75f);
        Toward(m,"Right Upper Leg Front-Back",kickHipTarget,chamber);
        Toward(m,"Right Lower Leg Stretch",.12f,chamber);
        Toward(m,"Right Lower Leg Stretch",.96f,extend);
        Add(m,"Spine Front-Back",-.08f*chamber);
        Set(m,"Left Lower Leg Stretch",.85f);
    }
    [MenuItem("UnDFight/Animation/Diagnose Reach Candidates")]
    public static void DiagnoseReachCandidates()
    {
        if(trustedMuscles==null) trustedMuscles=LoadTrustedMuscles();
        using var reference=new ReferencePose();
        var m=new float[HumanTrait.MuscleCount]; Guard(m);
        var idleHand=reference.Measure(m,HumanBodyBones.RightHand,false,out _);
        var idleFoot=reference.Measure(m,HumanBodyBones.RightFoot,true,out _);
        float bestPunch=float.NegativeInfinity,bestKick=float.NegativeInfinity;
        float selectedFront=punchFrontTarget,selectedUp=punchUpTarget,selectedHip=kickHipTarget;
        foreach(float front in new[]{-.95f,-.65f,-.35f,0f,.35f,.65f})
            foreach(float up in new[]{-.2f,-.05f,.15f})
            {
                punchFrontTarget=front; punchUpTarget=up; PunchPose(.43f,m);
                var hand=reference.Measure(m,HumanBodyBones.RightHand,false,out float straight);
                // Prefer a straight strike in the torso-height band, not a raised hand.
                float score=hand.z-2f*Mathf.Max(0,Mathf.Abs(hand.y-idleHand.y)-.18f)-.2f*(1f-straight);
                Debug.Log($"Punch candidate front={front:F2}, up={up:F2}: reach={hand.z:F4}, height={hand.y:F4}, armDot={straight:F4}, score={score:F4}");
                if(score>bestPunch) { bestPunch=score; selectedFront=front; selectedUp=up; }
            }
        punchFrontTarget=selectedFront; punchUpTarget=selectedUp;
        foreach(float hip in new[]{-.95f,-.75f,-.55f,-.35f,0f,.35f,.65f})
        {
            kickHipTarget=hip; KickPose(.5f,m);
            var foot=reference.Measure(m,HumanBodyBones.RightFoot,true,out _);
            float score=foot.z; // Same local-forward projection as runtime QA.
            Debug.Log($"Kick candidate hip={hip:F2}: reach={foot.z:F4}, height={foot.y:F4}");
            if(score>bestKick) { bestKick=score; selectedHip=hip; }
        }
        kickHipTarget=selectedHip;
        PunchPose(.43f,m); var chosenHand=reference.Measure(m,HumanBodyBones.RightHand,false,out var alignment);
        KickPose(.5f,m); var chosenFoot=reference.Measure(m,HumanBodyBones.RightFoot,true,out _);
        Debug.Log($"Selected punch front={punchFrontTarget:F2} up={punchUpTarget:F2}, idle/peak={idleHand.z:F4}/{chosenHand.z:F4}, armDot={alignment:F4}; kick hip={kickHipTarget:F2}, idle/peak={idleFoot.z:F4}/{chosenFoot.z:F4}");
        if(chosenHand.z<idleHand.z+.15f || chosenFoot.z<idleFoot.z+.2f)
            throw new InvalidOperationException("Candidate scan found insufficient forward attack reach; inspect diagnostic before generating clips");
    }
    static float Pulse(float t,float start,float peak,float end) => t<peak ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,peak,t)) : 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(peak,end,t));
    static void Set(float[] m,string name,float value) { int i=Array.IndexOf(HumanTrait.MuscleName,name); if(i>=0)m[i]=value; else throw new InvalidOperationException("Unknown muscle: "+name); }
    static string MuscleProperty(int index)
    {
        string name=HumanTrait.MuscleName[index];
        foreach(string side in new[]{"Left","Right"})
            foreach(string finger in new[]{"Thumb","Index","Middle","Ring","Little"})
            {
                string prefix=side+" "+finger+" ";
                if(name.StartsWith(prefix,StringComparison.Ordinal))
                    return side+"Hand."+finger+"."+name.Substring(prefix.Length);
            }
        return name;
    }
    static float[] LoadTrustedMuscles()
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Mixamo/Fighter_Idle.anim");
        var bindings=AnimationUtility.GetCurveBindings(clip);
        return Enumerable.Range(0,HumanTrait.MuscleCount).Select(i=>
        {
            string property=MuscleProperty(i);
            var binding=bindings.First(b=>b.type==typeof(Animator) && b.path=="" && b.propertyName==property);
            return AnimationUtility.GetEditorCurve(clip,binding).Evaluate(0f);
        }).ToArray();
    }
    static float Get(float[] m,string name) => m[Array.IndexOf(HumanTrait.MuscleName,name)];
    static void Add(float[] m,string name,float delta) => Set(m,name,Mathf.Clamp(Get(m,name)+delta,-1f,1f));
    static void Toward(float[] m,string name,float target,float weight) => Set(m,name,Mathf.Lerp(Get(m,name),target,weight));
    static void Guard(float[] m)
    {
        if(trustedMuscles==null) trustedMuscles=LoadTrustedMuscles();
        Array.Copy(trustedMuscles,m,m.Length);
        // Preserve the trusted idle's complete stance/twist/arm foundation.
        // Near +1 means extended knees; zero is already a substantial bend.
        Set(m,"Left Lower Leg Stretch",.9f);
        Set(m,"Right Lower Leg Stretch",.9f);
        for(int i=0;i<HumanTrait.MuscleCount;i++)
            if(HumanTrait.MuscleName[i].Contains("Stretched"))
                m[i]=HumanTrait.MuscleName[i].Contains("Thumb") ? -.45f : -.85f;
    }
    static void Crouch(float[] m,float weight)
    {
        foreach(string side in new[]{"Left","Right"})
        {
            Add(m,side+" Upper Leg Front-Back",-.40f*weight);
            Toward(m,side+" Lower Leg Stretch",.05f,weight);
            Add(m,side+" Foot Up-Down",.12f*weight);
        }
        Add(m,"Spine Front-Back",.08f*weight);
    }    // RootT/RootQ are serialized clip coordinates, NOT HumanPose.bodyPosition.
    // Solve only through the same SampleAnimation path used by the diagnostic.
    sealed class ReferencePose : IDisposable
    {
        static readonly string[] RootNames={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};
        readonly Scene scene;
        readonly GameObject root;
        readonly Animator animator;
        readonly AnimationClip probe;
        readonly float[] originalRoot;
        readonly float supportHeight, leftSupportHeight;
        readonly Vector3 airborneBody;
        float[] previousMuscles, probeMuscles;
        float previousY;
        bool previousKick, probeReady;
        public ReferencePose()
        {
            scene=EditorSceneManager.NewPreviewScene();
            try
            {
                string path=AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs/Characters"})
                    .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).First();
                root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
                ResetTransform();
                animator=root.GetComponent<Animator>();
                if(!animator || !animator.isHuman || !animator.avatar.isValid)
                    throw new InvalidOperationException("Reference prefab requires valid Humanoid Avatar: "+path);
                var idle=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/Mixamo/Fighter_Idle.anim");
                if(!idle) throw new InvalidOperationException("Trusted humanoid Fighter_Idle.anim missing");
                var bindings=AnimationUtility.GetCurveBindings(idle);
                originalRoot=RootNames.Select(name=>AnimationUtility.GetEditorCurve(idle,
                    bindings.First(b=>b.type==typeof(Animator) && b.path=="" && b.propertyName==name)).Evaluate(0f)).ToArray();
                if(originalRoot.Any(v=>float.IsNaN(v)||float.IsInfinity(v)) || originalRoot[1]<.2f)
                    throw new InvalidOperationException("Trusted serialized RootT/RootQ invalid");
                idle.SampleAnimation(root,0f);
                supportHeight=Mathf.Min(Foot(HumanBodyBones.LeftFoot),Foot(HumanBodyBones.RightFoot));
                leftSupportHeight=Foot(HumanBodyBones.LeftFoot);
                probe=new AnimationClip {name="AuthoredSupportProbe",frameRate=60};
                var guard=new float[HumanTrait.MuscleCount]; Guard(guard);
                Evaluate(guard,false,false,out var standing,out _); airborneBody=standing;
                Debug.Log($"Authored serialized COM reference {path}: trusted RootT.y={originalRoot[1]:F6}, solved guard RootT.y={airborneBody.y:F6}, trusted foot={supportHeight:F6}");
            }
            catch { Dispose(); throw; }
        }
        void ResetTransform() => root.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        float Foot(HumanBodyBones bone) => animator.GetBoneTransform(bone).position.y;
        static void Constant(AnimationClip clip,string property,float value) =>
            clip.SetCurve("",typeof(Animator),property,AnimationCurve.Constant(0f,1f,value));
        float Sample(float rootY,bool kick)
        {
            Constant(probe,"RootT.y",rootY);
            ResetTransform();
            probe.SampleAnimation(root,.5f);
            return kick ? Foot(HumanBodyBones.LeftFoot) : Mathf.Min(Foot(HumanBodyBones.LeftFoot),Foot(HumanBodyBones.RightFoot));
        }
        public void Evaluate(float[] muscles,bool airborne,bool kick,out Vector3 position,out Quaternion rotation)
        {
            position=new Vector3(originalRoot[0],originalRoot[1],originalRoot[2]);
            rotation=new Quaternion(originalRoot[3],originalRoot[4],originalRoot[5],originalRoot[6]);
            if(airborne) { position=airborneBody; return; }
            if(previousMuscles!=null && previousKick==kick && previousMuscles.SequenceEqual(muscles))
            { position.y=previousY; return; }
            for(int i=0;i<HumanTrait.MuscleCount;i++)
                if(probeMuscles==null || probeMuscles[i]!=muscles[i]) Constant(probe,MuscleProperty(i),muscles[i]);
            probeMuscles=(float[])muscles.Clone();
            if(!probeReady)
            {
                for(int i=0;i<RootNames.Length;i++) Constant(probe,RootNames[i],originalRoot[i]);
                ConfigureClipSettings(probe,false); probeReady=true;
            }
            float target=kick ? leftSupportHeight : supportHeight;
            float y=originalRoot[1];
            for(int iteration=0;iteration<8;iteration++)
            {
                float foot=Sample(y,kick),error=target-foot;
                if(Mathf.Abs(error)<.0005f) { position.y=y; Cache(muscles,kick,y); return; }
                const float step=.05f;
                float slope=(Sample(y+step,kick)-foot)/step;
                if(float.IsNaN(slope)||float.IsInfinity(slope)||Mathf.Abs(slope)<.01f)
                    throw new InvalidOperationException($"SampleAnimation root sensitivity invalid: y={y}, foot={foot}, slope={slope}");
                y+=Mathf.Clamp(error/slope,-.5f,.5f);
            }
            float residual=Sample(y,kick)-target;
            if(Mathf.Abs(residual)>.002f)
                throw new InvalidOperationException($"Serialized RootT solve failed: y={y}, residual={residual}");
            position.y=y; Cache(muscles,kick,y);
        }
        void Cache(float[] muscles,bool kick,float y)
        {
            previousMuscles=(float[])muscles.Clone(); previousKick=kick; previousY=y;
        }
        public Vector3 Measure(float[] muscles,HumanBodyBones bone,bool kick,out float straightness)
        {
            Evaluate(muscles,false,kick,out var position,out _);
            Sample(position.y,kick);
            var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm).position;
            var elbow=animator.GetBoneTransform(HumanBodyBones.RightLowerArm).position;
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand).position;
            straightness=Vector3.Dot((elbow-upper).normalized,(hand-elbow).normalized);
            return root.transform.InverseTransformPoint(animator.GetBoneTransform(bone).position);
        }
        public void ValidateBaked(AnimationClip clip,bool airborne,bool kick)
        {
            // Verify the final, multi-key clip too: probe success alone is insufficient.
            if(airborne) return;
            float target=kick ? leftSupportHeight : supportHeight;
            for(int frame=0;frame<=60;frame++)
            {
                ResetTransform(); clip.SampleAnimation(root,clip.length*frame/60f);
                float foot=kick ? Foot(HumanBodyBones.LeftFoot) : Mathf.Min(Foot(HumanBodyBones.LeftFoot),Foot(HumanBodyBones.RightFoot));
                if(Mathf.Abs(foot-target)>.003f)
                    throw new InvalidOperationException($"Final {clip.name} frame {frame}: foot={foot:F6}, trusted={target:F6}; serialized curve validation failed");
            }
        }
        public void Dispose()
        {
            if(probe) UnityEngine.Object.DestroyImmediate(probe);
            if(root) UnityEngine.Object.DestroyImmediate(root);
            if(scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }
    }
    static void ConfigureClipSettings(AnimationClip clip,bool loop)
    {
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=loop; settings.keepOriginalPositionY=true; settings.keepOriginalPositionXZ=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
    }
    static AnimationCurve Smooth(Keyframe[] keys) { var curve=new AnimationCurve(keys); for(int i=0;i<keys.Length;i++) curve.SmoothTangents(i,0f); return curve; }
    static AnimationClip Clip(string name,float duration,bool loop,Action<float,float[]> pose)
    {
        using var reference = new ReferencePose();
        var clip=new AnimationClip {name=name,frameRate=60};
        var samples=Enumerable.Range(0,61).Select(frame=>
        {
            var muscles=new float[HumanTrait.MuscleCount]; pose(frame/60f,muscles); return muscles;
        }).ToArray();
        var rootKeys = Enumerable.Range(0,7).Select(_=>new Keyframe[61]).ToArray();
        for(int frame=0;frame<=60;frame++)
        {
            reference.Evaluate(samples[frame], name=="Airborne", name=="FrontKick", out var position, out var rotation);
            float[] values={position.x,position.y,position.z,rotation.x,rotation.y,rotation.z,rotation.w};
            for(int axis=0;axis<7;axis++) rootKeys[axis][frame]=new Keyframe(duration*frame/60f,values[axis]);
        }
        string[] rootNames={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};
        for(int axis=0;axis<7;axis++) clip.SetCurve("",typeof(Animator),rootNames[axis],Smooth(rootKeys[axis]));
        for(int muscle=0;muscle<HumanTrait.MuscleCount;muscle++)
        {
            var keys=new Keyframe[61];
            for(int frame=0;frame<=60;frame++)
            {
                keys[frame]=new Keyframe(duration*frame/60f,samples[frame][muscle]);
            }
            clip.SetCurve("",typeof(Animator),MuscleProperty(muscle),Smooth(keys));
        }
        clip.EnsureQuaternionContinuity();
        ConfigureClipSettings(clip,loop);
        reference.ValidateBaked(clip,name=="Airborne",name=="FrontKick");
        string path=Folder+"/"+name+".anim";
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing) { EditorUtility.CopySerialized(clip,existing); UnityEngine.Object.DestroyImmediate(clip); return existing; }
        AssetDatabase.CreateAsset(clip,path); return clip;
    }
}
