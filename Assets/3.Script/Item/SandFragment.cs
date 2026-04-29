using UnityEngine;

public class SandFragment : MonoBehaviour
{
    [SerializeField] private float lifetime = 1.8f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 force)
    {
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
        Destroy(gameObject, lifetime);
    }
}