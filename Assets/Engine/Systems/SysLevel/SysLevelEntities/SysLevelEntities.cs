using System;
using System.Collections.Generic;
using UnityEngine;

public class SysLevelEntities : MonoBehaviour
{
    // ---------- References ---------- //

    public static SysLevelEntities Instance { get; private set; }

    // ---------- Data ---------- //

    public EntStateManager stateManager = new EntStateManager();
    public EntRenderManager renderManager = new EntRenderManager();
    public EntComponentManager componentManager = new EntComponentManager();

    // ---------- Lifecycle ---------- //

    public void Init()
    {
        InitSingleton();
    }

    public void Tick()
    {
        componentManager.TickComponents();
        renderManager.RenderEntities();
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

    // ---------- Level Entity Loading ---------- //

    internal void LoadLevelEntityData(AssetColorMap mapEntities)
    {
        CacheLevelEntData(mapEntities, () =>
        {
            stateManager.SetEntities(stateManager.BuildEntities(mapEntities));
            Debug.Log("SysLevelEntities.LoadLevelEntityData: Cached all entity resources!");
            renderManager.BuildEntityRenderers();
        });
    }

    private void CacheLevelEntData(AssetColorMap mapEntities, Action callback)
    {
        // Collect unique entity IDs
        List<string> entityIDs = new List<string>();
        foreach (ResCfgColorMapItem item in mapEntities.ColorMappings)
        {
            if (!string.IsNullOrEmpty(item.ResRefID) && !entityIDs.Contains(item.ResRefID))
            {
                entityIDs.Add(item.ResRefID);
            }
        }

        SysResource.Instance.CacheResources(entityIDs.ToArray(), () =>
        {
            Debug.Log("SysLevelEntities.CacheLevelEntData: Cached all entities!");
            stateManager.CacheEntResources(entityIDs.ToArray(), () =>
            {
                callback?.Invoke();
            });
        });
    }

    

}
