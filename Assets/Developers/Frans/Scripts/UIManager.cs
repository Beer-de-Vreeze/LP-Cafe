using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void GoToGame()
    {
        // Load the game scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("TestCafe");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
