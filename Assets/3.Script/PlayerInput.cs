using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PlayerInputAction inputActions;
    private PlayerNetwork network;

    private void Awake()
    {
        TryGetComponent<PlayerNetwork>(out network);
        inputActions = new PlayerInputAction();
    }

    private void Start()
    {
        if (!network.IsOwner)
        {
            enabled = false;
            return;
        }

        inputActions.Player.Enable();
        
        // 움직임
        inputActions.Player.Move.performed += context =>
           network.SendMoveInput(context.ReadValue<Vector2>());
        inputActions.Player.Move.canceled += context =>
           network.SendMoveInput(Vector2.zero);
        
        // 점프
        inputActions.Player.Jump.performed += context =>
           network.SendJumpInput();

        // 제트팩
        inputActions.Player.JetPack.performed += context =>
           network.SendJetpackInput(true);
        inputActions.Player.JetPack.canceled += context =>
           network.SendJetpackInput(false);

        // 대쉬
        inputActions.Player.Dash.performed += context =>
           network.SendDashInput();

        // 발사
        inputActions.Player.Fire.performed += context =>
           network.SendFireInput();

    }

    void OnDisable()
    {
        inputActions?.Player.Disable();
    }
}
