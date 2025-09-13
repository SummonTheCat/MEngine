using System;
using System.IO;
using UnityEngine;

public static class ModKitResIO
{
    public static MKModuleData LoadModuleFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("Module file not found: " + filePath);
            return null;
        }

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            ResConfigModule moduleConfig = JsonUtility.FromJson<ResConfigModule>(jsonContent);

            MKModuleData moduleData = new MKModuleData
            {
                LoadedModule = moduleConfig
            };

            Debug.Log("Module loaded successfully from: " + filePath);
            return moduleData;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error loading module from file: " + ex.Message);
            return null;
        }
    }

    public static void SaveModuleToFile(string filePath, ResConfigModule module)
    {
        if (string.IsNullOrEmpty(filePath) || module == null)
        {
            Debug.LogError("Invalid parameters for saving module.");
            return;
        }

        try
        {
            string jsonContent = JsonUtility.ToJson(module, true);
            File.WriteAllText(filePath, jsonContent);
            Debug.Log("Module saved to: " + filePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save module: " + ex.Message);
        }
    }

    public static void SaveAssetToFile(string directoryPath, string assetID, AssetType type)
    {
        if (string.IsNullOrEmpty(directoryPath) || string.IsNullOrEmpty(assetID))
        {
            Debug.LogError("Invalid parameters for saving asset.");
            return;
        }

        string outputPath = Path.Combine(directoryPath, assetID + ".json");
        object assetObject = null;

        switch (type)
        {
            case AssetType.TextureAtlas:
                assetObject = new ResCfgTextureAtlas
                {
                    TextureAtlasSource = ""
                };
                break;

            case AssetType.Sprite:
                assetObject = new ResCfgSprite
                {
                    TextureAtlasID = "",
                    PosX = 0,
                    PosY = 0,
                    Width = 64,
                    Height = 64,
                    PivotX = 0.5f,
                    PivotY = 0.5f,
                    PixelsPerUnit = 100f
                };
                break;

            case AssetType.RefMap:
                assetObject = new ResCfgColorMap
                {
                    MapSource = "",
                    ColorMappings = new ResCfgColorMapItem[0]
                };
                break;

            case AssetType.Level:
                assetObject = new ResCfgLevel
                {
                    LevelName = "New Level",
                    LevelDesc = "A newly created level.",
                    ShowInLevelSelect = true,
                    LevelMapCol = "",
                    LevelMapBack = "",
                    LevelMapFront = "",
                    LevelMapEntities = ""
                };
                break;

            case AssetType.Entity:
                assetObject = new ResCfgEntity
                {
                    EntityName = "NewEntity",
                    EntitySprite = "",
                    EntityHealth = 100f,
                    EntitySizeWidth = 1,
                    EntitySizeHeight = 1,
                    EntityLayer = 0,
                    EntityTags = new string[0],

                    UsesCompPlayerController = false,
                    UsesCompAI = false,
                    UsesCompPhysics = false,
                    UsesCompCollision = false,
                    UsesCompAnimation = false,
                    UsesCompDamageEmitter = false,
                    UsesCompDamageReceiver = false,
                    UsesCompDeathReward = false,
                    UsesCompCollectible = false,
                    UsesCompSpawner = false,
                    UsesCompDoor = false,

                    CompPlayerController = new ResCfgEntCompPlayerController(),
                    CompAI = new ResCfgEntCompAI(),
                    CompPhysics = new ResCfgEntCompPhysics(),
                    CompCollision = new ResCfgEntCompCollision(),
                    CompAnimation = new ResCfgEntCompAnimation(),
                    CompDamageEmitter = new ResCfgEntCompDamageEmitter(),
                    CompDamageReceiver = new ResCfgEntCompDamageReceiver(),
                    CompDeathReward = new ResCfgEntCompDeathReward(),
                    CompCollectible = new ResCfgEntCompCollectible(),
                    CompSpawner = new ResCfgEntCompSpawner(),
                    CompDoor = new ResCfgEntCompDoor()
                };
                break;

            case AssetType.AnimationCollection:
                assetObject = new ResCfgAnimationCollection
                {
                    AnimationID = "NewAnimationCollection",
                    AnimationScenes = new ResCfgAnimationScene[]
                    {
                    new ResCfgAnimationScene
                    {
                        SceneID = "Idle",
                        FrameSpriteIDs = new string[] { }
                    }
                    },
                    FrameRate = 12
                };
                break;

            default:
                Debug.LogError("Unsupported asset type: " + type);
                return;
        }

        try
        {
            string json = JsonUtility.ToJson(assetObject, true);
            File.WriteAllText(outputPath, json);
            Debug.Log($"Asset file written: {outputPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError("Error writing asset file: " + ex.Message);
        }
    }

    public static void DeleteAssetFile(string directoryPath, string assetID)
    {
        if (string.IsNullOrEmpty(directoryPath) || string.IsNullOrEmpty(assetID))
        {
            Debug.LogError("Invalid parameters for deleting asset.");
            return;
        }

        string fullPath = Path.Combine(directoryPath, assetID + ".json");
        string fullPathMeta = fullPath + ".meta";

        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
                Debug.Log("Deleted asset file: " + fullPath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to delete asset file: " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("Asset file not found for deletion: " + fullPath);
        }

        if (File.Exists(fullPathMeta))
        {
            try
            {
                File.Delete(fullPathMeta);
                Debug.Log("Deleted asset meta file: " + fullPathMeta);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to delete asset meta file: " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("Asset meta file not found for deletion: " + fullPathMeta);
        }
    }



}