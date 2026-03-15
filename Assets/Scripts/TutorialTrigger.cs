using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    [Header("Treœæ Tutoriala")]
    [TextArea] public string komunikat;
    public string unikalneID; // Np. "DoorTutorial" lub "NPCTutorial"

    private void OnTriggerEnter(Collider other)
    {
        // Sprawdzamy, czy wszed³ gracz (po tagu lub skrypcie)
        if (other.CompareTag("PlayerA") || other.CompareTag("PlayerB"))
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.PokazTutorial(komunikat, unikalneID);

                // Po wyœwietleniu mo¿emy usun¹æ ten trigger, ¿eby nie obci¹¿a³ gry
                // (Manager i tak zablokuje ponowne wyœwietlenie po ID, ale destroy jest czystszy)
                Destroy(gameObject);
            }
        }
    }
}