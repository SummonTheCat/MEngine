using System;
using System.Collections.Generic;
using UnityEngine;

public class SysLevelEntities : MonoBehaviour
{
    // ---------- References ---------- //
    public static SysLevelEntities Instance { get; private set; }

    // ---------- Data ---------- //
    [SerializeField] private EntState[] levelEntities;

    private List<EntityRenderer> entityRenderers = new();

    // ---------- Lifecycle ---------- //

    public void Init()
    {
        InitSingleton();
    }

    public void Tick()
    {
        RenderEntities();
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
        // Cache the entities in the resource system
        List<string> entityIDs = new List<string>();
        foreach (ResCfgColorMapItem item in mapEntities.ColorMappings)
        {
            if (!entityIDs.Contains(item.ResRefID) && !string.IsNullOrEmpty(item.ResRefID))
            {
                entityIDs.Add(item.ResRefID);
            }
        }

        SysResource.Instance.CacheResources(entityIDs.ToArray(), () =>
        {
            Debug.Log("SysLevelEntities.LoadLevelEntityData: Cached all entities!");
            // Now cache the entity resources
            CacheEntResources(entityIDs.ToArray(), () =>
            {
                levelEntities = BuildEntities(mapEntities);
                Debug.Log("SysLevelEntities.LoadLevelEntityData: Cached all entity resources!");

                BuildEntityRenderers();
            });
        });
    }

    private void CacheEntResources(string[] entityIDs, Action onComplete)
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

    private EntState[] BuildEntities(AssetColorMap mapEntities)
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

    // ---------- Ent Rendering ---------- //

    private void BuildEntityRenderers()
    {
        entityRenderers.Clear();
        if (levelEntities == null) return;

        foreach (var ent in levelEntities)
        {
            entityRenderers.Add(new EntityRenderer(ent, 1f));
        }
    }

    private void RenderEntities()
    {
        foreach (var er in entityRenderers)
        {
            er.Render();
        }
    }
}

// ---------- Helper Classes ---------- //

[Serializable]
public class EntityRenderer
{
    private static Dictionary<string, Mesh> meshCache = new(); // cache by sprite+size key

    private EntState ent;
    private Mesh mesh;
    private Material material;
    private Matrix4x4 transform;
    private float zOffset = 1f; // Entities render above tiles

    private Vector2 lastPosition; // cached position to detect movement

    public EntityRenderer(EntState ent, float zOffset)
    {
        this.ent = ent;
        this.zOffset = zOffset;

        // Build or reuse mesh (cache by sprite + size key)
        string meshKey = ent.EntTargetSprite.name + "_" + ent.RefEntity.EntitySizeWidth + "x" + ent.RefEntity.EntitySizeHeight;
        if (!meshCache.TryGetValue(meshKey, out mesh))
        {
            mesh = BuildMesh(ent.EntTargetSprite, ent.RefEntity.EntitySizeWidth, ent.RefEntity.EntitySizeHeight);
            meshCache[meshKey] = mesh;
        }

        // Each entity gets its own material instance
        material = new Material(Shader.Find("Custom/InstancedSprite"));
        material.mainTexture = ent.EntTargetSprite.texture;

        // Initialize position tracking
        lastPosition = ent.EntPosition;
        UpdateTransform(force: true);
    }

    public void Render()
    {
        if (mesh == null || material == null) return;

        // Only update transform if entity moved
        if (ent.EntPosition != lastPosition)
        {
            UpdateTransform();
        }

        Graphics.DrawMesh(mesh, transform, material, 0);
    }

    private void UpdateTransform(bool force = false)
    {
        lastPosition = ent.EntPosition;
        transform = Matrix4x4.TRS(
            new Vector3(ent.EntPosition.x, ent.EntPosition.y, zOffset / 10f),
            Quaternion.identity,
            Vector3.one
        );
    }

    private Mesh BuildMesh(Sprite sprite, int width, int height)
    {
        Rect tr = sprite.textureRect;
        float texW = sprite.texture.width;
        float texH = sprite.texture.height;

        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(tr.xMin / texW, tr.yMin / texH);
        uvs[1] = new Vector2(tr.xMax / texW, tr.yMin / texH);
        uvs[2] = new Vector2(tr.xMin / texW, tr.yMax / texH);
        uvs[3] = new Vector2(tr.xMax / texW, tr.yMax / texH);

        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(width, 0, 0),
            new Vector3(0, height, 0),
            new Vector3(width, height, 0)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.uv = uvs;

        return mesh;
    }
}
