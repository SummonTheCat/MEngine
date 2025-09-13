using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class WinModKitConfigEditor : EditorWindow
{
    private object currentConfig;
    private Type currentConfigType;
    Vector2 scrollPos = Vector2.zero;

    // Track the asset being edited
    private string lastAssetID;

    [MenuItem("ModKit/Config Editor")]
    public static void ShowWindow()
    {
        GetWindow<WinModKitConfigEditor>("Config Editor");
    }

    private void OnEnable()
    {
        LoadSelectedAssetConfig();
    }

    private void OnGUI()
    {
        // Detect asset change and reload
        if (ModKitState.SelectedAsset != null && ModKitState.SelectedAsset.AssetID != lastAssetID)
        {
            LoadSelectedAssetConfig();
        }

        if (string.IsNullOrEmpty(ModKitState.LoadedModulePath))
        {
            EditorGUILayout.HelpBox("No module loaded. Please load a module in ModKit Core.", MessageType.Warning);
            return;
        }

        var module = ModKitState.LoadedModuleData?.LoadedModule;
        if (module == null)
        {
            EditorGUILayout.HelpBox("Module data is missing or invalid.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Loaded Module", module.ModuleName, EditorStyles.boldLabel);

        if (ModKitState.SelectedAsset == null)
        {
            EditorGUILayout.HelpBox("No asset selected. Use the Edit button in ModKit Core to select an asset.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Editing Asset: " + ModKitState.SelectedAsset.AssetID);

        if (currentConfig != null && currentConfigType != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            currentConfig = ModKitJsonEditorUtil.DrawObject("Root", currentConfig, currentConfigType);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Save"))
            {
                SaveCurrentAsset();
            }
        }
        else
        {
            if (GUILayout.Button("Load Asset Data"))
            {
                LoadSelectedAssetConfig();
            }
        }
    }

    private void LoadSelectedAssetConfig()
    {
        if (ModKitState.SelectedAsset == null) return;

        string moduleDir = Path.GetDirectoryName(ModKitState.LoadedModulePath);
        Debug.Log("Module Directory: " + moduleDir);

        string assetPathRelative = ModKitState.SelectedAsset.AssetPath.Replace("Modules/" + ModKitState.LoadedModuleData.LoadedModule.ModuleName + "/", "");

        string assetPath = Path.Combine(moduleDir, assetPathRelative);

        if (!File.Exists(assetPath))
        {
            Debug.LogError("Asset file not found: " + assetPath);
            return;
        }

        string json = File.ReadAllText(assetPath);
        switch (ModKitState.SelectedAsset.Type)
        {
            case AssetType.TextureAtlas:
                currentConfigType = typeof(ResCfgTextureAtlas);
                currentConfig = JsonUtility.FromJson<ResCfgTextureAtlas>(json);
                break;
            case AssetType.Sprite:
                currentConfigType = typeof(ResCfgSprite);
                currentConfig = JsonUtility.FromJson<ResCfgSprite>(json);
                break;
            case AssetType.RefMap:
                currentConfigType = typeof(ResCfgColorMap);
                currentConfig = JsonUtility.FromJson<ResCfgColorMap>(json);
                break;
            case AssetType.Level:
                currentConfigType = typeof(ResCfgLevel);
                currentConfig = JsonUtility.FromJson<ResCfgLevel>(json);
                break;
            case AssetType.Entity:
                currentConfigType = typeof(ResCfgEntity);
                currentConfig = JsonUtility.FromJson<ResCfgEntity>(json);
                break;
            case AssetType.AnimationCollection:
                currentConfigType = typeof(ResCfgAnimationCollection);
                currentConfig = JsonUtility.FromJson<ResCfgAnimationCollection>(json);
                break;
        }

        // Update tracker
        lastAssetID = ModKitState.SelectedAsset.AssetID;
    }

    private void SaveCurrentAsset()
    {
        if (ModKitState.SelectedAsset == null || currentConfig == null) return;

        string moduleDir = Path.GetDirectoryName(ModKitState.LoadedModulePath);
        string assetPathRelative = ModKitState.SelectedAsset.AssetPath.Replace("Modules/" + ModKitState.LoadedModuleData.LoadedModule.ModuleName + "/", "");

        string assetPath = Path.Combine(moduleDir, assetPathRelative);

        try
        {
            string json = JsonUtility.ToJson(currentConfig, true);
            File.WriteAllText(assetPath, json);
            Debug.Log("Asset saved: " + assetPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save asset: " + ex.Message);
        }
    }
}
