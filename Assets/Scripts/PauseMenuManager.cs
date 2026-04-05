using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Główne Canvasy UI")]
    public GameObject pauzaCanvas;     // Podepnij tu cały "Pauza Canvas"
    public GameObject controlCanvas;   // Podepnij tu cały "Control Canvas"
    public GameObject settingsCanvas;  // Podepnij tu cały "Settings Canvas"

    [Header("Elementy UI - Przyciski Główne")]
    public Button resumeButton;
    public Button saveButton;
    public Button returnToMainMenuButton;

    [Header("Elementy UI - Przyciski Przełączania")]
    public Button controlsButton;      // Twój przycisk "Sterowanie"
    public Button settingsButton;      // Twój przycisk "Ustawienia"

    [Header("Elementy UI - Przyciski Powrotu")]
    public Button closeControlsButton; // Przycisk "Return to Main Menu" w Control Canvas
    public Button closeSettingsButton; // Przycisk powrotu w Settings Canvas

    [Header("Obiekty do ukrycia podczas pauzy")]
    public GameObject inventoryGameObject;

    private bool isPaused = false;

    void Start()
    {
        // Na starcie wyłączamy wszystko, żeby pauza nie zasłaniała gry
        HideAllCanvases();

        // --- PODPINANIE GŁÓWNYCH PRZYCISKÓW ---
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (returnToMainMenuButton != null) returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);

        // --- PODPINANIE PRZEŁĄCZANIA CANVASÓW ---
        if (controlsButton != null) controlsButton.onClick.AddListener(ShowControls);
        if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettings);

        // --- PODPINANIE POWROTÓW DO GŁÓWNEJ PAUZY ---
        if (closeControlsButton != null) closeControlsButton.onClick.AddListener(ShowMainPauseMenu);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(ShowMainPauseMenu);

        // (Opcjonalnie) Zapis gry
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(() => {
                if (SaveLoadManager.Instance != null) SaveLoadManager.Instance.SaveGame();
            });
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                // Jeśli jesteśmy głębiej w menu (Ustawienia/Sterowanie), ESC cofa nas do głównej pauzy
                if ((controlCanvas != null && controlCanvas.activeSelf) ||
                    (settingsCanvas != null && settingsCanvas.activeSelf))
                {
                    ShowMainPauseMenu();
                }
                else
                {
                    // Jeśli jesteśmy w głównym oknie pauzy, ESC wyłącza pauzę i wznawia grę
                    Resume();
                }
            }
            else
            {
                // Jeśli gra nie jest zapauzowana, włączamy pauzę
                Pause();
            }
        }
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // Otwieramy główny Canvas pauzy
        ShowMainPauseMenu();

        // Ukrywanie logiki gry (Inventory/Dziennik)
        if (inventoryGameObject != null) inventoryGameObject.SetActive(false);
        if (InventoryManager.Instance != null) InventoryManager.Instance.ForceCloseInventory();
        if (JournalManager.Instance != null && JournalManager.Instance.isJournalOpen) JournalManager.Instance.ToggleJournal();

        // Odblokowanie kursora
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Chowamy całe UI pauzy
        HideAllCanvases();

        if (inventoryGameObject != null) inventoryGameObject.SetActive(true);
    }

    // --- FUNKCJE ZARZĄDZAJĄCE CANVASAMI ---

    public void ShowMainPauseMenu()
    {
        HideAllCanvases();
        if (pauzaCanvas != null) pauzaCanvas.SetActive(true);
    }

    public void ShowControls()
    {
        HideAllCanvases();
        if (controlCanvas != null) controlCanvas.SetActive(true);
    }

    public void ShowSettings()
    {
        HideAllCanvases();
        if (settingsCanvas != null) settingsCanvas.SetActive(true);
    }

    private void HideAllCanvases()
    {
        if (pauzaCanvas != null) pauzaCanvas.SetActive(false);
        if (controlCanvas != null) controlCanvas.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("MainMenu");
    }
}