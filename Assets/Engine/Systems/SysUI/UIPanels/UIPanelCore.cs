using UnityEngine;

public class UIPanelCore : MonoBehaviour
{
    // ---------- References ---------- //

    public RectTransform panelTransform;

    // ---------- Data ---------- //

    [SerializeField] private bool panelIsActive = true;
    [SerializeField] private string panelID = "DefaultPanel";
    [SerializeField] private bool panelActiveOnStart = true;


    // ---------- Lifecycle ---------- //

    public virtual void Init()
    {
        // Initialize panel
        if (panelTransform == null)
        {
            panelTransform = GetComponent<RectTransform>();
        }

        panelIsActive = panelActiveOnStart;
        SetPanelIsActive(panelIsActive);
    }

    public virtual void Tick()
    {
        // Update UI elements
    }

    public virtual void TickHotkeys()
    {
        
    }



    // ---------- Public API ---------- //

    public string GetPanelID()
    {
        return panelID;
    }

    public bool GetPanelIsActive()
    {
        return panelIsActive;
    }

    public void SetPanelIsActive(bool isActive)
    {
        panelIsActive = isActive;

        if (panelIsActive)
        {
            // Move panel on screen
            panelTransform.anchoredPosition = Vector2.zero;
        }
        else
        {
            // Move panel off screen
            panelTransform.anchoredPosition = SysUI.Instance.GetOffscreenPosition();
        }

    }
}