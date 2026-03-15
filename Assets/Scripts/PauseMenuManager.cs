using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Elementy UI - Panele")]
    public GameObject pauseMenuPanel;        // Panel tła pauzy

    [Header("Elementy UI - Przyciski")]
    public Button resumeButton;              // Przycisk "Wznów"
    public Button returnToMainMenuButton;    // Przycisk "Menu Główne"
    public Button controlsButton;            // Przycisk "Sterowanie"
    public Button saveButton;                // NOWOŚĆ: Przycisk "Zapisz"
    public Button loadButton;                // NOWOŚĆ: Przycisk "Wczytaj"

    [Header("Obiekty do ukrycia podczas pauzy")]
    // PRZYPISZ TUTAJ OBIEKT "Inventory" Z HIERARCHII
    public GameObject inventoryGameObject;   // Twój "szary prostokąt" z ekwipunkiem

    // Zmienna prywatna śledząca stan
    private bool isPaused = false;

    void Start()
    {
        // Zabezpieczenie przed brakiem przypisania paneli
        if (pauseMenuPanel == null)
        {
            Debug.LogError("BŁĄD: PauseMenuPanel nie jest przypisany w Inspectorze!");
        }
        if (inventoryGameObject == null)
        {
            Debug.LogWarning("Ostrzeżenie: inventoryGameObject nie jest przypisany w Inspectorze. Prostokąt nie zniknie.");
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

        // NOWOŚĆ: Obsługa Zapisu i Wczytywania
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
                    Resume(); // Po wczytaniu automatycznie wznawiamy grę
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
        // Obsługa klawisza ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);

        // 1. Ukrywamy "szary prostokąt" (cały obiekt Inventory)
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(false);
        }

        // 2. Wymuszamy zamknięcie logiki Inventory (reset zmiennych)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ForceCloseInventory();
        }

        // 3. Zamykamy Dziennik jeśli jest otwarty
        if (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen)
        {
            JournalManager.Instance.ToggleJournal();
        }

        Time.timeScale = 0f;        // Zatrzymujemy czas
        AudioListener.pause = true; // Wyciszamy dźwięki

        // Odblokowanie kursora
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);

        // Przywracamy widoczność obiektu Inventory (prostokąta)
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(true);
        }

        Time.timeScale = 1f;         // Wznawiamy czas
        AudioListener.pause = false; // Wznawiamy dźwięki

        isPaused = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Reset czasu przed zmianą sceny
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