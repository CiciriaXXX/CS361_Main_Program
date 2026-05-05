using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 视图1：调色板列表。
/// 负责加载 UXML、绑定事件、动态生成列表行。
/// </summary>
public class PaletteListView : VisualElement
{
    // UXML 文件路径（相对于 Assets/）
    const string UxmlPath = "Assets/Editor/ColorPaletteTool/UI/UXML/PaletteListView.uxml";
    const string RowTemplatePath = "Assets/Editor/ColorPaletteTool/UI/UXML/PaletteRowItem.uxml";

    PaletteLibrary _library;
    ColorPaletteTool _window;

    public PaletteListView(PaletteLibrary library, ColorPaletteTool window)
    {
        _library = library;
        _window = window;

        // 加载并克隆 UXML 结构
        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        if (uxml == null)
        {
            Debug.LogError($"[ColorPaletteTool] 找不到 UXML：{UxmlPath}");
            return;
        }
        var container = uxml.Instantiate();
        Add(container);

        BindUI(container);
    }

    void BindUI(VisualElement container)
    {
        // + New Palette 按钮
        container.Q<Button>("addPaletteBtn").clicked += () => _window.ShowCreatePalette();

        // 空状态提示
        var emptyHint = container.Q<Label>("emptyHint");
        var listContainer = container.Q<ScrollView>("paletteListContainer");

        bool isEmpty = _library.palettes.Count == 0;
        emptyHint.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;

        // 加载行模板
        var rowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RowTemplatePath);
        if (rowTemplate == null)
        {
            Debug.LogError($"[ColorPaletteTool] 找不到行模板：{RowTemplatePath}");
            return;
        }

        // 动态生成每一行
        foreach (var palette in _library.palettes)
        {
            // 必须用局部变量捕获，避免闭包全捕获到最后一个
            var pal = palette;

            var row = rowTemplate.CloneTree();

            // 填入名字
            row.Q<Label>("paletteName").text = pal.name;

            // 填入色块
            var swatchRow = row.Q<VisualElement>("swatchRow");
            foreach (var color in pal.colors)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("palette-row__swatch");
                swatch.style.backgroundColor = color.ToUnityColor();
                swatch.tooltip = $"#{color.ToHex()}";
                swatchRow.Add(swatch);
            }

            // 点击整行 → 进入编辑（排除 Delete 按钮的点击冒泡）
            var rowRoot = row.Q<VisualElement>("rowRoot");
            rowRoot.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is Button) return;
                _window.ShowEditPalette(pal);
            });

            // Delete 按钮
            row.Q<Button>("deleteBtn").clicked += () => ConfirmDelete(pal);

            listContainer.Add(row);
        }
    }

    void ConfirmDelete(Palette palette)
    {
        bool confirmed = EditorUtility.DisplayDialog(
            title: "Delete Palette",
            message: $"Are you sure you want to delete \"{palette.name}\"?\n\n" +
                     "This cannot be undone. The palette will be permanently removed " +
                     "from your local storage and will not reappear after restarting Unity.",
            ok: "Delete",
            cancel: "Cancel"
        );

        if (confirmed)
        {
            _library.palettes.Remove(palette);
            _window.SaveAndRefresh();
        }
    }
}
