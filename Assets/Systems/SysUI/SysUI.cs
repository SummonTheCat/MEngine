using UnityEngine;

public class SysUI : MonoBehaviour
{
    // ---------- References ---------- //

    public static SysUI Instance { get; private set; }

    // ---------- Data ---------- //
    private bool showingLevelMenu = true;
    private ResCfgLevel[] levelData;

    // ---------- Lifecycle ---------- //

    public void Init()
    {
        InitSingleton();
    }

    public void Tick()
    {
        levelData = SysResource.Instance.GetAllResourcesOfType<ResCfgLevel>().ToArray();
    }

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

    // ---------- TEMP UI ---------- //

    public void ShowLevelMenu()
    {
        showingLevelMenu = true;
        Debug.Log("Level Menu Shown");
    }

    public void HideLevelMenu()
    {
        showingLevelMenu = false;
        Debug.Log("Level Menu Hidden");
    }

    void OnGUI()
    {
        if (showingLevelMenu)
        {
            // Button for each level
            foreach (var level in levelData)
            {
                // Horizontal row for level display
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(level.LevelName))
                {
                    Debug.Log($"Level {level.LevelName} selected");
                    SysLevel.Instance.LoadLevel(level);
                }
                GUILayout.Label(level.LevelDesc);

                GUILayout.EndHorizontal();
            }
        }
    }
}