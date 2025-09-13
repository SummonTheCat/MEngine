using System;
using System.Collections.Generic;

[Serializable]
public class EntRenderManager
{

    private List<EntityRenderer> entityRenderers = new();

    internal void BuildEntityRenderers()
    {
        entityRenderers.Clear();
        if (SysLevelEntities.Instance.stateManager.GetEntities() == null) return;

        foreach (var ent in SysLevelEntities.Instance.stateManager.GetEntities())
        {
            entityRenderers.Add(new EntityRenderer(ent, ent.RefEntity.EntityLayer));
        }
    }

    internal void RenderEntities()
    {
        foreach (var er in entityRenderers)
        {
            er.Render();
        }
    }
}