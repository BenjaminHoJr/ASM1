using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Sever : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrapIfMissing()
    {
        if (FindFirstObjectByType<Sever>() != null)
        {
            return;
        }

        GameObject auto = new GameObject("ServerManager");
        auto.AddComponent<Sever>();
    }

    [Header("Server Selection")]
    [SerializeField] private string[] serverNames = { "Server-01", "Server-02", "Server-03" };
    [SerializeField] private string selectedServer = "Server-01";

    [Header("Mode Selection")]
    [SerializeField] private string[] modeNames = { "Coop" };
    [SerializeField] private string selectedMode = "Coop";

    [Header("Game Settings")]
    [SerializeField] private string gameSceneName = "Terrain";
    [SerializeField] private int playersRequiredToStart = 2;
    [SerializeField] private bool allowLocalSceneFallbackIfFusionLoadFails = true;

    [Header("Built-in Lobby UI")]
    [SerializeField] private bool showBuiltInLobbyUI = true;
    [SerializeField] private bool showLobbyOnStart = false;
    [SerializeField] private Vector2 lobbyPanelSize = new Vector2(460f, 270f);

    private NetworkRunner runner;
    private bool isStarting;
    private bool hasRequestedSceneLoad;
    private int cachedPlayerCount = -1;
    private bool cachedConnected;
    private int selectedServerIndex;
    private int selectedModeIndex;
    private bool lobbyVisible;

    public event Action StateChanged;

    public string SelectedServer => selectedServer;
    public string SelectedMode => selectedMode;
    public string SessionName => BuildSessionName();
    public bool IsJoining => isStarting;
    public bool IsConnected => runner != null && runner.IsRunning;
    public bool IsHost => IsConnected && runner.IsServer;
    public int PlayersRequiredToStart => playersRequiredToStart;
    public int CurrentPlayerCount => IsConnected ? GetActivePlayerCount() : 0;

    private void Awake()
    {
        EnsureValidOptions();
        playersRequiredToStart = Mathf.Max(2, playersRequiredToStart);
        SyncSelectedIndicesFromCurrentValues();
        lobbyVisible = showLobbyOnStart;
        NotifyStateChanged();
    }

    public void OpenLobbyPanel()
    {
        showBuiltInLobbyUI = true;
        lobbyVisible = true;
        NotifyStateChanged();
    }

    public void HideLobbyPanel()
    {
        lobbyVisible = false;
        NotifyStateChanged();
    }

    public void SetGameSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        gameSceneName = sceneName.Trim();
    }

    public async void JoinSelectedServer()
    {
        if (isStarting)
        {
            return;
        }

        if (runner != null && runner.IsRunning)
        {
            Debug.Log("Sever: Runner da chay, dang o session " + BuildSessionName());
            NotifyStateChanged();
            return;
        }

        isStarting = true;
        hasRequestedSceneLoad = false;

        try
        {
            if (runner == null)
            {
                runner = CreateRunner();
            }

            string sessionName = BuildSessionName();
            Scene activeScene = SceneManager.GetActiveScene();

            StartGameArgs args = new StartGameArgs
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = sessionName,
                Scene = SceneRef.FromIndex(activeScene.buildIndex),
                SceneManager = runner.GetComponent<NetworkSceneManagerDefault>()
            };

            StartGameResult result = await runner.StartGame(args);
            if (result.Ok)
            {
                Debug.Log("Sever: Join server thanh cong. Session = " + sessionName);
                NotifyStateChanged();
            }
            else
            {
                Debug.LogError("Sever: Join server that bai: " + result.ShutdownReason);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Sever: Loi khi tao/join session: " + ex.Message);
        }
        finally
        {
            isStarting = false;
            NotifyStateChanged();
        }
    }

    public void SelectServer(string serverName)
    {
        if (!string.IsNullOrWhiteSpace(serverName))
        {
            selectedServer = serverName.Trim();
            NotifyStateChanged();
        }
    }

    public void SelectServerByIndex(int index)
    {
        if (index >= 0 && index < serverNames.Length)
        {
            selectedServer = serverNames[index];
            NotifyStateChanged();
        }
    }

    public void SelectMode(string modeName)
    {
        if (!string.IsNullOrWhiteSpace(modeName))
        {
            selectedMode = modeName.Trim();
            NotifyStateChanged();
        }
    }

    public void SelectModeByIndex(int index)
    {
        if (index >= 0 && index < modeNames.Length)
        {
            selectedMode = modeNames[index];
            NotifyStateChanged();
        }
    }

    private void Update()
    {
        NotifyIfRuntimeStateChanged();

        if (runner == null || !runner.IsRunning)
        {
            return;
        }

        if (hasRequestedSceneLoad)
        {
            return;
        }

        if (GetActivePlayerCount() < playersRequiredToStart)
        {
            return;
        }

        bool loaded = false;
        if (runner.IsServer)
        {
            loaded = TryFusionLoadScene(gameSceneName);
        }

        if (!loaded && allowLocalSceneFallbackIfFusionLoadFails)
        {
            loaded = TryLocalLoadScene(gameSceneName);
        }

        hasRequestedSceneLoad = loaded;
        if (hasRequestedSceneLoad)
        {
            lobbyVisible = false;
        }
        NotifyStateChanged();
    }

    private NetworkRunner CreateRunner()
    {
        GameObject go = new GameObject("Prototype Runner");
        DontDestroyOnLoad(go);

        NetworkRunner networkRunner = go.AddComponent<NetworkRunner>();
        networkRunner.ProvideInput = true;

        if (go.GetComponent<NetworkSceneManagerDefault>() == null)
        {
            go.AddComponent<NetworkSceneManagerDefault>();
        }

        return networkRunner;
    }

    private string BuildSessionName()
    {
        return selectedServer + "|" + selectedMode;
    }

    private void OnGUI()
    {
        if (!showBuiltInLobbyUI)
        {
            return;
        }

        if (!lobbyVisible)
        {
            return;
        }

        float x = (Screen.width - lobbyPanelSize.x) * 0.5f;
        float y = (Screen.height - lobbyPanelSize.y) * 0.5f;
        Rect panelRect = new Rect(x, y, lobbyPanelSize.x, lobbyPanelSize.y);
        GUILayout.BeginArea(panelRect, "Lobby", GUI.skin.window);

        GUILayout.Label("Chon server va che do de vao cung 1 phong:");

        GUI.enabled = !IsConnected && !IsJoining;

        GUILayout.Space(6f);
        GUILayout.Label("Server:");
        string[] safeServers = GetSafeServerNames();
        int newServerIndex = GUILayout.SelectionGrid(selectedServerIndex, safeServers, Mathf.Clamp(safeServers.Length, 1, 3));
        if (newServerIndex != selectedServerIndex)
        {
            selectedServerIndex = newServerIndex;
            SelectServerByIndex(selectedServerIndex);
        }

        GUILayout.Space(6f);
        GUILayout.Label("Mode:");
        string[] safeModes = GetSafeModeNames();
        int newModeIndex = GUILayout.SelectionGrid(selectedModeIndex, safeModes, Mathf.Clamp(safeModes.Length, 1, 3));
        if (newModeIndex != selectedModeIndex)
        {
            selectedModeIndex = newModeIndex;
            SelectModeByIndex(selectedModeIndex);
        }

        GUI.enabled = true;

        GUILayout.Space(10f);
        GUILayout.BeginHorizontal();
        GUI.enabled = !IsConnected && !IsJoining;
        if (GUILayout.Button("Vao phong", GUILayout.Height(32f)))
        {
            JoinSelectedServer();
        }

        GUI.enabled = IsConnected || IsJoining;
        if (GUILayout.Button("Roi phong", GUILayout.Height(32f)))
        {
            LeaveServer();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(8f);
        GUILayout.Label("Session: " + SessionName);
        GUILayout.Label(GetStatusText());

        if (IsConnected)
        {
            GUILayout.Label("Vai tro: " + (IsHost ? "Host" : "Client"));
            GUILayout.Label("Nguoi choi: " + CurrentPlayerCount + "/" + PlayersRequiredToStart);
        }

        GUILayout.EndArea();
    }

    public IReadOnlyList<string> GetServerNames()
    {
        return serverNames;
    }

    public IReadOnlyList<string> GetModeNames()
    {
        return modeNames;
    }

    private string[] GetSafeServerNames()
    {
        if (serverNames == null || serverNames.Length == 0)
        {
            serverNames = new[] { "Server-01" };
        }

        return serverNames;
    }

    private string[] GetSafeModeNames()
    {
        if (modeNames == null || modeNames.Length == 0)
        {
            modeNames = new[] { "Coop" };
        }

        return modeNames;
    }

    public string GetStatusText()
    {
        if (isStarting)
        {
            return "Dang ket noi server...";
        }

        if (!IsConnected)
        {
            return "Chua ket noi. Chon server va bam vao phong.";
        }

        int count = CurrentPlayerCount;
        if (count < playersRequiredToStart)
        {
            return "Phong " + SessionName + ": " + count + "/" + playersRequiredToStart + " nguoi. Dang cho nguoi thu 2...";
        }

        if (hasRequestedSceneLoad)
        {
            return "Da du nguoi. Dang vao game...";
        }

        return "Da du nguoi cho tran dau.";
    }

    private int GetActivePlayerCount()
    {
        int count = 0;
        foreach (PlayerRef _ in runner.ActivePlayers)
        {
            count++;
        }

        return count;
    }

    private bool TryFusionLoadScene(string sceneName)
    {
        if (runner == null || !runner.IsRunning)
        {
            return false;
        }

        int buildIndex = ResolveBuildIndexBySceneName(sceneName);
        if (buildIndex < 0)
        {
            Debug.LogError("Sever: Khong tim thay scene trong Build Settings: " + sceneName);
            return false;
        }

        SceneRef sceneRef = SceneRef.FromIndex(buildIndex);
        MethodInfo[] methods = typeof(NetworkRunner).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        bool foundOverload = false;

        foreach (MethodInfo method in methods)
        {
            if (method.Name != "LoadScene")
            {
                continue;
            }

            foundOverload = true;

            ParameterInfo[] parameters = method.GetParameters();
            try
            {
                if (parameters.Length == 1 && parameters[0].ParameterType == typeof(SceneRef))
                {
                    method.Invoke(runner, new object[] { sceneRef });
                    Debug.Log("Sever: Da yeu cau vao scene game khi du 2 nguoi.");
                    return true;
                }

                if (parameters.Length == 2 && parameters[0].ParameterType == typeof(SceneRef))
                {
                    object secondArg = parameters[1].ParameterType.IsValueType
                        ? Activator.CreateInstance(parameters[1].ParameterType)
                        : null;

                    method.Invoke(runner, new object[] { sceneRef, secondArg });
                    Debug.Log("Sever: Da yeu cau vao scene game khi du 2 nguoi.");
                    return true;
                }
            }
            catch
            {
                // Try next overload.
            }
        }

        if (!foundOverload)
        {
            Debug.LogWarning("Sever: Khong tim thay NetworkRunner.LoadScene overload phu hop. Se thu fallback local scene load.");
        }

        return false;
    }

    private bool TryLocalLoadScene(string sceneName)
    {
        int buildIndex = ResolveBuildIndexBySceneName(sceneName);
        if (buildIndex < 0)
        {
            return false;
        }

        if (SceneManager.GetActiveScene().buildIndex == buildIndex)
        {
            return true;
        }

        Debug.LogWarning("Sever: Dang dung fallback SceneManager.LoadScene(" + sceneName + ").");
        SceneManager.LoadScene(buildIndex);
        return true;
    }

    private int ResolveBuildIndexBySceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return -1;
        }

        // Nếu scene đang loaded thì dùng trực tiếp.
        Scene loaded = SceneManager.GetSceneByName(sceneName);
        if (loaded.IsValid())
        {
            return loaded.buildIndex;
        }

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(scenePath))
            {
                continue;
            }

            string nameFromPath = Path.GetFileNameWithoutExtension(scenePath);
            if (string.Equals(nameFromPath, sceneName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public async void LeaveServer()
    {
        if (runner == null)
        {
            return;
        }

        try
        {
            await runner.Shutdown();
        }
        catch
        {
            // Ignore shutdown errors.
        }

        if (runner != null)
        {
            Destroy(runner.gameObject);
            runner = null;
        }

        hasRequestedSceneLoad = false;
        lobbyVisible = true;
        cachedPlayerCount = -1;
        cachedConnected = false;
        NotifyStateChanged();
    }

    private void NotifyIfRuntimeStateChanged()
    {
        bool connected = IsConnected;
        int playerCount = connected ? GetActivePlayerCount() : 0;

        if (connected != cachedConnected || playerCount != cachedPlayerCount)
        {
            cachedConnected = connected;
            cachedPlayerCount = playerCount;
            NotifyStateChanged();
        }
    }

    private void EnsureValidOptions()
    {
        GetSafeServerNames();
        GetSafeModeNames();

        if (Array.IndexOf(serverNames, selectedServer) < 0)
        {
            selectedServer = serverNames[0];
        }

        if (Array.IndexOf(modeNames, selectedMode) < 0)
        {
            selectedMode = modeNames[0];
        }
    }

    private void SyncSelectedIndicesFromCurrentValues()
    {
        selectedServerIndex = Mathf.Max(0, Array.IndexOf(GetSafeServerNames(), selectedServer));
        selectedModeIndex = Mathf.Max(0, Array.IndexOf(GetSafeModeNames(), selectedMode));
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
