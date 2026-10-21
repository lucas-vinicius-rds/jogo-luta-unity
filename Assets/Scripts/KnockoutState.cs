using UnityEngine;

/// <summary>
/// Estado de Nocaute (KO / Dying).
/// Trava permanentemente a locomoção e ações do lutador derrotado,
/// desativa hitboxes e executa a animação oficial "Dying" do Mixamo até a queda no chão.
/// </summary>
public class KnockoutState : FighterStateBase
{
    public override void Enter(FighterController fighter)
    {
        fighter.LockDown();
        fighter.CrossFadeAnimation(fighter.KnockoutAnimHash, 0.1f);
    }

    public override void Exit(FighterController fighter)
    {
        // Permite reabilitação em caso de reinício de round / rematch
        if (fighter.Movement != null)
        {
            fighter.Movement.CanMove = true;
        }
    }
}
