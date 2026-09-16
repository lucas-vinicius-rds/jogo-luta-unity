using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Movimento 2.5D: A/D movem, W salta e S agacha.</summary>
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class FighterMovement : MonoBehaviour
{
    [SerializeField] private Transform opponent;
    [SerializeField] private bool isPlayerControlled = true;
    [SerializeField, Min(0f)] private float forwardSpeed = 4.5f;
    [SerializeField, Min(0f)] private float backwardSpeed = 3.5f;
    [SerializeField, Min(0.1f)] private float minDistanceToOpponent = 0.75f;
    [SerializeField, Min(1f)] private float arenaHalfWidth = 9f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedGravity = -2f;
    [SerializeField, Min(0f)] private float jumpHeight = 2.2f;
    [SerializeField] private float laneZ;
    [SerializeField, Min(0f)] private float laneDepth = 0.35f;
    [SerializeField, Range(0f, 1f)] private float airControl = .62f;
    [SerializeField, Range(0f, 1f)] private float crouchSpeedMultiplier = .58f;
    [SerializeField, Min(0f)] private float impulseDrag = 9f;
    [SerializeField, Min(1f)] private float maxAttackRootMotionSpeed = 8f;
    [SerializeField] private InputActionReference moveActionReference;

    private CharacterController characterController;
    private Animator animator;
    private InputAction runtimeMoveAction;
    private float verticalVelocity;
    private bool wasJumpHeld;
    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 horizontalImpulse;

    public Transform Opponent { get => opponent; set => opponent = value; }
    public bool IsPlayerControlled { get => isPlayerControlled; set => isPlayerControlled = value; }
    public bool CanMove { get; set; } = true;
    public bool IsGrounded => characterController != null && characterController.isGrounded;
    public CharacterController CharacterController => characterController;
    public Vector2 CurrentInput { get; private set; }
    public float CurrentSpeedMagnitude => CurrentInput.magnitude;
    public int CurrentMoveDirection { get; private set; }
    public Vector2 ExternalInput { get; set; }
    public bool IsFacingAway => false;
    public bool IsCrouching { get; private set; }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        standingHeight = characterController.height;
        standingCenter = characterController.center;
        if (isPlayerControlled) InitializeInput();
    }

    private void OnEnable()
    {
        if (!isPlayerControlled) return;
        moveActionReference?.action?.actionMap?.Enable();
        moveActionReference?.action?.Enable();
        runtimeMoveAction?.Enable();
    }

    private void OnDisable() => runtimeMoveAction?.Disable();

    private void OnDestroy()
    {
        if (moveActionReference == null) runtimeMoveAction?.Dispose();
    }

    private void Update()
    {
        CurrentInput = ReadMovementInput();
        HandleJumpAndCrouch(CurrentInput);
        UpdateMovementAnimation(CurrentInput);
        FaceOpponent();

        float moveSpeed = CurrentMoveDirection >= 0 ? forwardSpeed : backwardSpeed;
        if (!characterController.isGrounded) moveSpeed *= airControl;
        if (IsCrouching) moveSpeed *= crouchSpeedMultiplier;
        Vector3 locomotion = Vector3.right * (CurrentInput.x * moveSpeed * Time.deltaTime);
        Vector3 impulseMovement = horizontalImpulse * Time.deltaTime;
        horizontalImpulse = Vector3.MoveTowards(horizontalImpulse, Vector3.zero, impulseDrag * Time.deltaTime);
        MoveSafely(locomotion + impulseMovement + CalculateVerticalMovement(), true);
    }

    private void InitializeInput()
    {
        if (moveActionReference?.action != null)
        {
            runtimeMoveAction = moveActionReference.action;
            runtimeMoveAction.actionMap?.Enable();
            runtimeMoveAction.Enable();
            return;
        }

        runtimeMoveAction = new InputAction("Movement", InputActionType.Value, expectedControlType: "Vector2");
        runtimeMoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        runtimeMoveAction.AddBinding("<Gamepad>/leftStick");
        runtimeMoveAction.Enable();
    }

    private Vector2 ReadMovementInput()
    {
        if (!CanMove) return Vector2.zero;
        if (ExternalInput.sqrMagnitude > 0.001f) return ExternalInput;
        if (!isPlayerControlled) return Vector2.zero;

        Vector2 input = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
        }
        if (input.sqrMagnitude < 0.001f && runtimeMoveAction?.enabled == true)
            input = runtimeMoveAction.ReadValue<Vector2>();
        return Vector2.ClampMagnitude(input, 1f);
    }

    private void HandleJumpAndCrouch(Vector2 input)
    {
        IsCrouching = input.y < -0.5f && characterController.isGrounded;
        characterController.height = IsCrouching ? standingHeight * 0.62f : standingHeight;
        characterController.center = IsCrouching
            ? new Vector3(standingCenter.x, standingCenter.y * 0.62f, standingCenter.z)
            : standingCenter;
        bool jumpHeld = input.y > 0.5f;
        if (jumpHeld && !wasJumpHeld && characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            int jumpDirection = GetRelativeMoveDirection(input.x);
            if (HasParameter("JumpType")) animator.SetInteger("JumpType", jumpDirection > 0 ? 2 : jumpDirection < 0 ? 1 : 0);
            if (HasParameter("Jump")) animator.SetTrigger("Jump");
        }
        wasJumpHeld = jumpHeld;
    }

    private void UpdateMovementAnimation(Vector2 input)
    {
        if (animator == null) return;
        CurrentMoveDirection = GetRelativeMoveDirection(input.x);
        if (HasParameter("Crouch")) animator.SetBool("Crouch", IsCrouching);
        if (HasParameter("CrouchDirection")) animator.SetInteger("CrouchDirection", CurrentMoveDirection);
        if (HasParameter("MoveDirection")) animator.SetInteger("MoveDirection", CurrentMoveDirection);
    }

    private int GetRelativeMoveDirection(float horizontalInput)
    {
        if (Mathf.Abs(horizontalInput) <= 0.15f) return 0;
        if (opponent == null) return horizontalInput > 0f ? 1 : -1;
        float towardOpponent = Mathf.Sign(opponent.position.x - transform.position.x);
        return horizontalInput * towardOpponent > 0f ? 1 : -1;
    }

    private bool HasParameter(string parameterName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName) return true;
        return false;
    }

    private Vector3 CalculateVerticalMovement()
    {
        if (characterController.isGrounded && verticalVelocity < 0f) verticalVelocity = groundedGravity;
        else verticalVelocity += gravity * Time.deltaTime;
        return Vector3.up * (verticalVelocity * Time.deltaTime);
    }

    // Retorna o delta Y sem acumular velocidade (usado por ApplyAttackRootMotion
    // para não duplicar o acréscimo já feito pelo Update no mesmo frame)
    private float GetCurrentVerticalDelta()
    {
        return verticalVelocity * Time.deltaTime;
    }

    public void ApplyImpulse(Vector3 velocity)
    {
        velocity.y = 0f;
        velocity.z = 0f;
        horizontalImpulse += velocity;
    }

    public void ApplyAttackRootMotion(Vector3 delta)
    {
        delta.y = 0f; // Nunca permite que Root Motion eleve o personagem no ar
        delta.z = 0f;
        float maxDelta = maxAttackRootMotionSpeed * Time.deltaTime;
        if (Mathf.Abs(delta.x) > maxDelta) delta.x = Mathf.Sign(delta.x) * maxDelta;
        // Usa o delta Y atual (já acumulado no Update) para manter o personagem no chão
        Vector3 withGravity = new Vector3(delta.x, GetCurrentVerticalDelta(), delta.z);
        MoveSafely(withGravity, true);
    }

    public void ResetMotion()
    {
        horizontalImpulse = Vector3.zero;
        verticalVelocity = groundedGravity;
    }

    private void MoveSafely(Vector3 delta, bool respectOpponentSpacing)
    {
        if (characterController == null) return;
        float desiredX = Mathf.Clamp(transform.position.x + delta.x, -arenaHalfWidth, arenaHalfWidth);
        if (respectOpponentSpacing && opponent != null)
        {
            float diff = transform.position.x - opponent.position.x;
            // Usa epsilon para evitar side=0 quando personagens se sobrepõem exatamente
            float side = Mathf.Abs(diff) > 0.01f ? Mathf.Sign(diff) : (transform.forward.x < 0f ? 1f : -1f);
            float boundary = opponent.position.x + side * minDistanceToOpponent;
            desiredX = side < 0f ? Mathf.Min(desiredX, boundary) : Mathf.Max(desiredX, boundary);
        }

        float desiredZ = Mathf.Clamp(transform.position.z + delta.z, laneZ - laneDepth, laneZ + laneDepth);
        Vector3 constrained = new Vector3(desiredX - transform.position.x, delta.y, desiredZ - transform.position.z);
        CollisionFlags flags = characterController.Move(constrained);
        if ((flags & CollisionFlags.Below) != 0 && verticalVelocity < 0f) verticalVelocity = groundedGravity;
    }

    private void FaceOpponent()
    {
        if (opponent == null) return;
        float direction = opponent.position.x >= transform.position.x ? 1f : -1f;
        transform.rotation = Quaternion.Euler(0f, direction > 0f ? 90f : -90f, 0f);
    }
}
