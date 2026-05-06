using UnityEngine;
using Unity.Netcode;

public class GunAlignToHand : NetworkBehaviour
{
    [Header("오프셋 (총 모델에 맞게 조정)")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;

    private Transform _handBone;
    private bool _isAligning = false;

    // ★ 런타임에 AimRigController가 호출해서 주입
    public void SetHandBone(Transform handBone)
    {
        _handBone = handBone;
    }

    public void StartAlign()
    {
        if (_handBone == null)
        {
            Debug.LogWarning($"[GunAlignToHand] {gameObject.name}: handBone이 없습니다. Initialize()가 먼저 호출되어야 합니다.");
            return;
        }
        _isAligning = true;
    }

    public void StopAlign() => _isAligning = false;

    private void LateUpdate()
    {
        if (!_isAligning || _handBone == null) return;

        transform.position = _handBone.position
            + _handBone.TransformDirection(localPositionOffset);
        transform.rotation = _handBone.rotation
            * Quaternion.Euler(localRotationOffset);
    }
}