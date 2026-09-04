
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;


public class MainMenu : MonoBehaviour
{
    [Header("Ustawienia Intro (Cinematic)")]
    public GameObject cinematicPanel;
    public VideoPlayer videoPlayer;

    private bool isPlayingIntro = false;

    private void Start()
    {
        if (cinematicPanel != null) cinematicPanel.SetActive(false);

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
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
        // Upewniamy siê, ¿e gra NIE ³aduje zapisu przy nowej grze
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

    // NOWA FUNKCJA - podepnij j¹ pod przycisk "Wczytaj" w Menu
    public void LoadGameFromMenu()
    {
        // Ustawiamy flagê, ¿eby SaveLoadManager wczyta³ dane po wejœciu na scenê
        PlayerPrefs.SetInt("LoadGameOnStart", 1);
        PlayerPrefs.Save();

        // Od razu ³adujemy scenê z pominiêciem intro
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

    public void Sterowanie()
    {
        SceneManager.LoadScene("Sterowanie");
    }

    public void Credits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void Menu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}