using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PickupableObject : MonoBehaviour
{
    [Header("Pickup Settings")]
    public float carryHeight = 2f;
    public float dropDistance = 2f;
    public float followSpeed = 10f;

    [Header("Highlight Settings")]
    [Tooltip("Material used when object is highlighted (outline, glow, etc).")]
    public Material highlightMaterial;
    private Material originalMaterial;
    private Renderer rend;

    [HideInInspector] public bool IsCarried = false;

    private Transform carryTarget;
    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = false;

        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            originalMaterial = rend.material; // cache original
        }
    }

    private void Update()
    {
        if (IsCarried && carryTarget != null)
        {
            Vector3 targetPos = carryTarget.position + Vector3.up * carryHeight;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);
        }
    }

    public void Pickup(Transform player)
    {
        IsCarried = true;
        carryTarget = player;
        col.enabled = false;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        RemoveHighlight();
    }

    public void Drop(Transform player)
    {
        IsCarried = false;
        carryTarget = null;
        col.enabled = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        Vector3 dropPos = player.position + player.forward * dropDistance;
        dropPos.y = player.position.y;
        transform.position = dropPos;
    }

    // 🔹 Highlight methods
    public void ApplyHighlight()
    {
        if (rend != null && highlightMaterial != null)
        {
            rend.material = highlightMaterial;
        }
    }

    public void RemoveHighlight()
    {
        if (rend != null && originalMaterial != null)
        {
            rend.material = originalMaterial;
        }
    }
}
