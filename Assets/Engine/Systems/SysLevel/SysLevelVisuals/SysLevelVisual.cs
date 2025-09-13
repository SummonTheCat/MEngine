using System.Collections.Generic;
using UnityEngine;

public class SysLevelVisual : MonoBehaviour
{
    // --------- References ---------- //

    public static SysLevelVisual Instance { get; private set; }

    // ---------- Data ---------- //

    [SerializeField] private AssetColorMap mapBack;
    [SerializeField] private AssetColorMap mapFront;

    [SerializeField] private TileMapRenderer rendererBack = new();
    [SerializeField] private TileMapRenderer rendererFront = new();

    [SerializeField] private SpriteTileMapItem[] tileMappingsBack;
    [SerializeField] private SpriteTileMapItem[] tileMappingsFront;

    // --------- Lifecycle ---------- //

    public void Init()
    {
        InitSingleton();
    }

    public void Tick()
    {
        if (mapBack != null && mapFront != null)
        {
            rendererBack.Render();
            rendererFront.Render();
        }
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

    internal void LoadLevelVisualData(AssetColorMap mapBack, AssetColorMap mapFront)
    {
        this.mapBack = mapBack;
        this.mapFront = mapFront;

        List<string> spritesToLoad = new();
        spritesToLoad.AddRange(mapBack.GetAllRefIDs());
        spritesToLoad.AddRange(mapFront.GetAllRefIDs());

        SysResource.Instance.CacheResources(spritesToLoad.ToArray(), () =>
        {
            tileMappingsBack = GetTileMappings(mapBack);
            tileMappingsFront = GetTileMappings(mapFront);

            rendererBack.BuildCache(tileMappingsBack, mapBack, 2); // behind
            rendererFront.BuildCache(tileMappingsFront, mapFront, 0); // front
        });
    }

    private SpriteTileMapItem[] GetTileMappings(AssetColorMap mappings)
    {
        List<SpriteTileMapItem> tileMappings = new();

        foreach (var item in mappings.ColorMappings)
        {
            Sprite sprite = SysResource.Instance.GetResource<Sprite>(item.ResRefID);
            if (sprite == null)
            {
                Debug.LogWarning($"Sprite with RefID {item.ResRefID} not found.");
                continue;
            }

            tileMappings.Add(new SpriteTileMapItem
            {
                TileID = item.MapID,
                TileSprite = sprite
            });
        }

        return tileMappings.ToArray();
    }

}

