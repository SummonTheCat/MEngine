
using System;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class EntState
{
    public ResCfgEntity RefEntity;

    public bool HasControl = true;

    public int EntID;
    public Vector2 EntPosition;
    public Vector2 Velocity;
    public bool IsGrounded;

    public float EntHealth;

    public bool IsCameraTarget;

    public float BounceValue;
    public Sprite EntTargetSprite;
    public bool EntSpriteFlipped;

    // Animation State
    public string CurrentAnimationScene;
    public int CurrentFrameIndex;
    public float AnimationTimer;

    public bool IsJumping;
    public float JumpHoldTimer;

    // Damage Receiver State
    public float InvulnerabilityTimer;

    // AI: last horizontal direction (for flipping on collisions with other AIs)
    public float LastAIDirection = 1f;

    // Damage tracking for bounce feedback
    public bool WasDamagedRecently = false;
    public float LastDamagedTime = -999f;
    public Vector2 LastDamageSourceDir; // normalized direction from source -> this ent

    public static EntState CreateFromConfig(ResCfgEntity cfgEntity, Vector2 position)
    {
        if (cfgEntity == null)
        {
            Debug.LogError("EntState.CreateFromConfig: cfgEntity is null!");
            return null;
        }

        EntState newEnt = new EntState
        {
            RefEntity = cfgEntity,
            EntID = UnityEngine.Random.Range(100000, 999999),
            EntPosition = position,
            Velocity = Vector2.zero,
            BounceValue = 0f,
            EntSpriteFlipped = false,
            EntHealth = cfgEntity.EntityHealth
        };

        if (cfgEntity.UsesCompAnimation)
        {
            string[] animIDs = { cfgEntity.CompAnimation.AnimationID };

            // Cache the animation config 
            SysResource.Instance.CacheResources(animIDs, () =>
            {
                var anim = SysResource.Instance.GetResourceConfig<ResCfgAnimationCollection>(animIDs[0]);
                var defaultScene = cfgEntity.CompAnimation.DefaultAnimation;

                newEnt.CurrentAnimationScene = defaultScene;
                newEnt.CurrentFrameIndex = 0;
                newEnt.AnimationTimer = 0f;

                if (anim != null)
                {

                    // Get all the sprites from all scenes to cache
                    var allFrameIDs = new System.Collections.Generic.List<string>();
                    foreach (var animationScene in anim.AnimationScenes)
                    {
                        allFrameIDs.AddRange(animationScene.FrameSpriteIDs);
                    }

                    SysResource.Instance.CacheResources(allFrameIDs.ToArray(), () =>
                    {
                        string firstFrameID = allFrameIDs[0];
                        Sprite frameSprite = SysResource.Instance.GetResource<Sprite>(firstFrameID);
                        if (frameSprite != null)
                        {
                            newEnt.EntTargetSprite = frameSprite;
                        }
                        else
                        {
                            Debug.LogWarning($"EntState.CreateFromConfig: Could not find sprite '{firstFrameID}' for entity '{cfgEntity.EntityName}' animation.");
                        }
                    });


                }
            });


        }
        else
        {
            newEnt.EntTargetSprite = SysResource.Instance.GetResource<Sprite>(cfgEntity.EntitySprite);
        }

        if (newEnt.RefEntity.UsesCompPlayerController)
        {
            newEnt.IsCameraTarget = true;
        }

        return newEnt;
    }

    public bool IsEntOverlapping(EntState otherEnt)
    {
        if (otherEnt == null || RefEntity == null || otherEnt.RefEntity == null) return false;

        Rect thisRect = new Rect(EntPosition.x, EntPosition.y, RefEntity.EntitySizeWidth, RefEntity.EntitySizeHeight);
        Rect otherRect = new Rect(otherEnt.EntPosition.x, otherEnt.EntPosition.y, otherEnt.RefEntity.EntitySizeWidth, otherEnt.RefEntity.EntitySizeHeight);

        return thisRect.Overlaps(otherRect);
    }

}
