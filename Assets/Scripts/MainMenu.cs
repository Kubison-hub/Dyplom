using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;

public class MainMenu : MonoBehaviour
{
    [Header("UI Canvasy")]
    public GameObject mainMenuCanvas;
    public GameObject controlCanvas;
    public GameObject creditsCanvas;
    public GameObject settingsCanvas;

    [Header("Start Game Fade")]
    [SerializeField] private CanvasGroup startFadeCanvasGroup;
    [SerializeField] private Color startFadeColor = Color.black;
    [SerializeField, Min(0f)] private float startFadeDelay = 0.1f;
    [SerializeField, Min(0.01f)] private float startFadeDuration = 1.25f;
    [SerializeField, Min(0f)] private float startFadeHoldDuration = 0.1f;

    private bool isStartingGame;

    private void Start()
    {
        // W³¹cz Main Menu i ukryj resztê na starcie
        ShowCanvas(mainMenuCanvas);
    }

    // Nowa gra: zawsze przez intro.
    public void PlayGame()
    {
        if (isStartingGame)
            return;

        StartCoroutine(PlayGameAfterFade());
    }

    private IEnumerator PlayGameAfterFade()
    {
        isStartingGame = true;
        PlayerPrefs.SetInt("LoadGameOnStart", 0);
        PlayerPrefs.Save();

        if (startFadeDelay > 0f)
            yield return new WaitForSecondsRealtime(startFadeDelay);

        CanvasGroup overlay = ResolveStartFadeCanvasGroup();
        if (overlay != null)
        {
            overlay.gameObject.SetActive(true);
            overlay.blocksRaycasts = true;
            overlay.interactable = true;

            float startAlpha = overlay.alpha;
            float elapsed = 0f;
            while (elapsed < startFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.alpha = Mathf.Lerp(startAlpha, 1f, Mathf.Clamp01(elapsed / startFadeDuration));
                yield return null;
            }

            overlay.alpha = 1f;
        }

        if (startFadeHoldDuration > 0f)
            yield return new WaitForSecondsRealtime(startFadeHoldDuration);

        Debug.Log("MainMenu: nowa gra, laduje scene Intro.");
        SceneManager.LoadScene("Intro");
    }

    private CanvasGroup ResolveStartFadeCanvasGroup()
    {
        if (startFadeCanvasGroup != null)
            return startFadeCanvasGroup;

        GameObject canvasObject = new GameObject(
            "MainMenuStartFadeCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        startFadeCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        startFadeCanvasGroup.alpha = 0f;
        startFadeCanvasGroup.blocksRaycasts = false;
        startFadeCanvasGroup.interactable = false;

        GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);

        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = startFadeColor;
        image.raycastTarget = true;

        return startFadeCanvasGroup;
    }
    public void LoadGameFromMenu()
    {
        PlayerPrefs.SetInt("LoadGameOnStart", 1);
        PlayerPrefs.Save();
        LoadGameLevel();
    }

    public void LoadGameLevel()
    {
        Cursor.visible = true;
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