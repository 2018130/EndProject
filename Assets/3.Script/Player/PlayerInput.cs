using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PlayerInputAction inputActions;
    private PlayerNetwork network;

    public event Action OnFirePerformed;
    public event Action OnFireCanceled;

    public event Action OnSkillPerformed;

    public event Action<Vector2> OnLookPerformed;   // 물풍선

    public event Action OnExecutePerformed;
    public event Action OnSkipPerformed;
    public event Action OnRevivePerformed;

    public event Action<int> OnWeaponSwap;  //무기 스왑
    public bool IsPassenger { get; set; } = false;

    public bool IsDown { get; set; } = false;

    private bool isInitialized = false;

    public bool isZooming { get; private set; }
    public bool isFiring { get; private set; }
    public event Action OnZoomPerformed;
    public event Action OnZoomCanceled;

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
        Debug.Log(inputActions.Player);
        if (isInitialized) return; // 이벤트 중복 등록 방지
        isInitialized = true;

        // 움직임
        inputActions.Player.Move.performed += context => network.SendMoveInput(context.ReadValue<Vector2>());
        inputActions.Player.Move.canceled += context =>
           network.SendMoveInput(Vector2.zero);

        // 무기 스왑
        inputActions.Player.Weapon1.performed += context => OnWeaponSwap?.Invoke(0);
        inputActions.Player.Weapon2.performed += context => OnWeaponSwap?.Invoke(1);
        inputActions.Player.Weapon3.performed += context => OnWeaponSwap?.Invoke(2);

        // 발사
        inputActions.Player.Fire.performed += context =>
        {
            if (!IsPassenger && !IsDown)
            {
                isFiring = true;          
                OnFirePerformed?.Invoke();
            }
        };

        inputActions.Player.Fire.canceled += context =>
        {
            isFiring = false;
            OnFireCanceled?.Invoke();
        };

        // 스킬
        inputActions.Player.Skill.performed += context =>
            { if (!IsPassenger && !IsDown) OnSkillPerformed?.Invoke(); };


        // 처형
        inputActions.Player.Execute.performed += context => OnExecutePerformed?.Invoke();

        // 처형 스킵
        inputActions.Player.Skip.performed += context => OnSkipPerformed?.Invoke();

        // 살리기
        inputActions.Player.Revive.performed += context => OnRevivePerformed?.Invoke();

        // 물풍선 LookAt
        inputActions.Player.Look.performed += context => OnLookPerformed?.Invoke(context.ReadValue<Vector2>());

        // 점프
        inputActions.Player.Jump.performed += context =>
        { if (!IsDown) network.SendJumpInput(); };

        // 제트팩
        inputActions.Player.JetPack.performed += context => {
            if (!IsDown) network.SendJetpackInput(true);
        };

        inputActions.Player.JetPack.canceled += context =>
           network.SendJetpackInput(false);

        // 대쉬
        inputActions.Player.Dash.performed += context =>
        {
            if (!IsDown) network.SendDashInput();
        };

        // 줌
        inputActions.Player.Zoom.performed += context =>
        {
            isZooming = true;
            OnZoomPerformed?.Invoke();
        };
        inputActions.Player.Zoom.canceled += context =>
        {
            isZooming = false;
            OnZoomCanceled?.Invoke();
        };
    }

    void OnDisable()
    {
        if (isInitialized)
        {
            //inputActions?.Player.Disable();
        }
    }
}
