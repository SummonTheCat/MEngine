using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpriteTileMapItem
{
    public int TileID;
    public Sprite TileSprite;
}


[System.Serializable]
public class TileMapRenderer
{
    private const int MAX_BATCH_SIZE = 1023;

    private Dictionary<int, Matrix4x4[]> batchedTransforms = new(); // MapID → transforms
    private Dictionary<int, Sprite> spriteLookup = new();           // MapID → sprite
    private Dictionary<int, Material> materialLookup = new();       // MapID → material
    private Dictionary<int, Mesh> meshPerTileID = new();            // MapID → mesh with baked UVs

    public void BuildCache(SpriteTileMapItem[] tileMap, AssetColorMap colorMap, int zOffset)
    {
        batchedTransforms.Clear();
        spriteLookup.Clear();
        materialLookup.Clear();
        meshPerTileID.Clear();

        foreach (SpriteTileMapItem item in tileMap)
        {
            spriteLookup[item.TileID] = item.TileSprite;

            Rect tr = item.TileSprite.textureRect;
            float texW = item.TileSprite.texture.width;
            float texH = item.TileSprite.texture.height;

            Vector2[] uvs = new Vector2[4];
            uvs[0] = new Vector2(tr.xMin / texW, tr.yMin / texH);
            uvs[1] = new Vector2(tr.xMax / texW, tr.yMin / texH);
            uvs[2] = new Vector2(tr.xMin / texW, tr.yMax / texH);
            uvs[3] = new Vector2(tr.xMax / texW, tr.yMax / texH);

            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(1, 1, 0)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.uv = uvs;

            meshPerTileID[item.TileID] = mesh;

            if (!materialLookup.ContainsKey(item.TileID))
            {
                Material mat = new Material(Shader.Find("Custom/InstancedSprite"));
                mat.mainTexture = item.TileSprite.texture;
                mat.enableInstancing = true;
                materialLookup[item.TileID] = mat;
            }
        }

        Dictionary<int, List<Matrix4x4>> transformLists = new();

        for (int y = 0; y < colorMap.SourceHeight; y++)
        {
            for (int x = 0; x < colorMap.SourceWidth; x++)
            {
                int id = colorMap.ColorMap[x, y];
                if (id == -1 || !spriteLookup.ContainsKey(id)) continue;

                if (!transformLists.ContainsKey(id))
                    transformLists[id] = new List<Matrix4x4>();

                transformLists[id].Add(Matrix4x4.TRS(new Vector3(x, y, zOffset / 10f), Quaternion.identity, Vector3.one));
            }
        }

        foreach (var kvp in transformLists)
            batchedTransforms[kvp.Key] = kvp.Value.ToArray();
    }

    public void Render()
    {
        foreach (var kvp in batchedTransforms)
        {
            int mapID = kvp.Key;
            Matrix4x4[] matrices = kvp.Value;

            if (!spriteLookup.TryGetValue(mapID, out Sprite sprite) || sprite == null)
                continue;

            if (!meshPerTileID.TryGetValue(mapID, out Mesh mesh) || mesh == null)
                continue;

            if (!materialLookup.TryGetValue(mapID, out Material mat) || mat == null)
                continue;

            for (int i = 0; i < matrices.Length; i += MAX_BATCH_SIZE)
            {
                int count = Mathf.Min(MAX_BATCH_SIZE, matrices.Length - i);
                Matrix4x4[] batch = new Matrix4x4[count];
                System.Array.Copy(matrices, i, batch, 0, count);

                Graphics.DrawMeshInstanced(mesh, 0, mat, batch);
            }
        }
    }
}

