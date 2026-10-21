using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum FighterAttackType
{
    None,
    Punch,
    Attack2
}

[Serializable]
// Each prefab owns independent animation timing and playback tuning.
public sealed class FighterAttackTiming
{
    [Range(0f, 1f)] public float activeStartNormalized = 0.35f;
    [Range(0f, 1f)] public float activeEndNormalized = 0.5f;
    [Range(0.1f, 1.25f)] public float recoveryEndNormalized = 0.82f;
    [Min(0.1f)] public float playbackSpeed = 1f;
    public HitboxLimb hitboxLimb = HitboxLimb.RightHand;
}

/// <summary>
/// Controlador principal do lutador.
/// Gerencia a Máquina de Estados Finitos (FSM), coordena movimentação, animações,
/// sistema de combate com Hitboxes/Hurtboxes, Hitstop (frame freeze) e gestão de saúde.
/// </summary>
[RequireComponent(typeof(FighterMovement))]
[RequireComponent(typeof(HealthSystem))]
[DisallowMultipleComponent]
public partial class FighterController : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("Referência opcional ao Animator. Se não atribuído, busca no GameObject ou nós filhos.")]
    [SerializeField] private Animator animator;

    [Header("Combat Hitboxes")]
    [Tooltip("Lista de Hitboxes presentes nos membros de ataque deste lutador.")]
    [SerializeField] private Hitbox[] hitboxes;

    [Header("Combat Stats")]
    [Tooltip("Dano padrão desferido por golpes básicos.")]
    [SerializeField, Min(0f)] private float defaultAttackDamage = 10f;

    [SerializeField, Min(0f)] private float secondaryAttackDamage = 15f;

    [Tooltip("Força padrão de knockback aplicada ao atingir o oponente.")]
    [SerializeField, Min(0f)] private float defaultKnockbackForce = 4.0f;

    [Tooltip("Duração total do golpe (ativação + recovery) antes de voltar ao estado Neutro.")]
    [SerializeField, Min(0.05f)] private float attackDuration = 0.85f;

    [SerializeField, Min(0.05f)] private float secondaryAttackDuration = 1.0f;

    [Header("Per-animation attack timing")]
    [SerializeField] private FighterAttackTiming primaryAttackTiming = new FighterAttackTiming();
    [SerializeField] private FighterAttackTiming secondaryAttackTiming = new FighterAttackTiming
    {
        activeStartNormalized = 0.45f,
        activeEndNormalized = 0.58f,
        recoveryEndNormalized = 0.98f,
        hitboxLimb = HitboxLimb.RightFoot
    };

    [Tooltip("Duração padrão do congelamento por dano (Hit Stun) quando não especificado pelo golpe.")]
    [SerializeField, Min(0.05f)] private float defaultHitStunDuration = 0.55f;

    [Tooltip("Duração padrão do congelamento de quadros no impacto (Hitstop / Frame Freeze).")]
    [SerializeField, Range(0.02f, 0.2f)] private float defaultHitstopDuration = 0.08f;

    [Header("Input Actions")]
    [Tooltip("Ação de ataque do novo Input System. Opcional: cria fallback automático se nulo.")]
    [SerializeField] private InputActionReference attackActionReference;

    [Header("Input Configuration")]
    [Tooltip("Configuração centralizada de mapeamento de entrada para este lutador.")]
    [SerializeField] private FighterInputConfig inputConfig;

    [Header("Input Buffer")]
    [Tooltip("Janela de tolerância para armazenar comando de ataque durante HitStun (em segundos).")]
    [SerializeField, Min(0.01f)] private float hitStunInputBufferDuration = 0.15f;

    [Header("Animation State / Trigger Names")]
    [SerializeField] private string neutralAnimName = "Idle";
    [SerializeField] private string attackAnimName = "Attack";
    [SerializeField] private string attack2AnimName = "Attack2";
    [SerializeField] private string hitStunAnimName = "HitStun";
    [SerializeField] private string knockoutAnimName = "Dying";
    [SerializeField] private string turn180AnimName = "Turn180";

    [Header("Debug")]
    [SerializeField] private bool showOnScreenControls = true;
    [SerializeField] private string currentStateDebug;

    // Componentes e referências em cache
    private FighterMovement movement;
    private HealthSystem healthSystem;
    private InputAction runtimeAttackAction;
    private InputAction runtimeAttack2Action;
    private Coroutine hitstopCoroutine;
    private readonly Dictionary<HitboxLimb, Hitbox> hitboxMap = new Dictionary<HitboxLimb, Hitbox>();

    // Input buffer para registrar comandos durante HitStun
    private FighterAttackType bufferedAttackCommand = FighterAttackType.None;
    private float bufferedAttackTimestamp = -1f;

    // Edge-detection para inputs
    private bool wasPunchHeld;
    private bool wasAttack2Held;
    private bool wasMouseHeld;
    // Cache de referências e nomes do oponente para OnGUI e combate (Zero GC)
    private Transform cachedOpponentTransform;
    private FighterController cachedOpponentController;
    private HealthSystem cachedOpponentHealth;
    private FighterSparringAI cachedOpponentAI;
    private string cachedP1Name;
    private string cachedP2Name = "IA";

    // Hashes numéricos de animação
    public int NeutralAnimHash { get; private set; }
    public int AttackAnimHash { get; private set; }
    public int Attack2AnimHash { get; private set; }
    public int HitStunAnimHash { get; private set; }
    public int KnockoutAnimHash { get; private set; }
    public int Turn180AnimHash { get; private set; }

    // Instâncias cacheadas dos estados FSM (Zero GC em transições)
    public NeutralState NeutralState { get; private set; } = new NeutralState();
    public AttackState AttackState { get; private set; } = new AttackState();
    public HitStunState HitStunState { get; private set; } = new HitStunState();
    public KnockoutState KnockoutState { get; private set; } = new KnockoutState();

    // Estado ativo
    public IFighterState CurrentState { get; private set; }
    public string CurrentStateName => currentStateDebug;


    /// <summary>
    /// Indica se o congelamento de quadros (hitstop) está ativo no momento.
    /// </summary>
    public bool IsInHitstop => hitstopCoroutine != null;

    /// <summary>
    /// Indica se o lutador está em estado de ataque, reação a golpe ou nocaute (ações que travam a virada).
    /// </summary>
    public bool IsInAttackOrHitStun => CurrentState is AttackState || CurrentState is HitStunState || CurrentState is KnockoutState;

    // Getters públicos
    public FighterMovement Movement => movement;
    public FighterInputConfig InputConfig { get => inputConfig; set => inputConfig = value; }
    public HealthSystem HealthSystem => healthSystem;
    public Animator Animator => animator;
    public float AttackDuration => attackDuration;
    public float SecondaryAttackDuration => secondaryAttackDuration;
    public FighterAttackTiming PrimaryAttackTiming => primaryAttackTiming;
    public FighterAttackTiming SecondaryAttackTiming => secondaryAttackTiming;
    public float CurrentAttackDuration
    {
        get
        {
            float fallback = ActiveAttackType == FighterAttackType.Attack2 ? secondaryAttackDuration : attackDuration;
            if (animator == null || !animator.isActiveAndEnabled) return fallback;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            return state.length > 0.05f ? state.length : fallback;
        }
    }
    public FighterAttackTiming CurrentAttackTiming => ActiveAttackType == FighterAttackType.Attack2
        ? secondaryAttackTiming
        : primaryAttackTiming;

    /// <summary>Nome do golpe configurado para a hitbox, usado somente no Gizmo.</summary>
    public string GetAttackDebugName(HitboxLimb limb)
    {
        bool primaryUsesLimb = primaryAttackTiming != null && primaryAttackTiming.hitboxLimb == limb;
        bool secondaryUsesLimb = secondaryAttackTiming != null && secondaryAttackTiming.hitboxLimb == limb;

        if (primaryUsesLimb && secondaryUsesLimb) return $"{attackAnimName} / {attack2AnimName}";
        if (primaryUsesLimb) return attackAnimName;
        if (secondaryUsesLimb) return attack2AnimName;
        return "Sem golpe";
    }
    public int CurrentAttackAnimHash => ActiveAttackType == FighterAttackType.Attack2 ? Attack2AnimHash : AttackAnimHash;
    public FighterAttackType ActiveAttackType { get; private set; } = FighterAttackType.Punch;
    public float DefaultHitStunDuration => defaultHitStunDuration;
    public float DefaultHitstopDuration => defaultHitstopDuration;

    private void Awake()
    {
        movement = GetComponent<FighterMovement>();
        healthSystem = GetComponent<HealthSystem>();

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        RefreshHitboxCache();

        // Sincroniza configuração de entrada com o FighterMovement
        if (inputConfig == null)
        {
            inputConfig = movement != null && movement.InputConfig != null
                ? movement.InputConfig
                : FighterInputConfig.CreatePlayerOne();
        }

        NeutralAnimHash = Animator.StringToHash(neutralAnimName);
        AttackAnimHash = Animator.StringToHash(attackAnimName);
        Attack2AnimHash = Animator.StringToHash(attack2AnimName);
        HitStunAnimHash = Animator.StringToHash(hitStunAnimName);
        KnockoutAnimHash = Animator.StringToHash(knockoutAnimName);
        Turn180AnimHash = Animator.StringToHash(turn180AnimName);

        // Estados FSM ja instanciados nas propriedades




        if (movement != null && movement.IsPlayerControlled)
        {
            InitializeAttackInput();
        }
    }

    private void OnEnable()
    {
        if (movement != null && movement.IsPlayerControlled)
        {
            if (attackActionReference != null && attackActionReference.action != null)
            {
                attackActionReference.action.actionMap?.Enable();
                attackActionReference.action.Enable();
            }
            runtimeAttackAction?.Enable();
            runtimeAttack2Action?.Enable();
        }
    }

    private void OnDisable()
    {
        runtimeAttackAction?.Disable();
        runtimeAttack2Action?.Disable();
        DisableAllHitboxes();

        if (hitstopCoroutine != null)
        {
            StopCoroutine(hitstopCoroutine);
            hitstopCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (attackActionReference == null && runtimeAttackAction != null)
        {
            runtimeAttackAction.Dispose();
        }
        runtimeAttack2Action?.Dispose();
    }

    private void Start()
    {
        ChangeState(NeutralState);
    }

    private void Update()
    {
        CurrentState?.Update(this);
    }

    public void SetAttackRootMotion(bool enabled)
    {
        if (animator != null) animator.applyRootMotion = enabled;
    }

    public void SetAnimatorSpeed(float speed)
    {
        if (animator != null) animator.speed = Mathf.Max(0.1f, speed);
    }

    private void OnAnimatorMove()
    {
        if (animator == null || !animator.applyRootMotion || movement == null || movement.CharacterController == null) return;
        movement.ApplyAttackRootMotion(animator.deltaPosition);
    }

    public void RefreshHitboxCache()
    {
        hitboxMap.Clear();
        if (hitboxes == null || hitboxes.Length == 0)
        {
            hitboxes = GetComponentsInChildren<Hitbox>(true);
        }

        foreach (var hb in hitboxes)
        {
            if (hb != null)
            {
                hb.Owner = this;
                hitboxMap[hb.LimbType] = hb;
            }
        }
    }

    public void ChangeState(IFighterState newState)
    {
        if (newState == null || CurrentState == newState) return;

        CurrentState?.Exit(this);
        CurrentState = newState;
        currentStateDebug = newState.GetType().Name;
        CurrentState.Enter(this);
    }

    public void CrossFadeAnimation(int animHash, float transitionDuration = 0.05f)
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.Play(animHash, 0, 0f);
        }
    }

    public void TriggerAttack()
    {
        TriggerAttack(FighterAttackType.Punch);
    }

    public void TriggerSecondaryAttack()
    {
        // playbackSpeed dos prefabs pode ser muito maior que 1 — sempre usa 1f para tempo natural
        SetAnimatorSpeed(1f);
        TriggerAttack(FighterAttackType.Attack2);
    }

    public void TriggerAttack(FighterAttackType attackType)
    {
        if (healthSystem != null && healthSystem.IsDead) return;

        if (CurrentState is NeutralState)
        {
            ActiveAttackType = attackType == FighterAttackType.Attack2
                ? FighterAttackType.Attack2
                : FighterAttackType.Punch;
            ChangeState(AttackState);
        }
    }

    /// <summary>
    /// Guarda o último comando de ataque pressionado durante o HitStun no buffer.
    /// Não cancela o HitStun.
    /// </summary>
    public void BufferAttackInput()
    {
        FighterAttackType attack = ReadAttackCommand();
        if (attack != FighterAttackType.None)
        {
            bufferedAttackCommand = attack;
            bufferedAttackTimestamp = Time.time;
        }
    }

    /// <summary>
    /// Verifica se há um comando de ataque no buffer que ainda não expirou.
    /// </summary>
    public bool HasBufferedAttack()
    {
        if (bufferedAttackCommand == FighterAttackType.None) return false;

        if (Time.time - bufferedAttackTimestamp <= hitStunInputBufferDuration)
        {
            return true;
        }

        // Buffer expirou
        ClearAttackBuffer();
        return false;
    }

    /// <summary>
    /// Consome e retorna o comando de ataque armazenado no buffer, limpando-o em seguida.
    /// </summary>
    public FighterAttackType ConsumeBufferedAttack()
    {
        if (HasBufferedAttack())
        {
            FighterAttackType attack = bufferedAttackCommand;
            ClearAttackBuffer();
            return attack;
        }

        ClearAttackBuffer();
        return FighterAttackType.None;
    }

    /// <summary>
    /// Limpa o buffer de ataque.
    /// </summary>
    public void ClearAttackBuffer()
    {
        bufferedAttackCommand = FighterAttackType.None;
        bufferedAttackTimestamp = -1f;
    }

    /// <summary>
    /// Trava completamente as aÃ§Ãµes do lutador: interrompe locomoÃ§Ã£o, desativa todas as hitboxes e limpa buffers.
    /// Utilizado ao entrar em HitStun, Knockout ou estados restritivos.
    /// </summary>
    public void LockDown()
    {
        if (movement != null) movement.CanMove = false;
        DisableAllHitboxes();
        ClearAttackBuffer();
    }

    public void EnableCurrentAttackHitboxes()
    {
        float damage = ActiveAttackType == FighterAttackType.Attack2
            ? secondaryAttackDamage
            : defaultAttackDamage;
        float knockbackMultiplier = ActiveAttackType == FighterAttackType.Attack2 ? 1.35f : 1f;
        DamageData data = new DamageData(
            damage,
            defaultHitStunDuration,
            transform.forward * defaultKnockbackForce * knockbackMultiplier,
            this,
            defaultHitstopDuration
        );

        EnableHitbox(CurrentAttackTiming.hitboxLimb, data);
    }

    public bool TryGetCurrentAttackProgress(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (animator == null || !animator.isActiveAndEnabled) return false;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash != CurrentAttackAnimHash) return false;
        normalizedTime = state.normalizedTime;
        return true;
    }

    public void TriggerKnockout()
    {
        ChangeState(KnockoutState);
    }

    public void TriggerTurn180()
    {
        if (healthSystem != null && healthSystem.IsDead) return;

        CrossFadeAnimation(Turn180AnimHash, 0.1f);
    }

    // ========================================================================
    // HITSTOP (FRAME FREEZE FEEDBACK)
    // ========================================================================

    public void ApplyHitstop(float duration)
    {
        if (duration <= 0f || animator == null) return;

        if (hitstopCoroutine != null)
        {
            StopCoroutine(hitstopCoroutine);
        }

        hitstopCoroutine = StartCoroutine(HitstopRoutine(duration));
    }

    private IEnumerator HitstopRoutine(float duration)
    {
        float previousSpeed = animator.speed;
        animator.speed = 0f;

        yield return new WaitForSecondsRealtime(duration);

        if (animator != null)
        {
            animator.speed = previousSpeed > 0f ? previousSpeed : 1f;
        }

        hitstopCoroutine = null;
    }

    // ========================================================================
    // MÉTODOS PÚBLICOS PARA ANIMATION EVENTS
    // ========================================================================

    public void EnableHitbox(string limbName)
    {
        if (Enum.TryParse(limbName, true, out HitboxLimb limb))
        {
            EnableHitbox(limb);
        }
        else
        {
            Debug.LogWarning($"[FighterController] Membro inválido no Animation Event: {limbName}", this);
        }
    }

    public void DisableHitbox(string limbName)
    {
        if (Enum.TryParse(limbName, true, out HitboxLimb limb))
        {
            DisableHitbox(limb);
        }
    }

    public void EnableHitbox(HitboxLimb limb)
    {
        Vector3 knockbackDir = transform.forward * defaultKnockbackForce;
        DamageData defaultData = new DamageData(
            defaultAttackDamage,
            defaultHitStunDuration,
            knockbackDir,
            this,
            defaultHitstopDuration
        );
        EnableHitbox(limb, defaultData);
    }

    public void EnableHitbox(HitboxLimb limb, DamageData data)
    {
        if (hitboxMap.TryGetValue(limb, out var hb))
        {
            hb.Activate(data);
        }
    }

    public void DisableHitbox(HitboxLimb limb)
    {
        if (hitboxMap.TryGetValue(limb, out var hb))
        {
            hb.Deactivate();
        }
    }

    public void DisableAllHitboxes()
    {
        if (hitboxes == null) return;
        foreach (var hb in hitboxes)
        {
            if (hb != null && hb.IsActive)
            {
                hb.Deactivate();
            }
        }
    }

    // ========================================================================
    // PROCESSAMENTO DE DANO RECEBIDO VIA HURTBOX
    // ========================================================================

    public void TakeDamage(DamageData data, Hitbox sourceHitbox)
    {
        ApplyDamage(data, sourceHitbox);
    }

    public void ApplyDamage(DamageData data, Hitbox sourceHitbox)
    {
        if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
        if (movement == null) movement = GetComponent<FighterMovement>();

        float hitstopTime = data.hitstopDuration > 0f ? data.hitstopDuration : defaultHitstopDuration;
        ApplyHitstop(hitstopTime);

        if (data.attacker != null)
        {
            data.attacker.ApplyHitstop(hitstopTime);
        }

        if (healthSystem != null)
        {
            healthSystem.TakeDamage(data);
        }

        if (movement != null && data.knockback.sqrMagnitude > 0.01f) movement.ApplyImpulse(data.knockback);

        if (healthSystem == null || !healthSystem.IsDead)
        {
            TakeHit(data.hitStunDuration);
        }
    }

    public void TakeHit(float stunDuration = -1f)
    {
        DisableAllHitboxes();

        if (stunDuration > 0f)
        {
            HitStunState.SetStunDuration(stunDuration);
        }

        ChangeState(HitStunState);
    }

    public FighterAttackType ReadAttackCommand()
    {
        if (healthSystem != null && healthSystem.IsDead) return FighterAttackType.None;
        if (movement != null && !movement.IsPlayerControlled) return FighterAttackType.None;

        // Garante sincronia com o InputConfig do Movement
        if (movement != null && movement.InputConfig != null && inputConfig != movement.InputConfig)
        {
            inputConfig = movement.InputConfig;
        }

        // Lê comandos de ataque da configuração centralizada do jogador (sem teclas hardcoded nem conflito P1/P2)
        if (inputConfig != null)
        {
            FighterAttackType attack = inputConfig.ReadAttack(ref wasPunchHeld, ref wasAttack2Held, out string detected);
            if (attack != FighterAttackType.None)
            {
                return attack;
            }
        }

        if (Mouse.current != null && (inputConfig == null || inputConfig.PlayerIndex == 1))
        {
            bool isMouse = Mouse.current.leftButton.isPressed;
            bool triggered = isMouse && !wasMouseHeld;
            wasMouseHeld = isMouse;
            if (triggered)
            {
                return FighterAttackType.Punch;
            }
        }

        if (runtimeAttack2Action != null && (runtimeAttack2Action.triggered || runtimeAttack2Action.WasPressedThisFrame()))
        {
            return FighterAttackType.Attack2;
        }

        if (runtimeAttackAction != null && (runtimeAttackAction.triggered || runtimeAttackAction.WasPressedThisFrame()))
        {
            return FighterAttackType.Punch;
        }

        return FighterAttackType.None;
    }

    public bool IsAttackTriggered() => ReadAttackCommand() == FighterAttackType.Punch;

    private void InitializeAttackInput()
    {
        if (attackActionReference != null && attackActionReference.action != null)
        {
            runtimeAttackAction = attackActionReference.action;
            attackActionReference.action.actionMap?.Enable();
            runtimeAttackAction.Enable();
        }
        else
        {
            runtimeAttackAction = new InputAction(name: "Attack", type: InputActionType.Button);
            // Bug C corrigido: buttonSouth exclusivo para Punch (A / Cruz)
            runtimeAttackAction.AddBinding("<Gamepad>/buttonSouth");
            runtimeAttackAction.Enable();
        }

        runtimeAttack2Action = new InputAction(name: "Attack2", type: InputActionType.Button);
        // Bug C corrigido: buttonWest exclusivo para Attack2 (X / Quadrado)
        runtimeAttack2Action.AddBinding("<Gamepad>/buttonWest");
        runtimeAttack2Action.Enable();
    }

    public void ResetMatch()
    {
        // Se o fluxo da partida estiver ativo, reseta posições, rotações e estados via GameFlowController (Bug A)
        if (GameFlowController.Instance != null && GameFlowController.Instance.HasInitialTransforms)
        {
            GameFlowController.Instance.ResetRound();
            return;
        }
        ClearAttackBuffer();
        if (healthSystem != null) healthSystem.ResetHealth();
        if (movement != null) movement.ResetMotion();
        ChangeState(NeutralState);

        if (movement != null && movement.Opponent != null)
        {
            FighterController op = cachedOpponentController;
            if (op == null && movement != null && movement.Opponent != null)
            {
                movement.Opponent.TryGetComponent(out op);
                cachedOpponentController = op;
            }
            if (op != null)
            {
                if (op.HealthSystem != null) op.HealthSystem.ResetHealth();
                if (op.Movement != null) op.Movement.ResetMotion();
                op.ClearAttackBuffer();
                op.ChangeState(op.NeutralState);
            }
        }
    }
}
