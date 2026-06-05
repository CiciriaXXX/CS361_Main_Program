using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class PaletteListView : VisualElement
{
    const string UxmlPath = "Assets/Editor/ColorPaletteTool/UI/UXML/PaletteListView.uxml";
    const string RowTemplatePath = "Assets/Editor/ColorPaletteTool/UI/UXML/PaletteRowItem.uxml";
    const int MaxVisibleListSwatches = 30;

    PaletteLibrary _library;
    ColorPaletteTool _window;

    public PaletteListView(PaletteLibrary library, ColorPaletteTool window)
    {
        _library = library;
        _window = window;

        var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
        if (uxml == null)
        {
            Debug.LogError($"[ColorPaletteTool] UXML not found: {UxmlPath}");
            return;
        }

        var container = uxml.Instantiate();
        Add(container);
        BindUI(container);
    }

    void BindUI(VisualElement container)
    {
        container.Q<Button>("addPaletteBtn").clicked += ShowCreateMenu;

        var emptyHint = container.Q<Label>("emptyHint");
        var listContainer = container.Q<ScrollView>("paletteListContainer");

        bool isEmpty = _library.palettes.Count == 0;
        emptyHint.style.display = isEmpty ? DisplayStyle.Flex : DisplayStyle.None;

        var rowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(RowTemplatePath);
        if (rowTemplate == null)
        {
            Debug.LogError($"[ColorPaletteTool] Row template not found: {RowTemplatePath}");
            return;
        }

        foreach (var palette in _library.palettes)
        {
            var pal = palette;
            var row = rowTemplate.CloneTree();

            row.Q<Label>("paletteName").text = pal.name;

            var swatchRow = row.Q<VisualElement>("swatchRow");
            int visibleCount = Mathf.Min(pal.colors.Count, MaxVisibleListSwatches);
            for (int i = 0; i < visibleCount; i++)
            {
                var color = pal.colors[i];
                var swatch = new VisualElement();
                swatch.AddToClassList("palette-row__swatch");
                swatch.style.backgroundColor = color.ToUnityColor();
                swatch.tooltip = $"#{color.ToHex()}";
                swatchRow.Add(swatch);
            }
            
            var chipRow = row.Q<VisualElement>("chipRow");
            for (int i = 0; i < pal.tags.Count ; i++)
            {
                var tag = pal.tags[i];
                var chip = new VisualElement();
                chip.AddToClassList("palette-row__chip");
                var label = new Label(tag);
                label.AddToClassList("palette-chip__label");
                chip.Add (label);
                chipRow.Add(chip);
            }

            var rowRoot = row.Q<VisualElement>("rowRoot");
            rowRoot.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is Button)
                    return;

                _window.ShowEditPalette(pal);
            });

            row.Q<Button>("deleteBtn").clicked += () => ConfirmDelete(pal);
            listContainer.Add(row);
        }
    }

    void ShowCreateMenu()
    {
        // Keep creation choices in the list view, while the image workflow lives in its own window.
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("Create From Empty"), false, () => _window.ShowCreatePalette());
        menu.AddItem(new GUIContent("Create From Image"), false, () => ImagePaletteWindow.Open(_window));
        menu.ShowAsContext();
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
