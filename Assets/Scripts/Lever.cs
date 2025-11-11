using UnityEngine;

public class Lever : MonoBehaviour
{
    [Header("Ustawienia DŸwigni")]
    [Tooltip("Przeci¹gnij tutaj obiekt drzwi, które ta dŸwignia ma otworzyæ.")]
    [SerializeField] private Door doorToOpen;

    [Tooltip("Jak blisko gracz musi byæ, aby u¿yæ dŸwigni.")]
    [SerializeField] private float interactDistance = 3.0f;

    // Ta funkcja jest publiczna, aby PointAndClickController móg³ j¹ wywo³aæ
    public void AttemptInteraction(Transform playerTransform)
    {
        // SprawdŸ, czy mamy wszystkie odniesienia
        if (doorToOpen == null)
        {
            Debug.LogError("Nie przypisano drzwi do tej dŸwigni!", this);
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogError("Nie uda³o siê znaleŸæ gracza!", this);
            return;
        }

        // --- G£ÓWNA LOGIKA: SPRAWDZANIE DYSTANSU ---
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= interactDistance)
        {
            // Sukces! Gracz jest wystarczaj¹co blisko.
            Debug.Log("Gracz jest blisko, otwieram drzwi.");
            doorToOpen.OpenDoor();

            // Opcjonalnie: odtwórz animacjê dŸwigni lub dŸwiêk
            // ...
        }
        else
        {
            // Pora¿ka. Gracz jest za daleko.
            Debug.Log("Gracz jest za daleko, aby u¿yæ dŸwigni.");

            // Opcjonalnie: odtwórz dŸwiêk "zaciêcia" lub poka¿ dymek "Nie siêgam"
            // ...
        }
    }
}