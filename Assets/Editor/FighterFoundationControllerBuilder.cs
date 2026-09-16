#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using System.Linq;

/// <summary>Gera o controller mínimo: somente a base Bouncing Fight Idle.</summary>
[InitializeOnLoad]
public static class FighterFoundationControllerBuilder
{
    private const string ControllerPath = "Assets/Resources/Animations/FighterFoundation.controller";
    private const string IdlePath = "Assets/Animations/Mixamo/X Bot@Bouncing Fight Idle.fbx";
    private const string JumpPath = "Assets/Animations/Mixamo/X Bot@Jump.fbx";
    private const string PunchPath = "Assets/Animations/Mixamo/X Bot@Martelo 2.fbx";
    private const string KickPath = "Assets/Animations/Mixamo/X Bot@Kicking.fbx";

    static FighterFoundationControllerBuilder() => EditorApplication.delayCall += Ensure;

    public static void Ensure()
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null) return;
        Rebuild();
    }

    [MenuItem("UnDFight/Rebuild Fighter Foundation")]
    public static void Rebuild()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources/Animations");
        AnimationClip idle = LoadClip(IdlePath);
        AnimationClip jump = LoadClip(JumpPath);
        AnimationClip punch = LoadClip(PunchPath);
        AnimationClip kick = LoadClip(KickPath);
        if (idle == null || jump == null || punch == null || kick == null)
        {
            Debug.LogError("UnDFight: um ou mais clips base nao foram encontrados.");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idle;
        idleState.speed = 1f;
        machine.AddState("Jump").motion = jump;
        machine.AddState("Attack").motion = punch;
        machine.AddState("Attack2").motion = kick;
        machine.defaultState = idleState;
        AssetDatabase.SaveAssets();
    }

    private static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));
    }
}
#endif
