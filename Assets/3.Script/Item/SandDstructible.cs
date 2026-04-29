using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class SandDestructible : NetworkBehaviour
{
    [Header("Deform (구멍)")]
    [SerializeField] private float deformRadius = 1.2f;
    [SerializeField] private float deformDepth = 0.8f;
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Fragments (파편)")]
    [SerializeField] private GameObject[] fragmentPrefabs;  // 로우폴리 모래 조각들
    [SerializeField] private int fragmentCount = 8;
    [SerializeField] private float fragmentForce = 4f;

    [Header("Particle")]
    [SerializeField] private ParticleSystem hitParticle;    // 모래 먼지 파티클

    // ── NGO 상태 동기화 ──────────────────────────────
    private NetworkVariable<bool> isDestroyed = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private NetworkVariable<Vector3> lastHitPoint = new(
        Vector3.zero,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // ── 메시 ─────────────────────────────────────────
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] originalVertices;

    // 히트 누적 (늦은 접속자 복원용)
    private readonly List<Vector3> hitHistory = new();

    // ─────────────────────────────────────────────────
    //  초기화
    // ─────────────────────────────────────────────────

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        mesh = Instantiate(meshFilter.sharedMesh); // 에셋 오염 방지
        meshFilter.mesh = mesh;
        originalVertices = mesh.vertices;
        vertices = (Vector3[])originalVertices.Clone();
    }

    public override void OnNetworkSpawn()
    {
        isDestroyed.OnValueChanged += OnDestroyedChanged;

        // 늦게 접속한 클라이언트 → 기존 히트 이력 요청
        if (!IsServer)
            RequestHistory_ServerRpc();
    }

    // ─────────────────────────────────────────────────
    //  외부 진입점 (발사체에서 서버가 호출)
    // ─────────────────────────────────────────────────

    public void RegisterHit(Vector3 worldHitPos)
    {
        if (!IsServer) return;

        hitHistory.Add(worldHitPos);
        lastHitPoint.Value = worldHitPos;

        // 완전히 부숴지는 타입이면 isDestroyed = true
        // 여러 번 맞는 타입이면 isDestroyed 안 써도 됨
        BroadcastHit_Rpc(worldHitPos);
    }

    // ─────────────────────────────────────────────────
    //  RPC
    // ─────────────────────────────────────────────────

    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastHit_Rpc(Vector3 worldHitPos)
    {
        DeformMesh(worldHitPos);
        SpawnFragmentsLocally(worldHitPos);
        PlayParticle(worldHitPos);
    }

    [Rpc(SendTo.Server)]
    private void RequestHistory_ServerRpc(RpcParams rpc = default)
    {
        if (hitHistory.Count == 0) return;
        SendHistory_Rpc(hitHistory.ToArray(),
            RpcTarget.Single(rpc.Receive.SenderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendHistory_Rpc(Vector3[] hits, RpcParams rpc = default)
    {
        foreach (var hit in hits)
            DeformMesh(hit); // 파편/파티클은 재생 안 함 (이미 지난 이벤트)
    }

    private void OnDestroyedChanged(bool prev, bool curr)
    {
        // 완전 파괴 타입 쓸 때 활용
    }

    // ─────────────────────────────────────────────────
    //  메시 변형
    // ─────────────────────────────────────────────────

    private void DeformMesh(Vector3 worldHitPos)
    {
        Vector3 localHit = transform.InverseTransformPoint(worldHitPos);
        bool changed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            float dist = new Vector2(
                vertices[i].x - localHit.x,
                vertices[i].z - localHit.z).magnitude;

            if (dist >= deformRadius) continue;

            float factor = falloff.Evaluate(dist / deformRadius);

            // 이미 많이 패인 버텍스는 덜 파임
            float alreadyDeformed = originalVertices[i].y - vertices[i].y;
            float maxDeform = 0.3f; // 최대 패임 깊이 제한
            if (alreadyDeformed >= maxDeform) continue;

            float actualDepth = Mathf.Min(
                deformDepth * factor,
                maxDeform - alreadyDeformed // 남은 여유만큼만
            );

            vertices[i].y -= actualDepth;
            changed = true;
        }

        if (!changed) return;

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        // 콜라이더 강제 갱신
        Mesh colliderMesh = new Mesh();
        colliderMesh.vertices = mesh.vertices;
        colliderMesh.triangles = mesh.triangles;
        colliderMesh.RecalculateNormals();
        meshCollider.enabled = false;
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = colliderMesh;
        meshCollider.enabled = true;
    }

    // ─────────────────────────────────────────────────
    //  로컬 이펙트 (동기화 불필요)
    // ─────────────────────────────────────────────────

    private void SpawnFragmentsLocally(Vector3 worldHitPos)
    {
        if (fragmentPrefabs == null || fragmentPrefabs.Length == 0) return;

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject prefab = fragmentPrefabs[Random.Range(0, fragmentPrefabs.Length)];
            GameObject frag = Instantiate(prefab, worldHitPos, Random.rotation);

            // 위쪽 + 바깥쪽으로 퍼지는 힘
            Vector3 dir = (Random.insideUnitSphere + Vector3.up).normalized;
            frag.GetComponent<SandFragment>().Launch(dir * fragmentForce);
        }
    }

    private void PlayParticle(Vector3 worldHitPos)
    {
        if (hitParticle == null) return;
        hitParticle.transform.position = worldHitPos;
        hitParticle.Play();
    }

    // ─────────────────────────────────────────────────
    //  정리
    // ─────────────────────────────────────────────────

    public override void OnDestroy()
    {
        isDestroyed.OnValueChanged -= OnDestroyedChanged;
    }

    public void ResetSand()
    {
        if (!IsServer) return;
        hitHistory.Clear();
        ResetSand_Rpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ResetSand_Rpc()
    {
        vertices = (Vector3[])originalVertices.Clone();
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = mesh;
    }
}