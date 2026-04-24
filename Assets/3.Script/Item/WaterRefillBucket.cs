using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class WaterRefillBucket : MonoBehaviour
{
    [SerializeField] private float refillAmount = 25f;
    [SerializeField] private float respawnDelay = 5f;

    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float bobSpeed = 2f;

    private bool isCollected = false;

    private Vector3 originPos;

    private void Start()
    {
        originPos = transform.position;
    }

    private void Update()
    {
        if (isCollected) return;

        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = originPos + Vector3.up * y;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isCollected) return;

        NetworkObject networkObject = other.GetComponent<NetworkObject>();
        if (networkObject == null || !networkObject.IsOwner) return;

        PlayerNetwork player = other.GetComponent<PlayerNetwork>();
        if (player == null) return;

        player.ApplyWaterRefill(refillAmount);
        RequestCollect_ServerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RequestCollect_ServerRpc()
    {
        if (isCollected) return;
        SetVisible_Rpc(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        SetVisible_Rpc(true);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetVisible_Rpc(bool visible)
    {
        isCollected = !visible;

        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;

        GetComponent<Collider>().enabled = visible;
    }
}