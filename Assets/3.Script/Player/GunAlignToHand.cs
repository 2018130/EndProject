using UnityEngine;
using Unity.Netcode;

public class GunAlignToHand : NetworkBehaviour
{
    [Header("오프셋")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;

    [Header("조준 설정")]
    [SerializeField] private float aimRotationSpeed = 8f;

    private Transform _handBone;
    private Transform _aimTarget;
    private AimController _aimController;
    private bool _isAligning = false;

    // 비조준 시 기본 회전 저장
    private Quaternion _defaultLocalRotation;
    private bool _defaultSaved = false;

    public void SetHandBone(Transform handBone) => _handBone = handBone;

    public void SetAimInfo(Transform aimTarget, AimController aimController)
    {
        _aimTarget = aimTarget;
        _aimController = aimController;
    }

    public void StartAlign()
    {
        if (_handBone == null)
        {
            Debug.LogWarning($"[GunAlignToHand] {gameObject.name}: handBone 없음");
            return;
        }
        _isAligning = true;
    }

    public void StopAlign() => _isAligning = false;

    private void LateUpdate()
    {
        if (!_isAligning || _handBone == null) return;

        // 위치: HandBone 위치로 고정
        transform.position = _handBone.position
            + _handBone.TransformDirection(localPositionOffset);

        // 기본 회전 저장 (최초 1회)
        if (!_defaultSaved)
        {
            _defaultLocalRotation = Quaternion.Euler(localRotationOffset);
            _defaultSaved = true;
        }

        if (_aimController != null && _aimController.GetIsAiming() && _aimTarget != null)
        {
            // 조준 시: AimTarget 방향으로 총 회전
            Vector3 dir = (_aimTarget.position - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion aimRot = Quaternion.LookRotation(dir)
                                  * Quaternion.Euler(localRotationOffset);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, aimRot, aimRotationSpeed * Time.deltaTime);
            }
        }
        else
        {
            // 비조준 시: HandBone 회전 + 기본 오프셋으로 복원
            Quaternion defaultRot = _handBone.rotation
                                  * Quaternion.Euler(localRotationOffset);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, defaultRot, aimRotationSpeed * Time.deltaTime);
        }
    }
}