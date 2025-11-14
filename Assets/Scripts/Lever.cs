using UnityEngine;

public class Lever : MonoBehaviour
{
    [Header("Ustawienia")]
    [Tooltip("Przeciπgnij tutaj obiekt HINGE ze skryptem Door.")]
    [SerializeField] private Door doorToOpen;

    [Tooltip("Maksymalny dystans do uøycia düwigni.")]
    [SerializeField] private float interactDistance = 3.0f;

    // Wywo≥ywane przez PointAndClickController
    public void AttemptInteraction(Transform playerTransform)
    {
        if (doorToOpen == null)
        {
            Debug.LogError("Nie przypisano drzwi do tej düwigni!");
            return;
        }

        float dist = Vector3.Distance(transform.position, playerTransform.position);

        if (dist <= interactDistance)
        {
            Debug.Log("Düwignia uøyta. Otwieram drzwi.");
            doorToOpen.OpenDoor();
        }
        else
        {
            Debug.Log("Jesteú za daleko od düwigni.");
        }
    }
}