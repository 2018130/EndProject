using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : SingletonBehaviour<InputManager>
{
    public Vector2 MoveInput;
    public Vector2 MousePosition;
    public event Action OnClickStarted;
    public event Action OnClickEnded;
    public bool OnClicked = false;

    public void OnMove(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.phase == InputActionPhase.Performed)
        {
            MoveInput = callbackContext.ReadValue<Vector2>();
        }
        else if (callbackContext.phase == InputActionPhase.Canceled)
        {
            MoveInput = Vector2.zero;
        }
    }

    public void OnLook(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.phase == InputActionPhase.Performed)
        {
            MousePosition = callbackContext.ReadValue<Vector2>();
        }
        else if (callbackContext.phase == InputActionPhase.Canceled)
        {
            MousePosition = Vector2.zero;
        }
    }

    public void OnAttack(InputAction.CallbackContext callbackContext)
    {
        if (callbackContext.phase == InputActionPhase.Performed)
        {
            OnClickStarted?.Invoke();
            OnClicked = true;
        }
        else if(callbackContext.phase == InputActionPhase.Canceled)
        {
            OnClickEnded?.Invoke();
            OnClicked = false;
        }
    }
}
