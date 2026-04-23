using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MainMenu : MonoBehaviour
{
    [Header("Ustawienia Intro (Cinematic)")]
    public GameObject cinematicPanel;
    public VideoPlayer videoPlayer;

    [Header("Zarz¹dzanie Canvasami")]
    public GameObject mainMenuCanvas; // G³ówne menu (przyciski Start, Opcje, itp.)
    public GameObject controlCanvas;  // Canvas sterowania
    public GameObject creditsCanvas;  // Canvas twórców
    public GameObject settingsCanvas; // Canvas ustawieñ (widoczny na Twoim screenie)

    private bool isPlayingIntro = false;

    private void Start()
    {
        // Wy³¹czamy panel intro na starcie
        if (cinematicPanel != null) cinematicPanel.SetActive(false);

        // Ustawiamy event dla koñca wideo
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        // Upewniamy siê, ¿e po w³¹czeniu gry widaæ tylko G³ówne Menu
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

        // --- TYMCZASOWE DO TESTÓW ---
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerPrefs.DeleteKey("IntroObejrzane");
            Debug.Log("Zresetowano intro! Teraz odtworzy siê ponownie.");
        }
    }

    public void PlayGame()
    {
        if (PlayerPrefs.GetInt("IntroObejrzane", 0) == 1)
        {
            Debug.Log("Intro ju¿ by³o ogl¹dane. Pomijam.");
            LoadGameLevel();
            return;
        }

        if (cinematicPanel != null && videoPlayer != null)
        {
            // Przed odpaleniem wideo warto wy³¹czyæ ca³e UI, ¿eby nic nie przeœwitywa³o
            ShowCanvas(null);

            cinematicPanel.SetActive(true);
            videoPlayer.Play();
            isPlayingIntro = true;
            Cursor.visible = false;

            PlayerPrefs.SetInt("IntroObejrzane", 1);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning("Brak VideoPlayera! £adujê grê od razu.");
            LoadGameLevel();
        }
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        LoadGameLevel();
    }

    void LoadGameLevel()
    {
        // Tutaj nadal ³adujemy now¹ scenê, bo to w³aœciwa gra
        Cursor.visible = true;
        isPlayingIntro = false;
        SceneManager.LoadScene("SH_GAME_LEVEL_1");
    }

    public void QuitGame()
    {
        Debug.Log("Zamykam grê...");
        Application.Quit();
    }

    // --- FUNKCJE PRZE£¥CZANIA CANVASÓW ---

    public void Sterowanie()
    {
        ShowCanvas(controlCanvas);
    }

    public void Credits()
    {
        ShowCanvas(creditsCanvas);
    }

    public void Settings() // Doda³em dla Settings Canvas, który widaæ na screenie
    {
        ShowCanvas(settingsCanvas);
    }

    public void Menu()
    {
        ShowCanvas(mainMenuCanvas);
    }

    // --- METODA POMOCNICZA ---

    // Ta funkcja wy³¹cza wszystkie Canvasy i w³¹cza tylko ten, który jej przeka¿emy
    private void ShowCanvas(GameObject canvasToShow)
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
        if (controlCanvas != null) controlCanvas.SetActive(false);
        if (creditsCanvas != null) creditsCanvas.SetActive(false);
        if (settingsCanvas != null) settingsCanvas.SetActive(false);

        if (canvasToShow != null)
        {
            canvasToShow.SetActive(true);
        }
    }
}