using System;
using UnityEngine;

[Serializable]
public class ColorEntry
{
    public float r; // 0-255
    public float g;
    public float b;

    public string ToHex()
    {
        int ri = Mathf.RoundToInt(r);
        int gi = Mathf.RoundToInt(g);
        int bi = Mathf.RoundToInt(b);
        return $"{ri:X2}{gi:X2}{bi:X2}";
    }

    public Color ToUnityColor() => new Color(r / 255f, g / 255f, b / 255f);

    public static ColorEntry FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        return new ColorEntry
        {
            r = Convert.ToInt32(hex.Substring(0, 2), 16),
            g = Convert.ToInt32(hex.Substring(2, 2), 16),
            b = Convert.ToInt32(hex.Substring(4, 2), 16),
        };
    }
}
