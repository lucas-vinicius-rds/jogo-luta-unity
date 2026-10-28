using UnityEngine;

/// <summary>
/// Estado de Ataque: Trava o deslocamento, dispara a animação de golpe no Animator,
/// ativa a Hitbox do membro correspondente e aguarda a janela de recovery para retornar ao Neutro.
/// </summary>
public class AttackState : FighterStateBase
{
    private float elapsedTime;
    private bool hitboxesEnabled;
    private bool windowOpened;
    private float currentAttackDuration;
    private FighterAttackTiming currentTiming;

    public override void Enter(FighterController fighter)
    {
        if (fighter.Movement != null) fighter.Movement.CanMove = false;
        fighter.SetAttackRootMotion(false);

        elapsedTime = 0f;
        hitboxesEnabled = false;
        windowOpened = false;

        // Dispara a animação de ataque
        fighter.CrossFadeAnimation(fighter.CurrentAttackAnimHash, 0.05f);
        
        // Ajusta a velocidade da animação respeitando a configuração do golpe
        // (limitada entre 0.75f e 1.1f; o gerador autoral configura 1f)
        currentTiming = fighter.CurrentAttackTiming;
        currentAttackDuration = fighter.CurrentAttackDuration;
        float targetSpeed = currentTiming != null && currentTiming.playbackSpeed > 0f ? currentTiming.playbackSpeed : 1f;
        fighter.SetAnimatorSpeed(Mathf.Clamp(targetSpeed, 0.75f, 1.1f));
    }

    public override void Update(FighterController fighter)
    {
        elapsedTime += Time.deltaTime;
        FighterAttackTiming timing = currentTiming ?? fighter.CurrentAttackTiming;
        bool hasAnimationProgress = fighter.TryGetCurrentAttackProgress(out float progress);
        float attackProgress = hasAnimationProgress
            ? progress
            : elapsedTime / Mathf.Max(0.05f, currentAttackDuration);

        if (!windowOpened && attackProgress >= timing.activeStartNormalized && attackProgress < timing.activeEndNormalized)
        {
            fighter.EnableCurrentAttackHitboxes();
            hitboxesEnabled = true;
            windowOpened = true;
        }

        // Desativa as hitboxes após a janela ativa do golpe
        if (hitboxesEnabled && attackProgress >= timing.activeEndNormalized)
        {
            fighter.DisableAllHitboxes();
            hitboxesEnabled = false;
        }

        // Retorna ao Neutro ao atingir recoveryEndNormalized
        // O timeout de segurança é dinâmico baseado na duração real do ataque (evitando cortar prematuramente)
        float maxDuration = Mathf.Max(2.5f, currentAttackDuration + 0.5f);
        bool recoveryDone = attackProgress >= timing.recoveryEndNormalized;
        bool timedOut = elapsedTime > maxDuration;
        if (recoveryDone || timedOut)
        {
            fighter.ChangeState(fighter.NeutralState);
        }
    }

    public override void Exit(FighterController fighter)
    {
        elapsedTime = 0f;
        hitboxesEnabled = false;
        windowOpened = false;
        // Garante que nenhuma hitbox permaneça ativa após sair do ataque
        fighter.DisableAllHitboxes();
        fighter.SetAttackRootMotion(false);
        fighter.SetAnimatorSpeed(1f);
    }
}
