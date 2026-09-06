using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    [Header("Ustawienia Intro (Cinematic)")]
    public GameObject cinematicPanel;
    public VideoPlayer videoPlayer;

    [Header("UI Canvasy")]
    public GameObject mainMenuCanvas;
    public GameObject controlCanvas;
    public GameObject creditsCanvas;
    public GameObject settingsCanvas;

    private bool isPlayingIntro = false;

    private void Start()
    {
        if (cinematicPanel != null) cinematicPanel.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // W³¹cz Main Menu i ukryj resztê na starcie
        ShowCanvas(mainMenuCanvas);
    }

    private void Update()
    {
        if (isPlayingIntro)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape))
            {
                LoadGameLevel();
            }
        }

        // Resetowanie zapisu intro do testów
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerPrefs.DeleteKey("IntroObejrzane");
            Debug.Log("Zresetowano intro!");
        }
    }

    public void PlayGame()
    {
        PlayerPrefs.SetInt("LoadGameOnStart", 0);
        PlayerPrefs.Save();

        if (PlayerPrefs.GetInt("IntroObejrzane", 0) == 1)
        {
            LoadGameLevel();
            return;
        }

        if (cinematicPanel != null && videoPlayer != null)
        {
            cinematicPanel.SetActive(true);
            videoPlayer.Play();
            isPlayingIntro = true;
            Cursor.visible = false;

            PlayerPrefs.SetInt("IntroObejrzane", 1);
            PlayerPrefs.Save();
        }
        else
        {
            LoadGameLevel();
        }
    }

    public void LoadGameFromMenu()
    {
        PlayerPrefs.SetInt("LoadGameOnStart", 1);
        PlayerPrefs.Save();
        LoadGameLevel();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        LoadGameLevel();
    }

    void LoadGameLevel()
    {
        Cursor.visible = true;
        isPlayingIntro = false;
        SceneManager.LoadScene("GameLeveL_DEV");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // --- ZARZ¥DZANIE CANVASAMI ---

    public void Sterowanie()
    {
        ShowCanvas(controlCanvas);
    }

    public void Credits()
    {
        ShowCanvas(creditsCanvas);
    }

    public void Settings()
    {
        ShowCanvas(settingsCanvas);
    }

    public void Menu()
    {
        ShowCanvas(mainMenuCanvas);
    }

    private void ShowCanvas(GameObject canvasToShow)
    {
        // Wy³¹cza wszystkie canvasy
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        if (controlCanvas != null) controlCanvas.SetActive(false);
        if (creditsCanvas != null) creditsCanvas.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);

        // W³¹cza tylko ten docelowy
        if (canvasToShow != null) canvasToShow.SetActive(true);
    }
}