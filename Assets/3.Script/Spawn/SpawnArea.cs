using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SpawnArea : MonoBehaviour
{
    [SerializeField] private Faction ownerFaction;

    private BoxCollider spawnCollider;
    public Faction OwnerFaction => ownerFaction;

    private void Awake()
    {
        spawnCollider = GetComponent<BoxCollider>();
        spawnCollider.isTrigger = true;
    }

    public Vector3 GetRandomSpawnPoint()
    {
        if (spawnCollider == null)
            spawnCollider = GetComponent<BoxCollider>();

        Bounds bounds = spawnCollider.bounds;
        float x = Random.Range(bounds.min.x, bounds.max.x);
        float z = Random.Range(bounds.min.z, bounds.max.z);
        float y = bounds.min.y;
        return new Vector3(x, y, z);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        // ¾Æ±ºÀÌ¸é Åë°ú
        if ((Faction)health.PlayerFactionInt.Value == ownerFaction) return;

        // Àû±ºÀÌ¸é ¹Ð¾î³»±â
        CharacterController cc = other.GetComponent<CharacterController>();
        Rigidbody rb = other.GetComponent<Rigidbody>();

        Vector3 pushDir = (other.transform.position - transform.position).normalized;
        if (rb != null)
            rb.AddForce(pushDir * 10f, ForceMode.Impulse);
    }

    private void OnDrawGizmos()
    {
        if (spawnCollider == null)
            spawnCollider = GetComponent<BoxCollider>();

        // TeamA¸é ÆÄ¶û, TeamB¸é »¡°­
        Gizmos.color = ownerFaction == Faction.TeamA
            ? new Color(0f, 0.5f, 1f, 0.3f)  // ¹ÝÅõ¸í ÆÄ¶û
            : new Color(1f, 0.2f, 0.2f, 0.3f); // ¹ÝÅõ¸í »¡°­

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(spawnCollider.center, spawnCollider.size);

        // ¿Ü°û¼±
        Gizmos.color = ownerFaction == Faction.TeamA
            ? Color.blue : Color.red;
        Gizmos.DrawWireCube(spawnCollider.center, spawnCollider.size);
    }
}