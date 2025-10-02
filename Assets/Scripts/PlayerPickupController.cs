using UnityEngine;

public class PlayerPickupController : MonoBehaviour
{
    [Header("Pickup Controls")]
    [Tooltip("Key used to pick up / drop objects.")]
    public KeyCode pickupKey = KeyCode.E;

    [Tooltip("Radius around player to detect objects.")]
    public float pickupRange = 2f;

    [Tooltip("LayerMask for pickupable objects.")]
    public LayerMask pickupLayer;

    private PickupableObject carriedObject;

    private void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            if (carriedObject == null)
            {
                TryPickup();
            }
            else
            {
                DropObject();
            }
        }
    }

    private void TryPickup()
    {
        // Check nearby colliders within pickup range
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, pickupLayer);

        foreach (Collider hit in hits)
        {
            PickupableObject obj = hit.GetComponent<PickupableObject>();
            if (obj != null && !obj.IsCarried)
            {
                carriedObject = obj;
                obj.Pickup(transform);
                return;
            }
        }
    }

    private void DropObject()
    {
        if (carriedObject != null)
        {
            carriedObject.Drop(transform);
            carriedObject = null;
        }
    }

    // Optional: visualize pickup range in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
