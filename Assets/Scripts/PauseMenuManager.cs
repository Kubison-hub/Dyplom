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

        // Przypisanie funkcji do przycisków (mo¿na to te¿ zrobiæ rêcznie w Inspectorze)
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

        Time.timeScale = 0f;        // Zatrzymujemy czas
        AudioListener.pause = true; // Wyciszamy dŸwiêki

        // Odblokowanie kursora, ¿eby gracz móg³ klikaæ
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;         // Wznawiamy czas
        AudioListener.pause = false; // Wznawiamy dŸwiêki

        // Opcjonalnie: Zablokuj kursor z powrotem (dla gier FPP/TPP)
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

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

        // UWAGA: To zresetuje postêp w obecnym poziomie!
        SceneManager.LoadScene("Sterowanie Pauza");
    }
}