using UnityEngine;

/// <summary>
/// Estado Neutro: Permite movimentação 3D livre via FighterMovement e escuta inputs de ataque.
/// Transiciona para AttackState ao receber o comando de ataque.
/// Atualiza o parâmetro 'Speed' do Animator para transicionar entre Idle e Run.
/// </summary>
public class NeutralState : IFighterState
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public void Enter(FighterController fighter)
    {
        // Garante que o movimento 3D está habilitado
        if (fighter.Movement != null)
        {
            fighter.Movement.CanMove = true;
        }

        // Toca animação neutra (Idle/Locomoção)
        fighter.CrossFadeAnimation(fighter.NeutralAnimHash, 0.1f);
    }

    public void Update(FighterController fighter)
    {
        // Monitora entrada de ataque para transicionar
        FighterAttackType attack = fighter.ReadAttackCommand();
        if (attack != FighterAttackType.None)
        {
            fighter.TriggerAttack(attack);
            return;
        }

        // Alimenta o parâmetro Speed do Animator com a magnitude do movimento
        if (fighter.Animator != null && fighter.Movement != null)
        {
            float speed = fighter.Movement.CurrentMoveDirection != 0 && !fighter.Movement.IsCrouching
                ? fighter.Movement.CurrentSpeedMagnitude
                : 0f;
            fighter.Animator.SetFloat(SpeedHash, speed);
        }
    }

    public void Exit(FighterController fighter)
    {
        // Zera o Speed ao sair para outros estados (Ataque / HitStun)
        if (fighter.Animator != null)
        {
            fighter.Animator.SetFloat(SpeedHash, 0f);
        }
    }
}

