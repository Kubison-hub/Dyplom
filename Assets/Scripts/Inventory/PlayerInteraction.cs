using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float pickupRange = 4f;
    public KeyCode pickupKey = KeyCode.E;

    void Update()
    {
        if (Input.GetKeyDown(pickupKey))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PickupItem item = hit.collider.GetComponent<PickupItem>();
                if (item != null)
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (distance <= pickupRange)
                        item.Pickup();
                }
            }
        }
    }
}
