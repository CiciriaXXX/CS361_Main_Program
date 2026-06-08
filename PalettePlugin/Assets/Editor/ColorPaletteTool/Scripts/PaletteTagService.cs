using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles tag generation and output for the local Tag Generator microservice.
/// </summary>
public static class PaletteTagService
{
    const string TagUrl = "http://localhost:5300/analyze";
    const int MaxColorInput = 10;

    public static async Task<TagResult> GenerateTagsAsync(List<ColorEntry> colors)
    {
        // build  request colors(up to 10 colors)
        string[] valid_colors = colors.Take(MaxColorInput).Select(c => "#" + c.ToHex()).ToArray();
        string json = JsonUtility.ToJson(new TagRequest
        {
            colors = valid_colors
        });
        
        // send request and parse  response
        using (var request = new UnityWebRequest(TagUrl, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await PaletteMicroserviceClient.SendAsync(request);
            
            // if request failed, return error
            if (request.result != UnityWebRequest.Result.Success)
                return TagResult.Failed(PaletteMicroserviceClient.ReadError(request));
            
            // parse response
            var response = JsonUtility.FromJson<TagResponse>(request.downloadHandler.text);
            
            // if no moods returned, return error
            if (response == null || response.moods == null)
            {
                return TagResult.Failed("Microservice returned an empty or invalid response.");
            }

            return TagResult.Succeeded(response.moods);
        }
    }
    

    [Serializable]
    public class TagRequest
    {
        public string[] colors;
    }
    [Serializable]
    public class TagResponse
    {
        public string[] moods;
    }
}

public class TagResult
{
    public bool success;
    public string error;
    public string[] moods;

    public static TagResult Succeeded(string[] moods)
    {
        return new TagResult
        {
            success = true,
            moods = moods
        };
    }

    public static TagResult Failed(string error)
    {
        return new TagResult
        {
            success = false,
            error = error
        };
    }
}