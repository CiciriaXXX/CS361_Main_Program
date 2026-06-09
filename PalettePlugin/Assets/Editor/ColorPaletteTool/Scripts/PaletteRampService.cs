using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Handles ramp generation and file output inside Unity.
/// </summary>
public static class PaletteRampService
{
    const string RampFolder = "Assets/Texture/Ramp";
    const int RampWidth = 256;
    const int RampHeight = 1;

    public static string OutputFolder => RampFolder;

    public static async Task<RampResult> CreateRampAsync(string paletteName, List<ColorEntry> colors)
    {
        await Task.Yield();

        if (colors == null || colors.Count < 2)
            return RampResult.Failed("At least 2 colors are required to create a ramp.");

        try
        {
            var texture = GenerateRampTexture(colors);
            byte[] pngBytes = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);

            if (pngBytes == null || pngBytes.Length == 0)
                return RampResult.Failed("Could not encode ramp texture.");

            string assetPath = GetUniqueRampPath(paletteName);
            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, pngBytes);
            AssetDatabase.Refresh();

            return RampResult.Succeeded(assetPath, pngBytes);
        }
        catch (Exception ex)
        {
            return RampResult.Failed(ex.Message);
        }
    }

    static Texture2D GenerateRampTexture(List<ColorEntry> colors)
    {
        var texture = new Texture2D(RampWidth, RampHeight, TextureFormat.RGBA32, false);
        int stopCount = colors.Count;

        for (int x = 0; x < RampWidth; x++)
        {
            float position = stopCount == 1 ? 0f : (x / (float)(RampWidth - 1)) * (stopCount - 1);
            int left = Mathf.Clamp(Mathf.FloorToInt(position), 0, stopCount - 2);
            float t = position - left;
            Color color = Color.Lerp(colors[left].ToUnityColor(), colors[left + 1].ToUnityColor(), t);
            texture.SetPixel(x, 0, color);
        }

        texture.Apply();
        return texture;
    }

    static string GetUniqueRampPath(string paletteName)
    {
        string baseName = string.IsNullOrWhiteSpace(paletteName) ? "palette_ramp" : SanitizeFileName(paletteName);
        string path = $"{RampFolder}/{baseName}.png";
        int index = 1;

        while (File.Exists(Path.GetFullPath(path)))
        {
            path = $"{RampFolder}/{baseName}_{index}.png";
            index++;
        }

        return path;
    }

    static string SanitizeFileName(string fileName)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            fileName = fileName.Replace(invalid, '_');

        return string.IsNullOrWhiteSpace(fileName) ? "palette_ramp" : fileName.Trim();
    }
}

public class RampResult
{
    public bool success;
    public string error;
    public string assetPath;
    public byte[] pngBytes;

    public static RampResult Succeeded(string assetPath, byte[] pngBytes)
    {
        return new RampResult
        {
            success = true,
            assetPath = assetPath,
            pngBytes = pngBytes
        };
    }

    public static RampResult Failed(string error)
    {
        return new RampResult
        {
            success = false,
            error = error
        };
    }
}
