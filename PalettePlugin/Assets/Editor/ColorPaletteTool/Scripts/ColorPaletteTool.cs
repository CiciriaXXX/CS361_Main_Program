using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 编辑器窗口入口。负责窗口生命周期和视图切换。
/// 菜单路径：Window → Color Palette Tool
/// </summary>
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

    // CreateGUI 是 UI Toolkit 推荐的入口，比 OnEnable 更晚执行，
    // 此时 AssetDatabase 已经准备好，USS 加载不会返回 null
    void CreateGUI()
    {
        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        if (uss != null)
            rootVisualElement.styleSheets.Add(uss);
        else
            Debug.LogWarning("[ColorPaletteTool] 找不到 USS，请确认路径: " + UssPath);

        ShowPaletteList();
    }

    // ── 视图切换 ──────────────────────────────────────────

    public void ShowPaletteList()
    {
        rootVisualElement.Clear();
        var view = new PaletteListView(_library, this);
        rootVisualElement.Add(new PaletteListView(_library, this));
    }

    public void ShowCreatePalette()
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new CreateEditPaletteView(_library, editingPalette: null, window: this));
    }

    public void ShowEditPalette(Palette palette)
    {
        rootVisualElement.Clear();
        rootVisualElement.Add(new CreateEditPaletteView(_library, editingPalette: palette, window: this));
    }

    /// <summary>保存到磁盘并返回列表视图。</summary>
    public void SaveAndRefresh()
    {
        PaletteStorage.Save(_library);
        ShowPaletteList();
    }
}
