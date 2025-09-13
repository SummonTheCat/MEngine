// ---------- Structures ---------- //

// Core asset configuration, holds the mappings for a specific asset
using UnityEngine;

// - Texture Atlas -

[System.Serializable]
public class ResCfgTextureAtlas
{
    public string TextureAtlasSource;
}

// - Sprite -

[System.Serializable]
public class ResCfgSprite
{
    public string TextureAtlasID;
    public int PosX;
    public int PosY;
    public int Width;
    public int Height;
    public float PivotX;
    public float PivotY;
    public float PixelsPerUnit = 100f;
}

// - Map -

[System.Serializable]
public class ResCfgColorMap
{
    public string MapSource;                        // The source file for the png
    public ResCfgColorMapItem[] ColorMappings;      // What color maps to what ID
}

[System.Serializable]
public class ResCfgColorMapItem
{
    public int MapID;                               // The ID we store this color as.
    public string ResRefID;                         // The reference to the resource that this color refers to (Sprite, Entity, Etc).
    public Color32 Color;                           // The color value in the source image.
}

// - Level -

[System.Serializable]
public class ResCfgLevel
{
    public string LevelName;
    public string LevelDesc;
    public bool ShowInLevelSelect;
    public string LevelMapCol;
    public string LevelMapBack;
    public string LevelMapFront;
    public string LevelMapEntities;
}

// - AnimationCollection -

[System.Serializable]
public class ResCfgAnimationCollection
{
    public string AnimationID;
    public ResCfgAnimationScene[] AnimationScenes;
    public int FrameRate;
}

[System.Serializable]
public class ResCfgAnimationScene
{
    public string SceneID;
    public string[] FrameSpriteIDs;
}

// - Entity -

[System.Serializable]
public class ResCfgEntity
{
    // General Entity Details
    public string EntityName;
    public string EntitySprite;
    public float EntityHealth;
    public int EntitySizeWidth;
    public int EntitySizeHeight;
    public int EntityLayer;
    public string[] EntityTags;

    // Component Usage
    public bool UsesCompPlayerController;
    public bool UsesCompAI;
    public bool UsesCompPhysics;
    public bool UsesCompCollision;
    public bool UsesCompAnimation;
    public bool UsesCompDamageEmitter;
    public bool UsesCompDamageReceiver;
    public bool UsesCompDeathReward;
    public bool UsesCompCollectible;
    public bool UsesCompSpawner;
    public bool UsesCompDoor;

    // Component Data
    public ResCfgEntCompPlayerController CompPlayerController;
    public ResCfgEntCompAI CompAI;
    public ResCfgEntCompPhysics CompPhysics;
    public ResCfgEntCompCollision CompCollision;
    public ResCfgEntCompAnimation CompAnimation;
    public ResCfgEntCompDamageEmitter CompDamageEmitter;
    public ResCfgEntCompDamageReceiver CompDamageReceiver;
    public ResCfgEntCompDeathReward CompDeathReward;
    public ResCfgEntCompCollectible CompCollectible;
    public ResCfgEntCompSpawner CompSpawner;
    public ResCfgEntCompDoor CompDoor;
}

[System.Serializable]
public class ResCfgEntCompPlayerController
{
    public float MoveSpeed;             // Min Movement speed       E.g. 3
    public float RunSpeed;              // Running speed            E.g. 6
    public float JumpHeight;            // Jump height              E.g. 2
    public float JumpHeightMax;         // Maximum jump height      E.g. 4
    public float Acceleration;          // Acceleration rate        E.g. 2
    public float Deceleration;          // Deceleration rate        E.g. 4
    public float AirDeceleration;       // Air deceleration rate    E.g. 2

    public float JumpHoldTime = 0.15f;
    public float HeldGravityScale = 0.35f;
    public float FallGravityScale = 1.6f;

}

[System.Serializable]
public class ResCfgEntCompAI
{
    public int Type;
    public float MoveSpeed;
}

[System.Serializable]
public class ResCfgEntCompPhysics
{
    public float Mass;
    public float Drag;
    public float Bounciness;
}

[System.Serializable]
public class ResCfgEntCompCollision
{
    public float Width;
    public float Height;
}

[System.Serializable]
public class ResCfgEntCompAnimation
{
    public string AnimationID;
    public float AnimationSpeed;
    public string DefaultAnimation;
    public bool Loop;
}

[System.Serializable]
public class ResCfgEntCompDamageEmitter
{
    public float Radius;
    public float Damage;
    public string[] TargetTags;
    public bool damageDirUp;
    public bool damageDirDown;
    public bool damageDirLeft;
    public bool damageDirRight;
}

[System.Serializable]
public class ResCfgEntCompDamageReceiver
{
    public string[] SourceTags;
    public float InvulnerabilityTime;
}

[System.Serializable]
public class ResCfgEntCompDeathReward
{
    public int Value;
}

[System.Serializable]
public class ResCfgEntCompCollectible
{
    public float Radius;
    public int Value;
}

[System.Serializable]
public class ResCfgEntCompSpawner
{
    public float SpawnRate;
    public string SpawnedEntity;
}

[System.Serializable]
public class ResCfgEntCompDoor
{
    public string TargetLevel;
    public int RewardValue;
}
