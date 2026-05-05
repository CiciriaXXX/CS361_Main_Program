using System.IO;
using UnityEngine;

public static class PaletteStorage
{
    // 存在 Library/ 下，不进版本控制，但随项目本地持久化
    static string FilePath =>
        Path.Combine(Application.dataPath, "../Library/ColorPalettes.json");

    public static PaletteLibrary Load()
    {
        if (!File.Exists(FilePath))
            return new PaletteLibrary();

        try
        {
            string json = File.ReadAllText(FilePath);
            return JsonUtility.FromJson<PaletteLibrary>(json) ?? new PaletteLibrary();
        }
        catch
        {
            Debug.LogWarning("[ColorPaletteTool] Failed to load palette data, starting fresh.");
            return new PaletteLibrary();
        }
    }

    public static void Save(PaletteLibrary library)
    {
        string json = JsonUtility.ToJson(library, prettyPrint: true);
        File.WriteAllText(FilePath, json);
    }
}
