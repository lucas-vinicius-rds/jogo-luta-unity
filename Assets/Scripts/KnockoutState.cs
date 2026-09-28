using UnityEngine;

/// <summary>
/// Estado de Nocaute (KO / Dying).
/// Trava permanentemente a locomoção e ações do lutador derrotado,
/// desativa hitboxes e executa a animação oficial "Dying" do Mixamo até a queda no chão.
/// </summary>
public class KnockoutState : IFighterState
{
    public void Enter(FighterController fighter)
    {
        // 1. Trava movimentação e física
        if (fighter.Movement != null)
        {
            fighter.Movement.CanMove = false;
        }

        // 2. Desativa quaisquer hitboxes ativas e limpa buffer
        fighter.DisableAllHitboxes();
        fighter.ClearAttackBuffer();

        // 3. Executa a animação "Dying" de morte/queda no chão
        fighter.CrossFadeAnimation(fighter.KnockoutAnimHash, 0.1f);
    }

    public void Update(FighterController fighter)
    {
        // Lutador nocauteado permanece inativo até o reinício da luta
    }

    public void Exit(FighterController fighter)
    {
        // Permite reabilitação em caso de reinício de round / rematch
        if (fighter.Movement != null)
        {
            fighter.Movement.CanMove = true;
        }
    }
}
