using System.Collections.Generic;
using UnityEngine;

public class SysLevel : MonoBehaviour
{
    // ---------- References ---------- //
    public static SysLevel Instance { get; private set; }

    public SysLevelVisual LevelVisual;
    public SysLevelEntities LevelEntities;
    public SysLevelPhysics LevelPhysics;

    // ---------- Data ---------- //

    ResCfgLevel CurrentLevelData;

    // ---------- Lifecycle ---------- //

    public void Init()
    {
        InitSingleton();

        // Init SubSystems
        if (LevelVisual == null)
        {
            Debug.LogError("SysLevelVisual reference is missing in SysLevel.");
            return;
        }

        if (LevelEntities == null)
        {
            Debug.LogError("SysLevelEntities reference is missing in SysLevel.");
            return;
        }

        if (LevelPhysics == null)
        {
            Debug.LogError("SysLevelPhysics reference is missing in SysLevel.");
            return;
        }

        LevelVisual.Init();
        LevelEntities.Init();
        LevelPhysics.Init();
    }

    public void Tick()
    {
        LevelVisual.Tick();
        LevelEntities.Tick();
        LevelPhysics.Tick();
    }

    private void InitSingleton()
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

    public void LoadLevel(ResCfgLevel level)
    {
        Debug.Log($"Loading Level: {level.LevelName}");
        CurrentLevelData = level;

        string[] colorMapsToLoad = new string[]
        {
            level.LevelMapBack,
            level.LevelMapFront,
            level.LevelMapCol,
            level.LevelMapEntities
        };

        SysResource.Instance.CacheResources(colorMapsToLoad, () =>
        {
            Debug.Log($"All color maps for level {level.LevelName} cached successfully.");

            AssetColorMap mapBack = SysResource.Instance.GetResource<AssetColorMap>(level.LevelMapBack);
            AssetColorMap mapFront = SysResource.Instance.GetResource<AssetColorMap>(level.LevelMapFront);
            AssetColorMap mapCol = SysResource.Instance.GetResource<AssetColorMap>(level.LevelMapCol);
            AssetColorMap mapEntities = SysResource.Instance.GetResource<AssetColorMap>(level.LevelMapEntities);

            LevelVisual.LoadLevelVisualData(mapBack, mapFront);
            LevelEntities.LoadLevelEntityData(mapEntities);

        });
    }

    // ---------- Utility ---------- //

    
}

