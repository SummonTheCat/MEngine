using System;
using System.IO;
using UnityEngine;

public static class ConfigIO
{
    public static void ApplyGameConfig()
    {
        SysResource sysRes = SysResource.Instance;

        const string configJsonRelPath = "Config/Game.json";
        string configSysPath = UtilPaths.ToSystemPath(configJsonRelPath);
        string configDirSys = Path.GetDirectoryName(configSysPath);

        if (!Directory.Exists(configDirSys))
        {
            Directory.CreateDirectory(configDirSys);
            Debug.Log("Created Game Config Directory: " + configDirSys);
        }

        if (File.Exists(configSysPath))
        {
            sysRes.GameConfig = LoadGameConfig(configSysPath);
        }
        else
        {
            sysRes.GameConfig = CreateConfig(configSysPath, configJsonRelPath);
        }

        Debug.Log("Loaded Game Config (system path): " + configSysPath);
    }

    // ---------- Config Utilities ---------- //

    private static ResConfigGame LoadGameConfig(string configSysPath)
    {
        string json = File.ReadAllText(configSysPath);
        var config = JsonUtility.FromJson<ResConfigGame>(json);

        if (config?.ModulePaths != null)
        {
            for (int i = 0; i < config.ModulePaths.Length; i++)
            {
                config.ModulePaths[i] = UtilPaths.NormalizeJsonPath(config.ModulePaths[i]);
            }

            string normalized = JsonUtility.ToJson(config, true);
            File.WriteAllText(configSysPath, normalized);
        }

        return config;
    }

    private static ResConfigGame CreateConfig(string configSysPath, string configJsonRelPath)
    {
        Debug.Log("Game Config not found, creating default at (JSON path): " + configJsonRelPath);

        var config = new ResConfigGame
        {
            GameName = "DefaultGame",
            Version = "1.0",
            ModulePaths = new[] { "Modules/DevMod" }
        };

        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(configSysPath, json);

        return config;
    }

}