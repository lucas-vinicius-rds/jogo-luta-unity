#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class FightingPrototypeSetup
{
    private const string SourceScene = "Assets/Scenes/SampleScene.unity";
    private const string TestScene = "Assets/Scenes/FightingPrototype.unity";
    private const string BaseControllerPath = "Assets/Animator/Characters/BaseFighter.controller";
    private const string PrefabFolder = "Assets/Prefabs/Characters";
    private const string OverrideFolder = "Assets/Animator/Characters/Overrides";

    private sealed class CharacterDefinition
    {
        public string Name;
        public string Model;
        public string Material;
        public string Attack1;
        public string Attack2;
        public float Attack1Start;
        public float Attack1End;
        public HitboxLimb Attack1Limb;
        public float Attack2Start;
        public float Attack2End;
        public HitboxLimb Attack2Limb;
    }

    private static readonly CharacterDefinition[] Characters =
    {
        new CharacterDefinition
        {
            Name = "CasualSmilingMan",
            Model = "Assets/MeshyImports/Casual Smiling Man with Curly Hair_20260902_174546/Meshy_AI_Casual_Smiling_Man_wi_biped_Animation_Running_withSkin.fbx",
            Attack1 = "Assets/Animations/Mixamo/X Bot@Punching.fbx",
            Material = "Assets/MeshyImports/Casual Smiling Man with Curly Hair_20260902_174546/Material_1.mat",
            Attack2 = "Assets/Animations/Mixamo/X Bot@Flying Kick.fbx",
            Attack1Start = .31f, Attack1End = .43f, Attack1Limb = HitboxLimb.RightHand,
            Attack2Start = .48f, Attack2End = .62f, Attack2Limb = HitboxLimb.RightFoot
        },
        new CharacterDefinition
        {
            Name = "EmeraldStrength",
            Model = "Assets/MeshyImports/Emerald Strength_20260902_201323/Meshy_AI_Emerald_Strength_biped_Animation_Running_withSkin.fbx",
            Attack1 = "Assets/Animations/Mixamo/X Bot@Punching.fbx",
            Material = "Assets/MeshyImports/Emerald Strength_20260902_201323/Material_1.mat",
            Attack2 = "Assets/Animations/Mixamo/X Bot@Kicking.fbx",
            Attack1Start = .31f, Attack1End = .43f, Attack1Limb = HitboxLimb.RightHand,
            Attack2Start = .42f, Attack2End = .54f, Attack2Limb = HitboxLimb.RightFoot
        },
        new CharacterDefinition
        {
            Name = "ManInBlack",
            Model = "Assets/MeshyImports/Man in Black_20260902_155135/Meshy_AI_Man_in_Black_biped_Animation_Running_withSkin.fbx",
            Attack1 = "Assets/Animations/Mixamo/X Bot@Surprise Uppercut.fbx",
            Material = "Assets/MeshyImports/Man in Black_20260902_155135/Material_1.mat",
            Attack2 = "Assets/Animations/Mixamo/X Bot@Headbutt.fbx",
            Attack1Start = .28f, Attack1End = .42f, Attack1Limb = HitboxLimb.RightHand,
            Attack2Start = .45f, Attack2End = .56f, Attack2Limb = HitboxLimb.Head
        },
        new CharacterDefinition
        {
            Name = "ModernGentleman",
            Model = "Assets/MeshyImports/Modern Gentleman poised_20260902_201744/Meshy_AI_Modern_Gentleman_pois_biped_Animation_01a05a9b-b701-7510-8e23-90b8faa3c118_withSkin.fbx",
            Attack1 = "Assets/Animations/Mixamo/X Bot@Martelo 2.fbx",
            Material = "Assets/MeshyImports/Modern Gentleman poised_20260902_201746/Material_1.mat",
            Attack2 = "Assets/Animations/Mixamo/X Bot@Macaco Side.fbx",
            Attack1Start = .30f, Attack1End = .50f, Attack1Limb = HitboxLimb.LeftFoot,
            Attack2Start = .48f, Attack2End = .63f, Attack2Limb = HitboxLimb.RightFoot
        },
        new CharacterDefinition
        {
            Name = "ShadowSentinel",
            Model = "Assets/MeshyImports/Shadow Sentinel_20260902_201914/Meshy_AI_Shadow_Sentinel_biped_Animation_Running_withSkin.fbx",
            Attack1 = "Assets/Animations/Mixamo/X Bot@Kicking (1).fbx",
            Material = "Assets/MeshyImports/Shadow Sentinel_20260902_201914/Material_1.mat",
            Attack2 = "Assets/Animations/Mixamo/X Bot@Surprise Uppercut (1).fbx",
            Attack1Start = .39f, Attack1End = .52f, Attack1Limb = HitboxLimb.RightFoot,
            Attack2Start = .28f, Attack2End = .42f, Attack2Limb = HitboxLimb.RightHand
        },
        new CharacterDefinition
        {
            Name = "StudioRocker",
            Model = "Assets/MeshyImports/Studio Rocker Pose_20260902_200943/Meshy_AI_Studio_Rocker_Pose_biped_Animation_Running_withSkin.fbx",
            Attack1 = "Assets/Animations/Mixamo/X Bot@Headbutt.fbx",
            Material = "Assets/MeshyImports/Studio Rocker Pose_20260902_200943/Material_1.mat",
            Attack2 = "Assets/Animations/Mixamo/X Bot@Flying Kick.fbx",
            Attack1Start = .45f, Attack1End = .56f, Attack1Limb = HitboxLimb.Head,
            Attack2Start = .48f, Attack2End = .62f, Attack2Limb = HitboxLimb.RightFoot
        },
        new CharacterDefinition
        {
            Name = "VascoJacket",
            Model = "Assets/MeshyImports/Vasco Jacket Avatar_20260902_201115/Meshy_AI_Vasco_Jacket_Avatar_biped_Animation_Running_withSkin.fbx",
            Attack1 = "Assets/Animations/Mixamo/X Bot@Punching.fbx",
            Material = "Assets/MeshyImports/Vasco Jacket Avatar_20260902_201115/Material_1.mat",
            Attack2 = "Assets/Animations/Mixamo/X Bot@Martelo 2.fbx",
            Attack1Start = .31f, Attack1End = .43f, Attack1Limb = HitboxLimb.RightHand,
            Attack2Start = .30f, Attack2End = .50f, Attack2Limb = HitboxLimb.LeftFoot
        }
    };

    private static readonly string[] SharedAnimationPaths =
    {
        "Assets/Animations/Mixamo/X Bot@Bouncing Fight Idle.fbx",
        "Assets/Animations/Mixamo/X Bot@Walking.fbx",
        "Assets/Animations/Mixamo/X Bot@Walking Turn 180.fbx",
        "Assets/Animations/Mixamo/X Bot@Hit To Body.fbx",
        "Assets/Animations/Mixamo/X Bot@Dying.fbx"
    };

    [MenuItem("Tools/Fighting Prototype/Build or Refresh")]
    public static void BuildOrRefresh()
    {
        EnsureFolders();
        ConfigureHumanoidImports();
        AnimatorController baseController = CreateBaseController();

        var prefabs = new Dictionary<string, GameObject>();
        foreach (CharacterDefinition character in Characters)
        {
            AnimatorOverrideController overrideController = CreateOverrideController(character, baseController);
            prefabs[character.Name] = CreateCharacterPrefab(character, overrideController);
        }

        // Reload references after every generated controller has been imported. The first
        // override asset can otherwise still be represented by Unity's pre-import object.
        foreach (CharacterDefinition character in Characters)
        {
            string prefabPath = PrefabFolder + "/" + character.Name + ".prefab";
            string controllerPath = OverrideFolder + "/" + character.Name + ".overrideController";
            EnsurePrefabController(prefabPath, AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath));
            prefabs[character.Name] = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        CreateTestScene(prefabs);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FightingPrototypeSetup] Complete: 7 playable prefabs and FightingPrototype scene are ready.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Editor");
        EnsureFolder("Assets", "Animator");
        EnsureFolder("Assets/Animator", "Characters");
        EnsureFolder("Assets/Animator/Characters", "Overrides");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets/Prefabs", "Characters");
        EnsureFolder("Assets", "Animations");
        EnsureFolder("Assets/Animations", "Fighting");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }

    private static void ConfigureHumanoidImports()
    {
        IEnumerable<string> animationPaths = AssetDatabase
            .FindAssets("t:Model", new[] { "Assets/Animations/Mixamo" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .Distinct();

        foreach (string path in animationPaths)
        {
            ConfigureModelImporter(path, true);
        }

        foreach (CharacterDefinition character in Characters)
        {
            ConfigureModelImporter(character.Model, false);
        }
    }

    private static void ConfigureModelImporter(string path, bool configureLoop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning("[FightingPrototypeSetup] ModelImporter not found: " + path);
            return;
        }

        bool changed = importer.animationType != ModelImporterAnimationType.Human ||
                       importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation = true;

        if (configureLoop)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            string lower = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            bool isVictory = lower == "tigas" || lower == "isaac" || lower == "lusca" ||
                             lower == "enomoto" || lower == "jompis" || lower == "gabutas" ||
                             lower == "ricardin";
            bool shouldLoop = lower.Contains("idle") || lower.EndsWith("@walking") ||
                              lower.Contains("crouch") || lower.Contains("kneeling down") || isVictory;
            bool isAttack = Characters.Any(character => character.Attack1 == path || character.Attack2 == path);
            bool preserveRootMotion = isAttack || lower.Contains("jump") || lower.Contains("pulo");
            foreach (ModelImporterClipAnimation clip in clips)
            {
                if (clip.loopTime != shouldLoop)
                {
                    clip.loopTime = shouldLoop;
                    changed = true;
                }
                if (!clip.lockRootRotation || !clip.lockRootHeightY || clip.lockRootPositionXZ == preserveRootMotion)
                {
                    changed = true;
                }
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = !preserveRootMotion;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = preserveRootMotion;
            }
            if (clips.Length > 0) importer.clipAnimations = clips;
        }

        if (changed) importer.SaveAndReimport();
    }

    private static AnimationClip LoadClip(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
            .OrderByDescending(c => c.length)
            .FirstOrDefault();
    }

    private static AnimatorController CreateBaseController()
    {
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BaseControllerPath) != null)
            AssetDatabase.DeleteAsset(BaseControllerPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(BaseControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Punch", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack2", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Crouch", AnimatorControllerParameterType.Bool);
        controller.AddParameter("CrouchDirection", AnimatorControllerParameterType.Int);
        controller.AddParameter("MoveDirection", AnimatorControllerParameterType.Int);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("JumpType", AnimatorControllerParameterType.Int);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimationClip idleClip = LoadClip(SharedAnimationPaths[0]);
        AnimationClip walkClip = LoadClip(SharedAnimationPaths[1]);
        AnimationClip turnClip = LoadClip(SharedAnimationPaths[2]);
        AnimationClip hitClip = LoadClip(SharedAnimationPaths[3]);
        AnimationClip deathClip = LoadClip(SharedAnimationPaths[4]);
        AnimationClip punchClip = LoadClip("Assets/Animations/Mixamo/X Bot@Punching.fbx");
        AnimationClip attack2Clip = LoadClip("Assets/Animations/Mixamo/X Bot@Flying Kick.fbx");
        AnimationClip crouchForwardClip = LoadClip("Assets/Animations/Mixamo/X Bot@Crouch Walk Forward.fbx");
        AnimationClip crouchBackClip = LoadClip("Assets/Animations/Mixamo/X Bot@Crouch Walk Back.fbx");
        AnimationClip crouchIdleClip = LoadClip("Assets/Animations/Mixamo/X Bot@Kneeling Down.fbx");
        AnimationClip jumpIdleClip = LoadClip("Assets/Animations/Mixamo/X Bot@Jumping.fbx");
        AnimationClip jumpBackClip = LoadClip("Assets/Animations/Mixamo/X Bot@Jump.fbx");
        AnimationClip jumpForwardClip = LoadClip("Assets/Animations/Mixamo/Pulo pra frente.fbx");

        AnimatorState idle = AddState(machine, "Idle", idleClip, 1f, new Vector3(220, 30));
        AnimatorState walk = AddState(machine, "Walk", walkClip, 1.35f, new Vector3(430, 30));
        AnimatorState punch = AddState(machine, "Attack", punchClip, 1f, new Vector3(320, 140));
        AnimatorState attack2 = AddState(machine, "Attack2", attack2Clip, 1f, new Vector3(500, 140));
        AnimatorState hit = AddState(machine, "HitStun", hitClip, hitClip != null ? Mathf.Max(1f, hitClip.length / .55f) : 1f, new Vector3(230, 250));
        AnimatorState death = AddState(machine, "Dying", deathClip, deathClip != null ? Mathf.Max(1f, deathClip.length / 2.2f) : 1f, new Vector3(480, 250));
        AnimatorState turn = AddState(machine, "Turn180", turnClip, 1f, new Vector3(650, 30));
        AnimatorState crouchForward = AddState(machine, "CrouchForward", crouchForwardClip, 1.25f, new Vector3(650, 140));
        AnimatorState crouchBack = AddState(machine, "CrouchBack", crouchBackClip, 1.25f, new Vector3(650, 220));
        AnimatorState crouchIdle = AddState(machine, "CrouchIdle", crouchIdleClip, 1f, new Vector3(650, 300));
        const float physicalAirTime = .94f;
        AnimatorState jumpIdle = AddState(machine, "Jumping", jumpIdleClip, jumpIdleClip != null ? jumpIdleClip.length / physicalAirTime : 1f, new Vector3(820, 30));
        AnimatorState jumpBack = AddState(machine, "JumpBack", jumpBackClip, jumpBackClip != null ? jumpBackClip.length / physicalAirTime : 1f, new Vector3(820, 110));
        AnimatorState jumpForward = AddState(machine, "JumpForward", jumpForwardClip, jumpForwardClip != null ? jumpForwardClip.length / physicalAirTime : 1f, new Vector3(820, 190));
        machine.defaultState = idle;

        AddConditionTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.1f, false);
        AddConditionTransition(walk, idle, "Speed", AnimatorConditionMode.Less, 0.1f, false);
        AddConditionTransition(idle, punch, "Punch", AnimatorConditionMode.If, 0f, false);
        AddConditionTransition(walk, punch, "Punch", AnimatorConditionMode.If, 0f, false);
        AddConditionTransition(idle, attack2, "Attack2", AnimatorConditionMode.If, 0f, false);
        AddConditionTransition(walk, attack2, "Attack2", AnimatorConditionMode.If, 0f, false);
        AddExitTransition(punch, idle, 0.9f);
        AddExitTransition(attack2, idle, 0.9f);
        AddExitTransition(turn, idle, 0.9f);
        AddCrouchTransition(idle, crouchIdle, AnimatorConditionMode.Equals, 0f);
        AddCrouchTransition(walk, crouchIdle, AnimatorConditionMode.Equals, 0f);
        AddCrouchTransition(idle, crouchForward, AnimatorConditionMode.Greater, 0f);
        AddCrouchTransition(walk, crouchForward, AnimatorConditionMode.Greater, 0f);
        AddCrouchTransition(idle, crouchBack, AnimatorConditionMode.Less, 0f);
        AddCrouchTransition(walk, crouchBack, AnimatorConditionMode.Less, 0f);
        AddCrouchDirectionTransition(crouchIdle, crouchForward, AnimatorConditionMode.Greater, 0f);
        AddCrouchDirectionTransition(crouchIdle, crouchBack, AnimatorConditionMode.Less, 0f);
        AddCrouchDirectionTransition(crouchForward, crouchIdle, AnimatorConditionMode.Equals, 0f);
        AddCrouchDirectionTransition(crouchBack, crouchIdle, AnimatorConditionMode.Equals, 0f);
        AddCrouchDirectionTransition(crouchForward, crouchBack, AnimatorConditionMode.Less, 0f);
        AddCrouchDirectionTransition(crouchBack, crouchForward, AnimatorConditionMode.Greater, 0f);
        AddConditionTransition(crouchIdle, idle, "Crouch", AnimatorConditionMode.IfNot, 0f, false);
        AddConditionTransition(crouchForward, idle, "Crouch", AnimatorConditionMode.IfNot, 0f, false);
        AddConditionTransition(crouchBack, idle, "Crouch", AnimatorConditionMode.IfNot, 0f, false);
        AddJumpTransition(idle, jumpIdle, 0f);
        AddJumpTransition(idle, jumpBack, 1f);
        AddJumpTransition(idle, jumpForward, 2f);
        AddJumpTransition(walk, jumpIdle, 0f);
        AddJumpTransition(walk, jumpBack, 1f);
        AddJumpTransition(walk, jumpForward, 2f);
        AddExitTransition(jumpIdle, idle, 1f);
        AddExitTransition(jumpBack, idle, 1f);
        AddExitTransition(jumpForward, idle, 1f);

        AnimatorStateTransition hitTransition = machine.AddAnyStateTransition(hit);
        ConfigureTransition(hitTransition, false, 0f, 0.05f);
        hitTransition.AddCondition(AnimatorConditionMode.If, 0f, "Hit");
        AddExitTransition(hit, idle, 0.9f);

        AnimatorStateTransition deathTransition = machine.AddAnyStateTransition(death);
        ConfigureTransition(deathTransition, false, 0f, 0.05f);
        deathTransition.AddCondition(AnimatorConditionMode.If, 0f, "IsDead");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name, AnimationClip clip, float speed, Vector3 position)
    {
        AnimatorState state = machine.AddState(name, position);
        state.motion = clip;
        state.speed = speed;
        return state;
    }

    private static void AddConditionTransition(AnimatorState source, AnimatorState destination, string parameter, AnimatorConditionMode mode, float threshold, bool hasExitTime)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureTransition(transition, hasExitTime, 0f, 0.08f);
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddJumpTransition(AnimatorState source, AnimatorState destination, float jumpType)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureTransition(transition, false, 0f, 0.05f);
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Jump");
        transition.AddCondition(AnimatorConditionMode.Equals, jumpType, "JumpType");
    }

    private static void AddCrouchTransition(AnimatorState source, AnimatorState destination, AnimatorConditionMode directionMode, float directionThreshold)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureTransition(transition, false, 0f, 0.08f);
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
        transition.AddCondition(directionMode, directionThreshold, "CrouchDirection");
    }

    private static void AddCrouchDirectionTransition(AnimatorState source, AnimatorState destination, AnimatorConditionMode directionMode, float threshold)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureTransition(transition, false, 0f, 0.08f);
        transition.AddCondition(AnimatorConditionMode.If, 0f, "Crouch");
        transition.AddCondition(directionMode, threshold, "CrouchDirection");
    }

    private static void AddExitTransition(AnimatorState source, AnimatorState destination, float exitTime)
    {
        AnimatorStateTransition transition = source.AddTransition(destination);
        ConfigureTransition(transition, true, exitTime, 0.08f);
    }

    private static void ConfigureTransition(AnimatorStateTransition transition, bool hasExitTime, float exitTime, float duration)
    {
        transition.hasExitTime = hasExitTime;
        transition.exitTime = exitTime;
        transition.duration = duration;
        transition.hasFixedDuration = true;
        transition.canTransitionToSelf = false;
    }

    private static AnimatorOverrideController CreateOverrideController(CharacterDefinition character, AnimatorController baseController)
    {
        string path = OverrideFolder + "/" + character.Name + ".overrideController";
        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null) AssetDatabase.DeleteAsset(path);

        var controller = new AnimatorOverrideController(baseController) { name = character.Name + "Animator" };
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        controller.GetOverrides(overrides);
        AnimationClip basePunch = LoadClip("Assets/Animations/Mixamo/X Bot@Punching.fbx");
        AnimationClip baseAttack2 = LoadClip("Assets/Animations/Mixamo/X Bot@Flying Kick.fbx");
        AnimationClip attack1 = LoadClip(character.Attack1);
        AnimationClip attack2 = LoadClip(character.Attack2);

        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i].Key == basePunch) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, attack1);
            if (overrides[i].Key == baseAttack2) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, attack2);
        }
        controller.ApplyOverrides(overrides);
        AssetDatabase.CreateAsset(controller, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
    }

    private static GameObject CreateCharacterPrefab(CharacterDefinition character, RuntimeAnimatorController controller)
    {
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(character.Model);
        if (modelAsset == null) throw new InvalidOperationException("Missing character model: " + character.Model);

        GameObject root = new GameObject(character.Name);
        try
        {
            CharacterController capsule = root.AddComponent<CharacterController>();
            capsule.center = new Vector3(0f, 1f, 0f);
            capsule.height = 2f;
            capsule.radius = 0.42f;
            capsule.stepOffset = 0.25f;

            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (visual == null) visual = UnityEngine.Object.Instantiate(modelAsset);
            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);

            Material material = AssetDatabase.LoadAssetAtPath<Material>(character.Material);
            if (material != null)
            {
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    if (materials == null || materials.Length == 0) materials = new[] { material };
                    else for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    renderer.sharedMaterials = materials;
                    renderer.enabled = true;
                }
            }

            Animator sourceAnimator = visual.GetComponentInChildren<Animator>(true);
            if (sourceAnimator == null || sourceAnimator.avatar == null)
                throw new InvalidOperationException("Humanoid avatar missing in " + character.Model);
            sourceAnimator.enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(sourceAnimator);

            Animator animator = root.AddComponent<Animator>();
            animator.avatar = sourceAnimator.avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

            HealthSystem health = root.AddComponent<HealthSystem>();
            FighterMovement movement = root.AddComponent<FighterMovement>();
            FighterController fighter = root.AddComponent<FighterController>();
            FighterAppearance appearance = root.AddComponent<FighterAppearance>();
            SerializedObject appearanceSo = new SerializedObject(appearance);
            appearanceSo.FindProperty("characterMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(character.Material);
            appearanceSo.ApplyModifiedPropertiesWithoutUndo();

            InputActionReference moveReference = LoadInputReference("Player/Move");
            SerializedObject movementSo = new SerializedObject(movement);
            movementSo.FindProperty("moveActionReference").objectReferenceValue = moveReference;
            movementSo.ApplyModifiedPropertiesWithoutUndo();

            CreateHurtbox(root.transform, fighter);
            var hitboxes = new List<Hitbox>
            {
                CreateHitbox(animator, fighter, HumanBodyBones.RightHand, HitboxLimb.RightHand),
                CreateHitbox(animator, fighter, HumanBodyBones.LeftHand, HitboxLimb.LeftHand),
                CreateHitbox(animator, fighter, HumanBodyBones.RightFoot, HitboxLimb.RightFoot),
                CreateHitbox(animator, fighter, HumanBodyBones.LeftFoot, HitboxLimb.LeftFoot),
                CreateHitbox(animator, fighter, HumanBodyBones.Head, HitboxLimb.Head)
            }.Where(h => h != null).ToList();

            SerializedObject fighterSo = new SerializedObject(fighter);
            fighterSo.FindProperty("animator").objectReferenceValue = animator;
            fighterSo.FindProperty("attackActionReference").objectReferenceValue = LoadInputReference("Player/Attack");
            fighterSo.FindProperty("defaultAttackDamage").floatValue = 10f;
            fighterSo.FindProperty("secondaryAttackDamage").floatValue = 15f;
            ConfigureAttackTiming(fighterSo.FindProperty("primaryAttackTiming"), character.Attack1, character.Attack1Start, character.Attack1End, character.Attack1Limb);
            ConfigureAttackTiming(fighterSo.FindProperty("secondaryAttackTiming"), character.Attack2, character.Attack2Start, character.Attack2End, character.Attack2Limb);
            fighterSo.FindProperty("hitboxes").arraySize = hitboxes.Count;
            for (int i = 0; i < hitboxes.Count; i++) fighterSo.FindProperty("hitboxes").GetArrayElementAtIndex(i).objectReferenceValue = hitboxes[i];
            fighterSo.ApplyModifiedPropertiesWithoutUndo();

            movement.IsPlayerControlled = false;
            string prefabPath = PrefabFolder + "/" + character.Name + ".prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            EnsurePrefabController(prefabPath, controller);
            return AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigureAttackTiming(SerializedProperty property, string animationPath, float start, float end, HitboxLimb limb)
    {
        property.FindPropertyRelative("activeStartNormalized").floatValue = start;
        property.FindPropertyRelative("activeEndNormalized").floatValue = end;
        float recovery = animationPath.Contains("Uppercut") ? .60f :
                         animationPath.Contains("Martelo") ? .75f : .82f;
        property.FindPropertyRelative("recoveryEndNormalized").floatValue = recovery;
        AnimationClip clip = LoadClip(animationPath);
        float targetDuration = animationPath.Contains("Macaco") ? 1.2f :
                               animationPath.Contains("Uppercut") ? 2.5f :
                               animationPath.Contains("Flying Kick") ? 1.1f :
                               animationPath.Contains("Kicking (1)") ? 1.1f :
                               animationPath.Contains("Kicking") ? 1.05f :
                               animationPath.Contains("Headbutt") ? .9f :
                               animationPath.Contains("Martelo") ? 1.1f : .85f;
        property.FindPropertyRelative("playbackSpeed").floatValue = clip != null ? Mathf.Clamp(clip.length / targetDuration, 1f, 1.4f) : 1f;
        property.FindPropertyRelative("hitboxLimb").enumValueIndex = (int)limb;
    }

    private static InputActionReference LoadInputReference(string actionName)
    {
        return AssetDatabase.LoadAllAssetsAtPath("Assets/InputSystem_Actions.inputactions")
            .OfType<InputActionReference>()
            .FirstOrDefault(reference => reference.name == actionName);
    }

    private static void EnsurePrefabController(string prefabPath, RuntimeAnimatorController controller)
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Animator animator = contents.GetComponent<Animator>();
            if (animator == null) throw new InvalidOperationException("Animator missing in " + prefabPath);
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EditorUtility.SetDirty(animator);

            FighterMovement movement = contents.GetComponent<FighterMovement>();
            FighterController fighter = contents.GetComponent<FighterController>();
            if (movement != null)
            {
                SerializedObject movementSo = new SerializedObject(movement);
                movementSo.FindProperty("moveActionReference").objectReferenceValue = LoadInputReference("Player/Move");
                movementSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(movement);
            }

            if (fighter != null)
            {
                SerializedObject fighterSo = new SerializedObject(fighter);
                fighterSo.FindProperty("attackActionReference").objectReferenceValue = LoadInputReference("Player/Attack");
                fighterSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(fighter);
            }

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
    }

    private static void CreateHurtbox(Transform parent, FighterController owner)
    {
        GameObject go = new GameObject("BodyHurtbox");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 1f, 0f);
        CapsuleCollider collider = go.AddComponent<CapsuleCollider>();
        collider.isTrigger = true;
        collider.height = 1.8f;
        collider.radius = 0.42f;
        Hurtbox hurtbox = go.AddComponent<Hurtbox>();
        SerializedObject so = new SerializedObject(hurtbox);
        so.FindProperty("owner").objectReferenceValue = owner;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Hitbox CreateHitbox(Animator animator, FighterController owner, HumanBodyBones bone, HitboxLimb limb)
    {
        GameObject go = new GameObject(limb + "Hitbox");
        go.transform.SetParent(owner.transform, false);
        bool isFoot = limb == HitboxLimb.RightFoot || limb == HitboxLimb.LeftFoot;
        bool isRight = limb == HitboxLimb.RightHand || limb == HitboxLimb.RightFoot;
        go.transform.localPosition = new Vector3(isRight ? 0.22f : -0.22f, isFoot ? 0.55f : 1.2f, isFoot ? 0.78f : 0.68f);
        SphereCollider collider = go.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.enabled = false;
        collider.radius = isFoot ? 0.38f : 0.35f;
        Hitbox hitbox = go.AddComponent<Hitbox>();
        SerializedObject so = new SerializedObject(hitbox);
        so.FindProperty("limbType").enumValueIndex = (int)limb;
        so.FindProperty("owner").objectReferenceValue = owner;
        so.ApplyModifiedPropertiesWithoutUndo();
        return hitbox;
    }

    private static Transform FindBoneByName(Transform root, string expected)
    {
        string normalized = expected.Replace("Upper", string.Empty).Replace("Lower", string.Empty);
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string name = child.name.Replace("mixamorig:", string.Empty).Replace("_", string.Empty);
            if (name.IndexOf(normalized, StringComparison.OrdinalIgnoreCase) >= 0) return child;
        }
        return null;
    }

    private static void CreateTestScene(Dictionary<string, GameObject> prefabs)
    {
        if (!AssetDatabase.CopyAsset(SourceScene, TestScene))
        {
            AssetDatabase.DeleteAsset(TestScene);
            if (!AssetDatabase.CopyAsset(SourceScene, TestScene)) throw new InvalidOperationException("Could not copy test scene.");
        }

        Scene scene = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);
        FighterController[] oldFighters = UnityEngine.Object.FindObjectsByType<FighterController>(FindObjectsSortMode.None);
        Vector3[] positions = oldFighters.Select(f => f.transform.position).OrderBy(p => p.x).ToArray();
        foreach (FighterController old in oldFighters) UnityEngine.Object.DestroyImmediate(old.gameObject);

        Vector3 playerPosition = positions.Length > 0 ? positions[0] : new Vector3(-1.25f, 0f, 0f);
        Vector3 opponentPosition = positions.Length > 1 ? positions[positions.Length - 1] : new Vector3(1.25f, 0f, 0f);
        playerPosition.y = 0f;
        opponentPosition.y = 0f;

        GameObject player = PrefabUtility.InstantiatePrefab(prefabs["CasualSmilingMan"], scene) as GameObject;
        GameObject opponent = PrefabUtility.InstantiatePrefab(prefabs["EmeraldStrength"], scene) as GameObject;
        player.name = "Player_CasualSmilingMan";
        opponent.name = "Opponent_EmeraldStrength";
        player.transform.position = playerPosition;
        opponent.transform.position = opponentPosition;
        player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        opponent.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        FighterMovement playerMovement = player.GetComponent<FighterMovement>();
        FighterMovement opponentMovement = opponent.GetComponent<FighterMovement>();
        playerMovement.IsPlayerControlled = true;
        opponentMovement.IsPlayerControlled = false;
        playerMovement.Opponent = opponent.transform;
        opponentMovement.Opponent = player.transform;

        FighterSparringAI ai = opponent.GetComponent<FighterSparringAI>();
        if (ai == null) ai = opponent.AddComponent<FighterSparringAI>();
        ai.Difficulty = AIDifficulty.Easy;

        FighterController playerController = player.GetComponent<FighterController>();
        FighterController opponentController = opponent.GetComponent<FighterController>();
        SetPrivateBool(playerController, "showOnScreenControls", true);
        SetPrivateBool(opponentController, "showOnScreenControls", false);

        TekkenCamera cameraRig = UnityEngine.Object.FindFirstObjectByType<TekkenCamera>();
        if (cameraRig == null && Camera.main != null) cameraRig = Camera.main.gameObject.AddComponent<TekkenCamera>();
        if (cameraRig != null)
        {
            cameraRig.Fighter1 = player.transform;
            cameraRig.Fighter2 = opponent.transform;
        }

        GameFlowController flow = UnityEngine.Object.FindFirstObjectByType<GameFlowController>();
        if (flow == null) flow = new GameObject("GameFlow").AddComponent<GameFlowController>();
        SerializedObject flowSo = new SerializedObject(flow);
        SerializedProperty prefabsProperty = flowSo.FindProperty("characterPrefabs");
        prefabsProperty.arraySize = Characters.Length;
        for (int i = 0; i < Characters.Length; i++)
            prefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[Characters[i].Name];
        SerializedProperty victoryProperty = flowSo.FindProperty("victoryClips");
        string[] victoryFiles = { "tigas.fbx", "isaac.fbx", "lusca.fbx", "enomoto.fbx", "jompis.fbx", "gabutas.fbx", "ricardin.fbx" };
        victoryProperty.arraySize = victoryFiles.Length;
        for (int i = 0; i < victoryFiles.Length; i++)
            victoryProperty.GetArrayElementAtIndex(i).objectReferenceValue = LoadClip("Assets/Animations/Mixamo/" + victoryFiles[i]);
        flowSo.FindProperty("defeatedClip").objectReferenceValue = LoadClip("Assets/Animations/Mixamo/X Bot@Dying.fbx");
        flowSo.FindProperty("fightSceneName").stringValue = "FightingPrototype";
        flowSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(flow);

        Selection.activeGameObject = player;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TestScene);
    }

    private static void SetPrivateBool(UnityEngine.Object target, string propertyName, bool value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null) property.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
#endif
