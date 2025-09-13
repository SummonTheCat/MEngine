using System.IO;

public static class UtilPaths
{
    // ---------- Path Utilities ---------- //

    public static string NormalizeJsonPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return string.Empty;

        string p = path.Replace('\\', '/').Trim();

        while (p.StartsWith("./")) p = p.Substring(2);
        while (p.StartsWith("/")) p = p.Substring(1);
        while (p.EndsWith("/") && p.Length > 1) p = p.Substring(0, p.Length - 1);
        while (p.Contains("//")) p = p.Replace("//", "/");

        return p;
    }

    public static string ToSystemPath(string jsonRelativePath)
    {
        string rel = NormalizeJsonPath(jsonRelativePath);
        string combined = Path.Combine(SysResource.Instance.gameDataPath, rel);
        return Path.GetFullPath(combined);
    }

    public static string ResolveAssetSystemPath(string assetJsonRelPath)
    {
        return ToSystemPath(assetJsonRelPath);
    }
}