using System.Collections;
using UnityEngine;

public enum AIDifficulty
{
    Easy,
    Medium,
    Hard
}

/// <summary>
/// IA de treino / sparring para o oponente com 3 níveis de dificuldade (Fácil, Médio, Difícil).
/// Executa movimentações independentes (aproximação, recuo, sidestep) e ataques de acordo com a agressividade.
/// </summary>
[RequireComponent(typeof(FighterController))]
[RequireComponent(typeof(FighterMovement))]
public class FighterSparringAI : MonoBehaviour
{
    [Header("Difficulty Settings")]
    [Tooltip("Nível de dificuldade e agressividade da IA.")]
    [SerializeField] private AIDifficulty difficulty = AIDifficulty.Medium;

    [Header("Control Settings")]
    [Tooltip("Habilita ou desabilita as ações autônomas da IA.")]
    [SerializeField] private bool autoSparring = true;

    private FighterController controller;
    private FighterMovement movement;
    private float timer;
    private float currentInterval;

    public AIDifficulty Difficulty
    {
        get => difficulty;
        set
        {
            difficulty = value;
            ResetTimerForDifficulty();
        }
    }

    public bool AutoSparring
    {
        get => autoSparring;
        set => autoSparring = value;
    }

    private void Awake()
    {
        controller = GetComponent<FighterController>();
        movement = GetComponent<FighterMovement>();

        ResetTimerForDifficulty();
    }

    private void Update()
    {
        if (!autoSparring || controller == null || movement == null) return;

        // Se o lutador estiver atordoado, atacando ou derrotado, não toma decisões
        if (controller.CurrentState is not NeutralState) return;

        timer += Time.deltaTime;
        if (timer >= currentInterval)
        {
            timer = 0f;
            PerformDecision();
            ResetTimerForDifficulty();
        }
    }

    /// <summary>
    /// Configura o tempo de reação de acordo com o nível de dificuldade selecionado.
    /// </summary>
    private void ResetTimerForDifficulty()
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy:
                currentInterval = Random.Range(2.8f, 3.8f); // Lento e paciente
                break;
            case AIDifficulty.Medium:
                currentInterval = Random.Range(1.6f, 2.2f); // Moderado
                break;
            case AIDifficulty.Hard:
                currentInterval = Random.Range(0.65f, 1.1f); // Rápido e agressivo
                break;
        }
    }

    /// <summary>
    /// Executa tomada de decisão autônoma baseada na distância para o oponente e na dificuldade.
    /// </summary>
    private void PerformDecision()
    {
        if (movement.Opponent == null) return;

        float distanceToOpponent = Vector3.Distance(transform.position, movement.Opponent.position);

        float attackChance;
        switch (difficulty)
        {
            case AIDifficulty.Easy:
                attackChance = distanceToOpponent < 1.6f ? 0.35f : 0.15f;
                break;
            case AIDifficulty.Medium:
                attackChance = distanceToOpponent < 1.8f ? 0.60f : 0.35f;
                break;
            case AIDifficulty.Hard:
                attackChance = distanceToOpponent < 2.0f ? 0.85f : 0.55f;
                break;
            default:
                attackChance = 0.40f;
                break;
        }

        float roll = Random.value;

        // Decisão de atacar
        if (roll < attackChance && distanceToOpponent <= 2.2f)
        {
            movement.ExternalInput = Vector2.zero;
            controller.TriggerAttack(Random.value < 0.65f ? FighterAttackType.Punch : FighterAttackType.Attack2);
            return;
        }

        // Decisão de movimentação tática
        Vector2 moveDir = Vector2.zero;
        float moveDuration = 0.6f;

        switch (difficulty)
        {
            case AIDifficulty.Easy:
                // Fácil: prefere recuar ou dar passos curtos laterais
                if (distanceToOpponent < 2.0f)
                {
                    moveDir = new Vector2(transform.position.x < movement.Opponent.position.x ? -0.7f : 0.7f, 0f); // Recua na faixa
                    moveDuration = 0.8f;
                }
                else
                {
                    float dirX = Random.value > 0.5f ? 1f : -1f;
                    moveDir = new Vector2(dirX * 0.6f, 0f); // Sidestep suave
                    moveDuration = 0.6f;
                }
                break;

            case AIDifficulty.Medium:
                // Médio: mescla aproximação, órbita lateral e recuo
                if (distanceToOpponent > 3.0f)
                {
                    moveDir = new Vector2(transform.position.x < movement.Opponent.position.x ? 0.8f : -0.8f, 0f); // Aproxima
                    moveDuration = 0.7f;
                }
                else
                {
                    float dirX = Random.value > 0.5f ? 1f : -1f;
                    moveDir = new Vector2(dirX, 0f);
                    moveDuration = 0.7f;
                }
                break;

            case AIDifficulty.Hard:
                // Difícil: fecha a distância agressivamente e circula rápido
                if (distanceToOpponent > 1.6f)
                {
                    moveDir = new Vector2(transform.position.x < movement.Opponent.position.x ? 1.0f : -1.0f, 0f); // Persegue
                    moveDuration = 0.5f;
                }
                else
                {
                    float dirX = Random.value > 0.5f ? 1f : -1f;
                    moveDir = new Vector2(dirX, 0f); // Sidestep rápido em curta distância
                    moveDuration = 0.4f;
                }
                break;
        }

        StartCoroutine(PerformMovementRoutine(moveDir, moveDuration));
    }

    private IEnumerator PerformMovementRoutine(Vector2 dir, float duration)
    {
        movement.ExternalInput = dir;
        yield return new WaitForSeconds(duration);
        movement.ExternalInput = Vector2.zero;
    }

    /// <summary>
    /// Alterna ciclicamente entre as 3 dificuldades (Easy -> Medium -> Hard -> Easy).
    /// </summary>
    public void CycleDifficulty()
    {
        switch (difficulty)
        {
            case AIDifficulty.Easy:
                Difficulty = AIDifficulty.Medium;
                break;
            case AIDifficulty.Medium:
                Difficulty = AIDifficulty.Hard;
                break;
            case AIDifficulty.Hard:
                Difficulty = AIDifficulty.Easy;
                break;
        }
    }
}
