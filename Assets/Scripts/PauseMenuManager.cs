using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pauseMenuPanel;
    public Button resumeButton;
    public Button returnToMainMenuButton;

    bool isPaused = false;

    void Start()
    {
        if (pauseMenuPanel == null)
            Debug.LogError("PauseMenuPanel nie przypisany w Inspectorze");

        pauseMenuPanel.SetActive(false);

        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (returnToMainMenuButton != null) returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        pauseMenuPanel.SetActive(true);

        Time.timeScale = 0f;

        AudioListener.pause = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isPaused = true;
    }

    public void Resume()
    {
        pauseMenuPanel.SetActive(false);

        Time.timeScale = 1f;

        AudioListener.pause = false;

        isPaused = false;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene("MainMenu");
    }
}
