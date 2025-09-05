
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
    TextureAtlas = 0,       // Texture Atlas
    Sprite = 1,             // Sprites within the texture atlas
    RefMap = 2,             // Color indexed map for procedural generation
    Level = 3,              // Level data
    Entity = 4              // Entity data
}