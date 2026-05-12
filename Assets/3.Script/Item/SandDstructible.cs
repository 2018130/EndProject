using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public class SandDestructible : NetworkBehaviour
{
    [Header("Deform")]
    [SerializeField] private float deformRadius = 1.2f;
    [SerializeField] private float deformDepth = 0.8f;
    [SerializeField] private float maxDeformDepth = 0.15f;
    [SerializeField] private AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("ÆÄ±« ´À³¦ ¼³Á¤")]
    [Tooltip("Ãæ°ÝÁ¡ Áß½ÉºÎ°¡ ¿òÇ« ÆÄÀÌ´Â ±íÀÌ")]
    [SerializeField] private float craterDepth = 0.3f;
    [Tooltip("Ãæ°ÝÁ¡ Å×µÎ¸®°¡ ¼Ú¾Æ¿À¸£´Â ³ôÀÌ (¸ð·¡ Æ¢¾î¿À¸§ È¿°ú)")]
    [SerializeField] private float rimHeight = 0.08f;
    [Tooltip("¼Ú¾Æ¿À¸§ÀÌ ½ÃÀÛµÇ´Â ¹Ý°æ ºñÀ² (0~1, 1¿¡ °¡±î¿ï¼ö·Ï Å×µÎ¸® ÂÊ)")]
    [SerializeField] private float rimStartRatio = 0.6f;
    [Tooltip("ºÒ±ÔÄ¢ÇÑ ¿äÃ¶ °­µµ")]
    [SerializeField] private float noiseStrength = 0.04f;
    [Tooltip("º¯ÇüÀÌ ÆÛÁ®³ª°¡´Â ½Ã°£ (ÃÊ)")]
    [SerializeField] private float deformDuration = 0.12f;

    [Header("ÆÄ±« ¼³Á¤")]
    [SerializeField] private int destroyThreshold = 5;

    [Header("Fragments")]
    [SerializeField] private GameObject[] fragmentPrefabs;
    [SerializeField] private int fragmentCount = 12;
    [SerializeField] private float fragmentForce = 6f;
    [Tooltip("ÆÄÆíÀÌ À§·Î Æ¢´Â Èû ºñÀ²")]
    [SerializeField] private float fragmentUpRatio = 0.6f;

    [Header("Particle")]
    [SerializeField] private ParticleSystem hitParticle;

    // ¦¡¦¡ ³×Æ®¿öÅ© »óÅÂ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private NetworkVariable<int> _hitCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // ¦¡¦¡ ¸Þ½Ã ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private Mesh _mesh;
    private Mesh _colliderMesh;
    private Vector3[] _vertices;
    private Vector3[] _originalVertices;


    // ¦¡¦¡ ÄÝ¶óÀÌ´õ µô·¹ÀÌ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private bool _colliderDirty = false;
    private float _colliderTimer = 0f;
    private const float ColliderDelay = 0.2f;

    // ¦¡¦¡ º¯Çü ¾Ö´Ï¸ÞÀÌ¼Ç ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // ÁøÇà ÁßÀÎ º¯Çü ÄÚ·çÆ¾ ¸ñ·Ï (¿©·¯ Ãæµ¹ÀÌ µ¿½Ã¿¡ ÀÏ¾î³¯ ¼ö ÀÖÀ½)
    private readonly List<Coroutine> _activeDeforms = new();

    // ´ÊÀº Á¢¼ÓÀÚ º¹¿ø¿ë
    private readonly List<Vector3> _hitHistory = new();

    // ¦¡¦¡ ÃÊ±âÈ­ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshCollider = GetComponent<MeshCollider>();

        _mesh = Instantiate(_meshFilter.sharedMesh);
        _meshFilter.mesh = _mesh;
        _originalVertices = _mesh.vertices;
        _vertices = (Vector3[])_originalVertices.Clone();
        _colliderMesh = new Mesh();
    }

    public override void OnNetworkSpawn()
    {
        _hitCount.OnValueChanged += OnHitCountChanged;
        if (!IsServer) RequestHistory_ServerRpc();
    }

    public override void OnDestroy()
    {
        _hitCount.OnValueChanged -= OnHitCountChanged;
    }

    // ¦¡¦¡ Update ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void Update()
    {
        if (!_colliderDirty) return;

        _colliderTimer += Time.deltaTime;
        if (_colliderTimer < ColliderDelay) return;

        ApplyColliderUpdate();
        _colliderDirty = false;
        _colliderTimer = 0f;
    }

    // ¦¡¦¡ ¿ÜºÎ ÁøÀÔÁ¡ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void RegisterHit(Vector3 worldHitPos)
    {
        if (!IsServer) return;

        _hitCount.Value++;
        _hitHistory.Add(worldHitPos);

        if (_hitCount.Value >= destroyThreshold)
        {
            DestroyObject_Rpc();
            return;
        }

        BroadcastHit_Rpc(worldHitPos);

        //Debug.Log($"[SandDestructible] RegisterHit È£Ãâ / IsServer:{IsServer} / hitCount:{_hitCount.Value}");
    }

    // ¦¡¦¡ RPC ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    [Rpc(SendTo.ClientsAndHost)]
    private void BroadcastHit_Rpc(Vector3 worldHitPos)
    {
        //Debug.Log($"[SandDestructible] BroadcastHit_Rpc ¼ö½Å / IsServer:{IsServer}");
        StartCoroutine(DeformMeshAnimated(worldHitPos));
        SpawnFragmentsLocally(worldHitPos);
        PlayParticle(worldHitPos);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DestroyObject_Rpc()
    {
        // ÆÄ±« ½Ã ´õ °­ÇÑ ÆÄÆí È¿°ú
        SpawnFragmentsLocally(transform.position, isDestroy: true);
        PlayParticle(transform.position);

        if (IsServer)
            GetComponent<NetworkObject>().Despawn();
    }

    [Rpc(SendTo.Server)]
    private void RequestHistory_ServerRpc(RpcParams rpc = default)
    {
        if (_hitHistory.Count == 0) return;
        SendHistory_Rpc(_hitHistory.ToArray(),
            RpcTarget.Single(rpc.Receive.SenderClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendHistory_Rpc(Vector3[] hits, RpcParams rpc = default)
    {
        // ´ÊÀº Á¢¼ÓÀÚ: ¾Ö´Ï¸ÞÀÌ¼Ç ¾øÀÌ Áï½Ã º¹¿ø
        foreach (var hit in hits)
            DeformMeshImmediate(hit);
    }

    private void OnHitCountChanged(int prev, int curr) { }

    // ¦¡¦¡ ¸Þ½Ã º¯Çü (¾Ö´Ï¸ÞÀÌ¼Ç) ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    // º¯ÇüÀÌ deformDuration µ¿¾È ÆÛÁ®³ª°¡´Â È¿°ú
    private IEnumerator DeformMeshAnimated(Vector3 worldHitPos)
    {
        Vector3 localHit = transform.InverseTransformPoint(worldHitPos);
        float elapsed = 0f;

        // º¯Çü Àü ¹öÅØ½º ½º³À¼¦ (´Ù¸¥ ÄÚ·çÆ¾°ú °£¼· ¹æÁö)
        Vector3[] startVertices = (Vector3[])_vertices.Clone();
        Vector3[] targetVertices = CalculateTargetVertices(localHit, startVertices);

        while (elapsed < deformDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / deformDuration);

            for (int i = 0; i < _vertices.Length; i++)
            {
                // YÃà¸¸ º¸°£ (XZ´Â º¯Çü ¾È ÇÔ)
                _vertices[i].y = Mathf.Lerp(startVertices[i].y, targetVertices[i].y, t);
            }

            _mesh.vertices = _vertices;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _colliderDirty = true;
            _colliderTimer = 0f;

            yield return null;
        }

        // ÃÖÁ¾°ª È®Á¤
        _vertices = targetVertices;
        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _colliderDirty = true;
        _colliderTimer = 0f;

       

        // ¡Ú ½ÇÁ¦·Î º¯ÇüµÇ´Â ¹öÅØ½º°¡ ÀÖ´ÂÁö È®ÀÎ
        int changedCount = 0;
        float maxChange = 0f;
        for (int i = 0; i < startVertices.Length; i++)
        {
            float diff = Mathf.Abs(targetVertices[i].y - startVertices[i].y);
            if (diff > 0.0001f)
            {
                changedCount++;
                maxChange = Mathf.Max(maxChange, diff);
            }
        }
        Debug.Log($"[SandDestructible] º¯Çü ¹öÅØ½º:{changedCount} / ÃÖ´ëº¯Çü·®:{maxChange:F4} / localHit:{localHit}");
    }

    // ´ÊÀº Á¢¼ÓÀÚ¿ë Áï½Ã º¯Çü (¾Ö´Ï¸ÞÀÌ¼Ç ¾øÀ½)
    private void DeformMeshImmediate(Vector3 worldHitPos)
    {
        Vector3 localHit = transform.InverseTransformPoint(worldHitPos);
        _vertices = CalculateTargetVertices(localHit, _vertices);

        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _colliderDirty = true;
        _colliderTimer = 0f;
    }

    // ¦¡¦¡ º¯Çü ¸ñÇ¥ ¹öÅØ½º °è»ê ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private Vector3[] CalculateTargetVertices(Vector3 localHit, Vector3[] baseVertices)
    {
        Vector3[] result = (Vector3[])baseVertices.Clone();

        for (int i = 0; i < result.Length; i++)
        {
            float dist = new Vector2(
                result[i].x - localHit.x,
                result[i].z - localHit.z).magnitude;

            if (dist >= deformRadius) continue;

            float ratio = dist / deformRadius;
            float factor = falloff.Evaluate(ratio);

            // ÀÌ¹Ì ÃÖ´ë º¯Çü¿¡ µµ´ÞÇÑ ¹öÅØ½º´Â ½ºÅµ
            float alreadyDeformed = _originalVertices[i].y - result[i].y;

            // ¦¡¦¡ Áß½ÉºÎ: ¿òÇ« ÆÄÀÓ ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
            if (ratio < rimStartRatio)
            {
                float remaining = maxDeformDepth - alreadyDeformed;
                if (remaining <= 0f) continue;

                // Áß½É¿¡ °¡±î¿ï¼ö·Ï ±í°Ô ÆÄÀÓ
                float depth = craterDepth * (1f - ratio / rimStartRatio) * factor;

                // ¡Ú ºÒ±ÔÄ¢ ³ëÀÌÁî Ãß°¡ ¡æ ÀÚ¿¬½º·¯¿î ÆÄ±« ´À³¦
                float noise = Mathf.PerlinNoise(
                    result[i].x * 3f + localHit.x,
                    result[i].z * 3f + localHit.z) * noiseStrength;

                result[i].y -= Mathf.Min(depth + noise, remaining);
            }
            // ¦¡¦¡ Å×µÎ¸®: »ìÂ¦ ¼Ú¾Æ¿À¸§ (¸ð·¡ Æ¢°Ü¿À¸§ È¿°ú) ¦¡¦¡
            else
            {
                // Å×µÎ¸® ¹Ù±ùÂÊÀ¸·Î °¥¼ö·Ï ¼ÚÀ½ÀÌ ÁÙ¾îµê
                float rimFactor = 1f - (ratio - rimStartRatio) / (1f - rimStartRatio);
                float noise = Mathf.PerlinNoise(
                    result[i].x * 5f + localHit.x,
                    result[i].z * 5f + localHit.z) * noiseStrength * 0.5f;

                result[i].y += rimHeight * rimFactor + noise;
            }
        }

        return result;
    }

    // ¦¡¦¡ ÄÝ¶óÀÌ´õ °»½Å ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void ApplyColliderUpdate()
    {
        _colliderMesh.vertices = _mesh.vertices;
        _colliderMesh.triangles = _mesh.triangles;
        // _colliderMesh.RecalculateNormals(); // <--- ÀÌ ÁÙÀ» ÁÖ¼® Ã³¸®ÇÏ°Å³ª Áö¿ì¼¼¿ä!

        // ÄÝ¶óÀÌ´õ¸¦ °»½ÅÇÒ ¶§ 'Convex'¸¦ »ìÂ¦ Ä×´Ù ²¨ÁÖ¸é,
        // À¯´ÏÆ¼°¡ ¸é ³ë¸ÖÀ» ±âÁØÀ¸·Î ´õ ·Î¿ìÆú¸®½º·¯¿î Ãæµ¹Ã¼¸¦ ¸¸µì´Ï´Ù.
        _meshCollider.convex = true;
        _meshCollider.enabled = false;
        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _colliderMesh;
        _meshCollider.convex = false;
        _meshCollider.enabled = true;
    }

    // ¦¡¦¡ ÆÄÆí ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    private void SpawnFragmentsLocally(Vector3 worldHitPos, bool isDestroy = false)
    {
        if (fragmentPrefabs == null || fragmentPrefabs.Length == 0) return;

        int count = isDestroy ? fragmentCount * 2 : fragmentCount;
        float force = isDestroy ? fragmentForce * 1.5f : fragmentForce;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = fragmentPrefabs[Random.Range(0, fragmentPrefabs.Length)];
            GameObject frag = Instantiate(prefab, worldHitPos, Random.rotation);

            // ¡Ú Ãæ°ÝÁ¡ ±âÁØ ¹æ»çÇü + À§ÂÊÀ¸·Î Æ¢¾î³ª°¨
            Vector3 radialDir = Random.insideUnitSphere;
            radialDir.y = Mathf.Abs(radialDir.y) * fragmentUpRatio + fragmentUpRatio;
            radialDir = radialDir.normalized;

            frag.GetComponent<SandFragment>().Launch(radialDir * force);
        }
    }

    private void PlayParticle(Vector3 worldHitPos)
    {
        if (hitParticle == null) return;
        hitParticle.transform.position = worldHitPos;
        hitParticle.Play();
    }

    // ¦¡¦¡ ¸®¼Â ¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡¦¡
    public void ResetSand()
    {
        if (!IsServer) return;
        _hitCount.Value = 0;
        _hitHistory.Clear();
        ResetSand_Rpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ResetSand_Rpc()
    {
        _vertices = (Vector3[])_originalVertices.Clone();
        _mesh.vertices = _vertices;
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        _colliderMesh.vertices = _vertices;
        _colliderMesh.triangles = _mesh.triangles;
        _colliderMesh.RecalculateNormals();

        _meshCollider.sharedMesh = null;
        _meshCollider.sharedMesh = _colliderMesh;
    }
}