using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    public string gameSceneName = "Terrain"; // Đặt đúng tên Scene

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
