using UnityEditor;
using UnityEngine;

public class WinEntState : EditorWindow
{
    private Vector2 scrollPos;
    private int lastUpdatedFrame = -1;

    [MenuItem("Tools/Entity State Viewer")]
    public static void ShowWindow()
    {
        GetWindow<WinEntState>("Entity States");
    }

    private void OnEnable()
    {
        EditorApplication.update += EditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= EditorUpdate;
    }

    private void EditorUpdate()
    {
        if (DebugFrameTracker.GlobalFrameCount % 5 == 0 && DebugFrameTracker.GlobalFrameCount != lastUpdatedFrame)
        {
            lastUpdatedFrame = DebugFrameTracker.GlobalFrameCount;
            Repaint();
        }
    }

    private void OnGUI()
    {
        if (SysLevelEntities.Instance == null || SysLevelEntities.Instance.stateManager == null)
        {
            EditorGUILayout.HelpBox("SysLevelEntities not initialized in scene.", MessageType.Warning);
            return;
        }

        EntState[] entities = SysLevelEntities.Instance.stateManager.GetEntities();
        if (entities == null || entities.Length == 0)
        {
            EditorGUILayout.HelpBox("No entities loaded.", MessageType.Info);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (EntState ent in entities)
        {
            if (ent == null) continue;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Entity ID: {ent.EntID}", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("RefEntity", ent.RefEntity != null ? ent.RefEntity.EntityName : "null");
            }

            // Health
            float newHealth = EditorGUILayout.FloatField("Health", ent.EntHealth);
            if (!Mathf.Approximately(newHealth, ent.EntHealth))
            {
                Undo.RecordObject(SysLevelEntities.Instance, "Edit Entity Health");
                ent.EntHealth = Mathf.Max(0f, newHealth);
                EditorUtility.SetDirty(SysLevelEntities.Instance);
            }

            Vector2 newPos = EditorGUILayout.Vector2Field("Position", ent.EntPosition);
            if (newPos != ent.EntPosition)
            {
                Undo.RecordObject(SysLevelEntities.Instance, "Edit Entity Position");
                ent.EntPosition = newPos;
                EditorUtility.SetDirty(SysLevelEntities.Instance);
            }

            Vector2 newVel = EditorGUILayout.Vector2Field("Velocity", ent.Velocity);
            if (newVel != ent.Velocity)
            {
                Undo.RecordObject(SysLevelEntities.Instance, "Edit Entity Velocity");
                ent.Velocity = newVel;
                EditorUtility.SetDirty(SysLevelEntities.Instance);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Is Grounded", ent.IsGrounded);
            }

            bool newCameraTarget = EditorGUILayout.Toggle("Is Camera Target", ent.IsCameraTarget);
            if (newCameraTarget != ent.IsCameraTarget)
            {
                Undo.RecordObject(SysLevelEntities.Instance, "Toggle Camera Target");
                ent.IsCameraTarget = newCameraTarget;
                EditorUtility.SetDirty(SysLevelEntities.Instance);
            }

            float newBounce = EditorGUILayout.FloatField("Bounce Value", ent.BounceValue);
            if (!Mathf.Approximately(newBounce, ent.BounceValue))
            {
                Undo.RecordObject(SysLevelEntities.Instance, "Edit Bounce Value");
                ent.BounceValue = newBounce;
                EditorUtility.SetDirty(SysLevelEntities.Instance);
            }

            Sprite newSprite = (Sprite)EditorGUILayout.ObjectField("Target Sprite", ent.EntTargetSprite, typeof(Sprite), false);
            if (newSprite != ent.EntTargetSprite)
            {
                Undo.RecordObject(SysLevelEntities.Instance, "Edit Entity Sprite");
                ent.EntTargetSprite = newSprite;
                EditorUtility.SetDirty(SysLevelEntities.Instance);
            }

            bool newFlipped = EditorGUILayout.Toggle("Sprite Flipped", ent.EntSpriteFlipped);
            if (newFlipped != ent.EntSpriteFlipped)
            {
                Undo.RecordObject(SysLevelEntities.Instance, "Toggle Sprite Flipped");
                ent.EntSpriteFlipped = newFlipped;
                EditorUtility.SetDirty(SysLevelEntities.Instance);
            }

            if (ent.RefEntity != null && ent.RefEntity.UsesCompAnimation)
            {
                EditorGUILayout.LabelField("Animation State", EditorStyles.boldLabel);

                string newScene = EditorGUILayout.TextField("Current Scene", ent.CurrentAnimationScene);
                if (newScene != ent.CurrentAnimationScene)
                {
                    Undo.RecordObject(SysLevelEntities.Instance, "Edit Animation Scene");
                    ent.CurrentAnimationScene = newScene;
                    ent.CurrentFrameIndex = 0;
                    ent.AnimationTimer = 0f;
                    EditorUtility.SetDirty(SysLevelEntities.Instance);
                }

                int newFrameIndex = EditorGUILayout.IntField("Current Frame Index", ent.CurrentFrameIndex);
                if (newFrameIndex != ent.CurrentFrameIndex)
                {
                    Undo.RecordObject(SysLevelEntities.Instance, "Edit Frame Index");
                    ent.CurrentFrameIndex = Mathf.Max(0, newFrameIndex);
                    EditorUtility.SetDirty(SysLevelEntities.Instance);
                }

                float newAnimTimer = EditorGUILayout.FloatField("Animation Timer", ent.AnimationTimer);
                if (!Mathf.Approximately(newAnimTimer, ent.AnimationTimer))
                {
                    Undo.RecordObject(SysLevelEntities.Instance, "Edit Animation Timer");
                    ent.AnimationTimer = Mathf.Max(0f, newAnimTimer);
                    EditorUtility.SetDirty(SysLevelEntities.Instance);
                }
            }

            if (ent.RefEntity != null && ent.RefEntity.UsesCompPlayerController)
            {
                EditorGUILayout.LabelField("Player Controller", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Toggle("Uses Player Controller", ent.RefEntity.UsesCompPlayerController);

                    EditorGUILayout.Vector2Field("Velocity", ent.Velocity);

                }


            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        EditorGUILayout.EndScrollView();
    }
}
