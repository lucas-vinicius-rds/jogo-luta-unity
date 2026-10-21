using UnityEngine;

/// <summary>
/// Estado de Reação a Golpe (HitStun).
/// Congela ações e movimentação temporariamente e executa a animação "Hit To Body" do Mixamo.
/// </summary>
public class HitStunState : FighterStateBase
{
    private float stunDuration = 0.45f;
    private float elapsedTime;

    public void SetStunDuration(float duration)
    {
        stunDuration = duration;
    }

    public override void Enter(FighterController fighter)
    {
        fighter.LockDown();
        elapsedTime = 0f;
        fighter.CrossFadeAnimation(fighter.HitStunAnimHash, 0.05f);
    }

    public override void Update(FighterController fighter)
    {
        // O timer do HitStun só decai quando o hitstop NÃO estiver ativo
        if (fighter == null || !fighter.IsInHitstop)
        {
            elapsedTime += Time.deltaTime;
        }

        // Guarda comandos de ataque recebidos durante o HitStun no buffer (sem cancelar o estado)
        fighter.BufferAttackInput();

        // Ao completar a janela de atordoamento, retorna ao Neutro
        if (elapsedTime >= stunDuration)
        {
            fighter.ChangeState(fighter.NeutralState);
        }
    }
}
