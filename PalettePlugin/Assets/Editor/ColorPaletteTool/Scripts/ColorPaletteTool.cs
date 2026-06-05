using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ColorPaletteTool : EditorWindow
{
    [MenuItem("Window/Color Palette Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<ColorPaletteTool>();
        window.titleContent = new GUIContent("Color Palette Tool");
        window.minSize = new Vector2(420, 540);
    }

    const string UssPath = "Assets/Editor/ColorPaletteTool/UI/USS/ColorPaletteTool.uss";

    PaletteLibrary _library;

    void OnEnable()
    {
        _library = PaletteStorage.Load();
    }

    void CreateGUI()
    {
        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        if (uss != null)
            rootVisualElement.styleSheets.Add(uss);
        else
            Debug.LogWarning("[ColorPaletteTool] USS not found: " + UssPath);

        ShowPaletteList();
    }

    public void ShowPaletteList()
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new PaletteListView(_library, this));
    }

    public void ShowCreatePalette()
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new CreateEditPaletteView(_library, editingPalette: null, window: this));
    }

    public void ShowCreatePaletteFromColors(string paletteName, List<ColorEntry> colors)
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new CreateEditPaletteView(
            _library,
            editingPalette: null,
            window: this,
            initialName: paletteName,
            initialColors: colors));
    }

    public void ShowEditPalette(Palette palette)
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new CreateEditPaletteView(_library, editingPalette: palette, window: this));
    }

    public void SaveAndRefresh()
    {
        PaletteStorage.Save(_library);
        ShowPaletteList();
    }
}
