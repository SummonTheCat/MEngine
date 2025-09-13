using System.Collections.Generic;
using UnityEngine;

public class SysLevelPhysics : MonoBehaviour
{
    // ---------- References ---------- //
    public static SysLevelPhysics Instance { get; private set; }

    // ---------- Data ---------- //
    private List<RectInt> collisionRects = new List<RectInt>();
    private int mapWidth;
    private int mapHeight;

    // ---------- Lifecycle ---------- //
    public void Init()
    {
        InitSingleton();
    }

    public void Tick()
    {
        // Debug draw collision rects
        foreach (var rect in collisionRects)
        {
            DrawRect(rect, Color.green);
        }

        // Debug draw entities
        var ents = SysLevelEntities.Instance.stateManager.GetEntities();
        if (ents != null)
        {
            foreach (var ent in ents)
            {
                if (ent.RefEntity != null)
                {
                    DrawPoint(ent.EntPosition, Color.yellow);
                    DrawEntityRect(ent, Color.cyan);
                }
            }
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

    // ---------- Physics Loading ---------- //
    internal void LoadLevelPhysicsData(AssetColorMap mapPhysics)
    {
        collisionRects.Clear();

        mapWidth = mapPhysics.SourceWidth;
        mapHeight = mapPhysics.SourceHeight;

        // Find the MapID for "Solid"
        int solidID = -1;
        foreach (var m in mapPhysics.ColorMappings)
        {
            if (m.ResRefID == "Solid")
            {
                solidID = m.MapID;
                break;
            }
        }

        if (solidID == -1)
        {
            Debug.LogWarning("No Solid mapping found in color map.");
            return;
        }

        // Row by row, merge horizontal runs of solid tiles into rects
        for (int y = 0; y < mapHeight; y++)
        {
            int runStartX = -1;

            for (int x = 0; x < mapWidth; x++)
            {
                bool isSolid = mapPhysics.ColorMap[x, y] == solidID;

                if (isSolid)
                {
                    if (runStartX == -1)
                        runStartX = x;
                }
                else
                {
                    if (runStartX != -1)
                    {
                        collisionRects.Add(new RectInt(runStartX, y, x - runStartX, 1));
                        runStartX = -1;
                    }
                }
            }

            if (runStartX != -1)
            {
                collisionRects.Add(new RectInt(runStartX, y, mapWidth - runStartX, 1));
            }
        }

        // Merge vertical strips
        MergeRects();

        Debug.Log($"Generated {collisionRects.Count} merged collision rects.");
    }

    private void MergeRects()
    {
        bool mergedSomething;

        do
        {
            mergedSomething = false;
            var newList = new List<RectInt>();
            var used = new HashSet<int>();

            for (int i = 0; i < collisionRects.Count; i++)
            {
                if (used.Contains(i)) continue;
                RectInt a = collisionRects[i];

                bool merged = false;
                for (int j = i + 1; j < collisionRects.Count; j++)
                {
                    if (used.Contains(j)) continue;
                    RectInt b = collisionRects[j];

                    // Same x-range and width
                    if (a.xMin == b.xMin && a.width == b.width)
                    {
                        // Adjacent vertically
                        if (a.yMax == b.yMin || b.yMax == a.yMin)
                        {
                            // Merge into one taller rect
                            int newY = Mathf.Min(a.yMin, b.yMin);
                            int newHeight = a.height + b.height;
                            RectInt mergedRect = new RectInt(a.xMin, newY, a.width, newHeight);

                            newList.Add(mergedRect);
                            used.Add(i);
                            used.Add(j);
                            mergedSomething = true;
                            merged = true;
                            break;
                        }
                    }
                }

                if (!merged && !used.Contains(i))
                {
                    newList.Add(a);
                }
            }

            collisionRects = newList;
        }
        while (mergedSomething);
    }

    // ---------- Collision Checks ---------- //
    public CollisionInfo GetEntCollision(EntState ent)
    {
        if (ent == null || ent.RefEntity == null || !ent.RefEntity.UsesCompPhysics)
            return null;

        // Entity size
        float w = (ent.RefEntity.EntitySizeWidth > 0) ? ent.RefEntity.EntitySizeWidth : 1f;
        float h = (ent.RefEntity.EntitySizeHeight > 0) ? ent.RefEntity.EntitySizeHeight : 1f;

        // Entity rect (centered on EntPosition)
        Rect entRect = GetEntRect(ent);

        foreach (var rect in collisionRects)
        {
            Rect colRect = new Rect(rect.x, rect.y, rect.width, rect.height);

            if (entRect.Overlaps(colRect))
            {
                // Calculate penetration depths
                float dxLeft = colRect.xMax - entRect.xMin; // push right
                float dxRight = entRect.xMax - colRect.xMin; // push left
                float dyDown = colRect.yMax - entRect.yMin; // push up
                float dyUp = entRect.yMax - colRect.yMin; // push down

                float minDist = Mathf.Min(dxLeft, dxRight, dyDown, dyUp);
                Vector2 force = Vector2.zero;
                Vector2 nearest = ent.EntPosition;

                if (minDist == dxLeft)
                {
                    force = new Vector2(dxLeft, 0);
                    nearest = new Vector2(ent.EntPosition.x + dxLeft, ent.EntPosition.y);
                }
                else if (minDist == dxRight)
                {
                    force = new Vector2(-dxRight, 0);
                    nearest = new Vector2(ent.EntPosition.x - dxRight, ent.EntPosition.y);
                }
                else if (minDist == dyDown)
                {
                    force = new Vector2(0, dyDown);
                    nearest = new Vector2(ent.EntPosition.x, ent.EntPosition.y + dyDown);
                }
                else if (minDist == dyUp)
                {
                    force = new Vector2(0, -dyUp);
                    nearest = new Vector2(ent.EntPosition.x, ent.EntPosition.y - dyUp);
                }

                return new CollisionInfo
                {
                    Entity = ent,
                    CollisionRect = rect,
                    Force = force,
                    NearestValidPos = nearest
                };
            }
        }

        return null;
    }

    public CollisionInfo GetEntCollisionAtPos(EntState ent, Vector2 testPos)
    {
        if (ent == null || ent.RefEntity == null || !ent.RefEntity.UsesCompPhysics)
            return null;

        // Temporarily set ent position to testPos
        Vector2 originalPos = ent.EntPosition;
        ent.EntPosition = testPos;

        CollisionInfo hit = GetEntCollision(ent);

        // Restore original position
        ent.EntPosition = originalPos;

        return hit;
    }

    private static Rect GetEntRect(EntState ent)
    {
        float w = (ent.RefEntity.EntitySizeWidth > 0) ? ent.RefEntity.EntitySizeWidth : 1f;
        float h = (ent.RefEntity.EntitySizeHeight > 0) ? ent.RefEntity.EntitySizeHeight : 1f;

        // Shift rect so bottom-left is at EntPosition, then offset by half width/height
        float xMin = ent.EntPosition.x;
        float yMin = ent.EntPosition.y;

        return new Rect(xMin, yMin, w, h);
    }



    // ---------- Debug Drawing ---------- //
    private static void DrawRect(RectInt rect, Color color)
    {
        // Bottom left corner
        Vector3 p1 = new Vector3(rect.xMin, rect.yMin, 0);
        Vector3 p2 = new Vector3(rect.xMax, rect.yMin, 0);
        Vector3 p3 = new Vector3(rect.xMax, rect.yMax, 0);
        Vector3 p4 = new Vector3(rect.xMin, rect.yMax, 0);

        Debug.DrawLine(p1, p2, color, 0, false);
        Debug.DrawLine(p2, p3, color, 0, false);
        Debug.DrawLine(p3, p4, color, 0, false);
        Debug.DrawLine(p4, p1, color, 0, false);
    }

    private static void DrawEntityRect(EntState ent, Color color)
    {
        Rect r = GetEntRect(ent);

        Vector3 p1 = new Vector3(r.xMin, r.yMin, 0);
        Vector3 p2 = new Vector3(r.xMax, r.yMin, 0);
        Vector3 p3 = new Vector3(r.xMax, r.yMax, 0);
        Vector3 p4 = new Vector3(r.xMin, r.yMax, 0);

        Debug.DrawLine(p1, p2, color, 0, false);
        Debug.DrawLine(p2, p3, color, 0, false);
        Debug.DrawLine(p3, p4, color, 0, false);
        Debug.DrawLine(p4, p1, color, 0, false);
    }

    private static void DrawPoint(Vector2 pos, Color color)
    {
        float size = 0.1f;
        Vector3 p1 = new Vector3(pos.x - size, pos.y - size, 0);
        Vector3 p2 = new Vector3(pos.x + size, pos.y + size, 0);
        Vector3 p3 = new Vector3(pos.x - size, pos.y + size, 0);
        Vector3 p4 = new Vector3(pos.x + size, pos.y - size, 0);

        Debug.DrawLine(p1, p2, color, 0, false);
        Debug.DrawLine(p3, p4, color, 0, false);
    }

    internal static void DrawCollisionInfo(CollisionInfo info)
    {
        if (info == null || info.CollisionRect == null) return;

        // Draw collision rect
        DrawRect(info.CollisionRect, Color.red);

        // Draw entity rect
        DrawEntityRect(info.Entity, Color.yellow);

        // Draw force vector
        Vector3 start = new Vector3(info.Entity.EntPosition.x, info.Entity.EntPosition.y, 0);
        Vector3 end = start + new Vector3(info.Force.x, info.Force.y, 0);
        Debug.DrawLine(start, end, Color.magenta, 0, false);

        // Draw nearest valid position
        Vector2 nearest = new Vector3(info.NearestValidPos.x, info.NearestValidPos.y);
        DrawPoint(nearest, Color.cyan);
    }

}




public class CollisionInfo
{
    public EntState Entity;         // The entity we tested
    public RectInt CollisionRect;   // The rect it collided with
    public Vector2 Force;           // Suggested separation vector (not applied)
    public Vector2 NearestValidPos; // Suggested position outside the rect
}
