using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CreateEditPaletteView : VisualElement
{
    const string UxmlPath = "Assets/Editor/ColorPaletteTool/UI/UXML/CreateEditPaletteView.uxml";

    PaletteLibrary _library;
    Palette _editingPalette;
    ColorPaletteTool _window;

    bool IsEditMode => _editingPalette != null;

    string _paletteName = "";
    List<ColorEntry> _colors = new List<ColorEntry>();

    float _r = 200, _g = 80, _b = 50;

    TextField _nameField;
    Label _nameError;
    Label _nameNotice;
    Slider _sliderR, _sliderG, _sliderB;
    Label _sliderRValue, _sliderGValue, _sliderBValue;
    TextField _hexField;
    Label _hexError;
    Label _addNotice;
    VisualElement _colorPreview;
    VisualElement _swatchContainer;
    Label _swatchNotice;
    Button _rampButton;
    Label _rampError;
    Label _rampSuccess;
    Image _rampPreview;

    public CreateEditPaletteView(
        PaletteLibrary library,
        Palette editingPalette,
        ColorPaletteTool window,
        string initialName = "",
        List<ColorEntry> initialColors = null)
    {
        _library = library;
        _editingPalette = editingPalette;
        _window = window;

        if (IsEditMode)
        {
            _paletteName = _editingPalette.name;
            _colors = CloneColors(_editingPalette.colors);
        }
        else
        {
            _paletteName = initialName ?? "";
            _colors = initialColors != null ? CloneColors(initialColors) : new List<ColorEntry>();
        }

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
        container.Q<Button>("backBtn").clicked += () => _window.ShowPaletteList();

        container.Q<Label>("pageTitle").text = IsEditMode ? "Edit Palette" : "Create Palette";
        var editingTag = container.Q<Label>("editingTag");
        if (IsEditMode)
        {
            editingTag.text = $"editing: {_editingPalette.name}";
            editingTag.style.display = DisplayStyle.Flex;
        }
        else
        {
            editingTag.style.display = DisplayStyle.None;
        }

        _nameError = container.Q<Label>("nameError");
        _nameNotice = container.Q<Label>("nameNotice");

        _nameField = container.Q<TextField>("nameField");
        _nameField.value = _paletteName;
        _nameField.RegisterValueChangedCallback(evt =>
        {
            _paletteName = evt.newValue;
            HideError(_nameError);
            _nameNotice.style.display = DisplayStyle.Flex;
        });

        _sliderRValue = container.Q<Label>("sliderRValue");
        _sliderGValue = container.Q<Label>("sliderGValue");
        _sliderBValue = container.Q<Label>("sliderBValue");

        _sliderR = BindSlider(container, "sliderR", _sliderRValue, v => { _r = v; OnSliderChanged(); });
        _sliderG = BindSlider(container, "sliderG", _sliderGValue, v => { _g = v; OnSliderChanged(); });
        _sliderB = BindSlider(container, "sliderB", _sliderBValue, v => { _b = v; OnSliderChanged(); });

        _sliderR.value = _r;
        _sliderG.value = _g;
        _sliderB.value = _b;

        _hexError = container.Q<Label>("hexError");
        _addNotice = container.Q<Label>("addNotice");
        _hexField = container.Q<TextField>("hexField");
        _hexField.value = RgbToHex(_r, _g, _b);
        _hexField.RegisterValueChangedCallback(_ =>
        {
            HideError(_hexError);
            _addNotice.style.display = DisplayStyle.Flex;
        });
        _hexField.RegisterCallback<BlurEvent>(_ => ValidateAndApplyHex());

        _colorPreview = container.Q<VisualElement>("colorPreview");
        UpdatePreview();

        container.Q<Button>("addColorBtn").clicked += AddColor;

        _swatchNotice = container.Q<Label>("swatchNotice");
        _swatchContainer = container.Q<VisualElement>("swatchContainer");
        RefreshSwatches();

        container.Q<Button>("saveBtn").clicked += TrySave;

        _rampButton = container.Q<Button>("createRampBtn");
        _rampError = container.Q<Label>("rampError");
        _rampSuccess = container.Q<Label>("rampSuccess");
        _rampPreview = container.Q<Image>("rampPreview");
        _rampButton.clicked += () => _ = CreateRampAsync();
    }

    Slider BindSlider(VisualElement container, string sliderName, Label valueLabel, Action<float> onChanged)
    {
        var slider = container.Q<Slider>(sliderName);
        slider.RegisterValueChangedCallback(evt =>
        {
            valueLabel.text = Mathf.RoundToInt(evt.newValue).ToString();
            onChanged(evt.newValue);
        });
        return slider;
    }

    void OnSliderChanged()
    {
        _hexField.SetValueWithoutNotify(RgbToHex(_r, _g, _b));
        HideError(_hexError);
        _addNotice.style.display = DisplayStyle.Flex;
        UpdatePreview();
    }

    bool ValidateAndApplyHex()
    {
        string hex = _hexField.value.Trim().TrimStart('#').ToUpper();
        if (!Regex.IsMatch(hex, @"^[0-9A-F]{6}$"))
        {
            _addNotice.style.display = DisplayStyle.None;
            ShowError(_hexError);
            return false;
        }

        HideError(_hexError);

        _r = Convert.ToInt32(hex.Substring(0, 2), 16);
        _g = Convert.ToInt32(hex.Substring(2, 2), 16);
        _b = Convert.ToInt32(hex.Substring(4, 2), 16);

        _sliderR.SetValueWithoutNotify(_r);
        _sliderG.SetValueWithoutNotify(_g);
        _sliderB.SetValueWithoutNotify(_b);
        _sliderRValue.text = Mathf.RoundToInt(_r).ToString();
        _sliderGValue.text = Mathf.RoundToInt(_g).ToString();
        _sliderBValue.text = Mathf.RoundToInt(_b).ToString();

        UpdatePreview();
        return true;
    }

    void UpdatePreview()
    {
        _colorPreview.style.backgroundColor = new Color(_r / 255f, _g / 255f, _b / 255f);
    }

    void AddColor()
    {
        if (!ValidateAndApplyHex())
            return;

        _colors.Add(new ColorEntry { r = _r, g = _g, b = _b });
        RefreshSwatches();
    }

    void RefreshSwatches()
    {
        _swatchContainer.Clear();
        _swatchNotice.style.display = _colors.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;

        for (int i = 0; i < _colors.Count; i++)
        {
            int index = i;
            var color = _colors[i];

            var swatch = new VisualElement();
            swatch.AddToClassList("added-swatch");
            swatch.style.backgroundColor = color.ToUnityColor();
            swatch.tooltip = $"#{color.ToHex()}\nRight Click to remove";

            swatch.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    _colors.RemoveAt(index);
                    RefreshSwatches();
                }
            });

            _swatchContainer.Add(swatch);
        }
    }

    void TrySave()
    {
        if (string.IsNullOrWhiteSpace(_paletteName))
        {
            _nameError.text = "Palette name cannot be empty";
            ShowError(_nameError);
            _nameNotice.style.display = DisplayStyle.None;
            return;
        }

        bool nameConflict = _library.palettes.Exists(p =>
            p.name == _paletteName && p != _editingPalette);
        if (nameConflict)
        {
            _nameError.text = "A palette with this name already exists";
            ShowError(_nameError);
            _nameNotice.style.display = DisplayStyle.None;
            return;
        }

        if (IsEditMode)
        {
            _editingPalette.name = _paletteName;
            _editingPalette.colors = _colors;
        }
        else
        {
            _library.palettes.Add(new Palette
            {
                name = _paletteName,
                colors = _colors
            });
        }

        _window.SaveAndRefresh();
    }

    async Task CreateRampAsync()
    {
        // The view validates UI state; PaletteRampService owns the microservice call and file output.
        HideError(_rampError);
        _rampSuccess.style.display = DisplayStyle.None;

        if (_colors.Count < 2)
        {
            _rampError.text = "At least 2 colors are required to create a ramp.";
            ShowError(_rampError);
            return;
        }

        _rampButton.SetEnabled(false);

        RampResult result = await PaletteRampService.CreateRampAsync(_paletteName, _colors);
        _rampButton.SetEnabled(true);

        if (!result.success)
        {
            _rampError.text = result.error;
            ShowError(_rampError);
            return;
        }

        var texture = new Texture2D(2, 2);
        texture.LoadImage(result.pngBytes);
        _rampPreview.image = texture;
        _rampPreview.style.display = DisplayStyle.Flex;

        _rampSuccess.text = $"Success, image saved to {PaletteRampService.OutputFolder}";
        _rampSuccess.style.display = DisplayStyle.Flex;
    }

    static List<ColorEntry> CloneColors(List<ColorEntry> colors)
    {
        var clone = new List<ColorEntry>();
        foreach (var c in colors)
            clone.Add(new ColorEntry { r = c.r, g = c.g, b = c.b });

        return clone;
    }

    static string RgbToHex(float r, float g, float b)
    {
        return $"{Mathf.RoundToInt(r):X2}{Mathf.RoundToInt(g):X2}{Mathf.RoundToInt(b):X2}";
    }

    static void ShowError(Label label)
    {
        label.AddToClassList("error-label--visible");
    }

    static void HideError(Label label)
    {
        label.RemoveFromClassList("error-label--visible");
    }

}
