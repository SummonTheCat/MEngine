using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntStateManager
{
    private EntState[] levelEntities;

    public void SetEntities(EntState[] entities)
    {
        levelEntities = entities;
    }

    public EntState[] GetEntities()
    {
        return levelEntities;
    }

    public EntState GetEntityByID(int entID)
    {
        if (levelEntities == null) return null;

        foreach (EntState ent in levelEntities)
        {
            if (ent != null && ent.EntID == entID)
            {
                return ent;
            }
        }

        return null;
    }

    public EntState GetEntityWithName(string entityName)
    {
        if (levelEntities == null) return null;

        foreach (EntState ent in levelEntities)
        {
            if (ent != null && ent.RefEntity != null && ent.RefEntity.EntityName == entityName)
            {
                return ent;
            }
        }

        return null;
    }


    internal void CacheEntResources(string[] entityIDs, Action onComplete)
    {
        // Get the sprite resources for each entity
        List<string> spriteIDs = new List<string>();
        foreach (string entID in entityIDs)
        {
            ResCfgEntity cfgEntity = SysResource.Instance.GetResourceConfig<ResCfgEntity>(entID);
            if (cfgEntity != null && !string.IsNullOrEmpty(cfgEntity.EntitySprite) && !spriteIDs.Contains(cfgEntity.EntitySprite))
            {
                spriteIDs.Add(cfgEntity.EntitySprite);
            }
        }

        SysResource.Instance.CacheResources(spriteIDs.ToArray(), () =>
        {
            Debug.Log("SysLevelEntities.CacheEntResources: Cached all entity sprite resources!");
            onComplete?.Invoke();
        });
    }

    internal EntState[] BuildEntities(AssetColorMap mapEntities)
    {
        int sourceWidth = mapEntities.SourceWidth;
        int sourceHeight = mapEntities.SourceHeight;

        List<EntState> entities = new List<EntState>();

        for (int x = 0; x < sourceWidth; x++)
        {
            for (int y = 0; y < sourceHeight; y++)
            {
                string refID = mapEntities.GetRefIDAt(x, y);
                if (!string.IsNullOrEmpty(refID))
                {
                    ResCfgEntity cfgEntity = SysResource.Instance.GetResourceConfig<ResCfgEntity>(refID);
                    if (cfgEntity != null)
                    {
                        EntState newEnt = EntState.CreateFromConfig(cfgEntity, new Vector2(x, y));
                        if (newEnt != null)
                        {
                            entities.Add(newEnt);
                        }
                    }
                    else
                    {
                        Debug.LogError($"SysLevelEntities.BuildEntities: Could not find entity config for RefID '{refID}' at ({x},{y})");
                    }
                }
            }
        }

        return entities.ToArray();
    }
}