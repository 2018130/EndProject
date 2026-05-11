using UnityEngine;

public class GripTargetAim : MonoBehaviour
{
    private Transform _aimTarget;
    private AimController _aimController;

    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    [SerializeField] private float aimLerpSpeed = 8f;

    // ★ 초기 로컬 회전값 저장 (Initialize 시점의 기본 자세)
    private Quaternion _defaultLocalRotation;

    public void Initialize(Transform aimTarget, AimController aimController)
    {
        _aimTarget = aimTarget;
        _aimController = aimController;

        // 초기화 시점의 로컬 회전을 기본값으로 저장
        _defaultLocalRotation = transform.localRotation;
    }

    private bool _isInitialized = false;

    private void LateUpdate()
    {
        if (_aimTarget == null || _aimController == null) return;

        if (!_isInitialized)
        {
            _defaultLocalRotation = transform.localRotation;
            _isInitialized = true;
            //Debug.Log($"[GripTargetAim] 기본 회전 저장: {_defaultLocalRotation.eulerAngles}");
        }

        if (!_aimController.GetIsAiming())
        {
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                _defaultLocalRotation,
                aimLerpSpeed * Time.deltaTime
            );
            return;
        }

        Vector3 dir = (_aimTarget.position - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(rotationOffset);

        // ★ 적용 전후 값 확인
        //Debug.Log($"[GripTargetAim] 적용 전: {transform.rotation.eulerAngles} → 적용 후 목표: {targetRot.eulerAngles}");

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, aimLerpSpeed * Time.deltaTime);
    }
}