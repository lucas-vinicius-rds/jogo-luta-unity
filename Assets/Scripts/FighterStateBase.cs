using UnityEngine;

/// <summary>
/// Classe base abstrata para estados da FSM do lutador.
/// Fornece implementações virtuais vazias dos métodos do ciclo de vida de IFighterState,
/// eliminando código boilerplate e repetição entre estados.
/// </summary>
public abstract class FighterStateBase : IFighterState
{
    public virtual void Enter(FighterController fighter) { }
    public virtual void Update(FighterController fighter) { }
    public virtual void Exit(FighterController fighter) { }
}
