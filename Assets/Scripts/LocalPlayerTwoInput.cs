using UnityEngine;

/// <summary>
/// Configura o lutador como Player 2 local utilizando o esquema centralizado de inputs.
/// Elimina teclas hardcoded e desacopla as ações do Player 1.
/// </summary>
public sealed class LocalPlayerTwoInput : MonoBehaviour
{
    [Tooltip("Configuração de entrada do Player 2.")]
    [SerializeField] private FighterInputConfig inputConfig;

    private FighterMovement movement;
    private FighterController fighter;

    private void Awake()
    {
        movement = GetComponent<FighterMovement>();
        fighter = GetComponent<FighterController>();

        // Inicializa configuração padrão para Player 2 caso não atribuída
        if (inputConfig == null)
        {
            inputConfig = FighterInputConfig.CreatePlayerTwo();
        }

        // Aplica o mapeamento do P2 diretamente aos componentes do lutador
        if (movement != null)
        {
            movement.InputConfig = inputConfig;
            movement.IsPlayerControlled = true;
        }

        if (fighter != null)
        {
            fighter.InputConfig = inputConfig;
        }
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.ExternalInput = Vector2.zero;
        }
    }
}
