using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class WinResourceViewer : EditorWindow
{
    private Vector2 moduleScrollPos;
    private Vector2 resourceScrollPos;
    private Vector2 detailScrollPos;

    private string searchQuery = "";
    private AssetType? filterType = null;
    private bool? filterCached = null;

    private int selectedResourceIndex = -1;
    private List<ResConfigModuleAsset> allAssets = new List<ResConfigModuleAsset>();

    [MenuItem("Tools/Resource Viewer")]
    public static void ShowWindow()
    {
        GetWindow<WinResourceViewer>("Resource Viewer");
    }

    private void OnEnable()
    {
        RefreshAssets();
    }

    private void RefreshAssets()
    {
        allAssets.Clear();

        if (SysResource.Instance?.LoadedModuleMaps != null)
        {
            foreach (var module in SysResource.Instance.LoadedModuleMaps)
            {
                if (module?.ModuleAssets != null)
                {
                    allAssets.AddRange(module.ModuleAssets);
                }
            }
        }
    }

    private void OnGUI()
    {
        if (SysResource.Instance == null)
        {
            EditorGUILayout.HelpBox("SysResource.Instance is not initialized.", MessageType.Warning);
            return;
        }

        // Top header controls
        DrawHeaderControls();

        // Always refresh before drawing lists
        RefreshAssets();

        EditorGUILayout.BeginHorizontal();

        // ---------------------- Panel 01 ---------------------- //
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.45f));

        EditorGUILayout.LabelField("Loaded Modules", EditorStyles.boldLabel);
        DrawModules();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Resource Table", EditorStyles.boldLabel);
        DrawResourceTable();

        EditorGUILayout.EndVertical();

        // ---------------------- Panel 02 ---------------------- //
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField("Selected Resource", EditorStyles.boldLabel);
        DrawResourceDetails();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawHeaderControls()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        if (GUILayout.Button("Cache All", GUILayout.Height(24)))
        {
            SysResource.Instance.CacheAllResources(() => { Repaint(); });
        }

        if (GUILayout.Button("Clear Cache", GUILayout.Height(24)))
        {
            SysResource.Instance.ClearAllCaches();
            Repaint();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(6);
    }

    private void DrawModules()
    {
        moduleScrollPos = EditorGUILayout.BeginScrollView(moduleScrollPos, GUILayout.Height(100));

        var modules = SysResource.Instance.LoadedModuleMaps;
        if (modules == null || modules.Length == 0)
        {
            EditorGUILayout.LabelField("No modules loaded.");
        }
        else
        {
            foreach (var module in modules)
            {
                EditorGUILayout.LabelField($"{module.ModuleName} (v{module.Version})");
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawResourceTable()
    {
        // ---------------- Filter Controls ---------------- //
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Search ID:", GUILayout.Width(70));
        searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.Width(150));

        EditorGUILayout.LabelField("Type:", GUILayout.Width(40));
        string[] typeOptions = System.Enum.GetNames(typeof(AssetType));
        int currentTypeIndex = filterType.HasValue ? (int)filterType.Value + 1 : 0;
        int selectedType = EditorGUILayout.Popup(currentTypeIndex, new[] { "All" }.Concat(typeOptions).ToArray(), GUILayout.Width(120));
        filterType = selectedType == 0 ? (AssetType?)null : (AssetType)(selectedType - 1);

        EditorGUILayout.LabelField("Cached:", GUILayout.Width(60));
        int cacheState = filterCached == null ? 0 : (filterCached.Value ? 1 : 2);
        int newCacheState = EditorGUILayout.Popup(cacheState, new[] { "Any", "Yes", "No" }, GUILayout.Width(80));
        filterCached = newCacheState == 0 ? (bool?)null : (newCacheState == 1);

        EditorGUILayout.EndHorizontal();

        // ---------------- Table Header ---------------- //
        resourceScrollPos = EditorGUILayout.BeginScrollView(resourceScrollPos, GUILayout.Height(260));
        EditorGUILayout.BeginVertical();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Asset ID", EditorStyles.miniBoldLabel, GUILayout.Width(220));
        GUILayout.Label("Type", EditorStyles.miniBoldLabel, GUILayout.Width(100));
        GUILayout.Label("UnityObj", EditorStyles.miniBoldLabel, GUILayout.Width(80));
        GUILayout.Label("Config", EditorStyles.miniBoldLabel, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        // ---------------- Filtered Row Rendering ---------------- //
        for (int i = 0; i < allAssets.Count; i++)
        {
            var asset = allAssets[i];

            // Apply filters
            if (!string.IsNullOrEmpty(searchQuery) && !asset.AssetID.ToLower().Contains(searchQuery.ToLower()))
                continue;
            if (filterType.HasValue && asset.Type != filterType.Value)
                continue;

            bool isCachedResource = SysResource.Instance.IsResourceObjectCached(asset.AssetID);
            bool isCachedConfig = SysResource.Instance.IsResourceConfigCached(asset.AssetID);

            if (filterCached.HasValue)
            {
                bool isCached = isCachedResource || isCachedConfig;
                if (isCached != filterCached.Value)
                    continue;
            }

            // ---------------- Clickable Row ---------------- //
            Rect rowRect = GUILayoutUtility.GetRect(1, 20, GUILayout.ExpandWidth(true));
            bool clicked = Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition);

            Color rowColor = selectedResourceIndex == i
                ? new Color(0.3f, 0.5f, 0.85f, 0.2f)
                : new Color(0.1f, 0.1f, 0.1f, 0.05f);

            EditorGUI.DrawRect(rowRect, rowColor);

            EditorGUI.LabelField(new Rect(rowRect.x + 4, rowRect.y, 220, rowRect.height), asset.AssetID);
            EditorGUI.LabelField(new Rect(rowRect.x + 224, rowRect.y, 100, rowRect.height), asset.Type.ToString());
            EditorGUI.LabelField(new Rect(rowRect.x + 324, rowRect.y, 80, rowRect.height), isCachedResource ? "✔" : "✘");
            EditorGUI.LabelField(new Rect(rowRect.x + 404, rowRect.y, 80, rowRect.height), isCachedConfig ? "✔" : "✘");

            if (clicked)
            {
                selectedResourceIndex = i;
                GUI.FocusControl(null);
                Event.current.Use();
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }


    private void DrawResourceDetails()
    {
        if (selectedResourceIndex < 0 || selectedResourceIndex >= allAssets.Count)
        {
            EditorGUILayout.HelpBox("Select a resource from the table on the left.", MessageType.Info);
            return;
        }

        var asset = allAssets[selectedResourceIndex];

        detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

        EditorGUILayout.LabelField("Asset ID", asset.AssetID);
        EditorGUILayout.LabelField("Type", asset.Type.ToString());
        EditorGUILayout.LabelField("Asset Path", asset.AssetPath);

        EditorGUILayout.Space(6);
        DrawPerResourceControls(asset);
        EditorGUILayout.Space(10);

        // Config JSON
        EditorGUILayout.LabelField("Config JSON", EditorStyles.boldLabel);
        object config = SysResource.Instance.GetResourceConfig<object>(asset.AssetID);
        if (config != null)
        {
            string json = JsonUtility.ToJson(config, true);
            EditorGUILayout.TextArea(json, GUILayout.Height(150));
        }
        else
        {
            EditorGUILayout.HelpBox("Config not cached.", MessageType.Warning);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Unity Object / Details", EditorStyles.boldLabel);

        switch (asset.Type)
        {
            case AssetType.TextureAtlas:
                DisplayTextureAtlasDetails(asset);
                break;
            case AssetType.Sprite:
                DisplaySpriteDetails(asset);
                break;
            case AssetType.RefMap:
                DisplayMapDetails(asset);
                break;
            case AssetType.Level:
                DisplayLevelDetails(asset);
                break;
            case AssetType.Entity:
                DisplayEntityDetails(asset);
                break;
            case AssetType.AnimationCollection:
                DisplayAnimationCollectionDetails(asset);
                break;

            default:
                Object obj = SysResource.Instance.GetResource<Object>(asset.AssetID);
                if (obj != null)
                    EditorGUILayout.ObjectField("Unity Object", obj, typeof(Object), false);
                else
                    EditorGUILayout.HelpBox("No details available.", MessageType.Info);
                break;
        }


        EditorGUILayout.EndScrollView();
    }

    private void DisplayTextureAtlasDetails(ResConfigModuleAsset asset)
    {
        Texture2D tex = SysResource.Instance.GetResource<Texture2D>(asset.AssetID);
        if (tex != null)
        {
            DrawScaledPreview(tex, tex.width, tex.height);
        }
    }

    private void DisplaySpriteDetails(ResConfigModuleAsset asset)
    {
        Sprite sprite = SysResource.Instance.GetResource<Sprite>(asset.AssetID);
        if (sprite != null && sprite.texture != null)
        {
            Rect spriteRect = sprite.rect;
            Texture2D cropped = new Texture2D((int)spriteRect.width, (int)spriteRect.height);
            Color[] pixels = sprite.texture.GetPixels(
                (int)spriteRect.x,
                (int)spriteRect.y,
                (int)spriteRect.width,
                (int)spriteRect.height
            );
            cropped.SetPixels(pixels);
            cropped.Apply();
            DrawScaledPreview(cropped, (int)spriteRect.width, (int)spriteRect.height);
        }
        else
        {
            EditorGUILayout.LabelField("Sprite texture is null.");
        }
    }

    private void DisplayMapDetails(ResConfigModuleAsset asset)
    {
        AssetColorMap map = SysResource.Instance.GetResource<AssetColorMap>(asset.AssetID);
        if (map == null) { EditorGUILayout.HelpBox("Map not cached.", MessageType.Warning); return; }

        EditorGUILayout.LabelField($"Map Size: {map.SourceWidth} x {map.SourceHeight}");
        EditorGUILayout.LabelField($"Mappings: {map.ColorMappings.Count}");

        List<string> options = new List<string>();
        options.Add("None (Show All Colors)");
        foreach (var item in map.ColorMappings)
            options.Add($"ID {item.MapID} -> {item.ResRefID}");

        string cacheKey = asset.AssetID + "_selectedColor";
        if (!EditorPrefs.HasKey(cacheKey))
            EditorPrefs.SetInt(cacheKey, 0);

        int selectedIndex = EditorPrefs.GetInt(cacheKey);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Count - 1));
        selectedIndex = EditorGUILayout.Popup("Color Map Selection", selectedIndex, options.ToArray());
        EditorPrefs.SetInt(cacheKey, selectedIndex);

        Texture2D previewTex = new Texture2D(map.SourceWidth, map.SourceHeight, TextureFormat.RGBA32, false);
        Color32[] p = new Color32[map.SourceWidth * map.SourceHeight];

        if (selectedIndex == 0)
        {
            for (int y = 0; y < map.SourceHeight; y++)
            {
                for (int x = 0; x < map.SourceWidth; x++)
                {
                    int id = map.ColorMap[x, y];
                    if (id == -1) p[y * map.SourceWidth + x] = new Color32(0, 0, 0, 0);
                    else
                    {
                        var mapping = map.ColorMappings.Find(m => m.MapID == id);
                        p[y * map.SourceWidth + x] = mapping != null ? mapping.Color : new Color32(0, 0, 0, 0);
                    }
                }
            }
        }
        else
        {
            var selectedMapping = map.ColorMappings[selectedIndex - 1];
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Selected ID: {selectedMapping.MapID}");
            EditorGUILayout.LabelField($"ResRefID: {selectedMapping.ResRefID}");
            EditorGUILayout.LabelField($"Color: {selectedMapping.Color}");

            for (int y = 0; y < map.SourceHeight; y++)
            {
                for (int x = 0; x < map.SourceWidth; x++)
                {
                    int id = map.ColorMap[x, y];
                    p[y * map.SourceWidth + x] = (id == selectedMapping.MapID) ? selectedMapping.Color : new Color32(0, 0, 0, 0);
                }
            }
        }

        previewTex.SetPixels32(p);
        previewTex.Apply();

        DrawScaledPreview(previewTex, map.SourceWidth, map.SourceHeight);
    }

    private void DisplayLevelDetails(ResConfigModuleAsset asset)
    {
        ResCfgLevel level = SysResource.Instance.GetResourceConfig<ResCfgLevel>(asset.AssetID);
        if (level == null) { EditorGUILayout.HelpBox("Level config not cached.", MessageType.Warning); return; }

        EditorGUILayout.LabelField("Level Name", level.LevelName);
        EditorGUILayout.LabelField("Level Description", level.LevelDesc);
        EditorGUILayout.LabelField("Show In Level Select", level.ShowInLevelSelect.ToString());
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Collision Map", level.LevelMapCol);
        EditorGUILayout.LabelField("Back Map", level.LevelMapBack);
        EditorGUILayout.LabelField("Front Map", level.LevelMapFront);
        EditorGUILayout.LabelField("Entities Map", level.LevelMapEntities);
    }

    private void DisplayEntityDetails(ResConfigModuleAsset asset)
    {
        ResCfgEntity entity = SysResource.Instance.GetResourceConfig<ResCfgEntity>(asset.AssetID);
        if (entity == null)
        {
            EditorGUILayout.HelpBox("Entity config not cached.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Entity Name", entity.EntityName);
        EditorGUILayout.LabelField("Entity Sprite", entity.EntitySprite);
        EditorGUILayout.LabelField("Entity Health", entity.EntityHealth.ToString());
        EditorGUILayout.LabelField("Entity Size", $"{entity.EntitySizeWidth} x {entity.EntitySizeHeight}");
        EditorGUILayout.LabelField("Tags", entity.EntityTags != null ? string.Join(", ", entity.EntityTags) : "(none)");

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);

        DrawComponentIfPresent("Player Controller", entity.UsesCompPlayerController, entity.CompPlayerController, () =>
        {
            EditorGUILayout.LabelField("Move Speed", entity.CompPlayerController.MoveSpeed.ToString());
            EditorGUILayout.LabelField("Run Speed", entity.CompPlayerController.RunSpeed.ToString());
            EditorGUILayout.LabelField("Acceleration", entity.CompPlayerController.Acceleration.ToString());
            EditorGUILayout.LabelField("Jump Height", entity.CompPlayerController.JumpHeight.ToString());
        });

        DrawComponentIfPresent("AI", entity.UsesCompAI, entity.CompAI, () =>
        {
            EditorGUILayout.LabelField("Type", entity.CompAI.Type.ToString());
            EditorGUILayout.LabelField("Move Speed", entity.CompAI.MoveSpeed.ToString());
        });

        DrawComponentIfPresent("Physics", entity.UsesCompPhysics, entity.CompPhysics, () =>
        {
            EditorGUILayout.LabelField("Mass", entity.CompPhysics.Mass.ToString());
            EditorGUILayout.LabelField("Drag", entity.CompPhysics.Drag.ToString());
            EditorGUILayout.LabelField("Bounciness", entity.CompPhysics.Bounciness.ToString());
        });

        DrawComponentIfPresent("Animation", entity.UsesCompAnimation, entity.CompAnimation, () =>
        {
            EditorGUILayout.LabelField("Animation ID", entity.CompAnimation.AnimationID);
            EditorGUILayout.LabelField("Animation Speed", entity.CompAnimation.AnimationSpeed.ToString());
            EditorGUILayout.LabelField("Default Animation", entity.CompAnimation.DefaultAnimation);
            EditorGUILayout.LabelField("Loop", entity.CompAnimation.Loop.ToString());
        });

        DrawComponentIfPresent("Collision", entity.UsesCompCollision, entity.CompCollision, () =>
        {
            EditorGUILayout.LabelField("Width", entity.CompCollision.Width.ToString());
            EditorGUILayout.LabelField("Height", entity.CompCollision.Height.ToString());
        });

        DrawComponentIfPresent("Damage Emitter", entity.UsesCompDamageEmitter, entity.CompDamageEmitter, () =>
        {
            EditorGUILayout.LabelField("Radius", entity.CompDamageEmitter.Radius.ToString());
            EditorGUILayout.LabelField("Damage", entity.CompDamageEmitter.Damage.ToString());
            EditorGUILayout.LabelField("Target Tags", entity.CompDamageEmitter.TargetTags != null
                ? string.Join(", ", entity.CompDamageEmitter.TargetTags)
                : "(none)");

        });

        DrawComponentIfPresent("Damage Receiver", entity.UsesCompDamageReceiver, entity.CompDamageReceiver, () =>
        {
            EditorGUILayout.LabelField("Source Tags", entity.CompDamageReceiver.SourceTags != null
                ? string.Join(", ", entity.CompDamageReceiver.SourceTags)
                : "(none)");
        });

        DrawComponentIfPresent("Death Reward", entity.UsesCompDeathReward, entity.CompDeathReward, () =>
        {
            EditorGUILayout.LabelField("Value", entity.CompDeathReward.Value.ToString());
        });

        DrawComponentIfPresent("Collectible", entity.UsesCompCollectible, entity.CompCollectible, () =>
        {
            EditorGUILayout.LabelField("Radius", entity.CompCollectible.Radius.ToString());
            EditorGUILayout.LabelField("Value", entity.CompCollectible.Value.ToString());
        });

        DrawComponentIfPresent("Spawner", entity.UsesCompSpawner, entity.CompSpawner, () =>
        {
            EditorGUILayout.LabelField("Spawn Rate", entity.CompSpawner.SpawnRate.ToString());
            EditorGUILayout.LabelField("Spawned Entity", entity.CompSpawner.SpawnedEntity);
        });

        DrawComponentIfPresent("Door", entity.UsesCompDoor, entity.CompDoor, () =>
        {
            EditorGUILayout.LabelField("Target Level", entity.CompDoor.TargetLevel.ToString());
            EditorGUILayout.LabelField("Reward Value", entity.CompDoor.RewardValue.ToString());
        });
    }

    private void DisplayAnimationCollectionDetails(ResConfigModuleAsset asset)
    {
        ResCfgAnimationCollection animation = SysResource.Instance.GetResourceConfig<ResCfgAnimationCollection>(asset.AssetID);
        if (animation == null)
        {
            EditorGUILayout.HelpBox("Animation config not cached.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Animation ID", animation.AnimationID);
        EditorGUILayout.LabelField("Frame Rate", animation.FrameRate.ToString());
        EditorGUILayout.Space(6);

        if (animation.AnimationScenes == null || animation.AnimationScenes.Length == 0)
        {
            EditorGUILayout.LabelField("No scenes in this animation.");
            return;
        }

        foreach (var scene in animation.AnimationScenes)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Scene ID: {scene.SceneID}");
            if (scene.FrameSpriteIDs == null || scene.FrameSpriteIDs.Length == 0)
            {
                EditorGUILayout.LabelField("No frames.");
                EditorGUILayout.EndVertical();
                continue;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Frames:");
            EditorGUILayout.BeginHorizontal();

            foreach (string spriteID in scene.FrameSpriteIDs)
            {
                Sprite sprite = SysResource.Instance.GetResource<Sprite>(spriteID);
                if (sprite != null)
                {
                    Rect spriteRect = sprite.rect;
                    Texture2D cropped = new Texture2D((int)spriteRect.width, (int)spriteRect.height);
                    Color[] pixels = sprite.texture.GetPixels(
                        (int)spriteRect.x,
                        (int)spriteRect.y,
                        (int)spriteRect.width,
                        (int)spriteRect.height
                    );
                    cropped.SetPixels(pixels);
                    cropped.Apply();

                    float scale = 1.5f;
                    GUILayout.Label(cropped, GUILayout.Width(spriteRect.width * scale), GUILayout.Height(spriteRect.height * scale));
                }
                else
                {
                    EditorGUILayout.LabelField($"[Missing Sprite: {spriteID}]", GUILayout.Width(100));
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    private void DrawComponentIfPresent(string label, bool usageFlag, object comp, System.Action drawFields)
    {
        if (usageFlag && comp != null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            drawFields.Invoke();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }
    }

    private void DrawPerResourceControls(ResConfigModuleAsset asset)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Cache Resource", GUILayout.Height(22)))
        {
            SysResource.Instance.CacheResources(new[] { asset.AssetID }, () => { Repaint(); });
        }

        if (GUILayout.Button("Clear Resource Cache", GUILayout.Height(22)))
        {
            SysResource.Instance.ClearResource(asset.AssetID);
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("Open Asset Location", GUILayout.Height(22)))
        {
            if (!string.IsNullOrEmpty(asset.AssetPath))
            {
                string basePath = SysResource.Instance.gameDataPath;
                string assetPath = Path.Combine(basePath, asset.AssetPath);

                assetPath = assetPath.Replace("\\", "/"); // Normalize path

                Debug.Log("Opening asset location: " + assetPath);
                EditorUtility.RevealInFinder(assetPath);

            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Path",
                    "This asset does not have a valid path.",
                    "OK");
            }
        }
    }

    private void DrawScaledPreview(Texture2D tex, int width, int height)
    {
        float maxWidth = position.width * 0.5f;
        float aspect = (float)height / width;
        float drawWidth = Mathf.Min(maxWidth, width * 4);
        float drawHeight = drawWidth * aspect;

        GUILayout.Label(tex, GUILayout.Width(drawWidth), GUILayout.Height(drawHeight));
    }
}
