using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SandDestructible : NetworkBehaviour
{
    [Header("Stage Objects")]
    [Tooltip("0 = pristine / 마지막 = 완전 파괴")]
    [SerializeField] private GameObject[] stageObjects;

    [Header("Durability")]
    [SerializeField] private float maxHp = 100f;

    [SerializeField] private float despawnDelay = 2f;

    [Header("FX")]
    [SerializeField] private float effectCullDistance = 80f;

    private NetworkVariable<byte> _stage = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private float _hp;

    private int _currentStage = -1;

    private bool _destroyed;

    private Camera _mainCamera;

    // ─────────────────────────────
    // Unity
    // ─────────────────────────────

    private void Awake()
    {
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

        ApplyStage(_stage.Value);
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
        Debug.Log($"RegisterHit / IsServer:{IsServer}");
        _hp = Mathf.Max(_hp - damage, 0f);

        byte newStage = CalculateStage(_hp);

        // 피격 FX
        PlayHitEffect_ClientRpc(hitPoint);

        // 단계 변경
        if (newStage != _stage.Value)
        {
            _stage.Value = newStage;
        }

        // 파괴
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
        if (stageObjects == null || stageObjects.Length == 0)
            return 0;

        float ratio = hp / maxHp;

        int stageCount = stageObjects.Length;

        return (byte)Mathf.Clamp(
            Mathf.FloorToInt((1f - ratio) * stageCount),
            0,
            stageCount - 1
        );
    }

    private void OnStageChanged(byte previous, byte current)
    {
        ApplyStage(current);
    }

    private void ApplyStage(int stage)
    {
        if (_currentStage == stage)
            return;

        _currentStage = stage;

        if (stageObjects == null)
            return;

        for (int i = 0; i < stageObjects.Length; i++)
        {
            if (stageObjects[i] == null)
                continue;

            stageObjects[i].SetActive(i == stage);
        }

        Debug.Log($"[SandDestructible] Stage Changed → {stage}");
        Debug.Log(stageObjects.Length);
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
            (_mainCamera.transform.position - worldPos).sqrMagnitude;

        return sqrDistance <=
               effectCullDistance * effectCullDistance;
    }

    // ─────────────────────────────
    // Destroy
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