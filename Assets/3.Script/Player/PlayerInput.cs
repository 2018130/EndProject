using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private PlayerInputAction inputActions;
    private PlayerNetwork network;
    private AimController aimController;

    public event Action OnFirePerformed;
    public event Action OnFireCanceled;

    public event Action OnSkillPerformed;

    public event Action<Vector2> OnLookPerformed;   // 물풍선

    public event Action OnExecutePerformed;
    public event Action OnSkipPerformed;
    public event Action OnRevivePerformed;

    public event Action<int> OnWeaponSwap;  //무기 스왑


    public event Action OnZoomPerfomed;
    public event Action OnZoomCanceled;

    public Vector2 moseInput { get; private set; }
    public bool isFirePressed { get; private set; }
    public bool isZoomPressed { get; private set; }
    public bool isFiring => isFirePressed;
    public bool isZooming => isZoomPressed;



    private bool isInitialized = false;


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

        // 스킬
        inputActions.Player.Skill.performed += context => OnSkillPerformed?.Invoke();

        // 처형
        inputActions.Player.Execute.performed += context => OnExecutePerformed?.Invoke();

        // 처형 스킵
        inputActions.Player.Skip.performed += context => OnSkipPerformed?.Invoke();

        // 살리기
        inputActions.Player.Revive.performed += context => OnRevivePerformed?.Invoke();

        // 물풍선 LookAt
        inputActions.Player.Look.performed += context =>
        {
            OnLookPerformed?.Invoke(context.ReadValue<Vector2>());
            aimController?.OnLook(context);
        };

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
        //inputActions.Player.Fire.performed += context => OnFirePerformed?.Invoke();
        //inputActions.Player.Fire.canceled += context => OnFireCanceled?.Invoke();
        inputActions.Player.Fire.performed += context =>
        {
            isFirePressed = true;
            OnFirePerformed?.Invoke();
        };
        inputActions.Player.Fire.canceled += context =>
        {
            isFirePressed = false;
            OnFireCanceled?.Invoke();
        };

        //줌
        inputActions.Player.Zoom.performed += context =>
        {
            isZoomPressed = true;
            OnZoomPerfomed?.Invoke();
        };
        inputActions.Player.Zoom.canceled += context =>
        {
            isZoomPressed = false;
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
