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

        // --- TYMCZASOWE DO TESTÓW (Mo¿esz to usun¹æ potem) ---
        // Wciœnij 'R' w menu, ¿eby zresetowaæ pamiêæ o intrze
        if (Input.GetKeyDown(KeyCode.R))
        {
            PlayerPrefs.DeleteKey("IntroObejrzane");
            Debug.Log("Zresetowano intro! Teraz odtworzy siê ponownie.");
        }
        // -----------------------------------------------------
    }

    public void PlayGame()
    {
        // 1. SPRAWDZAMY CZY JU¯ OGL¥DA£
        // Pobieramy wartoœæ (jeœli nie ma klucza, domyœlnie 0)
        if (PlayerPrefs.GetInt("IntroObejrzane", 0) == 1)
        {
            // Jeœli 1, to znaczy ¿e widzia³ -> £adujemy od razu grê
            Debug.Log("Intro ju¿ by³o ogl¹dane. Pomijam.");
            LoadGameLevel();
            return;
        }

        // 2. JEŒLI NIE OGL¥DA£ -> ODPALAMY FILM
        if (cinematicPanel != null && videoPlayer != null)
        {
            cinematicPanel.SetActive(true);
            videoPlayer.Play();
            isPlayingIntro = true;
            Cursor.visible = false;

            // 3. ZAPISUJEMY W PAMIÊCI, ¯E JU¯ OGL¥DA£
            PlayerPrefs.SetInt("IntroObejrzane", 1);
            PlayerPrefs.Save(); // Wa¿ne: Zapisz zmiany na dysku
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
        Cursor.visible = true;
        isPlayingIntro = false;
        SceneManager.LoadScene("SH_DemoScene");
    }

    public void QuitGame()
    {
        Debug.Log("Zamykam grê...");
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