using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Elementy UI - Panele")]
    public GameObject pauseMenuPanel;        // Panel t³a pauzy

    [Header("Elementy UI - Przyciski")]
    public Button resumeButton;              // Przycisk "Wznów"
    public Button returnToMainMenuButton;    // Przycisk "Menu G³ówne"
    public Button controlsButton;            // Przycisk "Sterowanie"
    public Button saveButton;                // NOWOŒÆ: Przycisk "Zapisz"
    public Button loadButton;                // NOWOŒÆ: Przycisk "Wczytaj"

    [Header("Obiekty do ukrycia podczas pauzy")]
    public GameObject inventoryGameObject;   // Twój "szary prostok¹t" z ekwipunkiem

    // Zmienna prywatna œledz¹ca stan
    private bool isPaused = false;

    void Start()
    {
        // Zabezpieczenie przed brakiem przypisania panelu
        if (pauseMenuPanel == null)
        {
            Debug.LogError("B£¥D: PauseMenuPanel nie jest przypisany w Inspectorze!");
            return;
        }

        // Na starcie ukrywamy menu pauzy
        pauseMenuPanel.SetActive(false);

        // --- PRZYPISANIE FUNKCJI DO PRZYCISKÓW ---

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (returnToMainMenuButton != null)
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(GoToControls);

        // NOWOŒÆ: Obs³uga Zapisu i Wczytywania
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() =>
            {
                if (SaveLoadManager.Instance != null)
                    SaveLoadManager.Instance.SaveGame();
                else
                    Debug.LogError("Brak SaveLoadManager na scenie!");
            });
        }

        if (loadButton != null)
        {
            loadButton.onClick.AddListener(() =>
            {
                if (SaveLoadManager.Instance != null)
                {
                    SaveLoadManager.Instance.LoadGame();
                    Resume(); // Po wczytaniu automatycznie wznawiamy grê
                }
                else
                {
                    Debug.LogError("Brak SaveLoadManager na scenie!");
                }
            });
        }
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

        // 1. Ukrywamy "szary prostok¹t" (ca³y obiekt Inventory)
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(false);
        }

        // 2. Wymuszamy zamkniêcie logiki Inventory (reset zmiennych)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ForceCloseInventory();
        }

        // 3. Zamykamy Dziennik jeœli jest otwarty
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

        // Przywracamy widocznoœæ obiektu Inventory (prostok¹ta)
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(true);
        }

        Time.timeScale = 1f;         // Wznawiamy czas
        AudioListener.pause = false; // Wznawiamy dŸwiêki

        isPaused = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Reset czasu
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