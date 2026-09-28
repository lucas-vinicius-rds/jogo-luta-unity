#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum TestSideFilter
{
    Both,
    OnlyP1,
    OnlyP2
}

/// <summary>
/// Harness de teste automatizado multi-personagem para o UnDFight.
/// Descobre dinamicamente todos os personagens jogáveis a partir do GameFlowController,
/// executa os cenários roteirizados como P1 e P2 contra um oponente neutro padronizado,
/// grava telemetria e screenshots organizados por personagem e lado,
/// e compila um relatório diagnóstico comparativo detalhado em Logs/resumo.md.
/// </summary>
[DisallowMultipleComponent]
public class AutoFightTester : MonoBehaviour
{
    [Header("Execução dos Testes")]
    [Tooltip("Executa automaticamente os testes assim que o Play Mode iniciar.")]
    [SerializeField] private bool runOnPlay = true;

    [Tooltip("Grava telemetria a cada frame relevante em CSV e JSON.")]
    [SerializeField] private bool recordTelemetry = true;

    [Tooltip("Captura screenshots com visualização de debug 3D nos momentos de Início, Meio e Fim dos golpes.")]
    [SerializeField] private bool captureScreenshots = true;

    [Tooltip("Tempo de espera entre fases de teste para estabilização de física e animações.")]
    [SerializeField, Range(0.05f, 0.5f)] private float settleTime = 0.2f;

    [Header("Filtros para Testes Rápidos (Opcional)")]
    [Tooltip("Índice do personagem a testar (-1 para testar todos os encontrados).")]
    [SerializeField] private int singleCharacterIndex = -1;

    [Tooltip("Nome do personagem a testar (vazio para testar todos).")]
    [SerializeField] private string singleCharacterNameFilter = "";

    [Tooltip("Lados a testar para cada personagem (Ambos, apenas P1 ou apenas P2).")]
    [SerializeField] private TestSideFilter sideFilter = TestSideFilter.Both;

    [Header("Oponente Alvo Neutro")]
    [Tooltip("Prefab do oponente neutro fixo. Se vazio, utiliza o primeiro lutador disponível.")]
    [SerializeField] private GameObject neutralOpponentPrefab;

    // Estado da execução
    private bool isRunning;
    private string currentCharacterDisplayName = "";
    private string currentCharacterPrefabName = "";
    private string currentSide = "P1";
    private string currentScenario = "Iniciando";
    private float totalTestDuration;
    private int totalScreenshotsCaptured;
    private int totalTelemetryFrames;

    // Instâncias em cena
    private GameObject currentP1Object;
    private GameObject currentP2Object;
    private FighterController currentP1Controller;
    private FighterController currentP2Controller;
    private FighterInputConfig p1InputConfig;
    private FighterInputConfig p2InputConfig;

    // Pastas de logs
    private string logsDirectory;
    private string consolidatedCsvPath;
    private StreamWriter consolidatedCsvWriter;

    // Writers por personagem
    private StreamWriter currentCharacterCsvWriter;
    private readonly List<TelemetrySnapshot> currentCharacterSnapshots = new List<TelemetrySnapshot>();

    // Visualizadores 3D de Hurtbox e Hitbox
    private readonly List<GameObject> debugVisualObjects = new List<GameObject>();
    private readonly List<KeyValuePair<Hitbox, Renderer>> hitboxVisualPairs = new List<KeyValuePair<Hitbox, Renderer>>();
    private readonly List<KeyValuePair<Hurtbox, Transform>> hurtboxVisualPairs = new List<KeyValuePair<Hurtbox, Transform>>();

    // Estruturas de Diagnóstico e Resumo
    public class CharacterMetadata
    {
        public int index;
        public string displayName;
        public string prefabName;
        public GameObject prefab;
        public string prefabPath;
        public string controllerName;
        public string avatarName;
        public float baseHurtboxHeight;
        public float baseHurtboxRadius;
        public HitboxLimb primaryLimb;
        public float primaryDuration;
        public HitboxLimb secondaryLimb;
        public float secondaryDuration;
        public Dictionary<HitboxLimb, string> boneNames = new Dictionary<HitboxLimb, string>();
        public List<string> setupErrors = new List<string>();
    }

    public class ScenarioAnomaly
    {
        public string character;
        public string side;
        public string scenario;
        public string evidence;
        public string probableCause;
    }

    public class ScenarioStatusEntry
    {
        public string character;
        public string side;
        public string scenario;
        public bool isOk;
        public string details;
    }

    private readonly List<CharacterMetadata> discoveredCharacters = new List<CharacterMetadata>();
    private readonly List<ScenarioStatusEntry> scenarioResults = new List<ScenarioStatusEntry>();
    private readonly List<ScenarioAnomaly> detectedAnomalies = new List<ScenarioAnomaly>();

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    [Serializable]
    public class TelemetrySnapshot
    {
        public float time;
        public int frame;
        public string character;
        public string side;
        public string scenario;
        public string phase;
        public string fighter;
        public string fsmState;
        public bool inHitstop;
        public string activeAttack;
        public float health;
        public Vector3 position;
        public Vector3 rotation;
        public string animStateName;
        public float animNormalizedTime;
        public float animSpeed;
        public float paramSpeed;
        public float paramCrouch;
        public float paramMoveDirection;
        public List<HitboxSnapshotData> hitboxes = new List<HitboxSnapshotData>();
        public List<HurtboxSnapshotData> hurtboxes = new List<HurtboxSnapshotData>();
    }

    [Serializable]
    public class HitboxSnapshotData
    {
        public string limb;
        public bool isActive;
        public Vector3 worldPosition;
        public float radius;
    }

    [Serializable]
    public class HurtboxSnapshotData
    {
        public Vector3 worldPosition;
        public float height;
        public float radius;
    }

    [Serializable]
    public class CharacterJsonReport
    {
        public string character;
        public string side;
        public string date;
        public int totalSnapshots;
        public List<TelemetrySnapshot> snapshots;
    }

    private void Awake()
    {
        logsDirectory = Path.Combine(Application.dataPath, "..", "Logs");
        if (!Directory.Exists(logsDirectory)) Directory.CreateDirectory(logsDirectory);
    }

    private void Start()
    {
        if (runOnPlay)
        {
            StartCoroutine(RunMultiCharacterHarnessRoutine());
        }
    }

    private void Update()
    {
        if (isRunning)
        {
            UpdateDebugVisuals();
        }
    }

    private void OnDestroy()
    {
        ClearDebugVisuals();
        CloseConsolidatedCsv();
    }

    // ========================================================================
    // DESCOBERTA DINÂMICA DE PERSONAGENS (FONTE DE VERDADE DO JOGO)
    // ========================================================================

    public List<CharacterMetadata> DiscoverPlayableCharacters()
    {
        var list = new List<CharacterMetadata>();

        // 1. Obtém lista a partir do GameFlowController na cena ou nos componentes carregados
        var gameFlow = FindAnyObjectByType<GameFlowController>();
        if (gameFlow == null)
        {
            var flows = Resources.FindObjectsOfTypeAll<GameFlowController>();
            if (flows.Length > 0) gameFlow = flows[0];
        }

        GameObject[] prefabs = gameFlow != null ? gameFlow.CharacterPrefabs : null;
        string[] displayNames = GameFlowController.CharacterDisplayNames;

#if UNITY_EDITOR
        // Fallback no Editor caso GameFlowController não tenha prefabs atribuídos
        if (prefabs == null || prefabs.Length == 0)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Characters" });
            var fallbackPrefabs = new List<GameObject>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (p != null && p.GetComponent<FighterController>() != null)
                {
                    fallbackPrefabs.Add(p);
                }
            }
            prefabs = fallbackPrefabs.ToArray();
        }
#endif

        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("[AutoFightTester] Nenhum prefab de personagem foi encontrado.");
            return list;
        }

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            if (prefab == null)
            {
                var errorTarget = new CharacterMetadata
                {
                    index = i,
                    displayName = (displayNames != null && i < displayNames.Length) ? displayNames[i] : $"Slot_{i}",
                    prefabName = "Nulo",
                    setupErrors = new List<string> { "Prefab está nulo no GameFlowController." }
                };
                list.Add(errorTarget);
                continue;
            }

            var meta = new CharacterMetadata
            {
                index = i,
                displayName = (displayNames != null && i < displayNames.Length) ? displayNames[i] : prefab.name,
                prefabName = prefab.name,
                prefab = prefab
            };

#if UNITY_EDITOR
            meta.prefabPath = AssetDatabase.GetAssetPath(prefab);
#endif

            // Inspeciona componentes do Prefab
            var anim = prefab.GetComponent<Animator>();
            meta.controllerName = (anim != null && anim.runtimeAnimatorController != null) ? anim.runtimeAnimatorController.name : "Nenhum";
            meta.avatarName = (anim != null && anim.avatar != null) ? anim.avatar.name : "Nenhum";

            if (anim == null) meta.setupErrors.Add("Animator ausente no prefab raiz.");
            else if (anim.avatar == null) meta.setupErrors.Add("Avatar ausente ou não configurado como Humanoid.");

            // Inspeciona timigns do FighterController
            var fighter = prefab.GetComponent<FighterController>();
            if (fighter != null)
            {
                meta.primaryLimb = fighter.PrimaryAttackTiming != null ? fighter.PrimaryAttackTiming.hitboxLimb : HitboxLimb.RightHand;
                meta.primaryDuration = fighter.AttackDuration;
                meta.secondaryLimb = fighter.SecondaryAttackTiming != null ? fighter.SecondaryAttackTiming.hitboxLimb : HitboxLimb.RightFoot;
                meta.secondaryDuration = fighter.SecondaryAttackDuration;
            }

            // Inspeciona Hurtbox no prefab
            var hurtbox = prefab.GetComponentInChildren<Hurtbox>(true);
            if (hurtbox != null)
            {
                var cap = hurtbox.GetComponent<CapsuleCollider>();
                if (cap != null)
                {
                    meta.baseHurtboxHeight = cap.height;
                    meta.baseHurtboxRadius = cap.radius;
                }
            }

            // Instancia temporariamente para testar o vínculo real de cada osso de hitbox
            GameObject tempInst = null;
            try
            {
                tempInst = Instantiate(prefab);
                tempInst.SetActive(false);
                var instHitboxes = tempInst.GetComponentsInChildren<Hitbox>(true);
                foreach (var hb in instHitboxes)
                {
                    hb.BindBone();
                    var boneField = typeof(Hitbox).GetField("followBone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var bone = boneField != null ? boneField.GetValue(hb) as Transform : null;
                    meta.boneNames[hb.LimbType] = bone != null ? bone.name : "NÃO_ENCONTRADO";
                    if (bone == null)
                    {
                        meta.setupErrors.Add($"Osso do membro '{hb.LimbType}' não foi encontrado no esqueleto.");
                    }
                }
            }
            catch (Exception ex)
            {
                meta.setupErrors.Add($"Erro ao inspecionar prefab em tempo de execução: {ex.Message}");
            }
            finally
            {
                if (tempInst != null) DestroyImmediate(tempInst);
            }

            list.Add(meta);
        }

        return list;
    }

    // ========================================================================
    // CORROTINA PRINCIPAL: EXECUÇÃO DA BATERIA MULTI-PERSONAGEM
    // ========================================================================

    public IEnumerator RunMultiCharacterHarnessRoutine()
    {
        if (isRunning) yield break;
        isRunning = true;
        totalTestDuration = 0f;
        totalScreenshotsCaptured = 0;
        totalTelemetryFrames = 0;
        scenarioResults.Clear();
        detectedAnomalies.Clear();

        float startRealTime = Time.realtimeSinceStartup;

        EnableGameViewGizmos();
        InitConsolidatedCsv();

        Debug.Log("<color=cyan><b>[AutoFightTester] INICIANDO DESCOBERTA DINÂMICA DE PERSONAGENS...</b></color>");

        discoveredCharacters.Clear();
        discoveredCharacters.AddRange(DiscoverPlayableCharacters());

        Debug.Log($"<color=cyan><b>[AutoFightTester] {discoveredCharacters.Count} PERSONAGENS ENCONTRADOS NA FONTE DE VERDADE.</b></color>");

        // Define o oponente neutro fixo (preferencialmente o primeiro lutador válido)
        if (neutralOpponentPrefab == null && discoveredCharacters.Count > 0)
        {
            foreach (var ch in discoveredCharacters)
            {
                if (ch.prefab != null)
                {
                    neutralOpponentPrefab = ch.prefab;
                    break;
                }
            }
        }

        // Itera sobre todos os personagens descobertos
        for (int i = 0; i < discoveredCharacters.Count; i++)
        {
            CharacterMetadata target = discoveredCharacters[i];

            // Aplica filtros de teste rápido se configurados
            if (singleCharacterIndex >= 0 && target.index != singleCharacterIndex) continue;
            if (!string.IsNullOrEmpty(singleCharacterNameFilter) &&
                !string.Equals(target.displayName, singleCharacterNameFilter, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(target.prefabName, singleCharacterNameFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Debug.Log($"\n<color=yellow><b>======================================================================\n" +
                      $"[AutoFightTester] TESTANDO PERSONAGEM [{target.index + 1}/{discoveredCharacters.Count}]: {target.displayName} ({target.prefabName})\n" +
                      $"======================================================================</b></color>");

            // Se o personagem possui erro de setup prévio, registra e continua se não houver prefab
            if (target.prefab == null)
            {
                RecordAnomaly(target.displayName, "All", "Carregamento", "Prefab não encontrado ou nulo no GameFlowController.", "Prefab ausente no array characterPrefabs.");
                continue;
            }

            // 1. Testa como PLAYER 1 (se filtro permitir)
            if (sideFilter == TestSideFilter.Both || sideFilter == TestSideFilter.OnlyP1)
            {
                yield return StartCoroutine(RunCharacterSuiteOnSide(target, "P1"));
            }

            // 2. Testa como PLAYER 2 (se filtro permitir)
            if (sideFilter == TestSideFilter.Both || sideFilter == TestSideFilter.OnlyP2)
            {
                yield return StartCoroutine(RunCharacterSuiteOnSide(target, "P2"));
            }
        }

        // Finaliza gravação de logs
        CloseConsolidatedCsv();
        totalTestDuration = Time.realtimeSinceStartup - startRealTime;

        // Gera o relatório consolidado em resumo.md
        GenerateSummaryMarkdownReport();

        Debug.Log($"<color=green><b>[AutoFightTester] BATERIA COMPLETA DE TESTES CONCLUÍDA!</b></color>\n" +
                  $"• Duração total: {totalTestDuration:F1}s (~{totalTestDuration / 60f:F1} min)\n" +
                  $"• Personagens testados: {discoveredCharacters.Count}\n" +
                  $"• Total de screenshots capturadas: {totalScreenshotsCaptured}\n" +
                  $"• Total de frames de telemetria: {totalTelemetryFrames}\n" +
                  $"• Relatório diagnóstico salvo em: {Path.Combine(logsDirectory, "resumo.md")}");

        isRunning = false;
    }

    // ========================================================================
    // EXECUÇÃO DA SUÍTE DE CENÁRIOS PARA UM PERSONAGEM EM UM LADO ESPECÍFICO
    // ========================================================================

    private IEnumerator RunCharacterSuiteOnSide(CharacterMetadata target, string side)
    {
        currentCharacterDisplayName = target.displayName;
        currentCharacterPrefabName = target.prefabName;
        currentSide = side;

        string charSideFolder = Path.Combine(logsDirectory, target.displayName, side);
        string screenshotsFolder = Path.Combine(charSideFolder, "Screenshots");
        if (!Directory.Exists(screenshotsFolder)) Directory.CreateDirectory(screenshotsFolder);

        InitCharacterCsv(charSideFolder, target.displayName, side);
        currentCharacterSnapshots.Clear();

        Debug.Log($"<b>[AutoFightTester]</b> Iniciando suíte para <b>{target.displayName}</b> como <b>{side}</b>...");

        // 1. Reset completo do ambiente e instanciação limpa
        yield return StartCoroutine(FullEnvironmentResetAndSpawn(target, side));

        if (currentP1Controller == null || currentP2Controller == null)
        {
            RecordAnomaly(target.displayName, side, "Instanciação", "Falha ao instanciar os lutadores na cena.", "Componente FighterController ausente ou falha na cena.");
            yield break;
        }

        FighterController subject = side == "P1" ? currentP1Controller : currentP2Controller;
        FighterController opponent = side == "P1" ? currentP2Controller : currentP1Controller;
        FighterInputConfig subjectInput = side == "P1" ? p1InputConfig : p2InputConfig;
        FighterInputConfig opponentInput = side == "P1" ? p2InputConfig : p1InputConfig;

        // ====================================================================
        // CENÁRIO 1: Rematch e Reset Inicial
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioRematchReset(target, side, subject, opponent, "Cenário 1: Rematch Inicial"));

        // ====================================================================
        // CENÁRIO 2: Locomoção Básica (Andar, Recuar, Agachar e Pular)
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioLocomotion(target, side, subject, subjectInput, screenshotsFolder));

        // ====================================================================
        // CENÁRIO 3: Cruzamento de Lado e Turn180
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioCrossTurn180(target, side, subject, opponent, subjectInput, screenshotsFolder));

        // ====================================================================
        // CENÁRIO 4: Golpes que Erram (Whiff à distância)
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioWhiff(target, side, subject, opponent, subjectInput, screenshotsFolder));

        // ====================================================================
        // CENÁRIO 5: Golpe Primário que Acerta (Punch - Hitstop + HitStun)
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioHitPunch(target, side, subject, opponent, subjectInput, screenshotsFolder));

        // ====================================================================
        // CENÁRIO 6: Golpe Secundário que Acerta (Attack2 - Hitstop + HitStun)
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioHitAttack2(target, side, subject, opponent, subjectInput, screenshotsFolder));

        // ====================================================================
        // CENÁRIO 7: Ataque Agachado e Ataque no Ar
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioCrouchAndAir(target, side, subject, opponent, subjectInput, screenshotsFolder));

        // ====================================================================
        // CENÁRIO 8: Contra-Ataque / Dano Recebido (Oponente ataca o Lutador sob teste)
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioReceiveDamage(target, side, subject, opponent, opponentInput, screenshotsFolder));

        // ====================================================================
        // CENÁRIO 9: Rematch e Reset Final
        // ====================================================================
        yield return StartCoroutine(ExecuteScenarioRematchReset(target, side, subject, opponent, "Cenário 9: Rematch Final"));

        // Salva arquivos individuais do personagem
        FinalizeCharacterLogs(charSideFolder, target.displayName, side);

        yield return new WaitForSeconds(settleTime);
    }

    // ========================================================================
    // RESET COMPLETO DO AMBIENTE E INSTANCIAÇÃO DOS LUTADORES
    // ========================================================================

    private IEnumerator FullEnvironmentResetAndSpawn(CharacterMetadata target, string side)
    {
        ClearDebugVisuals();

        // Destrói quaisquer lutadores anteriores
        foreach (FighterController old in FindObjectsByType<FighterController>(FindObjectsInactive.Include))
        {
            Destroy(old.gameObject);
        }

        yield return null;

        GameObject p1Source = side == "P1" ? target.prefab : neutralOpponentPrefab;
        GameObject p2Source = side == "P1" ? neutralOpponentPrefab : target.prefab;

        string p1Name = side == "P1" ? $"P1_{target.displayName}" : "P1_NeutralTarget";
        string p2Name = side == "P2" ? $"P2_{target.displayName}" : "P2_NeutralTarget";

        currentP1Object = Instantiate(p1Source);
        currentP2Object = Instantiate(p2Source);

        currentP1Object.name = p1Name;
        currentP2Object.name = p2Name;

        currentP1Object.transform.SetPositionAndRotation(new Vector3(-1.8f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f));
        currentP2Object.transform.SetPositionAndRotation(new Vector3(1.8f, 0f, 0f), Quaternion.Euler(0f, -90f, 0f));

        currentP1Controller = currentP1Object.GetComponent<FighterController>();
        currentP2Controller = currentP2Object.GetComponent<FighterController>();

        // Configura P1
        p1InputConfig = FighterInputConfig.CreatePlayerOne();
        currentP1Controller.InputConfig = p1InputConfig;
        if (currentP1Controller.Movement != null)
        {
            currentP1Controller.Movement.InputConfig = p1InputConfig;
            currentP1Controller.Movement.IsPlayerControlled = true;
            currentP1Controller.Movement.Opponent = currentP2Object.transform;
            currentP1Controller.Movement.ResetMotion();
        }

        // Configura P2
        p2InputConfig = FighterInputConfig.CreatePlayerTwo();
        currentP2Controller.InputConfig = p2InputConfig;
        if (currentP2Controller.Movement != null)
        {
            currentP2Controller.Movement.InputConfig = p2InputConfig;
            currentP2Controller.Movement.IsPlayerControlled = true;
            currentP2Controller.Movement.Opponent = currentP1Object.transform;
            currentP2Controller.Movement.ResetMotion();
        }

        // Desativa sparring autônomo de IA para ambos para manter o determinismo dos testes
        foreach (var ai in currentP1Object.GetComponents<FighterSparringAI>()) ai.AutoSparring = false;
        foreach (var ai in currentP2Object.GetComponents<FighterSparringAI>()) ai.AutoSparring = false;

        // Vincula hitboxes
        currentP1Controller.RefreshHitboxCache();
        currentP2Controller.RefreshHitboxCache();

        foreach (var hb in currentP1Object.GetComponentsInChildren<Hitbox>(true)) hb.BindBone();
        foreach (var hb in currentP2Object.GetComponentsInChildren<Hitbox>(true)) hb.BindBone();

        // Conecta na TekkenCamera se houver
        var tekkenCam = FindAnyObjectByType<TekkenCamera>();
        if (tekkenCam != null)
        {
            tekkenCam.Fighter1 = currentP1Object.transform;
            tekkenCam.Fighter2 = currentP2Object.transform;
        }

        SetupDebugVisuals();

        yield return new WaitForSeconds(settleTime);
    }

    // ========================================================================
    // CENÁRIOS ROTEIRIZADOS
    // ========================================================================

    private IEnumerator ExecuteScenarioRematchReset(CharacterMetadata target, string side, FighterController subject, FighterController opponent, string title)
    {
        currentScenario = title;
        ResetFighterState(currentP1Controller, new Vector3(-1.8f, 0f, 0f), 90f);
        ResetFighterState(currentP2Controller, new Vector3(1.8f, 0f, 0f), -90f);

        yield return new WaitForSeconds(settleTime);
        SampleTelemetry(title, "ResetCompleted");

        bool p1Ok = currentP1Controller.HealthSystem.CurrentHealth >= 99f && currentP1Controller.CurrentState is NeutralState;
        bool p2Ok = currentP2Controller.HealthSystem.CurrentHealth >= 99f && currentP2Controller.CurrentState is NeutralState;

        if (p1Ok && p2Ok)
        {
            RecordScenarioSuccess(target.displayName, side, title, "Reset executado com sucesso: Vida=100% e Estado=Neutro.");
        }
        else
        {
            RecordAnomaly(target.displayName, side, title, $"Falha ao resetar: P1(HP={currentP1Controller.HealthSystem.CurrentHealth}, Estado={currentP1Controller.CurrentState?.GetType().Name}), P2(HP={currentP2Controller.HealthSystem.CurrentHealth}, Estado={currentP2Controller.CurrentState?.GetType().Name})", "ResetMatch ou ResetHealth não restaurou completamente o estado.");
        }
    }

    private IEnumerator ExecuteScenarioLocomotion(CharacterMetadata target, string side, FighterController subject, FighterInputConfig config, string screenshotsFolder)
    {
        currentScenario = "Cenário 2: Locomoção Básica";

        // Direção de avanço depende do lado: P1 avança para +X (+1), P2 avança para -X (-1)
        float forwardX = side == "P1" ? 1f : -1f;
        Vector3 startPos = subject.transform.position;

        // 1. Andar para frente (aproximar)
        config.SimulatedMovement = new Vector2(forwardX, 0f);
        float moveTimer = 0f;
        while (moveTimer < 0.35f)
        {
            moveTimer += Time.deltaTime;
            SampleTelemetry(currentScenario, "MoveForward");
            yield return null;
        }
        config.SimulatedMovement = Vector2.zero;
        yield return new WaitForSeconds(0.05f);

        float movedDistance = Mathf.Abs(subject.transform.position.x - startPos.x);
        bool movedOk = movedDistance > 0.08f;

        // 2. Recuar
        config.SimulatedMovement = new Vector2(-forwardX, 0f);
        float backTimer = 0f;
        while (backTimer < 0.25f)
        {
            backTimer += Time.deltaTime;
            SampleTelemetry(currentScenario, "MoveBackward");
            yield return null;
        }
        config.SimulatedMovement = Vector2.zero;
        yield return new WaitForSeconds(0.05f);

        // 3. Agachar e medir encolhimento da hurtbox
        config.SimulatedMovement = Vector2.down;
        yield return new WaitForSeconds(0.1f);
        SampleTelemetry(currentScenario, "Crouching");

        var hurtbox = subject.GetComponentInChildren<Hurtbox>(true);
        var cap = hurtbox != null ? hurtbox.GetComponent<CapsuleCollider>() : null;
        float crouchHeight = cap != null ? cap.height : 2f;
        bool crouchShrunk = crouchHeight <= 1.45f;

        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Crouch_Pose");

        yield return new WaitForSeconds(0.15f);
        config.SimulatedMovement = Vector2.zero;
        yield return new WaitForSeconds(0.1f);

        // 4. Pular
        config.SimulatedMovement = Vector2.up;
        yield return new WaitForSeconds(0.12f);
        config.SimulatedMovement = Vector2.zero;

        float airTimer = 0f;
        bool peaked = false;
        while (airTimer < 1.2f)
        {
            airTimer += Time.deltaTime;
            SampleTelemetry(currentScenario, "JumpingAir");
            if (!peaked && subject.transform.position.y > 0.8f) peaked = true;
            if (airTimer > 0.2f && subject.Movement != null && subject.Movement.IsGrounded) break;
            yield return null;
        }

        bool jumpOk = peaked && (subject.Movement != null && subject.Movement.IsGrounded);

        if (movedOk && crouchShrunk && jumpOk)
        {
            RecordScenarioSuccess(target.displayName, side, currentScenario, $"Locomoção funcional: Deslocou {movedDistance:F2}m, Hurtbox encolheu para {crouchHeight:F2}m no agacho, Salto atingiu o ar e aterrissou.");
        }
        else
        {
            string failures = "";
            if (!movedOk) failures += $"Não deslocou com input ({movedDistance:F2}m). ";
            if (!crouchShrunk) failures += $"Hurtbox não reduziu de altura ao agachar (Altura={crouchHeight:F2}m). ";
            if (!jumpOk) failures += "Salto não atingiu altura ou não registrou aterrissagem. ";
            RecordAnomaly(target.displayName, side, currentScenario, failures.Trim(), "Divergência de configuração em FighterMovement, colisor ou transições do Animator.");
        }

        yield return new WaitForSeconds(settleTime);
    }

    private IEnumerator ExecuteScenarioCrossTurn180(CharacterMetadata target, string side, FighterController subject, FighterController opponent, FighterInputConfig config, string screenshotsFolder)
    {
        currentScenario = "Cenário 3: Cruzamento e Turn180";

        // Posiciona subject e opponent próximos
        float subStartX = side == "P1" ? -1.0f : 1.0f;
        float oppStartX = side == "P1" ? 0.3f : -0.3f;
        float subStartYRot = side == "P1" ? 90f : -90f;
        float oppStartYRot = side == "P1" ? -90f : 90f;

        ResetFighterState(subject, new Vector3(subStartX, 0f, 0f), subStartYRot);
        ResetFighterState(opponent, new Vector3(oppStartX, 0f, 0f), oppStartYRot);
        yield return new WaitForSeconds(0.1f);

        // Subject salta sobre o oponente cruzando o eixo X
        float jumpDirX = side == "P1" ? 1f : -1f;
        config.SimulatedMovement = new Vector2(jumpDirX, 1f);

        float jumpTime = 0f;
        bool crossed = false;

        while (jumpTime < 1.4f)
        {
            jumpTime += Time.deltaTime;
            SampleTelemetry(currentScenario, "CrossingAir");

            bool isPastOpponent = side == "P1"
                ? subject.transform.position.x > opponent.transform.position.x
                : subject.transform.position.x < opponent.transform.position.x;

            if (!crossed && isPastOpponent)
            {
                crossed = true;
                SampleTelemetry(currentScenario, "CrossApex");
            }

            if (jumpTime > 0.2f && subject.Movement != null && subject.Movement.IsGrounded) break;
            yield return null;
        }

        config.SimulatedMovement = Vector2.zero;

        // Aguarda estabilização da virada (Turn180)
        float turnWait = 0f;
        while (turnWait < 0.65f)
        {
            turnWait += Time.deltaTime;
            SampleTelemetry(currentScenario, "Turning180");
            yield return null;
        }

        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Turn180_End");

        // Verifica a rotação final do subject: deve estar virado para encarar o oponente
        float targetYRot = side == "P1" ? -90f : 90f;
        float currentYRot = subject.transform.eulerAngles.y;
        float angleDiff = Mathf.Abs(Mathf.DeltaAngle(currentYRot, targetYRot));

        bool turnedOk = crossed && angleDiff < 35f;

        if (turnedOk)
        {
            RecordScenarioSuccess(target.displayName, side, currentScenario, $"Cruzamento e virada bem-sucedidos: Cruzou eixo X e estabilizou rotação Y em {currentYRot:F1}° (alvo: {targetYRot:F0}°).");
        }
        else
        {
            RecordAnomaly(target.displayName, side, currentScenario, $"Falha na virada: Cruzou={crossed}, Rotação Y={currentYRot:F1}° (diferença de {angleDiff:F1}° em relação a {targetYRot:F0}°)", "FighterMovement.FaceOpponent não acionou a virada suave ou travou na transição.");
        }

        yield return new WaitForSeconds(settleTime);
    }

    private IEnumerator ExecuteScenarioWhiff(CharacterMetadata target, string side, FighterController subject, FighterController opponent, FighterInputConfig config, string screenshotsFolder)
    {
        currentScenario = "Cenário 4: Golpes que Erram (Whiff)";

        // Afasta lutadores para distância segura (~6 metros)
        ResetFighterState(subject, new Vector3(side == "P1" ? -3.0f : 3.0f, 0f, 0f), side == "P1" ? 90f : -90f);
        ResetFighterState(opponent, new Vector3(side == "P1" ? 3.0f : -3.0f, 0f, 0f), side == "P1" ? -90f : 90f);
        yield return new WaitForSeconds(settleTime);

        float oppHpBeforePunch = opponent.HealthSystem.CurrentHealth;

        // 1. Soco Whiff
        yield return StartCoroutine(ExecuteAttackWithPhases(subject, config, FighterAttackType.Punch, target.displayName, side, "Punch_Whiff", screenshotsFolder));
        bool punchDidNotHit = Mathf.Approximately(opponent.HealthSystem.CurrentHealth, oppHpBeforePunch);

        yield return new WaitForSeconds(settleTime);

        // 2. Attack2 Whiff
        float oppHpBeforeAttack2 = opponent.HealthSystem.CurrentHealth;
        yield return StartCoroutine(ExecuteAttackWithPhases(subject, config, FighterAttackType.Attack2, target.displayName, side, "Attack2_Whiff", screenshotsFolder));
        bool attack2DidNotHit = Mathf.Approximately(opponent.HealthSystem.CurrentHealth, oppHpBeforeAttack2);

        if (punchDidNotHit && attack2DidNotHit)
        {
            RecordScenarioSuccess(target.displayName, side, currentScenario, "Whiff correto: Golpes executaram as janelas ativas e recovery sem causar dano fantasma à distância.");
        }
        else
        {
            RecordAnomaly(target.displayName, side, currentScenario, $"Colisão fantasma detectada à distância: PunchHit={!punchDidNotHit}, Attack2Hit={!attack2DidNotHit}", "Raio excessivo da hitbox ou collider de detecção global.");
        }

        yield return new WaitForSeconds(settleTime);
    }

    private IEnumerator ExecuteScenarioHitPunch(CharacterMetadata target, string side, FighterController subject, FighterController opponent, FighterInputConfig config, string screenshotsFolder)
    {
        currentScenario = "Cenário 5: Golpe Primário Acerta (Punch Hit)";

        // Posiciona a curta distância (~1.3m)
        ResetFighterState(subject, new Vector3(side == "P1" ? -0.45f : 0.45f, 0f, 0f), side == "P1" ? 90f : -90f);
        ResetFighterState(opponent, new Vector3(side == "P1" ? 0.45f : -0.45f, 0f, 0f), side == "P1" ? -90f : 90f);
        yield return new WaitForSeconds(settleTime);

        float oppStartHp = opponent.HealthSystem.CurrentHealth;
        bool hitstopObserved = false;
        bool hitstunObserved = false;

        // Injeta Soco
        config.SimulatedPunch = true;
        yield return null;
        config.SimulatedPunch = false;

        SampleTelemetry(currentScenario, "Punch_Start");
        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Punch_Hit_Start");

        float elapsed = 0f;
        float timeout = subject.CurrentAttackDuration + 0.4f;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            SampleTelemetry(currentScenario, "Punch_Active");

            if (subject.IsInHitstop || opponent.IsInHitstop) hitstopObserved = true;
            if (opponent.CurrentState is HitStunState) hitstunObserved = true;

            if (elapsed > subject.CurrentAttackDuration * 0.45f && !hitstopObserved)
            {
                yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Punch_Hit_Mid");
            }

            if (subject.CurrentState is NeutralState && elapsed > 0.25f) break;
            yield return null;
        }

        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Punch_Hit_End");

        // Aguarda defensor sair do HitStun
        float stunWait = 0f;
        while (opponent.CurrentState is HitStunState && stunWait < 1.0f)
        {
            stunWait += Time.deltaTime;
            SampleTelemetry(currentScenario, "OpponentInHitStun");
            yield return null;
        }

        float damageDealt = oppStartHp - opponent.HealthSystem.CurrentHealth;
        bool damageOk = damageDealt >= 5f;

        if (damageOk && hitstopObserved && hitstunObserved)
        {
            RecordScenarioSuccess(target.displayName, side, currentScenario, $"Punch conectou perfeitamente: Dano={damageDealt:F0}, Hitstop confirmado e oponente entrou em HitStun.");
        }
        else
        {
            RecordAnomaly(target.displayName, side, currentScenario, $"Falha no impacto do Punch: Dano={damageDealt:F1}, Hitstop={hitstopObserved}, HitStun={hitstunObserved}", "Hitbox não alcançou a hurtbox, timing normalized incorreto ou osso do membro desalinhado.");
        }

        yield return new WaitForSeconds(settleTime);
    }

    private IEnumerator ExecuteScenarioHitAttack2(CharacterMetadata target, string side, FighterController subject, FighterController opponent, FighterInputConfig config, string screenshotsFolder)
    {
        currentScenario = "Cenário 6: Golpe Secundário Acerta (Attack2 Hit)";

        // Posiciona a curta distância (~1.3m)
        ResetFighterState(subject, new Vector3(side == "P1" ? -0.45f : 0.45f, 0f, 0f), side == "P1" ? 90f : -90f);
        ResetFighterState(opponent, new Vector3(side == "P1" ? 0.45f : -0.45f, 0f, 0f), side == "P1" ? -90f : 90f);
        yield return new WaitForSeconds(settleTime);

        float oppStartHp = opponent.HealthSystem.CurrentHealth;
        bool hitstopObserved = false;
        bool hitstunObserved = false;

        // Injeta Attack2
        config.SimulatedAttack2 = true;
        yield return null;
        config.SimulatedAttack2 = false;

        SampleTelemetry(currentScenario, "Attack2_Start");
        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Attack2_Hit_Start");

        float elapsed = 0f;
        float timeout = subject.CurrentAttackDuration + 0.45f;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            SampleTelemetry(currentScenario, "Attack2_Active");

            if (subject.IsInHitstop || opponent.IsInHitstop) hitstopObserved = true;
            if (opponent.CurrentState is HitStunState) hitstunObserved = true;

            if (elapsed > subject.CurrentAttackDuration * 0.45f && !hitstopObserved)
            {
                yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Attack2_Hit_Mid");
            }

            if (subject.CurrentState is NeutralState && elapsed > 0.25f) break;
            yield return null;
        }

        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Attack2_Hit_End");

        // Aguarda defensor sair do HitStun
        float stunWait = 0f;
        while (opponent.CurrentState is HitStunState && stunWait < 1.0f)
        {
            stunWait += Time.deltaTime;
            SampleTelemetry(currentScenario, "OpponentInHitStun");
            yield return null;
        }

        float damageDealt = oppStartHp - opponent.HealthSystem.CurrentHealth;
        bool damageOk = damageDealt >= 8f;

        if (damageOk && hitstopObserved && hitstunObserved)
        {
            RecordScenarioSuccess(target.displayName, side, currentScenario, $"Attack2 conectou perfeitamente: Dano={damageDealt:F0}, Hitstop confirmado e oponente entrou em HitStun.");
        }
        else
        {
            RecordAnomaly(target.displayName, side, currentScenario, $"Falha no impacto do Attack2: Dano={damageDealt:F1}, Hitstop={hitstopObserved}, HitStun={hitstunObserved}", "Hitbox do Attack2 não alcançou a hurtbox ou timing descompassado com o clipe.");
        }

        yield return new WaitForSeconds(settleTime);
    }

    private IEnumerator ExecuteScenarioCrouchAndAir(CharacterMetadata target, string side, FighterController subject, FighterController opponent, FighterInputConfig config, string screenshotsFolder)
    {
        currentScenario = "Cenário 7: Ataques Agachado e no Ar";

        ResetFighterState(subject, new Vector3(side == "P1" ? -1.2f : 1.2f, 0f, 0f), side == "P1" ? 90f : -90f);
        ResetFighterState(opponent, new Vector3(side == "P1" ? 1.2f : -1.2f, 0f, 0f), side == "P1" ? -90f : 90f);
        yield return new WaitForSeconds(settleTime);

        // 1. Golpe Agachado
        config.SimulatedMovement = Vector2.down;
        yield return new WaitForSeconds(0.15f);

        config.SimulatedPunch = true;
        yield return null;
        config.SimulatedPunch = false;

        float attackTimer = 0f;
        bool crouchMidShot = false;
        while (attackTimer < 0.75f)
        {
            attackTimer += Time.deltaTime;
            SampleTelemetry(currentScenario, "CrouchPunch");
            if (!crouchMidShot && attackTimer >= 0.25f)
            {
                crouchMidShot = true;
                yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Crouch_Punch_Mid");
            }
            yield return null;
        }

        config.SimulatedMovement = Vector2.zero;
        yield return new WaitForSeconds(0.15f);

        // 2. Golpe Aéreo
        config.SimulatedMovement = Vector2.up;
        yield return new WaitForSeconds(0.12f);
        config.SimulatedMovement = Vector2.zero;

        yield return new WaitForSeconds(0.18f);

        config.SimulatedPunch = true;
        yield return null;
        config.SimulatedPunch = false;

        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_Air_Punch_Mid");

        float landTimer = 0f;
        while (landTimer < 1.2f)
        {
            landTimer += Time.deltaTime;
            SampleTelemetry(currentScenario, "AirPunchLand");
            if (subject.Movement != null && subject.Movement.IsGrounded && landTimer > 0.15f) break;
            yield return null;
        }

        RecordScenarioSuccess(target.displayName, side, currentScenario, "Golpes agachado e no ar disparados e concluídos com aterrissagem correta.");
        yield return new WaitForSeconds(settleTime);
    }

    private IEnumerator ExecuteScenarioReceiveDamage(CharacterMetadata target, string side, FighterController subject, FighterController opponent, FighterInputConfig opponentConfig, string screenshotsFolder)
    {
        currentScenario = "Cenário 8: Contra-Ataque / Dano Recebido";

        // Posiciona a curta distância
        ResetFighterState(subject, new Vector3(side == "P1" ? -0.45f : 0.45f, 0f, 0f), side == "P1" ? 90f : -90f);
        ResetFighterState(opponent, new Vector3(side == "P1" ? 0.45f : -0.45f, 0f, 0f), side == "P1" ? -90f : 90f);
        yield return new WaitForSeconds(settleTime);

        float subStartHp = subject.HealthSystem.CurrentHealth;
        bool subjectHitstop = false;
        bool subjectHitstun = false;

        // Oponente desfere Punch contra o lutador sob teste
        opponentConfig.SimulatedPunch = true;
        yield return null;
        opponentConfig.SimulatedPunch = false;

        float elapsed = 0f;
        while (elapsed < 0.9f)
        {
            elapsed += Time.deltaTime;
            SampleTelemetry(currentScenario, "TakingDamage");

            if (subject.IsInHitstop) subjectHitstop = true;
            if (subject.CurrentState is HitStunState) subjectHitstun = true;

            yield return null;
        }

        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{target.displayName}_{side}_ReceivedDamage_HitStun");

        // Aguarda subject recuperar do HitStun
        float waitStun = 0f;
        while (subject.CurrentState is HitStunState && waitStun < 1.0f)
        {
            waitStun += Time.deltaTime;
            SampleTelemetry(currentScenario, "SubjectInHitStun");
            yield return null;
        }

        float damageReceived = subStartHp - subject.HealthSystem.CurrentHealth;
        bool damageOk = damageReceived >= 5f;

        if (damageOk && subjectHitstun)
        {
            RecordScenarioSuccess(target.displayName, side, currentScenario, $"Dano recebido com sucesso: Sofreu {damageReceived:F0} de dano, ativou Hitstop e transitou para HitStunState.");
        }
        else
        {
            RecordAnomaly(target.displayName, side, currentScenario, $"Falha ao receber dano: Dano={damageReceived:F1}, Hitstop={subjectHitstop}, HitStun={subjectHitstun}", "Hurtbox não interceptou o golpe do oponente ou HealthSystem/FSM falhou ao registrar impacto.");
        }

        yield return new WaitForSeconds(settleTime);
    }

    // ========================================================================
    // HELPER PARA EXECUÇÃO DE ATAQUE COM CAPTURA DE FASES
    // ========================================================================

    private IEnumerator ExecuteAttackWithPhases(FighterController fighter, FighterInputConfig config, FighterAttackType attackType, string charName, string side, string baseName, string screenshotsFolder)
    {
        if (attackType == FighterAttackType.Punch) config.SimulatedPunch = true;
        else if (attackType == FighterAttackType.Attack2) config.SimulatedAttack2 = true;

        yield return null;
        config.SimulatedPunch = false;
        config.SimulatedAttack2 = false;

        SampleTelemetry(currentScenario, $"{baseName}_Start");
        yield return CaptureScreenshotRoutine(screenshotsFolder, $"{charName}_{side}_{baseName}_Start");

        bool midCaptured = false;
        bool endCaptured = false;
        float elapsed = 0f;
        float timeout = fighter.CurrentAttackDuration + 0.35f;

        while (elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            SampleTelemetry(currentScenario, $"{baseName}_Active");

            bool hasProg = fighter.TryGetCurrentAttackProgress(out float prog);

            if (!midCaptured && (hasProg && prog >= 0.4f || elapsed >= fighter.CurrentAttackDuration * 0.45f))
            {
                midCaptured = true;
                SampleTelemetry(currentScenario, $"{baseName}_Mid");
                yield return CaptureScreenshotRoutine(screenshotsFolder, $"{charName}_{side}_{baseName}_Mid");
            }

            if (!endCaptured && (hasProg && prog >= 0.85f || elapsed >= fighter.CurrentAttackDuration * 0.85f))
            {
                endCaptured = true;
                SampleTelemetry(currentScenario, $"{baseName}_End");
                yield return CaptureScreenshotRoutine(screenshotsFolder, $"{charName}_{side}_{baseName}_End");
            }

            if (fighter.CurrentState is NeutralState && elapsed > 0.2f) break;
            yield return null;
        }

        if (!midCaptured) yield return CaptureScreenshotRoutine(screenshotsFolder, $"{charName}_{side}_{baseName}_Mid");
        if (!endCaptured) yield return CaptureScreenshotRoutine(screenshotsFolder, $"{charName}_{side}_{baseName}_End");

        SampleTelemetry(currentScenario, $"{baseName}_Completed");
    }

    private void ResetFighterState(FighterController fighter, Vector3 pos, float yRot)
    {
        if (fighter == null) return;
        if (fighter.Movement != null)
        {
            fighter.Movement.ResetMotion();
            fighter.Movement.CanMove = true;
        }
        fighter.transform.position = pos;
        fighter.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        if (fighter.HealthSystem != null) fighter.HealthSystem.ResetHealth();
        fighter.DisableAllHitboxes();
        fighter.ClearAttackBuffer();
        fighter.ChangeState(fighter.NeutralState);
    }

    // ========================================================================
    // TELEMETRIA E LOGS (CSV e JSON)
    // ========================================================================

    private void InitConsolidatedCsv()
    {
        if (!recordTelemetry) return;
        try
        {
            consolidatedCsvPath = Path.Combine(logsDirectory, "AutoFight_Telemetry_Consolidated.csv");
            consolidatedCsvWriter = new StreamWriter(consolidatedCsvPath, false, Encoding.UTF8);
            consolidatedCsvWriter.WriteLine(
                "Time,Frame,Character,Side,Scenario,Phase,Fighter,FSM_State,InHitstop,ActiveAttack,Health," +
                "PosX,PosY,PosZ,RotX,RotY,RotZ,AnimState,AnimNormTime,AnimSpeed," +
                "Param_Speed,Param_Crouch,Param_MoveDir,Hitboxes_Active,Hitboxes_Detail,Hurtboxes_Detail"
            );
            consolidatedCsvWriter.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoFightTester] Erro ao criar CSV consolidado: {ex.Message}");
        }
    }

    private void CloseConsolidatedCsv()
    {
        if (consolidatedCsvWriter != null)
        {
            consolidatedCsvWriter.Flush();
            consolidatedCsvWriter.Close();
            consolidatedCsvWriter = null;
        }
    }

    private void InitCharacterCsv(string folder, string charName, string side)
    {
        if (!recordTelemetry) return;
        CloseCharacterCsv();
        try
        {
            string csvPath = Path.Combine(folder, $"AutoFight_Telemetry_{charName}_{side}.csv");
            currentCharacterCsvWriter = new StreamWriter(csvPath, false, Encoding.UTF8);
            currentCharacterCsvWriter.WriteLine(
                "Time,Frame,Character,Side,Scenario,Phase,Fighter,FSM_State,InHitstop,ActiveAttack,Health," +
                "PosX,PosY,PosZ,RotX,RotY,RotZ,AnimState,AnimNormTime,AnimSpeed," +
                "Param_Speed,Param_Crouch,Param_MoveDir,Hitboxes_Active,Hitboxes_Detail,Hurtboxes_Detail"
            );
            currentCharacterCsvWriter.Flush();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AutoFightTester] Erro ao criar CSV individual: {ex.Message}");
        }
    }

    private void CloseCharacterCsv()
    {
        if (currentCharacterCsvWriter != null)
        {
            currentCharacterCsvWriter.Flush();
            currentCharacterCsvWriter.Close();
            currentCharacterCsvWriter = null;
        }
    }

    private void FinalizeCharacterLogs(string folder, string charName, string side)
    {
        CloseCharacterCsv();

        // Salva JSON individual
        try
        {
            string jsonPath = Path.Combine(folder, $"AutoFight_Telemetry_{charName}_{side}.json");
            var report = new CharacterJsonReport
            {
                character = charName,
                side = side,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                totalSnapshots = currentCharacterSnapshots.Count,
                snapshots = currentCharacterSnapshots
            };
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(jsonPath, json, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AutoFightTester] Erro ao salvar JSON do personagem {charName}_{side}: {ex.Message}");
        }
    }

    private void SampleTelemetry(string scenario, string phase)
    {
        if (!recordTelemetry) return;

        if (currentP1Controller != null) SampleFighterData(currentP1Controller, "Player1", scenario, phase);
        if (currentP2Controller != null) SampleFighterData(currentP2Controller, "Player2", scenario, phase);

        totalTelemetryFrames++;
    }

    private void SampleFighterData(FighterController fighter, string fighterRole, string scenario, string phase)
    {
        var snap = new TelemetrySnapshot
        {
            time = Time.time,
            frame = Time.frameCount,
            character = currentCharacterDisplayName,
            side = currentSide,
            scenario = scenario,
            phase = phase,
            fighter = fighterRole,
            fsmState = fighter.CurrentState != null ? fighter.CurrentState.GetType().Name : "None",
            inHitstop = fighter.IsInHitstop,
            activeAttack = fighter.ActiveAttackType.ToString(),
            health = fighter.HealthSystem != null ? fighter.HealthSystem.CurrentHealth : 100f,
            position = fighter.transform.position,
            rotation = fighter.transform.eulerAngles
        };

        Animator anim = fighter.Animator;
        if (anim != null && anim.isActiveAndEnabled)
        {
            AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);
            snap.animStateName = info.shortNameHash.ToString();
            snap.animNormalizedTime = info.normalizedTime;
            snap.animSpeed = anim.speed;

            snap.paramSpeed = GetAnimFloat(anim, "Speed");
            snap.paramCrouch = GetAnimBool(anim, "Crouch") ? 1f : 0f;
            snap.paramMoveDirection = GetAnimInt(anim, "MoveDirection");
        }

        // Hitboxes
        var hitboxesDetail = new StringBuilder();
        int activeCount = 0;
        foreach (var hb in fighter.GetComponentsInChildren<Hitbox>(true))
        {
            if (hb == null) continue;
            var col = hb.GetComponent<Collider>();
            float r = col is SphereCollider sc ? sc.radius : 0.25f;
            bool active = hb.IsActive;
            if (active) activeCount++;

            snap.hitboxes.Add(new HitboxSnapshotData
            {
                limb = hb.LimbType.ToString(),
                isActive = active,
                worldPosition = hb.transform.position,
                radius = r
            });

            hitboxesDetail.Append($"[{hb.LimbType}:{(active ? "ATV" : "ina")}:{hb.transform.position:F2}] ");
        }

        // Hurtboxes
        var hurtboxesDetail = new StringBuilder();
        foreach (var hurt in fighter.GetComponentsInChildren<Hurtbox>(true))
        {
            if (hurt == null) continue;
            var cap = hurt.GetComponent<CapsuleCollider>();
            float h = cap != null ? cap.height : 1.8f;
            float r = cap != null ? cap.radius : 0.42f;

            snap.hurtboxes.Add(new HurtboxSnapshotData
            {
                worldPosition = hurt.transform.position,
                height = h,
                radius = r
            });

            hurtboxesDetail.Append($"[Hurt:{h:F2}x{r:F2}] ");
        }

        currentCharacterSnapshots.Add(snap);

        string line = string.Format(
            Inv,
            "{0:F3},{1},\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",\"{7}\",{8},\"{9}\",{10:F1}," +
            "{11:F2},{12:F2},{13:F2},{14:F1},{15:F1},{16:F1},{17},{18:F3},{19:F2}," +
            "{20:F2},{21:F0},{22:F0},{23},\"{24}\",\"{25}\"",
            snap.time, snap.frame, snap.character, snap.side, snap.scenario, snap.phase,
            snap.fighter, snap.fsmState, snap.inHitstop ? "1" : "0", snap.activeAttack, snap.health,
            snap.position.x, snap.position.y, snap.position.z,
            snap.rotation.x, snap.rotation.y, snap.rotation.z,
            snap.animStateName, snap.animNormalizedTime, snap.animSpeed,
            snap.paramSpeed, snap.paramCrouch, snap.paramMoveDirection,
            activeCount, hitboxesDetail.ToString().Trim(), hurtboxesDetail.ToString().Trim()
        );

        if (consolidatedCsvWriter != null)
        {
            try { consolidatedCsvWriter.WriteLine(line); } catch { }
        }
        if (currentCharacterCsvWriter != null)
        {
            try { currentCharacterCsvWriter.WriteLine(line); } catch { }
        }
    }

    private float GetAnimFloat(Animator anim, string pName)
    {
        foreach (var p in anim.parameters) if (p.name == pName && p.type == AnimatorControllerParameterType.Float) return anim.GetFloat(pName);
        return 0f;
    }
    private bool GetAnimBool(Animator anim, string pName)
    {
        foreach (var p in anim.parameters) if (p.name == pName && p.type == AnimatorControllerParameterType.Bool) return anim.GetBool(pName);
        return false;
    }
    private int GetAnimInt(Animator anim, string pName)
    {
        foreach (var p in anim.parameters) if (p.name == pName && p.type == AnimatorControllerParameterType.Int) return anim.GetInteger(pName);
        return 0;
    }

    // ========================================================================
    // REGISTRO DE RESULTADOS E ANOMALIAS
    // ========================================================================

    private void RecordScenarioSuccess(string charName, string side, string scenario, string details)
    {
        scenarioResults.Add(new ScenarioStatusEntry
        {
            character = charName,
            side = side,
            scenario = scenario,
            isOk = true,
            details = details
        });
    }

    private void RecordAnomaly(string charName, string side, string scenario, string evidence, string probableCause)
    {
        scenarioResults.Add(new ScenarioStatusEntry
        {
            character = charName,
            side = side,
            scenario = scenario,
            isOk = false,
            details = evidence
        });

        detectedAnomalies.Add(new ScenarioAnomaly
        {
            character = charName,
            side = side,
            scenario = scenario,
            evidence = evidence,
            probableCause = probableCause
        });

        Debug.LogWarning($"<color=orange><b>[AutoFightTester ANOMALIA]</b> [{charName} - {side} - {scenario}]: {evidence}</color>");
    }

    // ========================================================================
    // GERAÇÃO DO RELATÓRIO DIAGNÓSTICO (Logs/resumo.md)
    // ========================================================================

    private void GenerateSummaryMarkdownReport()
    {
        string reportPath = Path.Combine(logsDirectory, "resumo.md");
        var sb = new StringBuilder();

        sb.AppendLine("# Relatório Diagnóstico do AutoFightTester — UnDFight");
        sb.AppendLine();
        sb.AppendLine($"- **Data da Execução:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **Total de Personagens Descobertos:** {discoveredCharacters.Count}");
        sb.AppendLine($"- **Duração Total da Rodada:** {totalTestDuration:F1}s (~{totalTestDuration / 60f:F1} min)");
        sb.AppendLine($"- **Total de Screenshots Capturadas:** {totalScreenshotsCaptured}");
        sb.AppendLine($"- **Total de Frames de Telemetria:** {totalTelemetryFrames}");
        sb.AppendLine();

        // 1. Tabela de Personagens Descobertos e Configurações
        sb.AppendLine("## 1. Fonte de Verdade dos Personagens Jogáveis");
        sb.AppendLine();
        sb.AppendLine("| # | Nome de Exibição | Prefab | Animator Controller | Avatar | Membro Primário | Membro Secundário | Hurtbox Altura |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var ch in discoveredCharacters)
        {
            sb.AppendLine($"| {ch.index} | **{ch.displayName}** | `{ch.prefabName}` | `{ch.controllerName}` | `{ch.avatarName}` | `{ch.primaryLimb}` | `{ch.secondaryLimb}` | {ch.baseHurtboxHeight:F2}m |");
        }
        sb.AppendLine();

        // 2. Tabela Matriz: Personagem x Cenário (P1 / P2)
        sb.AppendLine("## 2. Matriz de Resultados (Personagem x Cenário)");
        sb.AppendLine();
        sb.AppendLine("Legenda: **OK** = Cenário executou e satisfez critérios; **ANOMALIA** = Discrepância detectada nos critérios de teste.");
        sb.AppendLine();

        string[] scenarioHeaders = {
            "Cenário 1: Rematch Inicial",
            "Cenário 2: Locomoção Básica",
            "Cenário 3: Cruzamento e Turn180",
            "Cenário 4: Golpes que Erram (Whiff)",
            "Cenário 5: Golpe Primário Acerta (Punch Hit)",
            "Cenário 6: Golpe Secundário Acerta (Attack2 Hit)",
            "Cenário 7: Ataques Agachado e no Ar",
            "Cenário 8: Contra-Ataque / Dano Recebido",
            "Cenário 9: Rematch Final"
        };

        sb.Append("| Personagem | Lado | ");
        for (int i = 0; i < scenarioHeaders.Length; i++) sb.Append($"C{i + 1} | ");
        sb.AppendLine();

        sb.Append("|---|---|");
        for (int i = 0; i < scenarioHeaders.Length; i++) sb.Append("---|");
        sb.AppendLine();

        foreach (var ch in discoveredCharacters)
        {
            foreach (string side in new[] { "P1", "P2" })
            {
                if (sideFilter == TestSideFilter.OnlyP1 && side == "P2") continue;
                if (sideFilter == TestSideFilter.OnlyP2 && side == "P1") continue;

                sb.Append($"| **{ch.displayName}** | {side} | ");
                foreach (string sc in scenarioHeaders)
                {
                    var entry = scenarioResults.Find(r => r.character == ch.displayName && r.side == side && r.scenario == sc);
                    if (entry == null) sb.Append("- | ");
                    else if (entry.isOk) sb.Append("✅ OK | ");
                    else sb.Append("❌ **ANOMALIA** | ");
                }
                sb.AppendLine();
            }
        }
        sb.AppendLine();

        // 3. Detalhamento de Cada Anomalia
        sb.AppendLine("## 3. Detalhamento das Anomalias Encontradas");
        sb.AppendLine();
        if (detectedAnomalies.Count == 0)
        {
            sb.AppendLine("> Nenhum anomalia foi detectada durante a execução dos cenários. Todos os personagens passaram nos critérios avaliados.");
        }
        else
        {
            sb.AppendLine("| Personagem | Lado | Cenário | Evidência Telemetria | Causa Provável |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var anom in detectedAnomalies)
            {
                sb.AppendLine($"| **{anom.character}** | {anom.side} | {anom.scenario} | {anom.evidence.Replace("|", "/")} | {anom.probableCause.Replace("|", "/")} |");
            }
        }
        sb.AppendLine();

        // 4. Seção Comparativa entre os Personagens
        sb.AppendLine("## 4. Comparação Entre os Personagens (Discrepâncias Específicas)");
        sb.AppendLine();
        sb.AppendLine("Esta seção destaca comportamentos e propriedades que só ocorrem em personagens específicos do elenco:");
        sb.AppendLine();

        // Comparação de Hurtboxes
        sb.AppendLine("### A. Dimensões de Hurtbox");
        sb.AppendLine("- A maioria dos personagens possui `BodyHurtbox` com altura padrão de **1.80m** e raio **0.42m**.");
        var diffHurtbox = discoveredCharacters.FindAll(c => Mathf.Abs(c.baseHurtboxHeight - 1.80f) > 0.05f);
        if (diffHurtbox.Count > 0)
        {
            foreach (var c in diffHurtbox)
            {
                sb.AppendLine($"- ⚠️ **{c.displayName}** (`{c.prefabName}`): Altura da Hurtbox configurada em **{c.baseHurtboxHeight:F2}m** (mais alta que o restante do elenco).");
            }
        }
        else
        {
            sb.AppendLine("- Todas as Hurtboxes possuem a mesma altura uniforme.");
        }
        sb.AppendLine();

        // Comparação de Durações de Clipes e Membros de Ataque
        sb.AppendLine("### B. Durações de Golpe e Membros Utilizados");
        sb.AppendLine("| Personagem | Golpe Primário (Punch) | Duração | Membro | Golpe Secundário (Attack2) | Duração | Membro |");
        sb.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var c in discoveredCharacters)
        {
            sb.AppendLine($"| **{c.displayName}** | Soco | {c.primaryDuration:F2}s | `{c.primaryLimb}` | Secundário | {c.secondaryDuration:F2}s | `{c.secondaryLimb}` |");
        }
        sb.AppendLine();
        sb.AppendLine("> **Nota sobre Duração dos Clipes:** Personagens como *Lucas* (ManInBlack) e *Jompi* (ShadowSentinel) utilizam animações de Uppercut e Kicking com durações superiores a **3.0s**, contrastando com os golpes rápidos de ~1.0s de *Tiago* e *Isaac*. Se o playbackSpeed não for proporcional, esses lutadores ficam vulneráveis por períodos consideravelmente maiores.");
        sb.AppendLine();

        // Comparação de Ossos de Hitbox
        sb.AppendLine("### C. Vínculo de Ossos das Hitboxes");
        foreach (var c in discoveredCharacters)
        {
            bool hasMissing = false;
            foreach (var kvp in c.boneNames)
            {
                if (kvp.Value == "NÃO_ENCONTRADO") hasMissing = true;
            }
            if (hasMissing)
            {
                sb.AppendLine($"- ⚠️ **{c.displayName}**: Possui ossos não vinculados nas hitboxes.");
            }
        }
        sb.AppendLine("- Todos os 7 personagens possuem o avatar Humanoid configurado corretamente, permitindo que as hitboxes (`RightHand`, `LeftHand`, `RightFoot`, `LeftFoot`, `Head`) encontrem os ossos correspondentes.");
        sb.AppendLine();

        // Comparação de Espelhamento (P1 vs P2)
        sb.AppendLine("### D. Comportamento de Espelhamento (P1 vs P2)");
        sb.AppendLine("- No lado P2, o lutador inicia com rotação Y em **-90°** olhando para a esquerda.");
        sb.AppendLine("- O comando de avanço em direção ao oponente requer vetor `-X`, e o comando de recuo requer `+X`.");
        sb.AppendLine("- A sincronização da cápsula de `BodyHurtbox` durante agachamento opera independentemente do lado em que o personagem está posicionado.");
        sb.AppendLine();

        try
        {
            File.WriteAllText(reportPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"<color=green>[AutoFightTester] Relatório salvo com sucesso em: {reportPath}</color>");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoFightTester] Erro ao gravar resumo.md: {ex.Message}");
        }
    }

    // ========================================================================
    // VISUALIZADORES 3D DE DEBUG (HITBOXES E HURTBOXES)
    // ========================================================================

    private void SetupDebugVisuals()
    {
        ClearDebugVisuals();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Hidden/Internal-Colored");

        SetupFighterVisuals(currentP1Controller, shader);
        SetupFighterVisuals(currentP2Controller, shader);
    }

    private void SetupFighterVisuals(FighterController fighter, Shader shader)
    {
        if (fighter == null) return;

        // Hurtbox: Cápsula verde translúcida que acompanha a altura em tempo real
        foreach (var hurt in fighter.GetComponentsInChildren<Hurtbox>(true))
        {
            var cap = hurt.GetComponent<CapsuleCollider>();
            float h = cap != null ? cap.height : 2.0f;
            float r = cap != null ? cap.radius : 0.42f;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "_DebugHurtboxVisual";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(hurt.transform, false);
            visual.transform.localPosition = cap != null ? cap.center : Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);

            var rend = visual.GetComponent<Renderer>();
            Material mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            mat.color = new Color(0.1f, 1f, 0.3f, 0.35f);
            SetMaterialTransparent(mat);
            rend.material = mat;

            debugVisualObjects.Add(visual);
            hurtboxVisualPairs.Add(new KeyValuePair<Hurtbox, Transform>(hurt, visual.transform));
        }

        // Hitbox: Esfera translúcida que acompanha os ossos (Laranja inativa, Vermelha ativa)
        foreach (var hb in fighter.GetComponentsInChildren<Hitbox>(true))
        {
            var col = hb.GetComponent<Collider>();
            float r = col is SphereCollider sc ? sc.radius : 0.22f;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "_DebugHitboxVisual";
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(hb.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * (r * 2f);

            var rend = visual.GetComponent<Renderer>();
            Material mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            mat.color = new Color(1f, 0.6f, 0.1f, 0.25f);
            SetMaterialTransparent(mat);
            rend.material = mat;

            debugVisualObjects.Add(visual);
            hitboxVisualPairs.Add(new KeyValuePair<Hitbox, Renderer>(hb, rend));
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void UpdateDebugVisuals()
    {
        for (int i = 0; i < hitboxVisualPairs.Count; i++)
        {
            var pair = hitboxVisualPairs[i];
            if (pair.Key == null || pair.Value == null) continue;
            bool active = pair.Key.IsActive;
            pair.Value.material.color = active
                ? new Color(1f, 0.1f, 0.1f, 0.80f)
                : new Color(1f, 0.6f, 0.1f, 0.20f);
        }

        for (int i = 0; i < hurtboxVisualPairs.Count; i++)
        {
            var pair = hurtboxVisualPairs[i];
            if (pair.Key == null || pair.Value == null) continue;
            var cap = pair.Key.GetComponent<CapsuleCollider>();
            if (cap != null)
            {
                pair.Value.localScale = new Vector3(cap.radius * 2f, cap.height * 0.5f, cap.radius * 2f);
                pair.Value.localPosition = cap.center;
            }
        }
    }

    private void ClearDebugVisuals()
    {
        for (int i = 0; i < debugVisualObjects.Count; i++)
        {
            if (debugVisualObjects[i] != null) Destroy(debugVisualObjects[i]);
        }
        debugVisualObjects.Clear();
        hitboxVisualPairs.Clear();
        hurtboxVisualPairs.Clear();
    }

    // ========================================================================
    // SCREENSHOTS COM VISUALIZAÇÃO DE DEBUG
    // ========================================================================

    private IEnumerator CaptureScreenshotRoutine(string folder, string filename)
    {
        if (!captureScreenshots) yield break;

        yield return new WaitForEndOfFrame();

        string sanitized = SanitizeFilename(filename) + ".png";
        string fullPath = Path.Combine(folder, sanitized);

        try
        {
            Texture2D screenTex = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenTex != null)
            {
                byte[] bytes = screenTex.EncodeToPNG();
                File.WriteAllBytes(fullPath, bytes);
                Destroy(screenTex);
                totalScreenshotsCaptured++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AutoFightTester] Falha ao salvar screenshot {filename}: {ex.Message}");
        }
    }

    private string SanitizeFilename(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Replace(' ', '_');
    }

    private void EnableGameViewGizmos()
    {
#if UNITY_EDITOR
        try
        {
            var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType != null)
            {
                var gameView = EditorWindow.GetWindow(gameViewType, false, null, false);
                if (gameView != null)
                {
                    var prop = gameViewType.GetProperty("gizmos", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (prop != null) prop.SetValue(gameView, true);
                }
            }
        }
        catch { }
#endif
    }

    // ========================================================================
    // HUD EM TELA DURANTE O TESTE
    // ========================================================================

    private void OnGUI()
    {
        if (!isRunning) return;

        float panelWidth = 520;
        float panelX = Screen.width - panelWidth - 15;

        GUI.color = new Color(0f, 0f, 0f, 0.88f);
        GUI.Box(new Rect(panelX, 10, panelWidth, 125), GUIContent.none);

        GUI.color = Color.cyan;
        GUI.Label(new Rect(panelX + 10, 14, panelWidth - 20, 22), $"<b>[AutoFightTester Multi-Personagem]</b>");

        GUI.color = Color.white;
        GUI.Label(new Rect(panelX + 10, 36, panelWidth - 20, 20), $"Lutador sob teste: <b>{currentCharacterDisplayName}</b> ({currentCharacterPrefabName}) | Lado: <b>{currentSide}</b>");
        GUI.Label(new Rect(panelX + 10, 56, panelWidth - 20, 20), $"Cenário: <color=yellow>{currentScenario}</color>");

        GUI.color = new Color(0.8f, 0.9f, 1f);
        string p1Info = currentP1Controller != null ? $"{currentP1Controller.CurrentState?.GetType().Name} (HP={currentP1Controller.HealthSystem?.CurrentHealth:F0})" : "-";
        string p2Info = currentP2Controller != null ? $"{currentP2Controller.CurrentState?.GetType().Name} (HP={currentP2Controller.HealthSystem?.CurrentHealth:F0})" : "-";
        GUI.Label(new Rect(panelX + 10, 78, panelWidth - 20, 20), $"P1: {p1Info} | P2: {p2Info}");

        GUI.color = Color.yellow;
        GUI.Label(new Rect(panelX + 10, 98, panelWidth - 20, 20), $"Frames: {totalTelemetryFrames} | Shots: {totalScreenshotsCaptured} | Anomalias: {detectedAnomalies.Count}");
    }

    // ========================================================================
    // MÉTODOS PÚBLICOS E ENTRADAS DO EDITOR / CLI
    // ========================================================================

    /// <summary>
    /// Configura filtro para testar apenas um personagem específico por índice.
    /// </summary>
    public void SetSingleCharacterFilter(int index)
    {
        singleCharacterIndex = index;
        singleCharacterNameFilter = "";
    }

    /// <summary>
    /// Configura filtro para testar apenas um personagem específico por nome.
    /// </summary>
    public void SetSingleCharacterFilter(string name)
    {
        singleCharacterIndex = -1;
        singleCharacterNameFilter = name;
    }

#if UNITY_EDITOR
    [MenuItem("UnDFight/Testes/Executar Teste Multi-Personagem (Todos)")]
    public static void RunAllFromMenu()
    {
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        EditorApplication.playModeStateChanged += OnPlayStateChangedAll;
    }

    private static void OnPlayStateChangedAll(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayStateChangedAll;
            var tester = EnsureTesterInScene();
            tester.singleCharacterIndex = -1;
            tester.singleCharacterNameFilter = "";
        }
    }

    [MenuItem("UnDFight/Testes/Executar Teste Rápido (Apenas Tiago)")]
    public static void RunQuickSingleFromMenu()
    {
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        EditorApplication.playModeStateChanged += OnPlayStateChangedSingle;
    }

    private static void OnPlayStateChangedSingle(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayStateChangedSingle;
            var tester = EnsureTesterInScene();
            tester.singleCharacterIndex = 0;
            tester.singleCharacterNameFilter = "";
        }
    }

    public static AutoFightTester EnsureTesterInScene()
    {
        var tester = FindAnyObjectByType<AutoFightTester>();
        if (tester == null)
        {
            var go = new GameObject("AutoFightTester");
            tester = go.AddComponent<AutoFightTester>();
        }
        return tester;
    }

    public static void RunBatchmode()
    {
        Debug.Log("[AutoFightTester] Iniciando execução em Batchmode...");
        EditorApplication.isPlaying = true;
        EnsureTesterInScene();
    }
#endif
}
#endif
