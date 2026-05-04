// GunAlignToHand.cs
// 각 Gun NetworkObject에 부착
// Hand 본의 위치/회전을 LateUpdate에서 따라감
using UnityEngine;
using Unity.Netcode;

public class GunAlignToHand : NetworkBehaviour
{
    [Header("Hand Bone (Inspector에서 연결)")]
    [SerializeField] private Transform handBone;

    [Header("손에 쥐었을 때 오프셋 (모델에 맞게 조정)")]
    [SerializeField] private Vector3 localPositionOffset = Vector3.zero;
    [SerializeField] private Vector3 localRotationOffset = Vector3.zero;

    public void SetHandBone(Transform bone) => handBone = bone;

    private bool _isAligning = false;

    /// <summary>
    /// AimRigController에서 총이 활성화될 때 호출
    /// </summary>
    public void StartAlign() => _isAligning = true;

    /// <summary>
    /// 총이 비활성화될 때 호출
    /// </summary>
    public void StopAlign() => _isAligning = false;

    // Animation Rigging이 LateUpdate에서 본을 확정하므로
    // 반드시 LateUpdate에서 따라가야 합니다
    private void LateUpdate()
    {
        if (!_isAligning || handBone == null) return;

        transform.position = handBone.position
            + handBone.TransformDirection(localPositionOffset);
        transform.rotation = handBone.rotation
            * Quaternion.Euler(localRotationOffset);
    }
}