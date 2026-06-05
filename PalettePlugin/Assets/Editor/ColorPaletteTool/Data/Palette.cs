using System;
using System.Collections.Generic;

[Serializable]
public class Palette
{
    public string name = "";
    public List<ColorEntry> colors = new List<ColorEntry>();
    public List<string> tags = new List<string>();


    public Palette Clone()
    {
        var clone = new Palette { name = name };
        foreach (var c in colors)
            clone.colors.Add(new ColorEntry { r = c.r, g = c.g, b = c.b });
        foreach (var t in tags)
            clone.tags.Add(t);
        return clone;
    }
}

[Serializable]
public class PaletteLibrary
{
    public List<Palette> palettes = new List<Palette>();
}
