using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fluxo UnDFight: gerencia menus, seleÃ§Ã£o de personagens, transiÃ§Ãµes de tela,
/// setup e reinÃ­cio de partidas e condiÃ§Ãµes de vitÃ³ria.
/// </summary>
public sealed partial class GameFlowController : MonoBehaviour
{
    private enum FlowScreen { Main, Mode, Select, VS, Fight, Victory }
    private enum GameMode { Cpu, Local }

    [SerializeField] private GameObject[] characterPrefabs;
    [SerializeField] private string fightSceneName = "FightingPrototype";
    [SerializeField] private AnimationClip[] victoryClips;
    [SerializeField] private AnimationClip defeatedClip;

    public GameObject[] CharacterPrefabs => characterPrefabs;
    public static string[] CharacterDisplayNames => DisplayNames;
    public static GameFlowController Instance => instance;
    public bool HasInitialTransforms => hasInitialTransforms;
    public FighterController CurrentPlayer => currentPlayer;
    public FighterController CurrentOpponent => currentOpponent;
    public bool IsVictoryScreen => screen == FlowScreen.Victory;
    public bool IsFightScreen => screen == FlowScreen.Fight;

    // Constantes e Hashes
    private static readonly Vector3 DefaultP1Position = new Vector3(-2.5f, 0f, 0f);
    private static readonly Vector3 DefaultP2Position = new Vector3(2.5f, 0f, 0f);
    private static readonly Quaternion DefaultP1Rotation = Quaternion.Euler(0f, 90f, 0f);
    private static readonly Quaternion DefaultP2Rotation = Quaternion.Euler(0f, -90f, 0f);
    private static readonly int DyingAnimHash = Animator.StringToHash("Dying");
    private static readonly int IdleAnimHash = Animator.StringToHash("Idle");
    private const int PreviewLayersMask = 0x7FFC0000;

    private static GameFlowController instance;
    private Vector3 initialPlayerPosition;
    private Quaternion initialPlayerRotation;
    private Vector3 initialOpponentPosition;
    private Quaternion initialOpponentRotation;
    private bool hasInitialTransforms;
    private static GameMode pendingMode;
    private static int pendingPlayer;
    private static int pendingOpponent;
    private static bool pendingFight;

    private Camera cachedMainCamera;
    private FlowScreen screen = FlowScreen.Main;
    private GameMode mode;
    private int playerSelection;
    private int opponentSelection = 1;
    private int focusedSelection;
    private bool choosingOpponent;
    private bool playerOneConfirmed;
    private float confirmationPulse;
    private float vsUntil;
    private bool victoryShown;
    private FighterController currentPlayer;
    private FighterController currentOpponent;
    private RuntimeAnimatorController originalPlayerAnimator;
    private RuntimeAnimatorController originalOpponentAnimator;
    private string victoryWinnerName;
    private string victoryLoserName;
    private int victoryOption;

    private static readonly string[] DisplayNames =
    {
        "Tiago", "Isaac", "Lucas", "Enomoto", "Jompi", "Gabutas", "Ricardinho"
    };

    private static readonly string[] FighterClasses =
    {
        "EQUILIBRADO", "VELOCISTA", "BRIGADOR", "ESTRATEGISTA",
        "ACROBATA", "PESO-PESADO", "IMPREVISIVEL"
    };

    private static readonly string[] FighterDescriptions =
    {
        "Lutador versatil, com bons ataques e defesa solida.",
        "Rapido e agil, domina o espaco com movimentos velozes.",
        "Pressao constante e golpes diretos de curta distancia.",
        "Controla o ritmo da luta e pune os erros do adversario.",
        "Movimentos amplos e acrobaticos, dificeis de prever.",
        "Ataques fortes e grande resistencia, mas menor velocidade.",
        "Estilo incomum que alterna velocidade, alcance e potencia."
    };

    private static readonly int[,] FighterStats =
    {
        { 4, 3, 4, 3, 4 }, { 3, 5, 3, 4, 3 }, { 4, 3, 4, 3, 3 },
        { 3, 3, 4, 4, 5 }, { 4, 4, 3, 4, 4 }, { 5, 2, 5, 3, 3 },
        { 4, 4, 3, 4, 4 }
    };

    private static readonly string[] StatNames = { "FORCA", "VELOCIDADE", "DEFESA", "ALCANCE", "TECNICA" };

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        screen = pendingFight ? FlowScreen.VS : FlowScreen.Main;
        if (!pendingFight) RemoveSceneFighters();
        StartCoroutine(LoadMenuAssets());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DestroyPreview(ref previewPlayerObject, ref previewPlayerCamera, ref previewPlayerTexture);
        DestroyPreview(ref previewOpponentObject, ref previewOpponentCamera, ref previewOpponentTexture);
        DestroyCardPreviews();
        DestroyPreview(ref victoryWinnerObject, ref victoryWinnerCamera, ref victoryWinnerTexture);
        DestroyPreview(ref victoryLoserObject, ref victoryLoserCamera, ref victoryLoserTexture);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!pendingFight || scene.name != fightSceneName) return;
        SetupFight();
        pendingFight = false;
        screen = FlowScreen.VS;
        vsUntil = Time.unscaledTime + 1.5f;
    }

    private void Update()
    {
        if (playerOneConfirmed) confirmationPulse += Time.unscaledDeltaTime;
        if (screen == FlowScreen.VS && Time.unscaledTime >= vsUntil) screen = FlowScreen.Fight;
        if (screen == FlowScreen.Fight && !victoryShown)
        {
            FighterController defeated = GetDefeatedFighter();
            if (defeated != null)
            {
                TriggerFighterKnockout(defeated);
                return;
            }
        }
        if (screen == FlowScreen.Victory)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow)) victoryOption = Mathf.Max(0, victoryOption - 1);
            if (Input.GetKeyDown(KeyCode.DownArrow)) victoryOption = Mathf.Min(2, victoryOption + 1);
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmVictoryOption();
            return;
        }
        if (screen != FlowScreen.Select) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (choosingOpponent) choosingOpponent = false;
            else
            {
                SetMainCameraPreviewVisibility(false);
                screen = FlowScreen.Mode;
            }
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) MoveFocus(-1, 0);
        if (Input.GetKeyDown(KeyCode.RightArrow)) MoveFocus(1, 0);
        if (Input.GetKeyDown(KeyCode.UpArrow)) MoveFocus(0, -1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) MoveFocus(0, 1);
        if (Input.GetKeyDown(KeyCode.R)) SelectRandomFighter();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) ConfirmCurrentSelection();
        if (screen == FlowScreen.Select) RefreshPreviews();
    }

    private void BeginSelection(GameMode selectedMode)
    {
        DestroyVictoryDisplays();
        mode = selectedMode;
        playerSelection = 0;
        opponentSelection = characterPrefabs != null && characterPrefabs.Length > 1 ? 1 : 0;
        focusedSelection = playerSelection;
        choosingOpponent = false;
        playerOneConfirmed = false;
        confirmationPulse = 0f;
        previewPlayer = previewOpponent = -1;
        screen = FlowScreen.Select;
        SetMainCameraPreviewVisibility(true);
        RefreshPreviews();
        CreateCardPreviews();
    }

    private void MoveFocus(int horizontal, int vertical)
    {
        int next = focusedSelection + horizontal + vertical * 3;
        if (next < 0 || next >= characterPrefabs.Length) return;
        if (choosingOpponent && next == playerSelection) return;
        focusedSelection = next;
        RefreshPreviews();
    }

    private void SelectRandomFighter()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;
        int candidate = focusedSelection;
        for (int attempt = 0; attempt < 12 && (candidate == focusedSelection || (choosingOpponent && candidate == playerSelection)); attempt++)
            candidate = Random.Range(0, characterPrefabs.Length);
        if (choosingOpponent && candidate == playerSelection) candidate = (playerSelection + 1) % characterPrefabs.Length;
        focusedSelection = candidate;
        RefreshPreviews();
    }

    private void ConfirmCurrentSelection()
    {
        if (characterPrefabs == null || characterPrefabs.Length == 0) return;
        if (!choosingOpponent)
        {
            playerSelection = focusedSelection;
            choosingOpponent = true;
            playerOneConfirmed = true;
            confirmationPulse = 0f;
            focusedSelection = opponentSelection == playerSelection ? (playerSelection + 1) % characterPrefabs.Length : opponentSelection;
            if (focusedSelection == playerSelection) focusedSelection = (focusedSelection + 1) % characterPrefabs.Length;
            RefreshPreviews();
            return;
        }
        if (focusedSelection == playerSelection) return;
        pendingMode = mode;
        pendingPlayer = playerSelection;
        pendingOpponent = focusedSelection;
        pendingFight = true;
        DestroyPreview(ref previewPlayerObject, ref previewPlayerCamera, ref previewPlayerTexture);
        DestroyPreview(ref previewOpponentObject, ref previewOpponentCamera, ref previewOpponentTexture);
        DestroyCardPreviews();
        SetupFight();
        pendingFight = false;
        screen = FlowScreen.VS;
        vsUntil = Time.unscaledTime + 1.5f;
    }

    private void ConfirmVictoryOption()
    {
        if (victoryOption == 0)
        {
            Rematch();
        }
        else if (victoryOption == 1)
        {
            victoryShown = false;
            DestroyVictoryDisplays();
            BeginSelection(pendingMode);
        }
        else
        {
            victoryShown = false;
            DestroyVictoryDisplays();
            RemoveSceneFighters();
            screen = FlowScreen.Main;
            SetMainCameraPreviewVisibility(false);
        }
    }

    private void SetupFight()
    {
        WireKnockoutEvents(currentPlayer, false);
        WireKnockoutEvents(currentOpponent, false);

        victoryShown = false;
        DestroyVictoryDisplays();
        foreach (FighterController fighter in FindObjectsByType<FighterController>(FindObjectsSortMode.None)) Destroy(fighter.gameObject);

        GameObject player = InstantiatePrefab(pendingPlayer);
        GameObject opponent = InstantiatePrefab(pendingOpponent);
        if (player == null || opponent == null)
        {
            if (player != null) Destroy(player);
            if (opponent != null) Destroy(opponent);
            Debug.LogError("UnDFight: não foi possível instanciar os prefabs dos lutadores selecionados.");
            screen = FlowScreen.Select;
            return;
        }

        player.name = "Player1_" + DisplayNames[pendingPlayer];
        opponent.name = (pendingMode == GameMode.Cpu ? "CPU_" : "Player2_") + DisplayNames[pendingOpponent];
        player.transform.SetPositionAndRotation(DefaultP1Position, DefaultP1Rotation);
        opponent.transform.SetPositionAndRotation(DefaultP2Position, DefaultP2Rotation);

        initialPlayerPosition = player.transform.position;
        initialPlayerRotation = player.transform.rotation;
        initialOpponentPosition = opponent.transform.position;
        initialOpponentRotation = opponent.transform.rotation;
        hasInitialTransforms = true;

        FighterMovement pm = player.GetComponent<FighterMovement>();
        FighterMovement om = opponent.GetComponent<FighterMovement>();
        currentPlayer = player.GetComponent<FighterController>();
        currentOpponent = opponent.GetComponent<FighterController>();

        originalPlayerAnimator = currentPlayer != null && currentPlayer.Animator != null ? currentPlayer.Animator.runtimeAnimatorController : null;
        originalOpponentAnimator = currentOpponent != null && currentOpponent.Animator != null ? currentOpponent.Animator.runtimeAnimatorController : null;

        WireKnockoutEvents(currentPlayer, true);
        WireKnockoutEvents(currentOpponent, true);

        pm.IsPlayerControlled = true; om.IsPlayerControlled = false;
        pm.Opponent = opponent.transform; om.Opponent = player.transform;

        if (pendingMode == GameMode.Cpu) opponent.AddComponent<FighterSparringAI>().Difficulty = AIDifficulty.Easy;
        else opponent.AddComponent<LocalPlayerTwoInput>();

        TekkenCamera camera = FindAnyObjectByType<TekkenCamera>();
        if (camera != null) { camera.Fighter1 = player.transform; camera.Fighter2 = opponent.transform; }

        SetMainCameraPreviewVisibility(false);
    }

    public void Rematch()
    {
        victoryShown = false;
        DestroyVictoryDisplays();
        ResetRound();
        screen = FlowScreen.Fight;
    }

    public void ResetRound()
    {
        RestoreOriginalAnimator(currentPlayer, originalPlayerAnimator);
        RestoreOriginalAnimator(currentOpponent, originalOpponentAnimator);

        if (currentPlayer != null && hasInitialTransforms)
        {
            ResetFighterPhysicsAndState(currentPlayer, initialPlayerPosition, initialPlayerRotation);
        }

        if (currentOpponent != null && hasInitialTransforms)
        {
            ResetFighterPhysicsAndState(currentOpponent, initialOpponentPosition, initialOpponentRotation);
        }

        SetMainCameraPreviewVisibility(false);
    }

    private static void RestoreOriginalAnimator(FighterController fighter, RuntimeAnimatorController original)
    {
        if (fighter != null && fighter.Animator != null && original != null)
        {
            fighter.Animator.runtimeAnimatorController = original;
            fighter.Animator.speed = 1f;
        }
    }

    private void WireKnockoutEvents(FighterController fighter, bool subscribe)
    {
        if (fighter != null && fighter.HealthSystem != null)
        {
            fighter.HealthSystem.OnKnockout -= OnFighterKnockout;
            if (subscribe)
            {
                fighter.HealthSystem.OnKnockout += OnFighterKnockout;
            }
        }
    }

    public static void ResetFighterPhysicsAndState(FighterController fighter, Vector3 initialPosition, Quaternion initialRotation)
    {
        if (fighter == null) return;

        CharacterController cc = fighter.Movement != null ? fighter.Movement.CharacterController : null;
        if (cc == null) fighter.TryGetComponent(out cc);
        if (cc != null) cc.enabled = false;

        fighter.transform.SetPositionAndRotation(initialPosition, initialRotation);

        if (cc != null) cc.enabled = true;

        if (fighter.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        FighterMovement movement = fighter.Movement;
        if (movement != null)
        {
            movement.ResetMotion();
            movement.CanMove = true;
        }

        if (fighter.HealthSystem != null)
        {
            fighter.HealthSystem.ResetHealth();
        }

        fighter.DisableAllHitboxes();
        fighter.ClearAttackBuffer();
        fighter.ChangeState(fighter.NeutralState);
    }

    private static bool IsFighterDefeated(FighterController fighter)
    {
        return fighter != null && (fighter.HealthSystem?.IsDead == true || fighter.HealthSystem?.CurrentHealth <= 0f || fighter.CurrentState is KnockoutState);
    }

    private FighterController GetDefeatedFighter()
    {
        if (IsFighterDefeated(currentPlayer)) return currentPlayer;
        if (IsFighterDefeated(currentOpponent)) return currentOpponent;
        return null;
    }

    private void OnFighterKnockout()
    {
        if (victoryShown) return;
        FighterController loser = GetDefeatedFighter();
        if (loser == null)
        {
            loser = currentPlayer != null && currentPlayer.HealthSystem != null && currentPlayer.HealthSystem.IsDead ? currentPlayer : currentOpponent;
        }
        TriggerFighterKnockout(loser);
    }

    private void TriggerFighterKnockout(FighterController loser)
    {
        if (victoryShown) return;
        victoryShown = true;
        StartCoroutine(ShowVictoryAfterKnockout(loser));
    }

    private IEnumerator ShowVictoryAfterKnockout(FighterController loser)
    {
        if (loser != null && loser.Animator != null)
        {
            float wait = 0f;
            while (wait < 1f && loser.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash != DyingAnimHash)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            float duration = loser.Animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSecondsRealtime(Mathf.Clamp(duration * .95f, 0.8f, 3f));
        }
        yield return new WaitForSecondsRealtime(.2f);

        FighterController winner = loser == currentPlayer ? currentOpponent : currentPlayer;
        victoryWinnerName = winner == currentPlayer ? DisplayNames[pendingPlayer] : DisplayNames[pendingOpponent];
        victoryLoserName = loser == currentPlayer ? DisplayNames[pendingPlayer] : DisplayNames[pendingOpponent];
        CreateVictoryDisplays(winner, loser);
        int winnerIndex = winner == currentPlayer ? pendingPlayer : pendingOpponent;
        int loserIndex = loser == currentPlayer ? pendingPlayer : pendingOpponent;
        PlayVictoryAnimation(winner, winnerIndex, true);
        PlayVictoryAnimation(loser, loserIndex, false);
        victoryOption = 0;
        screen = FlowScreen.Victory;
    }

    private GameObject InstantiatePrefab(int index)
    {
        if (characterPrefabs == null || index < 0 || index >= characterPrefabs.Length)
        {
            Debug.LogError("UnDFight: índice de personagem inválido: " + index);
            return null;
        }

        Object source = characterPrefabs[index];
        if (source == null)
        {
            Debug.LogError("UnDFight: prefab vazio no índice " + index);
            return null;
        }

        return Object.Instantiate(source) as GameObject;
    }

    private void RemoveSceneFighters()
    {
        foreach (FighterController fighter in FindObjectsByType<FighterController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Destroy(fighter.gameObject);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// Inicia uma luta imediatamente com os lutadores especificados para o harness de teste automatizado.
    /// Exclusivo para testes automatizados — nunca deve ser chamado pelo fluxo normal do jogo.
    /// </summary>
    public static void StartDirectFightForTesting(int p1Index = 0, int p2Index = 1, bool cpuMode = false)
    {
        pendingMode = cpuMode ? GameMode.Cpu : GameMode.Local;
        pendingPlayer = p1Index;
        pendingOpponent = p2Index;
        pendingFight = true;

        if (instance != null)
        {
            instance.DestroyVictoryDisplays();
            instance.DestroyPreview(ref instance.previewPlayerObject, ref instance.previewPlayerCamera, ref instance.previewPlayerTexture);
            instance.DestroyPreview(ref instance.previewOpponentObject, ref instance.previewOpponentCamera, ref instance.previewOpponentTexture);
            instance.DestroyCardPreviews();
            instance.SetupFight();
            instance.screen = FlowScreen.Fight;
        }
    }
#endif
}
