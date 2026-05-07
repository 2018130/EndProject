using UnityEngine;

public class GripTargetAim : MonoBehaviour
{
    private Transform _aimTarget;
    private AimController _aimController;

    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private float aimLerpSpeed = 8f;

    public void Initialize(Transform aimTarget, AimController aimController)
    {
        _aimTarget = aimTarget;
        _aimController = aimController;
        Debug.Log($"[GripTargetAim] Initialize 완료 - {gameObject.name}");
    }

    private void LateUpdate()
    {
        if (_aimTarget == null || _aimController == null)
        {
            Debug.LogWarning($"[GripTargetAim] {gameObject.name}: 레퍼런스 없음 - aimTarget:{_aimTarget != null}, aimController:{_aimController != null}");
            return;
        }

        if (!_aimController.GetIsAiming())
        {
            // 이 로그가 계속 찍히면 GetIsAiming()이 항상 false
            Debug.Log($"[GripTargetAim] {gameObject.name}: Not aiming");
            return;
        }

        //Debug.Log($"[GripTargetAim] {gameObject.name}: Aiming 실행 중");

        Vector3 dir = (_aimTarget.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, aimLerpSpeed * Time.deltaTime);
    }
}