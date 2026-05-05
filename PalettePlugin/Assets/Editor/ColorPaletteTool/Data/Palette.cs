using System;
using System.Collections.Generic;

[Serializable]
public class Palette
{
    public string name = "";
    public List<ColorEntry> colors = new List<ColorEntry>();

    // 深拷贝，用于Edit模式（Back时可丢弃修改）
    public Palette Clone()
    {
        var clone = new Palette { name = this.name };
        foreach (var c in colors)
            clone.colors.Add(new ColorEntry { r = c.r, g = c.g, b = c.b });
        return clone;
    }
}

[Serializable]
public class PaletteLibrary
{
    public List<Palette> palettes = new List<Palette>();
}
