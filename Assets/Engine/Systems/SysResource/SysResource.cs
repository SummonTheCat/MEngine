using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SysResource : MonoBehaviour
{
    // ---------- References ---------- //
    public static SysResource Instance { get; private set; }

    // ---------- Data ---------- //
    public string gameDataPath;

    public ResConfigGame GameConfig;
    public ResConfigModule[] LoadedModuleMaps;

    // Internal lookup caches
    private Dictionary<string, Object> cachedResources = new Dictionary<string, Object>(); // UnityEngine.Object runtime assets
    private Dictionary<string, object> cachedConfigs = new Dictionary<string, object>();   // JSON configs

    // ---------- Lifecycle ---------- //

    public void Init()
    {
        gameDataPath = Path.Combine(Application.dataPath, "GameData");

        // Clean up old caches
        cachedResources.Clear();
        cachedConfigs.Clear();
        GameConfig = null;
        LoadedModuleMaps = new ResConfigModule[0];

        InitSingleton();
        InitConfig();
        InitModules();
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

    void InitConfig()
    {
        ConfigIO.ApplyGameConfig();
    }

    void InitModules()
    {
        ModuleIO.ApplyGameModules();
    }

    // ---------- Public API: Bulk / Single Caching ---------- //

    /// <summary>
    /// Cache an explicit list of resource IDs.
    /// </summary>
    public void CacheResources(string[] resourceIDs, System.Action onComplete)
    {
        foreach (var resourceID in resourceIDs)
        {
            if (cachedResources.ContainsKey(resourceID) || cachedConfigs.ContainsKey(resourceID))
                continue; // already cached

            // Find asset in loaded modules
            ResConfigModuleAsset asset = ResourceIO.FindAssetConfig(resourceID);
            if (asset == null)
            {
                Debug.LogWarning($"Asset with ID {resourceID} not found in modules.");
                continue;
            }

            switch (asset.Type)
            {
                case AssetType.TextureAtlas:
                    ResourceIO.CacheTextureAtlas(asset);
                    break;

                case AssetType.Sprite:
                    ResourceIO.CacheSprite(asset);
                    break;

                case AssetType.RefMap:
                    ResourceIO.CacheMap(asset);
                    break;

                case AssetType.Level:
                    ResourceIO.CacheLevel(asset);
                    break;

                case AssetType.Entity:
                    ResourceIO.CacheEntity(asset);
                    break;

                case AssetType.AnimationCollection:
                    ResourceIO.CacheAnimationCollection(asset);
                    break;

                default:
                    Debug.LogWarning($"Unhandled asset type for {asset.AssetID}");
                    break;
            }
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// Cache every resource declared in all loaded modules.
    /// </summary>
    public void CacheAllResources(System.Action onComplete = null)
    {
        if (LoadedModuleMaps == null) { onComplete?.Invoke(); return; }

        List<string> ids = new List<string>();
        foreach (var module in LoadedModuleMaps)
        {
            if (module?.ModuleAssets == null) continue;
            foreach (var asset in module.ModuleAssets)
            {
                if (asset != null && !string.IsNullOrEmpty(asset.AssetID))
                    ids.Add(asset.AssetID);
            }
        }

        CacheResources(ids.ToArray(), onComplete);
    }

    /// <summary>
    /// Cache all resources of a given type T.
    /// Example: CacheAllOfType&lt;ResAssetLevel&gt;() will cache all Level configs.
    /// </summary>
    public void CacheAllOfType<T>(System.Action onComplete = null)
    {
        if (LoadedModuleMaps == null) { onComplete?.Invoke(); return; }

        List<string> ids = new List<string>();
        foreach (var module in LoadedModuleMaps)
        {
            if (module?.ModuleAssets == null) continue;
            foreach (var asset in module.ModuleAssets)
            {
                if (asset == null) continue;

                // Map AssetType -> Config type
                if (typeof(T) == typeof(ResCfgTextureAtlas) && asset.Type == AssetType.TextureAtlas)
                    ids.Add(asset.AssetID);
                else if (typeof(T) == typeof(ResCfgSprite) && asset.Type == AssetType.Sprite)
                    ids.Add(asset.AssetID);
                else if (typeof(T) == typeof(ResCfgColorMap) && asset.Type == AssetType.RefMap)
                    ids.Add(asset.AssetID);
                else if (typeof(T) == typeof(ResCfgLevel) && asset.Type == AssetType.Level)
                    ids.Add(asset.AssetID);
                else if (typeof(T) == typeof(ResCfgEntity) && asset.Type == AssetType.Entity)
                    ids.Add(asset.AssetID);

            }
        }

        CacheResources(ids.ToArray(), onComplete);
    }

    /// <summary>
    /// Clears both Unity object cache and config cache for a specific resource.
    /// </summary>
    public void ClearResource(string resourceID)
    {
        if (string.IsNullOrEmpty(resourceID)) return;
        cachedResources.Remove(resourceID);
        cachedConfigs.Remove(resourceID);
    }

    /// <summary>
    /// Clears all Unity objects and configs from caches. Does not touch GameConfig or LoadedModuleMaps.
    /// </summary>
    public void ClearAllCaches()
    {
        cachedResources.Clear();
        cachedConfigs.Clear();
    }

    /// <summary>
    /// Returns true if a Unity object for this resource is cached.
    /// </summary>
    public bool IsResourceObjectCached(string resourceID)
    {
        return cachedResources.ContainsKey(resourceID);
    }

    /// <summary>
    /// Returns true if the JSON/config for this resource is cached.
    /// </summary>
    public bool IsResourceConfigCached(string resourceID)
    {
        return cachedConfigs.ContainsKey(resourceID);
    }

    // ---------- Accessors ---------- //

    // Runtime resources (Unity objects like Texture2D, Sprite, AssetColorMap)
    public T GetResource<T>(string resourceID) where T : Object
    {
        if (cachedResources.TryGetValue(resourceID, out Object resource))
        {
            return resource as T;
        }
        return null;
    }

    // Configs (JSON data objects like ResAssetSprite, ResAssetTextureAtlas, ResConfigAssetMap)
    public T GetResourceConfig<T>(string resourceID) where T : class
    {
        if (cachedConfigs.TryGetValue(resourceID, out object config))
        {
            return config as T;
        }
        return null;
    }

    /// <summary>
    /// Get all cached resources/configs of type T.
    /// This checks both Unity objects and configs.
    /// </summary>
    public List<T> GetAllResourcesOfType<T>()
    {
        List<T> results = new List<T>();

        // Unity Object resources
        foreach (var kvp in cachedResources)
        {
            if (kvp.Value is T tObj)
                results.Add(tObj);
        }

        // Config objects
        foreach (var kvp in cachedConfigs)
        {
            if (kvp.Value is T tCfg)
                results.Add(tCfg);
        }

        return results;
    }

    // ---------- Caching (internals) ----------
    public void AddToResourceCache(string resourceID, Object resource)
    {
        if (!cachedResources.ContainsKey(resourceID))
        {
            cachedResources[resourceID] = resource;
        }
    }

    public void AddToConfigCache<T>(string resourceID, T config) where T : class
    {
        if (!cachedConfigs.ContainsKey(resourceID))
        {
            cachedConfigs[resourceID] = config;
        }
    }
}
