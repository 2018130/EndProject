// GripTargetAim.cs
// 각 Gun의 GripTarget 오브젝트에 부착
using UnityEngine;

public class GripTargetAim : MonoBehaviour
{
    // AimRigController에서 런타임에 주입
    private Transform _aimTarget;

    [Tooltip("손목 회전 보정값 (총 모델마다 다름, Inspector에서 조정)")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    public void Initialize(Transform aimTarget)
    {
        _aimTarget = aimTarget;
    }

    // Two Bone IK가 LateUpdate에서 실행되므로
    // 그 이전에 GripTarget 회전을 확정해야 함
    // → Script Execution Order에서 이 스크립트를 AimRigController보다 먼저 실행
    private void LateUpdate()
    {
        if (_aimTarget == null) return;

        // GripTarget에서 AimTarget 방향을 바라보도록 회전
        Vector3 direction = (_aimTarget.position - transform.position).normalized;
        if (direction.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation * Quaternion.Euler(rotationOffset);
    }
}