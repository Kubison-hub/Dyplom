using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Elementy UI - Panele")]
    public GameObject pauseMenuPanel;        // Panel t�a pauzy

    [Header("Elementy UI - Przyciski")]
    public Button resumeButton;              // Przycisk "Wzn�w"
    public Button returnToMainMenuButton;    // Przycisk "Menu G��wne"
    public Button controlsButton;            // Przycisk "Sterowanie"
    public Button saveButton;                // NOWO��: Przycisk "Zapisz"
    public Button loadButton;                // NOWO��: Przycisk "Wczytaj"

    [Header("Obiekty do ukrycia podczas pauzy")]
    public GameObject inventoryGameObject;   // Tw�j "szary prostok�t" z ekwipunkiem

    [Header("Obiekty do ukrycia")]
    // PRZYPISZ TUTAJ OBIEKT "Inventory" Z HIERARCHII
    public GameObject inventoryGameObject;

    // Zmienna prywatna �ledz�ca stan
    private bool isPaused = false;

    void Start()
    {
        // Zabezpieczenie przed brakiem przypisania paneli
        if (pauseMenuPanel == null)
        {
            Debug.LogError("B��D: PauseMenuPanel nie jest przypisany w Inspectorze!");
        }
        if (inventoryGameObject == null)
        {
            Debug.LogWarning("Ostrze�enie: inventoryGameObject nie jest przypisany w Inspectorze. Prostok�t nie zniknie.");
        }

        // Na starcie ukrywamy menu pauzy
        pauseMenuPanel.SetActive(false);

        // --- PRZYPISANIE FUNKCJI DO PRZYCISK�W ---

        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);

        if (returnToMainMenuButton != null)
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);

        if (controlsButton != null)
            controlsButton.onClick.AddListener(GoToControls);

        // NOWO��: Obs�uga Zapisu i Wczytywania
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
                    Resume(); // Po wczytaniu automatycznie wznawiamy gr�
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
        // Obs�uga klawisza ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);

        // 1. Ukrywamy "szary prostok�t" (ca�y obiekt Inventory)
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(false);
        }

        // 2. Wymuszamy zamkni�cie logiki Inventory (reset zmiennych)
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ForceCloseInventory();
        }

        // 3. Zamykamy Dziennik je�li jest otwarty
        if (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen)
        {
            JournalManager.Instance.ToggleJournal();
        }

        Time.timeScale = 0f;        // Zatrzymujemy czas
        AudioListener.pause = true; // Wyciszamy d�wi�ki

        // Odblokowanie kursora
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);

        // Przywracamy widoczno�� obiektu Inventory (prostok�ta)
        if (inventoryGameObject != null)
        {
            inventoryGameObject.SetActive(true);
        }

        Time.timeScale = 1f;         // Wznawiamy czas
        AudioListener.pause = false; // Wznawiamy d�wi�ki

        isPaused = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Reset czasu przed zmian� sceny
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