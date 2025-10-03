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

        // Jeœli trzymamy przedmiot, aktualizujemy jego pozycjê wzglêdem gracza
        if (carriedObject != null)
        {
            carriedObject.UpdatePosition();
        }
    }

    private void TryPickup()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRange, pickupLayer);

        foreach (Collider hit in hits)
        {
            PickupableObject obj = hit.GetComponent<PickupableObject>();
            if (obj != null)
            {
                // ignoruj jeœli ktoœ ju¿ trzyma przedmiot
                if (!obj.IsCarried)
                {
                    carriedObject = obj;
                    obj.Pickup(this.transform);
                    return;
                }
            }
        }
    }


    private void DropObject()
    {
        if (carriedObject != null)
        {
            carriedObject.Drop();   // <-- bez argumentów
            carriedObject = null;
        }
    }

    // Opcjonalnie: wizualizacja zasiêgu podnoszenia w edytorze
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
