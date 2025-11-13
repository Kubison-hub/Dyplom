using UnityEngine;

public class SwitchSphere : MonoBehaviour
{
    [SerializeField] DoorTrigger door;

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!door.isOpen) return;
            door.PerformInteraction();
        }
    }

    
}
