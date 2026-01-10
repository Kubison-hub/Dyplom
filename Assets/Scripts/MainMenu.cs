using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
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
}
