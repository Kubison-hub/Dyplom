using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToGame : MonoBehaviour
{
    // Funkcja dla przycisku "Wróæ" w scenie sterowania
    public void WrocDoGry()
    {
        // Dla pewnoœci resetujemy czas
        Time.timeScale = 1f;

        // £adujemy z powrotem scenê gry (zresetuje to postêp poziomu!)
        SceneManager.LoadScene("SH_DemoScene");
    }
}