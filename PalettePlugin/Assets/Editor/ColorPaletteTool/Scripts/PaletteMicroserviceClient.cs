using System;
using System.Threading.Tasks;
using UnityEngine.Networking;

/// <summary>
/// Small shared helpers for calling the local palette microservices from editor UI code.
/// </summary>
public static class PaletteMicroserviceClient
{
    public static async Task SendAsync(UnityWebRequest request)
    {
        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();
    }

    public static string ReadError(UnityWebRequest request)
    {
        string json = request.downloadHandler?.text;
        if (!string.IsNullOrEmpty(json))
        {
            var response = UnityEngine.JsonUtility.FromJson<ErrorResponse>(json);
            if (response != null && !string.IsNullOrEmpty(response.error))
                return response.error;
        }

        return string.IsNullOrEmpty(request.error) ? "Request failed." : request.error;
    }

    [Serializable]
    class ErrorResponse
    {
        public string error;
    }
}
