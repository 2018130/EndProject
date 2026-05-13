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

    private int _readyCount = 0;
    private bool _spawnStarted = false;

    private Dictionary<int, BeachObject> _poolMap = new();

    // GC 방지용 고정 배열 — OnNetworkSpawn에서 최대 크기로 미리 할당
    private int[] _syncIds;
    private Vector3[] _syncPositions;
    private Quaternion[] _syncRotations;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        // 최대 오브젝트 수 계산 후 배열 미리 할당
        int maxCount = 0;
        foreach (var entry in entries) maxCount += entry.count;

        _syncIds = new int[maxCount];
        _syncPositions = new Vector3[maxCount];
        _syncRotations = new Quaternion[maxCount];

        // 서버/클라이언트 모두 동일 순서로 풀 생성 → ObjectId 일치 보장
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                BeachObject obj = Instantiate(entry.prefab, transform).GetComponent<BeachObject>();
                int id = _pool.Count;
                obj.ObjectId = id;
                obj.gameObject.SetActive(false);
                _pool.Add(obj);
                _poolMap[id] = obj; // 빠른 검색을 위해 등록
            }
        }

        Debug.Log($"[BeachObjectPool] OnNetworkSpawn / IsServer:{IsServer} / 풀:{_pool.Count}");

        if (IsServer)
        {
            _readyCount = 1;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            StartCoroutine(BoundsCheckLoop());

            // 클라이언트가 없으면 (서버 단독) 바로 스폰
            if (NetworkManager.Singleton.ConnectedClientsList.Count <= 1)
                StartCoroutine(InitialSpawn());
        }
        else
        {
            // 클라이언트: 풀 생성 완료 후 서버에 준비 완료 알림
            NotifyReady_ServerRpc();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    // ── 클라이언트 준비 완료 수신 ────────────────────────────────────────────
    [ServerRpc(RequireOwnership = false)]
    private void NotifyReady_ServerRpc(ServerRpcParams rpcParams = default)
    {
        _readyCount++;
        ulong senderId = rpcParams.Receive.SenderClientId; // 발신자 ID 확보

        if (_spawnStarted)
        {
            // 이미 게임이 진행 중인 경우, 새로 들어온 클라이언트에게만 현재 상태 전송
            SendCurrentStateTo(senderId);
            return;
        }

        int totalClients = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (_readyCount >= totalClients)
            StartCoroutine(InitialSpawn());
    }

    // 특정 클라이언트에게 현재 활성 오브젝트 전체 전송
    private void SendCurrentStateTo(ulong clientId)
    {
        if (_objectMap.Count == 0) return;

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

        Debug.Log($"[BeachObjectPool] 클라이언트 {clientId}에 {_objectMap.Count}개 전송");
    }

    // 게임 중 새 클라이언트 합류 시
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (_spawnStarted)
            SendCurrentStateTo(clientId);
    }

    // ── 서버: 스폰 ───────────────────────────────────────────────────────────
    private IEnumerator InitialSpawn()
    {
        _spawnStarted = true;
        yield return new WaitForEndOfFrame();

        foreach (var obj in _pool)
        {
            SpawnSpecific(obj);
            yield return new WaitForSeconds(0.1f);
        }
    }

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

        // 전체 브로드캐스트
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
        int syncCount = 0;
        int maxLimit = _syncIds.Length;

        // activeObjects가 수정되는 동안 에러를 방지하기 위해 
        // 개수(Count)를 미리 확보하거나 역순 혹은 안전한 접근이 필요합니다.
        for (int i = 0; i < _activeObjects.Count; i++)
        {
            if (syncCount >= maxLimit) break;

            BeachObject obj = _activeObjects[i];

            // 중요: 리스트 순회 중 객체가 갑자기 제거되거나 Null이 될 가능성 체크
            if (obj == null || !obj.gameObject.activeSelf) continue;

            if (obj.IsSleeping) continue;
            if (_justWokeUp.Contains(obj.ObjectId)) continue;

            _syncIds[syncCount] = obj.ObjectId;
            _syncPositions[syncCount] = obj.transform.position;
            _syncRotations[syncCount] = obj.transform.rotation;
            syncCount++;
        }

        if (syncCount == 0) return;

        BatchSyncState_ClientRpc(syncCount, _syncIds, _syncPositions, _syncRotations);
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

        if (_pool.Count == 0)
        {
            StartCoroutine(RetrySpawn(objectId, pos, rot));
            return;
        }

        DoSpawn(objectId, pos, rot);
    }

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

    private void DoSpawn(int objectId, Vector3 pos, Quaternion rot)
    {
        if (!_poolMap.TryGetValue(objectId, out BeachObject obj))
        {
            Debug.LogError($"[BeachObjectPool] Client: ObjectId {objectId} 가 풀에 존재하지 않음");
            return;
        }

        if (obj.gameObject.activeSelf) return;

        // 위치 설정 및 활성화 로직...
        obj.transform.SetPositionAndRotation(pos, rot); // 성능상 이점
        obj.gameObject.SetActive(true);
        obj.Initialize(this);

        _objectMap[objectId] = obj;
        _activeObjects.Add(obj);
    }

    // count만큼만 처리 → 고정 배열 재사용 가능
    [ClientRpc]
    private void BatchSyncState_ClientRpc(
        int count, int[] ids, Vector3[] positions, Quaternion[] rotations)
    {
        if (IsServer) return;

        for (int i = 0; i < count; i++)
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