using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimController : NetworkBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float aimRotationSpeed = 15f;
    [SerializeField] private float moveRotationSpeed = 10f;

    [Header("Aim Ray & Assist")]
    [SerializeField] private LayerMask aimLayerMask = ~0;
    [SerializeField] private float maxAimDistance = 100f;
    [Tooltip("줌 시 에임 보정을 위한 반경")]
    [SerializeField] private float aimAssistRadius = 1.5f;
    [Tooltip("에임 보정으로 끌어당길 대상 레이어 (예: Enemy)")]
    [SerializeField] private LayerMask aimAssistLayerMask;

    [Tooltip("레이어 마스크에서 제외할 플레이어 레이어 이름")]
    [SerializeField] private string playerLayerName = "Player";

    public NetworkVariable<Vector3> NetAimDirection = new NetworkVariable<Vector3>(
        Vector3.forward,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public NetworkVariable<bool> NetIsAiming = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private Camera mainCamera;
    private Vector3 localAimDirection = Vector3.forward;
    private bool localIsAiming;
    private PlayerInput input;


    public Vector3 AimWorldPoint { get; private set; }
    public Vector3 GetAimDirection() => IsOwner ? localAimDirection : NetAimDirection.Value;
    public bool GetIsAiming() => IsOwner ? localIsAiming : NetIsAiming.Value;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        mainCamera = Camera.main;
        TryGetComponent(out input);

        int playerLayer = LayerMask.NameToLayer(playerLayerName);
        if (playerLayer >= 0)
            aimLayerMask &= ~(1 << playerLayer);
    }

    private void Update()
    {
        if (IsOwner)
        {
            UpdateLocalAimState();
            CalculateAimDirection();
            HandleAimRotation();
        }
        else
        {
            ApplyRemoteRotation();
        }
    }

    private void UpdateLocalAimState()
    {
        if (input == null) return;

        bool newIsAiming = input.isZooming || input.isFiring;
        if (newIsAiming == localIsAiming) return;

        localIsAiming = newIsAiming;
        NetIsAiming.Value = localIsAiming;
    }

    private void CalculateAimDirection()
    {
        if (mainCamera == null) return;

        Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        Ray ray = mainCamera.ScreenPointToRay(screenCenter);

        Vector3 targetPoint;

        if(input != null && input.isZooming)
        {
            if(Physics.SphereCast(ray, aimAssistRadius, out RaycastHit assistHit, maxAimDistance, aimAssistLayerMask))
            {
                if(IsTargetEnemy(assistHit.collider.gameObject))
                {
                    targetPoint = assistHit.collider.bounds.center;
                }
                else
                {
                    targetPoint = assistHit.point;
                }
            }
            else
            {
                targetPoint = ray.origin + ray.direction * maxAimDistance;
            }
        }
        else if(Physics.Raycast(ray, out RaycastHit hit, maxAimDistance, aimLayerMask))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = ray.origin + ray.direction * maxAimDistance;
        }

        AimWorldPoint = targetPoint;
        localAimDirection = (targetPoint - transform.position).normalized;
        NetAimDirection.Value = localAimDirection;

        Debug.DrawLine(ray.origin, AimWorldPoint, Color.yellow);
    }

    private bool IsTargetEnemy(GameObject target)
    {
        if(target.TryGetComponent<PlayerHealth>(out var targetHealth))
        {
            Faction myFaction = (Faction)GetComponent<PlayerHealth>().PlayerFactionInt.Value;
            Faction targetFaction = (Faction)targetHealth.PlayerFactionInt.Value;

            return myFaction != targetFaction;
        }
        return false;
    }

    private void HandleAimRotation()
    {
        if (!localIsAiming) return;
        RotateHorizontalToward(localAimDirection, aimRotationSpeed);
    }


    private void ApplyRemoteRotation()
    {
        if (!NetIsAiming.Value) return;
        RotateHorizontalToward(NetAimDirection.Value, aimRotationSpeed);
    }


    private void RotateHorizontalToward(Vector3 direction, float speed)
    {
        Vector3 horizontal = new Vector3(direction.x, 0f, direction.z);
        if (horizontal.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(horizontal.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, speed * Time.deltaTime);
    }

    public void RotateTowardMovement(Vector3 moveDirection)
    {
        if (!IsOwner) return;
        if (localIsAiming) return;

        Vector3 horizontal = new Vector3(moveDirection.x, 0f, moveDirection.z);
        if (horizontal.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(horizontal.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, moveRotationSpeed * Time.deltaTime);
    }

    public Vector3 GetProjectileDirection(Vector3 muzzlePosition)
    {
        if (IsOwner)
            return (AimWorldPoint - muzzlePosition).normalized;

        return NetAimDirection.Value;
    }

    private void OnDrawGizmos()
    {
        if (mainCamera == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(AimWorldPoint, 0.5f);

        Gizmos.color = Color.blue;
        Vector3 muzzlePos = transform.position;
        Gizmos.DrawLine(muzzlePos, AimWorldPoint);
    }
}
