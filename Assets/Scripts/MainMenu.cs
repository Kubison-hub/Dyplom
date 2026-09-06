using UnityEngine;
using UnityEngine.SceneManagement;

using Debug = UnityEngine.Debug;
using Application = UnityEngine.Application;

public class MainMenu : MonoBehaviour
{
    [Header("UI Canvasy")]
    public GameObject mainMenuCanvas;
    public GameObject controlCanvas;
    public GameObject creditsCanvas;
    public GameObject settingsCanvas;

    private void Start()
    {
        // W³¹cz Main Menu i ukryj resztê na starcie
        ShowCanvas(mainMenuCanvas);
    }

    // Nowa gra: zawsze przez intro.
    public void PlayGame()
    {
        // Upewniamy siê, ¿e nowa gra nie wczytuje zapisu
        PlayerPrefs.SetInt("LoadGameOnStart", 0);
        PlayerPrefs.Save();

        Debug.Log("MainMenu: nowa gra, ³adujê scenê Intro.");
        SceneManager.LoadScene("Intro");
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