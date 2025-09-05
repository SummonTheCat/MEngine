using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ResourceIO
{
    // ---------- Internal Helpers ---------- //

    public static ResConfigModuleAsset FindAssetConfig(string resourceID)
    {
        SysResource sysRes = SysResource.Instance;


        foreach (var module in sysRes.LoadedModuleMaps)
        {
            foreach (var asset in module.ModuleAssets)
            {
                if (asset.AssetID == resourceID)
                    return asset;
            }
        }
        return null;
    }

    public static void CacheTextureAtlas(ResConfigModuleAsset asset)
    {
        SysResource sysRes = SysResource.Instance;

        var atlasConfig = LoadAsset<ResCfgTextureAtlas>(asset.AssetPath);
        if (atlasConfig == null)
        {
            Debug.LogError($"Failed to load atlas config: {asset.AssetPath}");
            return;
        }

        string pngPath = UtilPaths.ToSystemPath(atlasConfig.TextureAtlasSource);
        if (!File.Exists(pngPath))
        {
            Debug.LogError($"Texture atlas source not found: {pngPath}");
            return;
        }

        byte[] data = File.ReadAllBytes(pngPath);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(data);
        texture.name = asset.AssetID;
        texture.filterMode = FilterMode.Point; // Pixel art style

        // Save to caches
        sysRes.AddToConfigCache(asset.AssetID, atlasConfig);
        sysRes.AddToResourceCache(asset.AssetID, texture);

        Debug.Log($"Cached TextureAtlas: {asset.AssetID}");
    }

    public static void CacheSprite(ResConfigModuleAsset asset)
    {
        SysResource sysRes = SysResource.Instance;

        var spriteConfig = LoadAsset<ResCfgSprite>(asset.AssetPath);
        if (spriteConfig == null)
        {
            Debug.LogError($"Failed to load sprite config: {asset.AssetPath}");
            return;
        }

        // Ensure atlas is cached
        Texture2D atlasTex = sysRes.GetResource<Texture2D>(spriteConfig.TextureAtlasID);
        if (atlasTex == null)
        {
            Debug.LogWarning($"Texture atlas {spriteConfig.TextureAtlasID} not cached yet. Caching it now...");
            sysRes.CacheResources(new string[] { spriteConfig.TextureAtlasID }, null);
            atlasTex = sysRes.GetResource<Texture2D>(spriteConfig.TextureAtlasID);
        }

        if (atlasTex == null)
        {
            Debug.LogError($"Failed to load atlas {spriteConfig.TextureAtlasID} for sprite {asset.AssetID}");
            return;
        }

        Rect rect = new Rect(spriteConfig.PosX, spriteConfig.PosY, spriteConfig.Width, spriteConfig.Height);
        Vector2 pivot = new Vector2(spriteConfig.PivotX, spriteConfig.PivotY);
        Sprite sprite = Sprite.Create(atlasTex, rect, pivot);
        sprite.name = asset.AssetID;

        // Save to caches
        sysRes.AddToResourceCache(asset.AssetID, sprite);
        sysRes.AddToConfigCache(asset.AssetID, spriteConfig);

        Debug.Log($"Cached Sprite: {asset.AssetID}");
    }

    public static void CacheMap(ResConfigModuleAsset asset)
    {
        SysResource sysRes = SysResource.Instance;

        // Load JSON config
        var mapConfig = LoadAsset<ResCfgColorMap>(asset.AssetPath);
        if (mapConfig == null)
        {
            Debug.LogError($"Failed to load map config: {asset.AssetPath}");
            return;
        }

        if (mapConfig.ColorMappings == null || mapConfig.ColorMappings.Length == 0)
        {
            Debug.LogError($"Map config has no ColorMappings: {asset.AssetPath}");
            return;
        }

        string pngPath = UtilPaths.ToSystemPath(mapConfig.MapSource);
        if (!File.Exists(pngPath))
        {
            Debug.LogError($"Map source PNG not found: {pngPath}");
            return;
        }

        // Load PNG
        byte[] data = File.ReadAllBytes(pngPath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool ok = texture.LoadImage(data, false);
        if (!ok)
        {
            Debug.LogError($"Failed to decode PNG: {pngPath}");
            return;
        }
        texture.name = asset.AssetID + "_MapTex";

        int width = texture.width;
        int height = texture.height;

        // Build lookup dictionary for faster color matching
        Dictionary<Color32, int> colorToID = new Dictionary<Color32, int>(new Color32Comparer());
        foreach (var mapping in mapConfig.ColorMappings)
        {
            // Ensure unique colors map deterministically; last write wins is acceptable but we avoid dup keys.
            if (!colorToID.ContainsKey(mapping.Color))
                colorToID[mapping.Color] = mapping.MapID;
        }

        // Fill int map from texture pixels
        int[,] colorMap = new int[width, height];
        Color32[] pixels = texture.GetPixels32();

        // y-major row read; texture data is left-to-right, bottom-to-top in GetPixels32 indexing
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                Color32 c = pixels[row + x];
                if (colorToID.TryGetValue(c, out int id))
                {
                    colorMap[x, y] = id;
                }
                else
                {
                    colorMap[x, y] = -1; // unmapped color
                }
            }
        }

        // Build resource ScriptableObject
        var mappingsList = new List<ResCfgColorMapItem>(mapConfig.ColorMappings);
        AssetColorMap mapObj = AssetColorMap.Create(width, height, mappingsList, colorMap);
        mapObj.name = asset.AssetID; // SAFE now (ScriptableObject)

        // Save to caches (config first is fine; order no longer causes NRE)
        sysRes.AddToConfigCache(asset.AssetID, mapConfig);
        sysRes.AddToResourceCache(asset.AssetID, mapObj);

        Debug.Log($"Cached AssetColorMap: {asset.AssetID} ({width}x{height})");
    }

    public static void CacheLevel(ResConfigModuleAsset asset)
    {
        SysResource sysRes = SysResource.Instance;

        // Load JSON config
        var levelConfig = LoadAsset<ResCfgLevel>(asset.AssetPath);
        if (levelConfig == null)
        {
            Debug.LogError($"Failed to load level config: {asset.AssetPath}");
            return;
        }

        // Save ONLY to config cache (no Unity Object resource)
        sysRes.AddToConfigCache(asset.AssetID, levelConfig);

        Debug.Log($"Cached Level Config: {asset.AssetID} (LevelName={levelConfig.LevelName})");


    }

    public static void CacheEntity(ResConfigModuleAsset asset)
{
    SysResource sysRes = SysResource.Instance;

    var entityConfig = LoadAsset<ResCfgEntity>(asset.AssetPath);
    if (entityConfig == null)
    {
        Debug.LogError($"Failed to load entity config: {asset.AssetPath}");
        return;
    }

    // Save ONLY to config cache (no Unity Object resource)
    sysRes.AddToConfigCache(asset.AssetID, entityConfig);

    Debug.Log($"Cached Entity Config: {asset.AssetID} (EntityName={entityConfig.EntityName})");
}


    // ---------- Asset Utilities ---------- //

    public static T LoadAsset<T>(string assetJsonRelPath) where T : class
    {
        if (string.IsNullOrEmpty(assetJsonRelPath))
        {
            Debug.LogError("LoadAsset: Provided path is null or empty.");
            return null;
        }

        string sysPath = UtilPaths.ResolveAssetSystemPath(assetJsonRelPath);

        if (!File.Exists(sysPath))
        {
            Debug.LogError($"LoadAsset: File not found at {sysPath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(sysPath);
            T obj = JsonUtility.FromJson<T>(json);
            if (obj == null)
            {
                Debug.LogError($"LoadAsset: Failed to parse JSON at {sysPath}");
            }
            return obj;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"LoadAsset: Exception while loading {sysPath}\n{ex}");
            return null;
        }
    }

    public class Color32Comparer : IEqualityComparer<Color32>
    {
        public bool Equals(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        }

        public int GetHashCode(Color32 c)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + c.r.GetHashCode();
                hash = hash * 23 + c.g.GetHashCode();
                hash = hash * 23 + c.b.GetHashCode();
                hash = hash * 23 + c.a.GetHashCode();
                return hash;
            }
        }
    }

}