using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures MainMenuScene loads first when the game starts, even if Editor Play begins with another scene open.
/// </summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureMainMenuFirst()
    {
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
