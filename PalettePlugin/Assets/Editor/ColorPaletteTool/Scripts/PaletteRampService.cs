using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles ramp generation and file output for the local Ramp Generator microservice.
/// </summary>
public static class PaletteRampService
{
    const string RampUrl = "http://localhost:5001/ramp";
    const string RampFolder = "Assets/Texture/Ramp";

    public static string OutputFolder => RampFolder;

    public static async Task<RampResult> CreateRampAsync(string paletteName, List<ColorEntry> colors)
    {
        string json = JsonUtility.ToJson(new RampRequest
        {
            colors = colors.ConvertAll(c => "#" + c.ToHex()).ToArray()
        });

        using (var request = new UnityWebRequest(RampUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await PaletteMicroserviceClient.SendAsync(request);

            if (request.result != UnityWebRequest.Result.Success)
                return RampResult.Failed(PaletteMicroserviceClient.ReadError(request));

            byte[] pngBytes = request.downloadHandler.data;
            string assetPath = GetUniqueRampPath(paletteName);
            string fullPath = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.WriteAllBytes(fullPath, pngBytes);
            AssetDatabase.Refresh();

            return RampResult.Succeeded(assetPath, pngBytes);
        }
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

    [Serializable]
    class RampRequest
    {
        public string[] colors;
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
