using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BeachObjectPool : NetworkBehaviour
{
    [Header("프리팹 설정")]
    [SerializeField] private BeachObjectEntry[] entries;

    [Header("스폰 범위")]
    [SerializeField] private Vector3 mapCenter = Vector3.zero;
    [SerializeField] private float mapRangeX = 30f;
    [SerializeField] private float mapRangeZ = 30f;
    [SerializeField] private float spawnHeight = 15f;
    [SerializeField] private float boundsBuffer = 2f;

    [Header("재낙하 설정")]
    [SerializeField] private bool enableRespawn = true;
    [SerializeField] private float respawnDelay = 5f;

    [Header("동기화/체크 주기")]
    [SerializeField] private float syncInterval = 0.1f;
    [SerializeField] private float boundsCheckInterval = 0.5f;

    private Dictionary<int, BeachObject> _objectMap = new();
    private List<BeachObject> _activeObjects = new();
    private List<BeachObject> _pool = new();
    private float _syncTimer = 0f;
    private readonly HashSet<int> _justWokeUp = new();

    // ── 초기화 ───────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        Debug.Log($"[BeachObjectPool] OnNetworkSpawn 진입 / IsServer:{IsServer} / IsClient:{IsClient} / IsHost:{IsHost}");
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                BeachObject obj = Instantiate(entry.prefab, transform)
                                    .GetComponent<BeachObject>();
                obj.ObjectId = _pool.Count;
                obj.gameObject.SetActive(false);
                _pool.Add(obj);
            }
        }

        // ★ 풀 생성 완료 로그
        Debug.Log($"[BeachObjectPool] OnNetworkSpawn 완료 / IsServer:{IsServer} / 풀:{_pool.Count}");

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            StartCoroutine(InitialSpawn());
            StartCoroutine(BoundsCheckLoop());
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // ── 서버: 새 클라이언트 접속 시 현재 상태 전송 ───────────────────────────
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer || _objectMap.Count == 0) return;

        var param = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        foreach (var kv in _objectMap)
        {
            SpawnObject_ClientRpc(
                kv.Key,
                kv.Value.transform.position,
                kv.Value.transform.rotation,
                param
            );
        }

        Debug.Log($"[BeachObjectPool] 새 클라이언트 {clientId}에 {_objectMap.Count}개 전송");
    }

    // ── 서버: 스폰 ───────────────────────────────────────────────────────────
    private IEnumerator InitialSpawn()
    {
        // 클라이언트 OnNetworkSpawn 완료 대기
        yield return new WaitForSeconds(1.0f);

        foreach (var obj in _pool)
        {
            SpawnSpecific(obj);
            yield return new WaitForSeconds(0.1f);
        }
    }

    // 특정 오브젝트를 지정해서 스폰 (최초/재스폰 공통)
    private void SpawnSpecific(BeachObject obj)
    {
        if (obj.gameObject.activeSelf) return;

        Vector3 spawnPos = new Vector3(
            mapCenter.x + Random.Range(-mapRangeX * 0.5f, mapRangeX * 0.5f),
            mapCenter.y + spawnHeight,
            mapCenter.z + Random.Range(-mapRangeZ * 0.5f, mapRangeZ * 0.5f)
        );

        obj.transform.position = spawnPos;
        obj.transform.rotation = Random.rotation;
        obj.gameObject.SetActive(true);
        obj.Initialize(this);

        _objectMap[obj.ObjectId] = obj;
        _activeObjects.Add(obj);

        // 전체 클라이언트에 브로드캐스트
        SpawnObject_ClientRpc(obj.ObjectId, spawnPos, obj.transform.rotation, default);
    }

    // ── 서버: 동기화 ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (!IsServer || !IsSpawned) return;

        _syncTimer += Time.deltaTime;
        if (_syncTimer < syncInterval) return;
        _syncTimer = 0f;

        BatchSyncAll();
        _justWokeUp.Clear();
    }

    private void BatchSyncAll()
    {
        // Sleep 중이 아닌 오브젝트만 동기화
        var moving = _activeObjects.FindAll(
            o => o != null && o.gameObject.activeSelf && !o.IsSleeping);

        if (moving.Count == 0) return;

        // 이번 프레임에 BroadcastObjectState로 이미 전송된 오브젝트 제외
        var toSync = moving.FindAll(o => !_justWokeUp.Contains(o.ObjectId));
        if (toSync.Count == 0) return;

        var ids = new int[toSync.Count];
        var positions = new Vector3[toSync.Count];
        var rotations = new Quaternion[toSync.Count];

        for (int i = 0; i < toSync.Count; i++)
        {
            ids[i] = toSync[i].ObjectId;
            positions[i] = toSync[i].transform.position;
            rotations[i] = toSync[i].transform.rotation;
        }

        BatchSyncState_ClientRpc(ids, positions, rotations);
    }

    // Sleep 진입/해제 시 BeachObject에서 호출 → 즉시 위치 전송
    public void BroadcastObjectState(BeachObject obj, Vector3 pos, Quaternion rot)
    {
        if (!IsSpawned || !IsServer) return;
        _justWokeUp.Add(obj.ObjectId);
        SyncObjectState_ClientRpc(obj.ObjectId, pos, rot);
    }

    // ── 서버: 범위 체크 ──────────────────────────────────────────────────────
    private IEnumerator BoundsCheckLoop()
    {
        var wait = new WaitForSeconds(boundsCheckInterval);
        while (true)
        {
            yield return wait;
            CheckBounds();
        }
    }

    private void CheckBounds()
    {
        float halfX = mapRangeX * 0.5f + boundsBuffer;
        float halfZ = mapRangeZ * 0.5f + boundsBuffer;

        for (int i = _activeObjects.Count - 1; i >= 0; i--)
        {
            BeachObject obj = _activeObjects[i];
            if (obj == null || !obj.gameObject.activeSelf) continue;

            Vector3 local = obj.transform.position - mapCenter;
            bool outOfBounds = Mathf.Abs(local.x) > halfX
                               || Mathf.Abs(local.z) > halfZ
                               || local.y < -10f;

            if (!outOfBounds) continue;

            DeactivateObject_ClientRpc(obj.ObjectId);
            ReturnToPool(obj);
        }
    }

    private void ReturnToPool(BeachObject obj)
    {
        obj.ReturnToPool();
        _activeObjects.Remove(obj);
        _objectMap.Remove(obj.ObjectId);

        if (enableRespawn)
            StartCoroutine(RespawnAfterDelay(obj));
    }

    private IEnumerator RespawnAfterDelay(BeachObject obj)
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnSpecific(obj);
    }

    // ── RPC ──────────────────────────────────────────────────────────────────
    [ClientRpc]
    private void SpawnObject_ClientRpc(
    int objectId, Vector3 pos, Quaternion rot,
    ClientRpcParams rpcParams = default)
    {
        if (IsServer) return;

        Debug.Log($"[BeachObjectPool] Client: SpawnObject_ClientRpc 수신 ObjectId:{objectId} / 풀:{_pool.Count}");

        if (_pool.Count == 0)
        {
            Debug.LogWarning($"[BeachObjectPool] Client: 풀 미준비 → RetrySpawn ObjectId:{objectId}");
            StartCoroutine(RetrySpawn(objectId, pos, rot));
            return;
        }

        DoSpawn(objectId, pos, rot);
    }

    // 풀 미준비 시 대기 후 재시도
    private IEnumerator RetrySpawn(int objectId, Vector3 pos, Quaternion rot)
    {
        float timeout = 3f;
        float elapsed = 0f;

        while (_pool.Count == 0 && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (_pool.Count == 0)
        {
            Debug.LogError($"[BeachObjectPool] RetrySpawn 타임아웃 ObjectId:{objectId}");
            yield break;
        }

        DoSpawn(objectId, pos, rot);
    }

    // 실제 스폰 처리 (SpawnObject_ClientRpc / RetrySpawn 공통)
    private void DoSpawn(int objectId, Vector3 pos, Quaternion rot)
    {
        BeachObject obj = _pool.Find(o => o.ObjectId == objectId);
        if (obj == null)
        {
            Debug.LogError($"[BeachObjectPool] Client: ObjectId {objectId} 찾기 실패 / 풀:{_pool.Count}");
            return;
        }
        if (obj.gameObject.activeSelf)
        {
            Debug.LogWarning($"[BeachObjectPool] Client: ObjectId {objectId} 이미 활성화");
            return;
        }

        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.gameObject.SetActive(true);
        obj.Initialize(this);

        _objectMap[objectId] = obj;
        _activeObjects.Add(obj);

        Debug.Log($"[BeachObjectPool] Client: ObjectId:{objectId} 등록 완료 / map:{_objectMap.Count}");
    }

    [ClientRpc]
    private void BatchSyncState_ClientRpc(int[] ids, Vector3[] positions, Quaternion[] rotations)
    {
        if (IsServer) return;

        for (int i = 0; i < ids.Length; i++)
        {
            if (!_objectMap.TryGetValue(ids[i], out BeachObject obj)) continue;
            obj.ApplyNetworkState(positions[i], rotations[i]);
        }
    }

    [ClientRpc]
    private void SyncObjectState_ClientRpc(int objectId, Vector3 pos, Quaternion rot)
    {
        if (IsServer) return;
        if (!_objectMap.TryGetValue(objectId, out BeachObject obj)) return;
        obj.ApplyNetworkState(pos, rot);
    }

    [ClientRpc]
    private void DeactivateObject_ClientRpc(int objectId)
    {
        if (IsServer) return;
        if (!_objectMap.TryGetValue(objectId, out BeachObject obj)) return;

        obj.ReturnToPool();
        _activeObjects.Remove(obj);
        _objectMap.Remove(objectId);
    }

    // ── Gizmo ────────────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            mapCenter + Vector3.up * spawnHeight * 0.5f,
            new Vector3(mapRangeX, spawnHeight, mapRangeZ)
        );
    }
}

[System.Serializable]
public class BeachObjectEntry
{
    public GameObject prefab;
    public int count = 5;
}