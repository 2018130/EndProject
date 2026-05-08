using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// 씬에 하나 배치 (NetworkObject 컴포넌트 필수)
// 서버: 오브젝트 스폰/물리/범위 체크 주도
// 클라이언트: RPC 수신 후 시각적 표현
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

    // ID → BeachObject 매핑 (동기화/비활성화에 사용)
    private Dictionary<int, BeachObject> _objectMap = new();
    private List<BeachObject> _activeObjects = new();

    // Queue 대신 List 사용 — ObjectId 기반으로 정확하게 찾기 위함
    // 큐 방식은 서버/클라이언트 간 Dequeue 순서가 달라질 수 있어 불일치 발생
    private List<BeachObject> _pool = new();

    private float _syncTimer = 0f;

    // ── 초기화 ───────────────────────────────────────────────────────────────
    public override void OnNetworkSpawn()
    {
        // 서버/클라이언트 모두 동일한 구조로 풀 생성
        // ObjectId는 생성 순서 기반으로 고정 부여 → 서버/클라이언트 동일 보장
        foreach (var entry in entries)
        {
            for (int i = 0; i < entry.count; i++)
            {
                BeachObject obj = Instantiate(entry.prefab, transform)
                                    .GetComponent<BeachObject>();
                obj.ObjectId = _pool.Count; // 고정 ID 부여
                obj.gameObject.SetActive(false);
                _pool.Add(obj);
            }
        }

        if (!IsServer) return;

        StartCoroutine(InitialSpawn());
        StartCoroutine(BoundsCheckLoop());
    }

    // ── 서버: 스폰 ───────────────────────────────────────────────────────────
    private IEnumerator InitialSpawn()
    {
        yield return new WaitUntil(() => IsSpawned);
        yield return new WaitForSeconds(0.3f);

        int total = _pool.Count;
        for (int i = 0; i < total; i++)
        {
            SpawnOne();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void SpawnOne()
    {
        // 비활성 오브젝트 찾기
        BeachObject obj = _pool.Find(o => !o.gameObject.activeSelf);
        if (obj == null) return;

        Vector3 spawnPos = new Vector3(
            mapCenter.x + Random.Range(-mapRangeX * 0.5f, mapRangeX * 0.5f),
            mapCenter.y + spawnHeight,
            mapCenter.z + Random.Range(-mapRangeZ * 0.5f, mapRangeZ * 0.5f)
        );

        obj.transform.position = spawnPos;
        obj.transform.rotation = Random.rotation;
        obj.gameObject.SetActive(true);
        obj.Initialize(this, isServer: true);

        _objectMap[obj.ObjectId] = obj;
        _activeObjects.Add(obj);

        // ObjectId를 함께 전송 → 클라이언트가 동일 오브젝트를 정확히 찾음
        SpawnObject_ClientRpc(obj.ObjectId, spawnPos, obj.transform.rotation);
    }

    // ── 서버: 동기화 ─────────────────────────────────────────────────────────
    private void Update()
    {
        if (!IsServer) return;

        _syncTimer += Time.deltaTime;
        if (_syncTimer < syncInterval) return;
        _syncTimer = 0f;

        BatchSyncAll();
    }

    private void BatchSyncAll()
    {
        // Sleep 중이 아닌 오브젝트만 동기화 (Sleep 오브젝트는 위치 변화 없음)
        var moving = _activeObjects.FindAll(
            o => o != null && o.gameObject.activeSelf && !o.IsSleeping);

        if (moving.Count == 0) return;

        var ids = new int[moving.Count];
        var positions = new Vector3[moving.Count];
        var rotations = new Quaternion[moving.Count];

        for (int i = 0; i < moving.Count; i++)
        {
            ids[i] = moving[i].ObjectId;
            positions[i] = moving[i].transform.position;
            rotations[i] = moving[i].transform.rotation;
        }

        BatchSyncState_ClientRpc(ids, positions, rotations);
    }

    // BeachObject.ServerUpdate에서 Sleep 진입 시 호출
    // BatchSyncAll에서 제외되는 마지막 위치를 강제 전송
    public void BroadcastObjectState(BeachObject obj, Vector3 pos, Quaternion rot)
    {
        if (!IsSpawned || !IsServer) return;
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

            Vector3 localPos = obj.transform.position - mapCenter;
            bool outOfBounds = Mathf.Abs(localPos.x) > halfX
                               || Mathf.Abs(localPos.z) > halfZ
                               || localPos.y < -10f;

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

        // enableRespawn: true → 일정 시간 후 재낙하
        //                false → 비활성 상태로 대기
        if (enableRespawn)
            StartCoroutine(RespawnAfterDelay(obj));
    }

    private IEnumerator RespawnAfterDelay(BeachObject obj)
    {
        yield return new WaitForSeconds(respawnDelay);
        SpawnOne();
    }

    // ── RPC ──────────────────────────────────────────────────────────────────
    // SpawnObject_ClientRpc에 로그 추가
    [ClientRpc]
    private void SpawnObject_ClientRpc(int objectId, Vector3 pos, Quaternion rot)
    {
        if (IsServer) return;

        BeachObject obj = _pool.Find(o => o.ObjectId == objectId);
        if (obj == null)
        {
            Debug.LogWarning($"[BeachObjectPool] ObjectId {objectId} 찾기 실패 / 풀 크기: {_pool.Count}");
            return;
        }

        // ★ 이미 활성화된 오브젝트가 다시 스폰되는지 확인
        if (obj.gameObject.activeSelf)
        {
            Debug.LogWarning($"[BeachObjectPool] ObjectId {objectId} 이미 활성화 상태");
            return;
        }

        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.gameObject.SetActive(true);
        obj.Initialize(this, isServer: false);

        _objectMap[objectId] = obj;
        _activeObjects.Add(obj);
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

    // ── Gizmo (Scene 뷰 범위 시각화) ────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            mapCenter + Vector3.up * spawnHeight * 0.5f,
            new Vector3(mapRangeX, spawnHeight, mapRangeZ)
        );
    }
}

// Inspector에서 프리팹 종류와 개수를 묶어 관리
[System.Serializable]
public class BeachObjectEntry
{
    public GameObject prefab;
    public int count = 5;
}