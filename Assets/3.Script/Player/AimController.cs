using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimController : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraHolder;

    [Header("Aim Settings")]
    [SerializeField] private float normalSensitivity = 1f;
    [SerializeField] private float aimSensitivity = 0.5f;
    [SerializeField] private float aimSpeed = 10f;

    [Header("Zoom Settings")]
    [SerializeField] private float normalFOV = 60f;
    [SerializeField] private float zoomFOV = 30f;
    [SerializeField] private float zoomSpeed = 10f;

    [Header("Aim Assist")]
    [SerializeField] private bool enableAimAssist = true;
    [SerializeField] private float aimAssistRadius = 2f;
    [SerializeField] private float aimAssistStrength = 0.3f;
    [SerializeField] private LayerMask targetLayer;

    private NetworkVariable<bool> isAiming = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isZooming = new NetworkVariable<bool>(false);

    private Vector2 lookInput;
    private float currentSensitivity;
    private float targetFOV;
    private Vector3 aimDirection;
    private float cameraPitch = 0f;

    private PlayerInput playerInput;
    private Transform playerTransform;

    public bool IsAiming => isAiming.Value;
    public bool IsZooming => isZooming.Value;
    public Vector3 AimDirection => aimDirection;
    public float CurrentSensitivity => currentSensitivity;

    private void Awake()
    {
        playerTransform = transform;
        currentSensitivity = normalSensitivity;
        targetFOV = normalFOV;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (cameraHolder == null)
            {
                cameraHolder = playerCamera.transform.parent;
            }
        }
        else
        {
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(false);
            }
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateCameraRotation();
        UpdateFOV();
        UpdateAimDirection();

        if (isZooming.Value && enableAimAssist)
        {
            ApplyAimAssist();
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        lookInput = context.ReadValue<Vector2>();
    }

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed)
        {
            SetAiming_ServerRpc(true);
        }
        else if (context.canceled)
        {
            SetAiming_ServerRpc(false);
        }
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (context.performed)
        {
            SetZooming_ServerRpc(true);
        }
        else if (context.canceled)
        {
            SetZooming_ServerRpc(false);
        }
    }

    [Rpc(SendTo.Server)]
    private void SetAiming_ServerRpc(bool aiming)
    {
        isAiming.Value = aiming;
    }

    [Rpc(SendTo.Server)]
    private void SetZooming_ServerRpc(bool zooming)
    {
        isZooming.Value = zooming;
    }

    private void UpdateCameraRotation()
    {
        if (cameraHolder == null || playerCamera == null) return;

        // Sensitivity 조정 (Aim 또는 Zoom 중일 때 감도 감소)
        currentSensitivity = (isAiming.Value || isZooming.Value) ? aimSensitivity : normalSensitivity;

        // 마우스 입력 적용
        float mouseX = lookInput.x * currentSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * currentSensitivity * Time.deltaTime;

        // Pitch 계산 (상하 회전, 카메라만)
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);

        // 카메라 회전 적용
        cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        // 좌우 회전 계산
        if (isAiming.Value || isZooming.Value)
        {
            // Aim 모드: 플레이어가 카메라 방향을 따라감
            playerTransform.Rotate(Vector3.up * mouseX);
        }
        else
        {
            // 일반 모드: 이동 방향을 바라봄 (PlayerMovement에서 처리)
            // 카메라만 자유롭게 회전
            cameraHolder.Rotate(Vector3.up * mouseX);
        }
    }

    private void UpdateFOV()
    {
        if (playerCamera == null) return;

        targetFOV = isZooming.Value ? zoomFOV : normalFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
    }

    private void UpdateAimDirection()
    {
        if (playerCamera == null) return;

        // 카메라가 바라보는 방향을 Aim 방향으로 설정
        aimDirection = playerCamera.transform.forward;

        // Aim 모드가 아니면 플레이어의 forward 방향 사용
        if (!isAiming.Value && !isZooming.Value)
        {
            aimDirection = playerTransform.forward;
        }
    }

    private void ApplyAimAssist()
    {
        if (playerCamera == null) return;

        // 카메라 중심에서 Raycast
        Ray ray = new Ray(playerCamera.transform.position, aimDirection);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, targetLayer))
        {
            // 가장 가까운 타겟 찾기
            Collider[] colliders = Physics.OverlapSphere(hit.point, aimAssistRadius, targetLayer);

            if (colliders.Length > 0)
            {
                Transform closestTarget = null;
                float closestDistance = float.MaxValue;

                foreach (Collider col in colliders)
                {
                    float distance = Vector3.Distance(playerCamera.transform.position, col.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestTarget = col.transform;
                    }
                }

                if (closestTarget != null)
                {
                    // 타겟 방향으로 보정
                    Vector3 targetDirection = (closestTarget.position - playerCamera.transform.position).normalized;
                    aimDirection = Vector3.Lerp(aimDirection, targetDirection, aimAssistStrength * Time.deltaTime);
                }
            }
        }
    }

    public Vector3 GetAimPoint(float distance = 100f)
    {
        if (playerCamera == null)
            return playerTransform.position + playerTransform.forward * distance;

        return playerCamera.transform.position + aimDirection * distance;
    }

    public Quaternion GetAimRotation()
    {
        return Quaternion.LookRotation(aimDirection);
    }

    // 총알 발사 위치 계산 (카메라 중심에서 Raycast)
    public bool GetFirePoint(out Vector3 hitPoint, out Vector3 direction, float maxDistance = 100f)
    {
        if (playerCamera == null)
        {
            hitPoint = Vector3.zero;
            direction = playerTransform.forward;
            return false;
        }

        Ray ray = new Ray(playerCamera.transform.position, aimDirection);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxDistance))
        {
            hitPoint = hit.point;
            direction = (hit.point - playerCamera.transform.position).normalized;
            return true;
        }
        else
        {
            hitPoint = playerCamera.transform.position + aimDirection * maxDistance;
            direction = aimDirection;
            return false;
        }
    }
}
