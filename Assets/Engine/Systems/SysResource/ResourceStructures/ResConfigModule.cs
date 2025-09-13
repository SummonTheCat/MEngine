
// ---------- Structures ---------- //

// Core module configuration, holds the asset mappings for a specific module
[System.Serializable]
public class ResConfigModule
{
    public string ModuleName;
    public string Version;

    public ResConfigModuleAsset[] ModuleAssets;
}

// Individual asset configuration within a module, points to the resource map file
[System.Serializable]
public class ResConfigModuleAsset
{
    public string AssetID;
    public AssetType Type;

    public string AssetPath;
}

// ---------- Enum Definitions ---------- //

// Enum defining different types of assets
public enum AssetType
{
    TextureAtlas = 0,           // Texture Atlas                                    Color: Yellow
    Sprite = 1,                 // Sprites within the texture atlas                 Color: Green    
    RefMap = 2,                 // Color indexed map for procedural generation      Color: Cyan
    Level = 3,                  // Level data                                       Color: Magenta  
    Entity = 4,                 // Entity data                                      Color: Blue
    AnimationCollection = 5     // Animation collections                            Color: Orange
}