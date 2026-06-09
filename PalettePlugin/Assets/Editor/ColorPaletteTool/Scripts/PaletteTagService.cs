using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Generates mood tags from palette colors inside Unity.
/// </summary>
public static class PaletteTagService
{
    const int MaxColorInput = 10;

    public static async Task<TagResult> GenerateTagsAsync(List<ColorEntry> colors)
    {
        await Task.Yield();

        if (colors == null || colors.Count == 0)
            return TagResult.Failed("Add at least one color before generating tags.");

        string[] moods = AnalyzeMood(colors.Take(MaxColorInput)).ToArray();
        return TagResult.Succeeded(moods.Length == 0 ? new[] { "neutral" } : moods);
    }

    static IEnumerable<string> AnalyzeMood(IEnumerable<ColorEntry> colors)
    {
        var moods = new HashSet<string>();

        foreach (var color in colors)
        {
            int r = Mathf.RoundToInt(color.r);
            int g = Mathf.RoundToInt(color.g);
            int b = Mathf.RoundToInt(color.b);
            float brightness = (r * 299f + g * 587f + b * 114f) / 1000f;

            if (brightness < 60f)
            {
                if (r > g && r > b)
                    moods.Add("gothic");
                else if (b > r && b > g)
                    moods.Add("mysterious");
                else
                    moods.Add("noir");
            }
            else if (brightness < 150f)
            {
                if (r > g && r > b)
                    moods.Add("romantic");
                else if (g > r && g > b)
                    moods.Add("natural");
                else if (b > r && b > g)
                    moods.Add("serene");
                else if (r > 150 && g > 150)
                    moods.Add("earthy");
                else
                    moods.Add("mysterious");
            }
            else
            {
                if (r > 200 && g < 100 && b < 100)
                    moods.Add("energetic");
                else if (r > 200 && g > 150)
                    moods.Add("whimsical");
                else if (b > 180 && r < 150)
                    moods.Add("serene");
                else if (r > 180 && b > 180 && g < 150)
                    moods.Add("fantasy");
                else
                    moods.Add("whimsical");
            }
        }

        return moods;
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
