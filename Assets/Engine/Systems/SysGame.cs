using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

public class SysGame : MonoBehaviour
{
    // ---------- References ---------- //
    public static SysGame Instance { get; private set; }

    public SysResource ResourceManager;
    public SysUI UIManager;
    public SysLevel LevelManager;
    public SysCamera CameraManager;

    // ----------- Data ----------- //
    [SerializeField] GameMode gameMode = GameMode.Game;

    // ---------- Unity Hooks ---------- //
    void Awake() { Init(); }
    void Update() { Tick(); }

    // ---------- Lifecycle ---------- //

    // - Lifecycle Core -

    void Init()
    {
        InitSingleton();

        // Ensure sub-systems are attached
        if (ResourceManager == null)
        {
            // Throw error and end
            Debug.LogError("ResourceManager not assigned in SysGame!");
            return;
        }

        if (UIManager == null)
        {
            // Throw error and end
            Debug.LogError("UIManager not assigned in SysGame!");
            return;
        }

        if (LevelManager == null)
        {
            // Throw error and end
            Debug.LogError("LevelManager not assigned in SysGame!");
            return;
        }

        if (CameraManager == null)
        {
            // Throw error and end
            Debug.LogError("CameraManager not assigned in SysGame!");
            return;
        }

        // Init Subsystems
        ResourceManager.Init();
        UIManager.Init();
        LevelManager.Init();
        CameraManager.Init();


        // Start the game
        StartGame();
    }

    void Tick()
    {
        DebugFrameTracker.GlobalFrameCount++;

        CameraManager.Tick();
        UIManager.Tick();
        LevelManager.Tick();
    }

    void StartGame()
    {
        if (gameMode == GameMode.Game)
        {
            SysResource.Instance.CacheAllOfType<ResCfgLevel>(() =>
            {
                Debug.Log("All levels cached successfully.");
                // Load the level menu
                UIManager.GetPanelByID("LevelSelect").SetPanelIsActive(true);
            });
        }
        else if (gameMode == GameMode.ModKit)
        {
            // Load all resources for all modules
            SysResource.Instance.CacheAllResources(() =>
            {
                Debug.Log("All resources cached successfully.");
                // Open the mod kit panel
                UIPanelModKitCore.Instance.OpenModKit();
            });

        }

    }

    // - Lifecycle Utility -

    void InitSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // - Getters / Setters -
    public GameMode GetGameMode() { return gameMode; }

}

public enum GameMode
{
    Game,
    ModKit,
}

public static class DebugFrameTracker
{
    public static int GlobalFrameCount = 0;
}
