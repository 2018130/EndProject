// DebugBoneAxis.cs — RigHead 오브젝트에 임시 부착
using UnityEngine;

public class DebugBoneAxis : MonoBehaviour
{
    [SerializeField] private float length = 0.3f;

    private void OnDrawGizmos()
    {
        // 빨강 = 로컬 X
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.right * length);

        // 초록 = 로컬 Y
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.up * length);

        // 파랑 = 로컬 Z
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.forward * length);
    }
}