using UnityEngine;

/// <summary>Controle mínimo do Player 2 local, separado do teclado do Player 1.</summary>
public sealed class LocalPlayerTwoInput : MonoBehaviour
{
    private FighterMovement movement;
    private FighterController fighter;

    private void Awake()
    {
        movement = GetComponent<FighterMovement>();
        fighter = GetComponent<FighterController>();
    }

    private void Update()
    {
        if (movement == null || fighter == null) return;
        float x = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        if (Input.GetKey(KeyCode.RightArrow)) x += 1f;
        float y = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;
        movement.ExternalInput = new Vector2(x, y);
        if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.J)) fighter.TriggerAttack();
        if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.K)) fighter.TriggerSecondaryAttack();
    }

    private void OnDisable()
    {
        if (movement != null) movement.ExternalInput = Vector2.zero;
    }
}
