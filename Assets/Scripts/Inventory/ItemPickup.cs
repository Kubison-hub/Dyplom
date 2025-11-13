using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public Item itemData;
    public float pickupRange = 3f;
    private Transform player;

    public void Pickup()
    {
        Inventory.instance.AddItem(itemData);
        Destroy(gameObject);
    }
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void OnMouseDown()
    {
        if (Vector3.Distance(player.position, transform.position) <= pickupRange)
        {
            Inventory.instance.AddItem(itemData);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Za daleko, by podnieœæ!");
        }
    }
}
