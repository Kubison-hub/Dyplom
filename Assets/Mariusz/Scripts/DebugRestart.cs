using UnityEngine;
using UnityEngine.SceneManagement;

public class DebugRestart : MonoBehaviour
{
    
    public KeyCode restartKey = KeyCode.T;

    private void Update()
    {
        if (Input.GetKeyDown(restartKey))
        {
            RestartScene();
        }
    }

    private void RestartScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}