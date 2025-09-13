using System.Collections.Generic;
using UnityEngine;

public class AssetColorMap : ScriptableObject
{
    // ---------- Data ---------- //
    [SerializeField] public int SourceWidth;
    [SerializeField] public int SourceHeight;

    // The int map covering the image
    // Note: Unity cannot serialize multidimensional arrays, but we keep it runtime-only.
    public int[,] ColorMap;

    // The list of color mappings (serializable)
    [SerializeField] public List<ResCfgColorMapItem> ColorMappings;

    // ---------- Factory ---------- //
    public static AssetColorMap Create(int width, int height, List<ResCfgColorMapItem> colorMappings, int[,] colorMap)
    {
        var map = CreateInstance<AssetColorMap>();
        map.SourceWidth = width;
        map.SourceHeight = height;
        map.ColorMappings = colorMappings;
        map.ColorMap = colorMap;
        return map;
    }

    // ---------- Methods ---------- //

    public int GetMapIDAt(int x, int y)
    {
        if (x < 0 || x >= SourceWidth || y < 0 || y >= SourceHeight)
        {
            Debug.LogError("GetMapIDAt: Coordinates out of bounds");
            return -1;
        }
        return ColorMap[x, y];
    }

    public string GetRefIDAt(int x, int y)
    {
        if (x < 0 || x >= SourceWidth || y < 0 || y >= SourceHeight)
        {
            Debug.LogError("GetRefIDAt: Coordinates out of bounds");
            return null;
        }

        int mapID = ColorMap[x, y];
        if (mapID == -1)
        {
            return null;
        }

        ResCfgColorMapItem mapping = ColorMappings.Find(m => m.MapID == mapID);
        return mapping != null ? mapping.ResRefID : null;
    }

    public string[] GetAllRefIDs()
    {
        List<string> refIDs = new List<string>();
        foreach (var mapping in ColorMappings)
        {
            if (!refIDs.Contains(mapping.ResRefID))
            {
                refIDs.Add(mapping.ResRefID);
            }
        }
        return refIDs.ToArray();
    }
}

