using UnityEngine;

public class SysUI : MonoBehaviour
{
    // ---------- References ---------- //

    public static SysUI Instance { get; private set; }

    public UIPanelCore[] uiPanels;
    public SysUICursor uiCursor;

    // ---------- Data ---------- //

    [SerializeField] private Vector2Int screenSize = new Vector2Int(1920, 1080);

    // ---------- Lifecycle ---------- //

    public void Init()
    {
        InitSingleton();

        for (int i = 0; i < uiPanels.Length; i++)
        {
            uiPanels[i].SetPanelIsActive(false);
            uiPanels[i].gameObject.SetActive(true);
            uiPanels[i].Init();
        }

        uiCursor.Init();
    }

    public void Tick()
    {
        for (int i = 0; i < uiPanels.Length; i++)
        {
            uiPanels[i].TickHotkeys();
            if (uiPanels[i].GetPanelIsActive())
            {
                uiPanels[i].Tick();
            }
        }

        // Update screen size
        screenSize.x = Screen.width;
        screenSize.y = Screen.height;

        uiCursor.Tick();
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

    // ---------- Public API ---------- //

    public UIPanelCore GetPanelByID(string panelID)
    {
        for (int i = 0; i < uiPanels.Length; i++)
        {
            if (uiPanels[i].GetPanelID() == panelID)
            {
                return uiPanels[i];
            }
        }

        Debug.LogWarning("SysUI: No panel found with ID " + panelID);
        return null;
    }

    public Vector2Int GetScreenSize()
    {
        return screenSize;
    }
    
    public Vector2 GetOffscreenPosition()
    {
        return new Vector2(-screenSize.x * 2, -screenSize.y * 2);
    }
    
}