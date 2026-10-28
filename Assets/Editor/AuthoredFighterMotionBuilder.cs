using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>Deterministic authored muscle curves; originals remain untouched.</summary>
public static class AuthoredFighterMotionBuilder
{
    const string Folder = "Assets/Animations/Authored";
    [MenuItem("UnDFight/Animation/Generate Authored Motions")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Animations", "Authored");
        var guard = Clip("Guard", 1.6f, true, (t,m) => Guard(m));
        var crouch = Clip("CrouchHold", 1f, true, (t,m) => { Guard(m); Crouch(m,1f); });
        var down = Clip("CrouchDown", .28f, false, (t,m) => { Guard(m); Crouch(m, Mathf.SmoothStep(0,1,t)); });
        var up = Clip("CrouchUp", .32f, false, (t,m) => { Guard(m); Crouch(m, 1-Mathf.SmoothStep(0,1,t)); });
        var punch = Clip("Cross", .92f, false, (t,m) =>
        {
            Guard(m);
            float prepare = Pulse(t,0,.22f,.36f);
            float extend = Pulse(t,.22f,.43f,.80f);
            Set(m,"Spine Twist Left-Right", -.13f*prepare+.24f*extend);
            Set(m,"Chest Twist Left-Right", -.12f*prepare+.28f*extend);
            Set(m,"Right Arm Front-Back", .22f-.55f*extend);
            Set(m,"Right Forearm Stretch", -.65f+1.5f*extend);
            Set(m,"Right Shoulder Front-Back", .25f*extend);
            Set(m,"Left Upper Leg Front-Back", .10f+.08f*extend);
        });
        var kick = Clip("FrontKick", 1.18f, false, (t,m) =>
        {
            Guard(m);
            float chamber = Pulse(t,.08f,.34f,.85f);
            float extend = Pulse(t,.34f,.50f,.75f);
            Set(m,"Right Upper Leg Front-Back", .1f+.68f*chamber);
            Set(m,"Right Lower Leg Stretch", -.1f-.72f*chamber+1.05f*extend);
            Set(m,"Spine Front-Back", -.12f*chamber);
            Set(m,"Left Lower Leg Stretch", -.16f);
            Set(m,"Left Foot Up-Down", .08f);
        });
        var jump = Clip("Airborne", 1f, false, (t,m) =>
        {
            Guard(m);
            float tuck = Mathf.Sin(Mathf.PI*t);
            Set(m,"Left Upper Leg Front-Back", .12f+.32f*tuck);
            Set(m,"Right Upper Leg Front-Back", .12f+.25f*tuck);
            Set(m,"Left Lower Leg Stretch", -.15f-.4f*tuck);
            Set(m,"Right Lower Leg Stretch", -.15f-.35f*tuck);
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
                movement.FindProperty("jumpHeight").floatValue=1.45f;
                movement.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var layers=controller.layers; layers[0].iKPass=true; controller.layers=layers;
        AssetDatabase.SaveAssets();
        Debug.Log("Authored motions generated. Review all seven avatars before acceptance; originals retained.");
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
    static float Pulse(float t,float start,float peak,float end) => t<peak ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(start,peak,t)) : 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(peak,end,t));
    static void Set(float[] m,string name,float value) { int i=Array.IndexOf(HumanTrait.MuscleName,name); if(i>=0)m[i]=value; else throw new InvalidOperationException("Unknown muscle: "+name); }
    static void Guard(float[] m)
    {
        foreach(string side in new[]{"Left","Right"})
        {
            Set(m,side+" Arm Down-Up",-.65f);
            Set(m,side+" Arm Front-Back",.22f);
            Set(m,side+" Forearm Stretch",-.65f);
            Set(m,side+" Upper Leg Front-Back",.10f);
            Set(m,side+" Lower Leg Stretch",-.12f);
        }
    }
    static void Crouch(float[] m,float weight)
    {
        foreach(string side in new[]{"Left","Right"})
        {
            Set(m,side+" Upper Leg Front-Back",.10f+.45f*weight);
            Set(m,side+" Lower Leg Stretch",-.12f-.65f*weight);
            Set(m,side+" Foot Up-Down",.30f*weight);
        }
        Set(m,"Spine Front-Back",.13f*weight);
    }
    static AnimationCurve Smooth(Keyframe[] keys) { var curve=new AnimationCurve(keys); for(int i=0;i<keys.Length;i++) curve.SmoothTangents(i,0f); return curve; }
    static AnimationClip Clip(string name,float duration,bool loop,Action<float,float[]> pose)
    {
        var clip=new AnimationClip {name=name,frameRate=60};
        for(int muscle=0;muscle<HumanTrait.MuscleCount;muscle++)
        {
            var keys=new Keyframe[61];
            for(int frame=0;frame<=60;frame++)
            {
                var m=new float[HumanTrait.MuscleCount]; pose(frame/60f,m);
                keys[frame]=new Keyframe(duration*frame/60f,m[muscle]);
            }
            clip.SetCurve("",typeof(Animator),HumanTrait.MuscleName[muscle],Smooth(keys));
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime=loop; settings.keepOriginalPositionY=true; settings.keepOriginalPositionXZ=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);
        string path=Folder+"/"+name+".anim";
        var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(existing) { EditorUtility.CopySerialized(clip,existing); UnityEngine.Object.DestroyImmediate(clip); return existing; }
        AssetDatabase.CreateAsset(clip,path); return clip;
    }
}
