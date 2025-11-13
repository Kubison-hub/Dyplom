using UnityEngine;

public class PlayerPickupController : MonoBehaviour
{
    [Header("Pickup Controls")]
    public KeyCode pickupKey = KeyCode.E;
    public float pickupRange = 2f;
    public LayerMask pickupLayer;

    [Tooltip("Podnieœ sferê detekcji, aby nie haczy³a o pod³ogê (dla Sprite'ów ustaw ok. 1.0)")]
    public float verticalOffset = 1.0f;

    private PickupableObject carriedObject;

    // --- 1. FLAGA STEROWANIA ---
    // Domyœlnie false, ¿eby nieaktywne postacie nie krad³y przedmiotów na starcie.
    // PlayerSwitcher w³¹czy to dla aktywnej postaci.
    private bool inputEnabled = false;

    private void Update()
    {
        // --- 2. BLOKADA ---
        // Jeœli ta postaæ nie jest aktywna, natychmiast przerwij i nie sprawdzaj klawiszy.
        if (!inputEnabled) return;

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

        if (carriedObject != null)
        {
            carriedObject.UpdatePosition();
        }
    }

    private void TryPickup()
    {
        Vector3 origin = transform.position + Vector3.up * verticalOffset;
        Collider[] hits = Physics.OverlapSphere(origin, pickupRange, pickupLayer);

        foreach (Collider hit in hits)
        {
            PickupableObject obj = hit.GetComponent<PickupableObject>();
            if (obj != null && !obj.IsCarried)
            {
                carriedObject = obj;
                obj.Pickup(this.transform);
                return;
            }
        }
    }

    private void DropObject()
    {
        if (carriedObject != null)
        {
            carriedObject.Drop();
            carriedObject = null;
        }
    }

    // --- 3. METODA DLA PLAYERSWITCHERA ---
    public void EnableInput(bool enable)
    {
        inputEnabled = enable;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * verticalOffset, pickupRange);
    }
}