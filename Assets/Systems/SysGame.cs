using NUnit.Framework;
using UnityEngine;

public class SysGame : MonoBehaviour
{
    // ---------- References ---------- //
    public static SysGame Instance { get; private set; }

    public SysResource ResourceManager;
    public SysUI UIManager;
    public SysLevel LevelManager;
    public SysCamera CameraManager;


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
        UIManager.Tick();
        LevelManager.Tick();
        CameraManager.Tick();
    }

    void StartGame()
    {
        SysResource sysResource = SysResource.Instance;

        sysResource.CacheAllOfType<ResCfgLevel>(() =>
        {
            Debug.Log("All levels cached successfully.");

        });
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

}
