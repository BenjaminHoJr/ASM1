using UnityEngine;
using System.Collections;
using Fusion;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PlayerSpawn : MonoBehaviour
{
    [Header("Fusion Spawn")]
    [Tooltip("Prefab co NetworkObject de spawn bang Prototype Runner")]
    [SerializeField] private NetworkObject networkPlayerPrefab;

    [Header("Fallback Local Spawn")]
    [Tooltip("Duoc dung khi chua co runner hoac prefab network chua gan")]
    [SerializeField] private GameObject localPlayerPrefab;

    [SerializeField] private float waitRunnerTimeout = 10f;
    [SerializeField] private bool allowLocalFallback = true;
    [SerializeField] private bool disableFallbackWhenSeverExists = true;

    private bool hasSpawned;
    private Coroutine pendingSpawnRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Prototype Runner co the ton tai qua scene, can reset de spawn lai o van moi.
        hasSpawned = false;
        if (pendingSpawnRoutine != null)
        {
            StopCoroutine(pendingSpawnRoutine);
            pendingSpawnRoutine = null;
        }
    }

    public bool SpawnAtMazeStart(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        return SpawnAtMazeStart(spawnPosition, spawnRotation, null);
    }

    public bool SpawnAtMazeStart(Vector3 spawnPosition, Quaternion spawnRotation, Transform mazePlayerPrefab)
    {
        NetworkRunner runner = FindPrototypeRunner();
        NetworkObject existingBefore = FindExistingLocalPlayerObject(runner);
        if (existingBefore != null)
        {
            SetTransformSafely(existingBefore.transform, spawnPosition, spawnRotation);
            hasSpawned = true;
            CleanupDuplicateLocalPlayers(existingBefore, runner);
            return true;
        }

        if (hasSpawned && !HasAlivePlayerInScene())
        {
            hasSpawned = false;
        }

        if (hasSpawned)
        {
            return true;
        }

        ResolvePrefabsIfNeeded(mazePlayerPrefab);

        if (pendingSpawnRoutine != null)
        {
            StopCoroutine(pendingSpawnRoutine);
        }

        pendingSpawnRoutine = StartCoroutine(SpawnWhenRunnerReady(spawnPosition, spawnRotation));
        return true;
    }

    private IEnumerator SpawnWhenRunnerReady(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        float elapsed = 0f;

        while (elapsed < waitRunnerTimeout && !hasSpawned)
        {
            if (TrySpawnWithPrototypeRunner(spawnPosition, spawnRotation))
            {
                pendingSpawnRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        NetworkRunner activeRunner = FindPrototypeRunner();
        if (!hasSpawned && (activeRunner == null || !activeRunner.IsRunning))
        {
            if (ShouldUseLocalFallback())
            {
                SpawnLocalFallback(spawnPosition, spawnRotation);
            }
            else
            {
                // Co lobby/server manager thi uu tien cho runner online thay vi spawn local sai luong.
                pendingSpawnRoutine = StartCoroutine(WaitForOnlineRunnerAndSpawn(spawnPosition, spawnRotation));
                yield break;
            }
        }
        else if (!hasSpawned)
        {
            Debug.LogError("PlayerSpawn: Runner dang chay nhung khong spawn duoc player network. Kiem tra NetworkObject va NetworkPrefabs.");
        }

        pendingSpawnRoutine = null;
    }

    private IEnumerator WaitForOnlineRunnerAndSpawn(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        float maxWait = Mathf.Max(waitRunnerTimeout, 5f) * 3f;
        float elapsed = 0f;

        while (!hasSpawned && elapsed < maxWait)
        {
            if (TrySpawnWithPrototypeRunner(spawnPosition, spawnRotation))
            {
                pendingSpawnRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!hasSpawned && ShouldUseLocalFallback())
        {
            SpawnLocalFallback(spawnPosition, spawnRotation);
        }

        pendingSpawnRoutine = null;
    }

    private bool TrySpawnWithPrototypeRunner(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        NetworkRunner runner = FindPrototypeRunner();
        if (runner == null || !runner.IsRunning || networkPlayerPrefab == null)
        {
            return false;
        }

        Vector3 adjustedSpawn = GetSpawnOffsetForPlayer(spawnPosition, runner.LocalPlayer);

        try
        {
            // Neu local player da ton tai thi khong spawn them nua.
            NetworkObject existingLocal = FindExistingLocalPlayerObject(runner);
            if (existingLocal != null)
            {
                SetTransformSafely(existingLocal.transform, adjustedSpawn, spawnRotation);
                try { runner.SetPlayerObject(runner.LocalPlayer, existingLocal); } catch { }
                hasSpawned = true;
                CleanupDuplicateLocalPlayers(existingLocal, runner);
                Debug.Log("PlayerSpawn: Da tai su dung local player, khong spawn trung.");
                return true;
            }

            NetworkObject spawned = runner.Spawn(networkPlayerPrefab, adjustedSpawn, spawnRotation, runner.LocalPlayer);
            if (spawned != null)
            {
                runner.SetPlayerObject(runner.LocalPlayer, spawned);
                CleanupDuplicateLocalPlayers(spawned, runner);
            }
            hasSpawned = true;
            Debug.Log("PlayerSpawn: Da spawn player bang Prototype Runner tai diem bat dau maze.");
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SpawnLocalFallback(Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (localPlayerPrefab == null)
        {
            Debug.LogWarning("PlayerSpawn: Chua spawn duoc qua Prototype Runner va khong co local fallback prefab.");
            return;
        }

        Instantiate(localPlayerPrefab, spawnPosition, spawnRotation);
        hasSpawned = true;
        CleanupDuplicateLocalPlayers(null, null);
        Debug.LogWarning("PlayerSpawn: Da fallback spawn local vi runner chua san sang.");
    }

    private bool ShouldUseLocalFallback()
    {
        if (!allowLocalFallback)
        {
            return false;
        }

        if (!disableFallbackWhenSeverExists)
        {
            return true;
        }

        // Khi co Sever manager trong scene thi uu tien cho online flow, tranh spawn local trung.
        return FindFirstObjectByType<Sever>() == null;
    }

    private NetworkRunner FindPrototypeRunner()
    {
        GameObject runnerObj = GameObject.Find("Prototype Runner");
        if (runnerObj != null && runnerObj.TryGetComponent(out NetworkRunner prototypeRunner))
        {
            return prototypeRunner;
        }

        return FindFirstObjectByType<NetworkRunner>();
    }

    private void ResolvePrefabsIfNeeded(Transform mazePlayerPrefab)
    {
        if (mazePlayerPrefab == null)
        {
            return;
        }

        if (networkPlayerPrefab == null)
        {
            networkPlayerPrefab = mazePlayerPrefab.GetComponent<NetworkObject>();
        }

        if (localPlayerPrefab == null)
        {
            localPlayerPrefab = mazePlayerPrefab.gameObject;
        }
    }

    private bool HasAlivePlayerInScene()
    {
        if (GameObject.FindWithTag("Player") != null)
        {
            return true;
        }

        return FindFirstObjectByType<PlayerMovement>() != null;
    }

    private NetworkObject FindExistingLocalPlayerObject(NetworkRunner runner)
    {
        // Thu lay player object da duoc runner gan.
        NetworkObject fromRunner = TryGetRunnerPlayerObject(runner);
        if (fromRunner != null)
        {
            return fromRunner;
        }

        // Fallback: tim object player co authority local.
        NetworkObject[] all = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        for (int i = 0; i < all.Length; i++)
        {
            NetworkObject n = all[i];
            if (n == null) continue;
            if (n.GetComponent<PlayerMovement>() == null) continue;
            if (IsLocalOwnedPlayer(n, runner))
            {
                return n;
            }
        }

        return null;
    }

    private NetworkObject TryGetRunnerPlayerObject(NetworkRunner runner)
    {
        try
        {
            // TryGetPlayerObject(PlayerRef, out NetworkObject)
            var method = runner.GetType().GetMethod("TryGetPlayerObject");
            if (method != null)
            {
                object[] args = new object[] { runner.LocalPlayer, null };
                object result = method.Invoke(runner, args);
                if (result is bool ok && ok)
                {
                    return args[1] as NetworkObject;
                }
            }
        }
        catch
        {
            // Ignore reflection failures and fallback to scene scan.
        }

        return null;
    }

    private bool IsLocalOwnedPlayer(NetworkObject n, NetworkRunner runner)
    {
        if (n == null) return false;

        if (runner == null || !runner.IsRunning)
        {
            return true;
        }

        if (n.HasInputAuthority || n.HasStateAuthority)
        {
            return true;
        }

        PlayerInput pi = n.GetComponent<PlayerInput>();
        return pi != null && pi.user.valid && pi.user.pairedDevices.Count > 0;
    }

    private void CleanupDuplicateLocalPlayers(NetworkObject keep, NetworkRunner runner)
    {
        NetworkObject[] all = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        int localCount = 0;

        for (int i = 0; i < all.Length; i++)
        {
            NetworkObject n = all[i];
            if (n == null) continue;
            if (n.GetComponent<PlayerMovement>() == null) continue;
            if (!IsLocalOwnedPlayer(n, runner)) continue;
            localCount++;
        }

        if (localCount <= 1)
        {
            return;
        }

        for (int i = 0; i < all.Length; i++)
        {
            NetworkObject n = all[i];
            if (n == null) continue;
            if (n == keep) continue;
            if (n.GetComponent<PlayerMovement>() == null) continue;
            if (!IsLocalOwnedPlayer(n, runner)) continue;

            if (runner != null && runner.IsRunning)
            {
                try
                {
                    runner.Despawn(n);
                    continue;
                }
                catch
                {
                    // fallback destroy below
                }
            }

            Destroy(n.gameObject);
        }
    }

    private void SetTransformSafely(Transform target, Vector3 position, Quaternion rotation)
    {
        if (target == null) return;

        CharacterController cc = target.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            target.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
            return;
        }

        target.SetPositionAndRotation(position, rotation);
    }

    private Vector3 GetSpawnOffsetForPlayer(Vector3 baseSpawn, PlayerRef player)
    {
        int idx = GetStablePlayerIndex(player);
        if (idx <= 0)
        {
            return baseSpawn;
        }

        float ringRadius = 0.9f;
        float angle = (idx % 8) * 45f * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
        return baseSpawn + offset;
    }

    private int GetStablePlayerIndex(PlayerRef player)
    {
        // Thu dung property RawEncoded neu co (Fusion API thuong co).
        try
        {
            var prop = typeof(PlayerRef).GetProperty("RawEncoded");
            if (prop != null)
            {
                object v = prop.GetValue(player, null);
                if (v is int raw)
                {
                    return Mathf.Abs(raw);
                }
            }
        }
        catch
        {
            // fallback below
        }

        // Fallback an toan: hash chuoi PlayerRef.
        string s = player.ToString();
        unchecked
        {
            int h = 17;
            for (int i = 0; i < s.Length; i++)
            {
                h = h * 31 + s[i];
            }
            return Mathf.Abs(h);
        }
    }
}
