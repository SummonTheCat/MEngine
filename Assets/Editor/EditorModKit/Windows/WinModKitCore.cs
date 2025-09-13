// ---------- Editor Window ---------- //

using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;
using Unity.VisualScripting;

public class WinModKitCore : EditorWindow
{
    private string searchQuery = "";
    private int filterIndex = 0;
    private Vector2 scrollPosition;

    private const float ButtonWidth = 60f;

    private string newAssetID = "";
    private string newAssetPath = "";
    private AssetType newAssetType = AssetType.Sprite;

    // Module editor state
    private string tempModuleName = "";
    private string tempModuleVersion = "";

    private string assetValidationError = null;


    [MenuItem("ModKit/ModKit Core")]
    public static void ShowWindow()
    {
        GetWindow<WinModKitCore>("ModKit Core");
    }
    //
    private void OnDisable()
    {
        ResetModuleEditorState();
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(ModKitState.LoadedModulePath))
        {
            DrawNonLoadedModule();
        }
        else
        {
            DrawLoadedModule();
            DrawModuleMetaEditor();
            DrawAssetsList();
            DrawCreateAssetRow();
        }
    }

    // ---------- UI: Module Not Loaded ---------- //

    public static void DrawNonLoadedModule()
    {
        EditorGUILayout.LabelField("No module loaded.");
        if (GUILayout.Button("Load Module"))
        {
            string path = EditorUtility.OpenFilePanel("Load Module", "", "json");
            if (!string.IsNullOrEmpty(path))
            {
                ModKitState.LoadModule(path);
            }
        }
    }

    // ---------- UI: Module Loaded ---------- //

    private void DrawLoadedModule()
    {
        EditorGUILayout.LabelField("Loaded Module: " + ModKitState.LoadedModulePath);

        if (ModKitState.IsModuleDirty)
            EditorGUILayout.LabelField("Status: Dirty", EditorStyles.boldLabel);
        else
            EditorGUILayout.LabelField("Status: Clean", EditorStyles.boldLabel);

        if(GUILayout.Button("Unload Module"))
        {
            ModKitState.LoadedModulePath = null;
            ModKitState.LoadedModuleData = null;
            ModKitState.IsModuleDirty = false;
            ModKitState.SelectedAsset = null;
            ResetModuleEditorState();
            GUI.FocusControl(null); // Deselect focused field
        }

        if (GUILayout.Button("Reload Module"))
        {
            ModKitState.ReloadModule();
            ResetModuleEditorState();         // <- Reset editable fields
            GUI.FocusControl(null);           // <- Deselect focused field
            Debug.Log("Module reloaded: " + ModKitState.LoadedModulePath);
        }
    }

    // ---------- UI: Module Info Editable ---------- //

    private void DrawModuleMetaEditor()
    {
        var module = ModKitState.LoadedModuleData?.LoadedModule;
        if (module == null)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Module Info", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        tempModuleName = EditorGUILayout.TextField("Module Name", tempModuleName);
        tempModuleVersion = EditorGUILayout.TextField("Version", tempModuleVersion);
        if (EditorGUI.EndChangeCheck())
        {
            if (tempModuleName != module.ModuleName || tempModuleVersion != module.Version)
            {
                ModKitState.IsModuleDirty = true;
            }
        }

        if (GUILayout.Button("Save", GUILayout.Width(80)))
        {
            module.ModuleName = tempModuleName;
            module.Version = tempModuleVersion;
            Debug.Log("Module info saved.");
        }

        EditorGUILayout.EndVertical();
    }

    // ---------- UI: Assets List ---------- //

    private void DrawAssetsList()
    {
        if (ModKitState.LoadedModuleData?.LoadedModule?.ModuleAssets == null)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Module Assets", EditorStyles.boldLabel);

        if (ModKitState.SelectedAsset != null)
        {
            EditorGUILayout.HelpBox(
                $"Selected Asset: {ModKitState.SelectedAsset.AssetID} ({ModKitState.SelectedAsset.Type})",
                MessageType.Info
            );
        }


        EditorGUILayout.BeginHorizontal();

        searchQuery = EditorGUILayout.TextField(searchQuery, GUILayout.MinWidth(200));

        string[] filterOptions = new string[] { "All" }.Concat(System.Enum.GetNames(typeof(AssetType))).ToArray();
        filterIndex = EditorGUILayout.Popup(filterIndex, filterOptions, GUILayout.MaxWidth(200));

        EditorGUILayout.EndHorizontal();

        var assets = ModKitState.LoadedModuleData.LoadedModule.ModuleAssets.AsEnumerable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            assets = assets.Where(a =>
                (!string.IsNullOrEmpty(a.AssetID) && a.AssetID.ToLower().Contains(searchQuery.ToLower())) ||
                (!string.IsNullOrEmpty(a.AssetPath) && a.AssetPath.ToLower().Contains(searchQuery.ToLower()))
            );
        }

        if (filterIndex > 0)
        {
            AssetType selectedType = (AssetType)(filterIndex - 1);
            assets = assets.Where(a => a.Type == selectedType);
        }

        assets = assets.OrderBy(a => a.AssetID);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        foreach (var asset in assets)
        {
            bool isSelected = ModKitState.SelectedAsset == asset;

            GUIStyle rowStyle = new GUIStyle("box");
            if (isSelected)
            {
                rowStyle.normal.background = Texture2D.whiteTexture;
                rowStyle.margin = new RectOffset(4, 4, 2, 2);
            }

            Color prevColor = GUI.backgroundColor;
            if (isSelected)
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f, 0.3f); // light blue overlay

            EditorGUILayout.BeginHorizontal(rowStyle);

            EditorGUILayout.LabelField("ID: " + asset.AssetID, GUILayout.Width(200));
            EditorGUILayout.LabelField("Type: " + asset.Type.ToString(), GUILayout.Width(150));

            string relativePath = asset.AssetPath.Replace(
                "Modules/" + ModKitState.LoadedModuleData.LoadedModule.ModuleName + "/", ""
            );
            EditorGUILayout.LabelField("Path: " + relativePath);

            if (GUILayout.Button("Edit", GUILayout.Width(ButtonWidth)))
            {
                OnEditAsset(asset);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(ButtonWidth)))
            {
                OnDeleteAsset(asset);
            }

            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = prevColor;
        }
        EditorGUILayout.EndScrollView();

    }

    // ---------- UI: Create Asset Row ---------- //

    private void DrawCreateAssetRow()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Create New Asset", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        newAssetID = EditorGUILayout.TextField(newAssetID, GUILayout.Width(200));
        newAssetPath = EditorGUILayout.TextField(newAssetPath, GUILayout.Width(250));
        newAssetType = (AssetType)EditorGUILayout.EnumPopup(newAssetType, GUILayout.Width(150));

        if (GUILayout.Button("Create", GUILayout.Width(ButtonWidth)))
        {
            OnCreateAsset();
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(assetValidationError))
        {
            GUIStyle errorStyle = new GUIStyle(EditorStyles.label);
            errorStyle.normal.textColor = Color.red;
            EditorGUILayout.LabelField(assetValidationError, errorStyle);
        }

        EditorGUILayout.EndVertical();
    }


    // ---------- State Sync ---------- //

    private void ResetModuleEditorState()
    {
        var module = ModKitState.LoadedModuleData?.LoadedModule;
        if (module == null)
        {
            tempModuleName = "";
            tempModuleVersion = "";
        }
        else
        {
            tempModuleName = module.ModuleName;
            tempModuleVersion = module.Version;
        }

        newAssetID = "";
        newAssetPath = "";
        newAssetType = AssetType.Entity;
        assetValidationError = null;
        ModKitState.IsModuleDirty = false;
    }

    // ---------- UI Actions ---------- //

    private void OnEditAsset(ResConfigModuleAsset asset)
    {
        ModKitState.SelectedAsset = asset;
        Debug.Log($"Selected asset: {asset.AssetID} ({asset.AssetPath})");
    }


    private void OnDeleteAsset(ResConfigModuleAsset asset)
    {
        if (!EditorUtility.DisplayDialog("Confirm Delete",
            $"Are you sure you want to delete asset '{asset.AssetID}'?",
            "Delete", "Cancel"))
        {
            return;
        }

        var module = ModKitState.LoadedModuleData?.LoadedModule;
        if (module == null)
        {
            Debug.LogError("No module loaded.");
            return;
        }

        // Remove from ModuleAssets array
        module.ModuleAssets = module.ModuleAssets.Where(a => a.AssetID != asset.AssetID).ToArray();
        ModKitState.IsModuleDirty = true;

        // Save updated module
        ModKitResIO.SaveModuleToFile(ModKitState.LoadedModulePath, module);

        // Delete associated asset file
        string moduleDir = Path.GetDirectoryName(ModKitState.LoadedModulePath);
        string assetPathRelative = asset.AssetPath.Replace("Modules/" + ModKitState.LoadedModuleData.LoadedModule.ModuleName + "/", "");

        string assetDir = Path.Combine(moduleDir, Path.GetDirectoryName(assetPathRelative));
        ModKitResIO.DeleteAssetFile(assetDir, asset.AssetID);

        // Reload updated state
        ModKitState.ReloadModule();
        ResetModuleEditorState();
        GUI.FocusControl(null);

        Debug.Log($"Deleted asset: {asset.AssetID}");
    }


    private void OnCreateAsset()
    {
        assetValidationError = null;

        if (string.IsNullOrEmpty(newAssetID))
        {
            assetValidationError = "Asset ID cannot be empty.";
            return;
        }

        if (string.IsNullOrEmpty(newAssetPath))
        {
            assetValidationError = "Asset path cannot be empty.";
            return;
        }

        Debug.Log(ModKitState.LoadedModulePath);
        string loadedModuleDir = Path.GetDirectoryName(ModKitState.LoadedModulePath);
        Debug.Log(loadedModuleDir);


        string dir = Path.Combine(loadedModuleDir, Path.GetDirectoryName(newAssetPath));
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            // Ask if we want to create the directory
            if (EditorUtility.DisplayDialog("Create Directory",
                $"The directory does not exist: {dir}. Do you want to create it?",
                "Create", "Cancel"))
            {
                Directory.CreateDirectory(dir);
            }
            else
            {
                assetValidationError = "Asset creation cancelled. Directory does not exist.";
                return;
            }

        }

        var module = ModKitState.LoadedModuleData?.LoadedModule;
        if (module == null)
        {
            assetValidationError = "No loaded module to add the asset to.";
            return;
        }

        if (module.ModuleAssets.Any(a => a.AssetID == newAssetID))
        {
            assetValidationError = $"An asset with ID '{newAssetID}' already exists.";
            return;
        }

        // Create and add new asset
        var newAsset = new ResConfigModuleAsset
        {
            AssetID = newAssetID,
            AssetPath = "Modules/" + ModKitState.LoadedModuleData.LoadedModule.ModuleName + "/" + newAssetPath + newAssetID + ".json",
            Type = newAssetType
        };

        module.ModuleAssets = module.ModuleAssets.Concat(new[] { newAsset }).ToArray();
        ModKitState.IsModuleDirty = true;

        // Save immediately
        ModKitResIO.SaveModuleToFile(ModKitState.LoadedModulePath, module);
        ModKitResIO.SaveAssetToFile(dir, newAssetID, newAssetType);

        // Reload module and UI to reflect changes
        ModKitState.ReloadModule();
        ResetModuleEditorState();
        GUI.FocusControl(null); // Deselect focused field

        Debug.Log($"Asset created: {newAssetID}");

        newAssetID = "";
        newAssetPath = "";
    }

}

// ---------- State Management ---------- //

public static class ModKitState
{
    public static string LoadedModulePath;
    public static bool IsModuleDirty;
    public static MKModuleData LoadedModuleData = null;

    // Currently selected asset (from ModKit Core window)
    public static ResConfigModuleAsset SelectedAsset = null;

    public static void LoadModule(string modulePath)
    {
        LoadedModulePath = modulePath;
        LoadedModuleData = ModKitResIO.LoadModuleFromFile(modulePath);
        IsModuleDirty = false;
        SelectedAsset = null;
    }

    public static void ReloadModule()
    {
        if (!string.IsNullOrEmpty(LoadedModulePath))
        {
            LoadModule(LoadedModulePath);
        }
    }
}
