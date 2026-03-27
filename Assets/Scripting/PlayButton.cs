using UnityEngine;

public class PlayButton : MonoBehaviour
{
    public string gameSceneName = "Terrain"; // Đặt đúng tên Scene
    [SerializeField] private bool openLobbyInsteadOfDirectLoad = true;

    public void StartGame()
    {
        if (openLobbyInsteadOfDirectLoad)
        {
            OpenLobby();
            return;
        }

        OpenLobby();
    }

    private void OpenLobby()
    {
        Sever sever = FindFirstObjectByType<Sever>();
        if (sever == null)
        {
            GameObject manager = new GameObject("ServerManager");
            sever = manager.AddComponent<Sever>();
        }

        sever.SetGameSceneName(gameSceneName);
        sever.OpenLobbyPanel();
    }
}
