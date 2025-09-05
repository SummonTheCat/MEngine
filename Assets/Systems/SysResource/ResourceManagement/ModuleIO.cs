using System.IO;
using UnityEngine;

public static class ModuleIO
{
    public static void ApplyGameModules()
    {
        SysResource sysRes = SysResource.Instance;

        if (sysRes.GameConfig == null || sysRes.GameConfig.ModulePaths == null || sysRes.GameConfig.ModulePaths.Length == 0)
        {
            Debug.LogWarning("No module paths defined in GameConfig.");
            sysRes.LoadedModuleMaps = new ResConfigModule[0];
            return;
        }

        sysRes.LoadedModuleMaps = new ResConfigModule[sysRes.GameConfig.ModulePaths.Length];

        for (int i = 0; i < sysRes.GameConfig.ModulePaths.Length; i++)
        {
            string moduleJsonRel = UtilPaths.NormalizeJsonPath(sysRes.GameConfig.ModulePaths[i]);
            string moduleSysDir = UtilPaths.ToSystemPath(moduleJsonRel);
            string moduleSysFile = Path.Combine(moduleSysDir, "Module.json");

            if (!Directory.Exists(moduleSysDir))
            {
                Directory.CreateDirectory(moduleSysDir);
                Debug.Log("Created Module Directory: " + moduleSysDir);
            }

            sysRes.LoadedModuleMaps[i] = File.Exists(moduleSysFile)
                ? LoadModuleConfig(moduleSysFile)
                : CreateModuleConfig(moduleSysFile, moduleJsonRel);
        }
    }

    // ---------- Module Utilities ---------- //

    private static ResConfigModule LoadModuleConfig(string moduleSysFile)
    {
        string json = File.ReadAllText(moduleSysFile);
        var moduleConfig = JsonUtility.FromJson<ResConfigModule>(json);

        if (moduleConfig?.ModuleAssets != null)
        {
            for (int a = 0; a < moduleConfig.ModuleAssets.Length; a++)
            {
                moduleConfig.ModuleAssets[a].AssetPath =
                    UtilPaths.NormalizeJsonPath(moduleConfig.ModuleAssets[a].AssetPath);
            }

            string normalized = JsonUtility.ToJson(moduleConfig, true);
            File.WriteAllText(moduleSysFile, normalized);
        }

        Debug.Log("Loaded Module Config: " + moduleSysFile);
        return moduleConfig;
    }

    private static ResConfigModule CreateModuleConfig(string moduleSysFile, string moduleJsonRel)
    {
        Debug.Log("Module Config not found, creating default at: " + moduleSysFile);

        var defaultAtlasJsonRel = UtilPaths.NormalizeJsonPath($"{moduleJsonRel}/Textures/DefaultAtlas.png");
        string moduleName = Path.GetFileNameWithoutExtension(moduleSysFile);

        var moduleConfig = new ResConfigModule
        {
            ModuleName = $"{moduleName}",
            Version = "1.0",
            ModuleAssets = new[]
            {
                new ResConfigModuleAsset
                {
                    AssetID = "DefaultAsset",
                    Type = AssetType.TextureAtlas,
                    AssetPath = defaultAtlasJsonRel
                }
            }
        };

        string json = JsonUtility.ToJson(moduleConfig, true);
        File.WriteAllText(moduleSysFile, json);

        return moduleConfig;
    }
}