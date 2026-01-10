using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Elementy UI")]
    public GameObject pauseMenuPanel;        // Panel t³a pauzy
    public Button resumeButton;              // Przycisk "Wznów"
    public Button returnToMainMenuButton;    // Przycisk "Menu G³ówne"
    public Button controlsButton;            // Przycisk "Sterowanie"

    [Header("Obiekty do ukrycia")]
    // PRZYPISZ TUTAJ OBIEKT "Inventory" Z HIERARCHII
    public GameObject inventoryGameObject;

    // Zmienna prywatna œledz¹ca stan
    private bool isPaused = false;

    void Start()
    {
        // Zabezpieczenie przed brakiem przypisania paneli
        if (pauseMenuPanel == null)
        {
            Debug.LogError("B£¥D: PauseMenuPanel nie jest przypisany w Inspectorze!");
        }
        if (inventoryGameObject == null)
        {
            Debug.LogWarning("Ostrze¿enie: inventoryGameObject nie jest przypisany w Inspectorze. Prostok¹t nie zniknie.");
        }

        // Na starcie ukrywamy menu pauzy
        pauseMenuPanel.SetActive(false);

        // Przypisanie funkcji do przycisków
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (returnToMainMenuButton != null)
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(GoToControls);
    }

    void Update()
    {
        // Obs³uga klawisza ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);

        // --- BEZPOŒREDNIE WY£¥CZENIE OBIEKTU ---
        // Wy³¹czamy obiekt ekwipunku w hierarchii
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(false);
        }
        // ---------------------------------------

        // Opcjonalnie: Zamknij Dziennik, jeœli jest otwarty
        if (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen)
        {
            JournalManager.Instance.ToggleJournal();
        }

        Time.timeScale = 0f;        // Zatrzymujemy czas
        AudioListener.pause = true; // Wyciszamy dŸwiêki

        // Odblokowanie kursora
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);

        // --- PRZYWRÓCENIE OBIEKTU ---
        // W³¹czamy obiekt ekwipunku z powrotem
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(true);
        }
        // ----------------------------

        Time.timeScale = 1f;         // Wznawiamy czas
        AudioListener.pause = false; // Wznawiamy dŸwiêki

        isPaused = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Reset czasu przed zmian¹ sceny
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }

    public void GoToControls()
    {
        Time.timeScale = 1f; // Reset czasu
        AudioListener.pause = false;
        SceneManager.LoadScene("Sterowanie Pauza");
    }
}