using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Configuração centralizada de mapeamento de entradas (teclado e gamepad) por jogador.
/// Evita teclas hardcoded e conflitos entre Player 1 e Player 2.
/// </summary>
[Serializable]
public class FighterInputConfig
{
    [Tooltip("Identificador do jogador (1 para P1, 2 para P2).")]
    [SerializeField] private int playerIndex = 1;

    [Header("Movimentação no Teclado")]
    [SerializeField] private Key moveUp = Key.W;
    [SerializeField] private Key moveDown = Key.S;
    [SerializeField] private Key moveLeft = Key.A;
    [SerializeField] private Key moveRight = Key.D;

    [Header("Ataques no Teclado")]
    [SerializeField] private Key punchKey = Key.F;
    [SerializeField] private Key punchKeyAlt = Key.Space;
    [SerializeField] private Key attack2Key = Key.G;
    [SerializeField] private Key attack2KeyAlt = Key.None;

    [Header("Gamepad")]
    [SerializeField] private bool enableGamepad = true;
    [Tooltip("Índice do controle (0 para primeiro gamepad, 1 para segundo gamepad).")]
    [SerializeField] private int gamepadIndex = 0;

    [Header("Injeção / Simulação de Entradas (Harness de Teste / IA)")]
    private Vector2 simulatedMovement;
    private bool simulatedPunch;
    private bool simulatedAttack2;

    public int PlayerIndex => playerIndex;
    public Key MoveUp => moveUp;
    public Key MoveDown => moveDown;
    public Key MoveLeft => moveLeft;
    public Key MoveRight => moveRight;
    public Key PunchKey => punchKey;
    public Key PunchKeyAlt => punchKeyAlt;
    public Key Attack2Key => attack2Key;
    public Key Attack2KeyAlt => attack2KeyAlt;
    public int GamepadIndex => gamepadIndex;

    public Vector2 SimulatedMovement { get => simulatedMovement; set => simulatedMovement = value; }
    public bool SimulatedPunch { get => simulatedPunch; set => simulatedPunch = value; }
    public bool SimulatedAttack2 { get => simulatedAttack2; set => simulatedAttack2 = value; }

    /// <summary>
    /// Lê a entrada de movimentação combinando teclado e gamepad atribuído.
    /// </summary>
    public Vector2 ReadMovement()
    {
        // 0. Injeção de movimento simulado (harness de teste / IA)
        if (simulatedMovement.sqrMagnitude > 0.001f)
        {
            return Vector2.ClampMagnitude(simulatedMovement, 1f);
        }

        Vector2 input = Vector2.zero;

        // Leitura de teclado
        if (Keyboard.current != null)
        {
            if (IsKeyPressed(moveLeft)) input.x -= 1f;
            if (IsKeyPressed(moveRight)) input.x += 1f;
            if (IsKeyPressed(moveUp)) input.y += 1f;
            if (IsKeyPressed(moveDown)) input.y -= 1f;
        }

        // Leitura de gamepad correspondente
        Gamepad pad = GetGamepad();
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            Vector2 dpad = pad.dpad.ReadValue();
            Vector2 padDirection = stick.sqrMagnitude > dpad.sqrMagnitude ? stick : dpad;
            if (padDirection.sqrMagnitude > 0.04f)
            {
                input = padDirection;
            }
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    /// <summary>
    /// Lê comandos de ataque discretos (edge-detection) sem sobreposição de botões.
    /// </summary>
    public FighterAttackType ReadAttack(ref bool wasPunchHeld, ref bool wasAttack2Held, out string detectedInput)
    {
        detectedInput = "Nenhum";

        // 0. Leitura de ataques simulados (harness de teste) com edge-detection idêntico ao hardware
        if (simulatedPunch || simulatedAttack2)
        {
            bool punchDown = simulatedPunch;
            bool attack2Down = simulatedAttack2;

            bool punchTriggered = punchDown && !wasPunchHeld;
            bool attack2Triggered = attack2Down && !wasAttack2Held;

            wasPunchHeld = punchDown;
            wasAttack2Held = attack2Down;

            if (attack2Triggered)
            {
                detectedInput = $"P{playerIndex} Simulated Attack2";
                return FighterAttackType.Attack2;
            }

            if (punchTriggered)
            {
                detectedInput = $"P{playerIndex} Simulated Punch";
                return FighterAttackType.Punch;
            }
        }

        // 1. Leitura no teclado
        if (Keyboard.current != null)
        {
            bool punchDown = IsKeyPressed(punchKey) || IsKeyPressed(punchKeyAlt);
            bool attack2Down = IsKeyPressed(attack2Key) || IsKeyPressed(attack2KeyAlt);

            bool punchTriggered = punchDown && !wasPunchHeld;
            bool attack2Triggered = attack2Down && !wasAttack2Held;

            wasPunchHeld = punchDown;
            wasAttack2Held = attack2Down;

            // Prioriza ataque secundário se ambos dispararem no mesmo frame
            if (attack2Triggered)
            {
                detectedInput = $"P{playerIndex} Attack2 ({attack2Key})";
                return FighterAttackType.Attack2;
            }

            if (punchTriggered)
            {
                detectedInput = $"P{playerIndex} Punch ({punchKey})";
                return FighterAttackType.Punch;
            }
        }

        // 2. Leitura no Gamepad correspondente
        Gamepad pad = GetGamepad();
        if (pad != null)
        {
            // Bug C corrigido: buttonWest exclusivo para Attack2, buttonSouth exclusivo para Punch
            if (pad.buttonWest.wasPressedThisFrame)
            {
                detectedInput = $"P{playerIndex} Gamepad Attack2 (West / X)";
                return FighterAttackType.Attack2;
            }

            if (pad.buttonSouth.wasPressedThisFrame)
            {
                detectedInput = $"P{playerIndex} Gamepad Punch (South / A)";
                return FighterAttackType.Punch;
            }
        }

        return FighterAttackType.None;
    }

    /// <summary>
    /// Retorna o gamepad correspondente a este jogador.
    /// </summary>
    private Gamepad GetGamepad()
    {
        if (!enableGamepad || Gamepad.all.Count == 0) return null;
        if (gamepadIndex >= 0 && gamepadIndex < Gamepad.all.Count)
        {
            return Gamepad.all[gamepadIndex];
        }

        // Se houver apenas 1 controle e for Player 1, usa o controle conectado
        return playerIndex == 1 ? Gamepad.current : null;
    }

    /// <summary>
    /// Checagem segura de tecla pressionada.
    /// </summary>
    private bool IsKeyPressed(Key key)
    {
        if (key == Key.None || Keyboard.current == null) return false;
        try
        {
            return Keyboard.current[key].isPressed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Configuração padrão de fábrica para o Player 1:
    /// Teclado: WASD para andar, F (e Espaço) para Soco, G para Ataque Secundário.
    /// Gamepad: Gamepad 0 (A para Soco, X para Ataque Secundário).
    /// </summary>
    public static FighterInputConfig CreatePlayerOne()
    {
        return new FighterInputConfig
        {
            playerIndex = 1,
            moveUp = Key.W,
            moveDown = Key.S,
            moveLeft = Key.A,
            moveRight = Key.D,
            punchKey = Key.F,
            punchKeyAlt = Key.Space,
            attack2Key = Key.G,
            attack2KeyAlt = Key.None,
            enableGamepad = true,
            gamepadIndex = 0
        };
    }

    /// <summary>
    /// Configuração padrão de fábrica para o Player 2:
    /// Teclado: Setas para andar, Keypad 1 (ou J) para Soco, Keypad 2 (ou K) para Ataque Secundário.
    /// Gamepad: Gamepad 1 (A para Soco, X para Ataque Secundário).
    /// </summary>
    public static FighterInputConfig CreatePlayerTwo()
    {
        return new FighterInputConfig
        {
            playerIndex = 2,
            moveUp = Key.UpArrow,
            moveDown = Key.DownArrow,
            moveLeft = Key.LeftArrow,
            moveRight = Key.RightArrow,
            punchKey = Key.Numpad1,
            punchKeyAlt = Key.J,
            attack2Key = Key.Numpad2,
            attack2KeyAlt = Key.K,
            enableGamepad = true,
            gamepadIndex = 1
        };
    }
}
