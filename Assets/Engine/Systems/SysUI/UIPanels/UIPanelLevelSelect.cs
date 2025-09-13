using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

class UIPanelLevelSelect : UIPanelCore
{
    public Transform levelScrollContent;
    public ElemButtonConfig buttonConfig;

    [SerializeField] private float listPadding = 10f;

    [SerializeField] private ResCfgLevel[] availableLevels;
    [SerializeField] private List<UIElemButton> levelButtons = new List<UIElemButton>();

    public override void Init()
    {
        base.Init();
        RefreshLevelButtons();
    }

    public override void Tick()
    {
        base.Tick();
        RefreshLevelButtons();
    }

    public override void TickHotkeys()
    {
        // 'Escape' key closes the panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SetPanelIsActive(false);
        }
    }

    public void RefreshLevelButtons()
    {
        var levels = SysResource.Instance.GetAllResourcesOfType<ResCfgLevel>();
        if (levels.Count == availableLevels?.Length) return;

        ClearLevelButtons();

        availableLevels = levels.ToArray();

        float spacingY = buttonConfig.size.y * 1.1f;
        float startY = -listPadding;

        int index = 0;
        foreach (var level in levels)
        {
            if (!level.ShowInLevelSelect) continue;

            UIElemButton button = UIElemButton.CreateButton(level.LevelName, buttonConfig);
            button.transform.SetParent(levelScrollContent, false);
            button.AlignTopCenter();

            float posY = startY - index * spacingY;
            button.SetPosition(new Vector2(0, posY));

            button.AddClickListener(() =>
            {
                Debug.Log($"Level {level.LevelName} selected");
                SysLevel.Instance.LoadLevel(level);
                // Hide level select panel after selection
                SetPanelIsActive(false);
            });

            levelButtons.Add(button);
            index++;
        }

        // Optional: Adjust scroll container height if vertical layout requires it
        float totalHeight = levels.Count * spacingY;
        if (levelScrollContent is RectTransform rt)
        {
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, totalHeight);
        }
    }

    public void ClearLevelButtons()
    {
        foreach (var btn in levelButtons)
        {
            Destroy(btn.gameObject);
        }
        levelButtons.Clear();
    }
    
}
