using UnityEngine;

public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance;

    /// <summary>
    /// MainScene (and tests) may not include this component; PlanningScene uses PersistentSystems.
    /// </summary>
    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("GameModeManager");
        go.AddComponent<GameModeManager>();
    }

    public enum GameMode
    {
        Management,
        Action
    }

    public GameMode CurrentMode = GameMode.Management;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetMode(GameMode newMode)
    {
        CurrentMode = newMode;
        Debug.Log("Game Mode changed to: " + CurrentMode);
    }

    public bool IsActionMode()
    {
        return CurrentMode == GameMode.Action;
    }

    public bool IsManagementMode()
    {
        return CurrentMode == GameMode.Management;
    }
}