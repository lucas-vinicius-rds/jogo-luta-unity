using UnityEngine;
using UnityEngine.InputSystem;
public sealed class MenuNavigationInput
{
    float repeat;
    public void Read(out int x,out int y,out bool confirm,out bool back,out bool random)
    {
        x = y = 0; confirm = back = random = false;
        Vector2 direction = Vector2.zero;
        foreach(var device in InputSystem.devices)
        {
            if(device is Keyboard k)
            {
                direction += new Vector2((k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0),(k.downArrowKey.isPressed?1:0)-(k.upArrowKey.isPressed?1:0));
                confirm |= k.enterKey.wasPressedThisFrame; back |= k.escapeKey.wasPressedThisFrame; random |= k.rKey.wasPressedThisFrame;
            }
            if(device is Gamepad g)
            {
                var stick = g.dpad.ReadValue(); if(stick.sqrMagnitude < .1f) stick = g.leftStick.ReadValue();
                direction += new Vector2(stick.x,-stick.y);
                confirm |= g.buttonSouth.wasPressedThisFrame; back |= g.buttonEast.wasPressedThisFrame; random |= g.buttonNorth.wasPressedThisFrame;
            }
        }
        if(direction.sqrMagnitude < .2f) { repeat = 0; return; }
        if(Time.unscaledTime < repeat) return;
        repeat = Time.unscaledTime + .2f;
        if(Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) x = direction.x > 0 ? 1 : -1; else y = direction.y > 0 ? 1 : -1;
    }
}
