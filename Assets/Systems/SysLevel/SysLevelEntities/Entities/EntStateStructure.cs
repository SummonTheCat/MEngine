
using UnityEngine;

[System.Serializable]
public class EntState
{
    public ResCfgEntity RefEntity;

    public int EntID;
    public Vector2 EntPosition;
    public Sprite EntTargetSprite;

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
            EntID = Random.Range(100000, 999999),
            EntPosition = position,
            EntTargetSprite = SysResource.Instance.GetResource<Sprite>(cfgEntity.EntitySprite)
        };

        return newEnt;
    }


}