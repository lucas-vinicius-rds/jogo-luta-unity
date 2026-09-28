using UnityEngine;

/// <summary>
/// Estado de Reação a Golpe (HitStun).
/// Congela ações e movimentação temporariamente e executa a animação "Hit To Body" do Mixamo.
/// </summary>
public class HitStunState : IFighterState
{
    private float stunDuration = 0.45f;
    private float elapsedTime;

    public void SetStunDuration(float duration)
    {
        stunDuration = duration;
    }

    public void Enter(FighterController fighter)
    {
        // Trava movimentação e desativa hitboxes durante o atordoamento
        if (fighter.Movement != null)
        {
            fighter.Movement.CanMove = false;
        }
        fighter.DisableAllHitboxes();

        // Limpa comandos anteriores ao iniciar um novo atordoamento
        fighter.ClearAttackBuffer();

        elapsedTime = 0f;

        // Dispara a animação "Hit To Body"
        fighter.CrossFadeAnimation(fighter.HitStunAnimHash, 0.05f);
    }

    public void Update(FighterController fighter)
    {
        elapsedTime += Time.deltaTime;

        // Guarda comandos de ataque recebidos durante o HitStun no buffer (sem cancelar o estado)
        fighter.BufferAttackInput();

        // Ao completar a janela de atordoamento, retorna ao Neutro
        if (elapsedTime >= stunDuration)
        {
            fighter.ChangeState(fighter.NeutralState);
        }
    }

    public void Exit(FighterController fighter)
    {
        elapsedTime = 0f;
    }
}
