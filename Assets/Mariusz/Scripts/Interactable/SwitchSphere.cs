using UnityEngine;

public class SwitchSphere : MonoBehaviour
{
    [SerializeField] Door door;

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!door.isOpen) return;
            door.PerformInteraction();
        }
    }

    
}
