using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SandDestructible : NetworkBehaviour
{
    [Header("Visual")]
    [SerializeField] private MeshFilter visualMeshFilter;

    [Header("Stage Meshes")]
    [SerializeField] private Mesh[] stageMeshes;

    [Header("Stage Colliders")]
    [SerializeField] private GameObject[] stageColliderRoots;

    [Header("Durability")]
    [SerializeField] private float maxHp = 100f;

    [SerializeField] private float despawnDelay = 2f;

    [Header("Optimization")]
    [SerializeField] private float effectCullDistance = 80f;

    [SerializeField] private bool useDeferredStageApply = true;

    // ─────────────────────────────
    // Network
    // ─────────────────────────────

    private NetworkVariable<byte> _stage = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ─────────────────────────────
    // Runtime
    // ─────────────────────────────

    private float _hp;

    private byte _currentStage = 255;

    private bool _destroyed;

    private Coroutine _stageRoutine;

    private Camera _mainCamera;

    // ─────────────────────────────
    // Unity
    // ─────────────────────────────

    private void Awake()
    {
        if (visualMeshFilter == null)
        {
            visualMeshFilter = GetComponent<MeshFilter>();
        }

        _mainCamera = Camera.main;
    }

    public override void OnNetworkSpawn()
    {
        _stage.OnValueChanged += OnStageChanged;

        if (IsServer)
        {
            _hp = maxHp;
            _stage.Value = 0;
        }

        ForceApplyStage(_stage.Value);
    }

    public override void OnNetworkDespawn()
    {
        _stage.OnValueChanged -= OnStageChanged;
    }

    // ─────────────────────────────
    // Damage
    // ─────────────────────────────

    public void RegisterHit(Vector3 hitPoint, float damage)
    {
        if (!IsServer)
            return;

        if (_destroyed)
            return;

        _hp = Mathf.Max(_hp - damage, 0f);

        byte newStage = CalculateStage(_hp);

        // 매 피격 FX
        PlayHitEffect_ClientRpc(hitPoint);

        // Stage 변경
        if (newStage != _stage.Value)
        {
            _stage.Value = newStage;
        }

        // Destroy
        if (_hp <= 0f)
        {
            _destroyed = true;

            PlayDestroyEffect_ClientRpc(hitPoint);

            StartCoroutine(DespawnAfterDelay());
        }
    }

    // ─────────────────────────────
    // Stage
    // ─────────────────────────────

    private byte CalculateStage(float hp)
    {
        float ratio = hp / maxHp;

        return (byte)Mathf.Clamp(
            Mathf.FloorToInt((1f - ratio) * 5f),
            0,
            4
        );
    }

    private void OnStageChanged(byte previous, byte current)
    {
        if (useDeferredStageApply)
        {
            if (_stageRoutine != null)
            {
                StopCoroutine(_stageRoutine);
            }

            _stageRoutine =
                StartCoroutine(DeferredApplyStage(current));
        }
        else
        {
            ForceApplyStage(current);
        }
    }

    private IEnumerator DeferredApplyStage(byte stage)
    {
        yield return null;

        ForceApplyStage(stage);
    }

    // ─────────────────────────────
    // Apply
    // ─────────────────────────────

    private void ForceApplyStage(byte stage)
    {
        if (_currentStage == stage)
            return;

        _currentStage = stage;

        ApplyMesh(stage);

        ApplyCollider(stage);
    }

    private void ApplyMesh(byte stage)
    {
        if (stageMeshes == null)
            return;

        if (stage >= stageMeshes.Length)
            return;

        Mesh targetMesh = stageMeshes[stage];

        if (targetMesh == null)
            return;

        visualMeshFilter.sharedMesh = targetMesh;
    }

    private void ApplyCollider(byte stage)
    {
        if (stageColliderRoots == null)
            return;

        for (int i = 0; i < stageColliderRoots.Length; i++)
        {
            GameObject root = stageColliderRoots[i];

            if (root == null)
                continue;

            root.SetActive(i == stage);
        }
    }

    // ─────────────────────────────
    // FX
    // ─────────────────────────────

    [ClientRpc]
    private void PlayHitEffect_ClientRpc(Vector3 hitPos)
    {
        if (!ShouldPlayEffect(hitPos))
            return;

        ParticleManager.Instance.PlayHit(hitPos);
    }

    [ClientRpc]
    private void PlayDestroyEffect_ClientRpc(Vector3 hitPos)
    {
        if (!ShouldPlayEffect(hitPos))
            return;

        ParticleManager.Instance.PlayDestroy(hitPos);
    }

    // ─────────────────────────────
    // Cull
    // ─────────────────────────────

    private bool ShouldPlayEffect(Vector3 worldPos)
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
                return false;
        }

        float sqrDistance =
            (_mainCamera.transform.position - worldPos)
            .sqrMagnitude;

        return sqrDistance <=
               effectCullDistance * effectCullDistance;
    }

    // ─────────────────────────────
    // Despawn
    // ─────────────────────────────

    private IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(despawnDelay);

        if (!IsServer)
            yield break;

        if (!IsSpawned)
            yield break;

        NetworkObject.Despawn();
    }
}