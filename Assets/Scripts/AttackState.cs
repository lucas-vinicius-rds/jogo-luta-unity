using UnityEngine;

/// <summary>
/// Estado de Ataque: Trava o deslocamento, dispara a animação de golpe no Animator,
/// ativa a Hitbox do membro correspondente e aguarda a janela de recovery para retornar ao Neutro.
/// </summary>
public class AttackState : IFighterState
{
    private float elapsedTime;
    private bool hitboxesEnabled;
    private bool windowOpened;

    public void Enter(FighterController fighter)
    {
        // Se o lutador estava avançando em direção ao oponente, aplica um
        // pequeno impulso forward para não parar bruscamente longe do alvo
        if (fighter.Movement != null)
        {
            int moveDir = fighter.Movement.CurrentMoveDirection;
            if (moveDir > 0) // avançando (em direção ao oponente)
            {
                // Impulso de 1.5 unidades na direção do oponente — dá sensação de lunge
                Vector3 lunge = fighter.transform.forward * 1.5f;
                fighter.Movement.ApplyImpulse(lunge);
            }
            fighter.Movement.CanMove = false;
        }
        fighter.SetAttackRootMotion(true);

        elapsedTime = 0f;
        hitboxesEnabled = false;
        windowOpened = false;

        // Dispara a animação de ataque
        fighter.CrossFadeAnimation(fighter.CurrentAttackAnimHash, 0.05f);
        
        // Ajusta a velocidade da animação respeitando a configuração do golpe
        // (limitada entre 0.85f e 1.6f para garantir fluidez natural sem acelerar excessivamente)
        FighterAttackTiming currentTiming = fighter.CurrentAttackTiming;
        float targetSpeed = currentTiming != null && currentTiming.playbackSpeed > 0f ? currentTiming.playbackSpeed : 1f;
        fighter.SetAnimatorSpeed(Mathf.Clamp(targetSpeed, 0.85f, 1.6f));
    }

    public void Update(FighterController fighter)
    {
        elapsedTime += Time.deltaTime;
        FighterAttackTiming timing = fighter.CurrentAttackTiming;
        bool hasAnimationProgress = fighter.TryGetCurrentAttackProgress(out float progress);
        float attackProgress = hasAnimationProgress
            ? progress
            : elapsedTime / Mathf.Max(0.05f, fighter.CurrentAttackDuration);

        if (!windowOpened && attackProgress >= timing.activeStartNormalized)
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
        float maxDuration = Mathf.Max(2.5f, fighter.CurrentAttackDuration + 0.5f);
        bool recoveryDone = attackProgress >= timing.recoveryEndNormalized;
        bool timedOut = elapsedTime > maxDuration;
        if (recoveryDone || timedOut)
        {
            fighter.ChangeState(fighter.NeutralState);
        }
    }

    public void Exit(FighterController fighter)
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
