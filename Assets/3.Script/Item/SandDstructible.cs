using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SandDestructible : NetworkBehaviour
{
    [Header("Stage Meshes")]
    [Tooltip("0 = pristine")]
    [SerializeField] private Mesh[] stageMeshes;

    [Header("Stage Collider Roots")]
    [Tooltip("단계별 Collider Root")]
    [SerializeField] private GameObject[] stageColliderRoots;

    [Header("Durability")]
    [SerializeField] private float maxHp = 100f;

    [SerializeField] private float despawnDelay = 2f;

    [Header("FX")]
    [SerializeField] private ParticleSystem hitParticle;

    [SerializeField] private ParticleSystem destroyParticle;

    [Header("Optimization")]
    [Tooltip("거리 밖이면 이펙트 생략")]
    [SerializeField] private float effectCullDistance = 80f;

    [Tooltip("Collider 변경 분산 적용")]
    [SerializeField] private bool useDeferredStageApply = true;

    // ─────────────────────────────────────────────
    // Network
    // ─────────────────────────────────────────────

    /*
        0 = pristine
        1 = light damage
        2 = medium damage
        3 = heavy damage
        4 = destroyed
    */

    private NetworkVariable<byte> _stage = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // 서버 전용
    private float _hp;

    // ─────────────────────────────────────────────
    // Cache
    // ─────────────────────────────────────────────

    private MeshFilter _meshFilter;

    private byte _currentStage = 255;

    private bool _destroyed;

    private Camera _mainCamera;

    private Coroutine _stageRoutine;

    // ─────────────────────────────────────────────
    // Unity
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();

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

        // Late Join 대응
        ForceApplyStage(_stage.Value);
    }

    public override void OnNetworkDespawn()
    {
        _stage.OnValueChanged -= OnStageChanged;
    }

    // ─────────────────────────────────────────────
    // Public Damage Entry
    // ─────────────────────────────────────────────

    public void RegisterHit(Vector3 hitPoint, float damage)
    {
        if (!IsServer)
            return;

        if (_destroyed)
            return;

        _hp = Mathf.Max(_hp - damage, 0f);

        byte newStage = CalculateStage(_hp);

        // ─────────────────────────────
        // Stage changed only
        // ─────────────────────────────

        if (newStage != _stage.Value)
        {
            _stage.Value = newStage;

            // Stage 변경시에만 FX 전송
            PlayHitEffect_ClientRpc(hitPoint);
        }

        // ─────────────────────────────
        // Destroy
        // ─────────────────────────────

        if (_hp <= 0f)
        {
            _destroyed = true;

            PlayDestroyEffect_ClientRpc(hitPoint);

            StartCoroutine(DespawnAfterDelay());
        }
    }

    // ─────────────────────────────────────────────
    // Stage Logic
    // ─────────────────────────────────────────────

    private byte CalculateStage(float hp)
    {
        float ratio = hp / maxHp;

        byte stage = (byte)Mathf.Clamp(
            Mathf.FloorToInt((1f - ratio) * 5f),
            0,
            4
        );

        return stage;
    }

    private void OnStageChanged(byte previous, byte current)
    {
        if (useDeferredStageApply)
        {
            if (_stageRoutine != null)
            {
                StopCoroutine(_stageRoutine);
            }

            _stageRoutine = StartCoroutine(DeferredApplyStage(current));
        }
        else
        {
            ForceApplyStage(current);
        }
    }

    // ─────────────────────────────────────────────
    // Deferred Apply
    // ─────────────────────────────────────────────

    private IEnumerator DeferredApplyStage(byte stage)
    {
        // collider spike 분산
        yield return null;

        ForceApplyStage(stage);
    }

    // ─────────────────────────────────────────────
    // Apply Stage
    // ─────────────────────────────────────────────

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

        _meshFilter.sharedMesh = targetMesh;
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

    // ─────────────────────────────────────────────
    // FX
    // ─────────────────────────────────────────────

    [ClientRpc]
    private void PlayHitEffect_ClientRpc(Vector3 hitPos)
    {
        if (hitParticle == null)
            return;

        hitParticle.transform.position = hitPos;

        hitParticle.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        hitParticle.Play(true);
    }

    [ClientRpc]
    private void PlayDestroyEffect_ClientRpc(Vector3 hitPos)
    {
        if (destroyParticle == null)
            return;

        if (!ShouldPlayEffect(hitPos))
            return;

        destroyParticle.transform.position = hitPos;
        destroyParticle.Play();
    }

    private bool ShouldPlayEffect(Vector3 worldPos)
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;

            if (_mainCamera == null)
                return false;
        }

        float sqrDistance =
            (_mainCamera.transform.position - worldPos).sqrMagnitude;

        return sqrDistance <= effectCullDistance * effectCullDistance;
    }

    // ─────────────────────────────────────────────
    // Despawn
    // ─────────────────────────────────────────────

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