using System;
using System.Collections.Generic;
using UnityEngine;

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
        this.zOffset = zOffset / 10f;

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
        if (ent.EntTargetSprite == null) return;

        // Detect sprite change (e.g. due to animation)
        if (ent.EntTargetSprite.name != material.mainTexture.name)
        {
            string meshKey = ent.EntTargetSprite.name + "_" + ent.RefEntity.EntitySizeWidth + "x" + ent.RefEntity.EntitySizeHeight;
            if (!meshCache.TryGetValue(meshKey, out mesh))
            {
                mesh = BuildMesh(ent.EntTargetSprite, ent.RefEntity.EntitySizeWidth, ent.RefEntity.EntitySizeHeight);
                meshCache[meshKey] = mesh;
            }

            material.mainTexture = ent.EntTargetSprite.texture;
        }

        // Update transform if position changed or camera is targeting this entity
        if (ent.EntPosition != lastPosition || ent.IsCameraTarget)
        {
            UpdateTransform();
        }

        Graphics.DrawMesh(mesh, transform, material, 0);
    }


    private void UpdateTransform(bool force = false)
    {
        lastPosition = ent.EntPosition;

        Vector3 renderPos = new Vector3(ent.EntPosition.x, ent.EntPosition.y, zOffset / 10f);

        if (ent.IsCameraTarget && SysCamera.Instance != null)
        {
            Vector3 camPos = SysCamera.Instance.TargetCamera.transform.position;
            renderPos = new Vector3(camPos.x, camPos.y, zOffset / 10f);
        }

        // Pixel-perfect adjustment
        if (SysCamera.Instance != null && SysCamera.Instance.Movement == SysCamera.MovementMode.PixelPerfect && SysCamera.Instance.PixelPerfectEntities)
        {
            int pixelsPerUnit = SysCamera.Instance.PixelsPerUnit * SysCamera.Instance.BasePPUZoom;
            float unitSize = 1f / pixelsPerUnit;

            renderPos.x = Mathf.Round(renderPos.x / unitSize) * unitSize;
            renderPos.y = Mathf.Round(renderPos.y / unitSize) * unitSize;
        }

        Vector3 scale = Vector3.one;
        if (ent.EntSpriteFlipped)
        {
            scale = new Vector3(-1f, 1f, 1f); // flip horizontally
            renderPos.x += ent.RefEntity.EntitySizeWidth; // offset to keep position consistent
        }

        transform = Matrix4x4.TRS(
            renderPos,
            Quaternion.identity,
            scale
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
