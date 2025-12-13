using UnityEngine;

public class Billboard : MonoBehaviour
{
    void LateUpdate()
    {
        // Obróæ siê przodem do kamery
        transform.forward = Camera.main.transform.forward;
    }
}