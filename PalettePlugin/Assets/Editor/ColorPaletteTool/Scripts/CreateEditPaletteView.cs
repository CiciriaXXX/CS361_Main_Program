using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 视图2（创建）和视图3（编辑）共用。
/// editingPalette == null → Create 模式；否则 → Edit 模式。
/// </summary>
public class CreateEditPaletteView : VisualElement
{
    const string UxmlPath = "Assets/Editor/ColorPaletteTool/UI/UXML/CreateEditPaletteView.uxml";

    // ── 依赖 ──────────────────────────────────────────────
    PaletteLibrary _library;
    Palette _editingPalette;   // null = Create 模式
    ColorPaletteTool _window;

    bool IsEditMode => _editingPalette != null;

    // ── 临时工作数据（不直接修改原对象，Back 时可丢弃） ──
    string _paletteName = "";
    List<ColorEntry> _colors = new List<ColorEntry>();

    float _r = 200, _g = 80, _b = 50;

    // ── UI 元素引用（绑定后缓存，避免重复 Q<>） ──────────
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

    public CreateEditPaletteView(PaletteLibrary library, Palette editingPalette, ColorPaletteTool window)
    {
        _library = library;
        _editingPalette = editingPalette;
        _window = window;

        if (IsEditMode)
        {
            // 深拷贝工作数据，Back 时原对象不变
            _paletteName = _editingPalette.name;
            _colors = new List<ColorEntry>();
            foreach (var c in _editingPalette.colors)
                _colors.Add(new ColorEntry { r = c.r, g = c.g, b = c.b });
        }

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

    // ─────────────────────────────────────────────────────
    //  绑定
    // ─────────────────────────────────────────────────────

    void BindUI(VisualElement container)
    {
        // Back
        container.Q<Button>("backBtn").clicked += () => _window.ShowPaletteList();

        // 页面标题 & editing tag
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

        // ── Palette Name ──
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

        // ── RGB 滑条 ──
        _sliderRValue = container.Q<Label>("sliderRValue");
        _sliderGValue = container.Q<Label>("sliderGValue");
        _sliderBValue = container.Q<Label>("sliderBValue");

        _sliderR = BindSlider(container, "sliderR", _sliderRValue, v => { _r = v; OnSliderChanged(); });
        _sliderG = BindSlider(container, "sliderG", _sliderGValue, v => { _g = v; OnSliderChanged(); });
        _sliderB = BindSlider(container, "sliderB", _sliderBValue, v => { _b = v; OnSliderChanged(); });

        _sliderR.value = _r;
        _sliderG.value = _g;
        _sliderB.value = _b;

        // ── HEX 输入 ──
        _hexError = container.Q<Label>("hexError");
        _addNotice = container.Q<Label>("addNotice");
        _hexField = container.Q<TextField>("hexField");
        _hexField.value = RgbToHex(_r, _g, _b);
        _hexField.RegisterValueChangedCallback(_ =>
        {
            HideError(_hexError);
            _addNotice.style.display = DisplayStyle.Flex;
        });
        // 失焦时验证并同步回滑条
        _hexField.RegisterCallback<BlurEvent>(_ => ValidateAndApplyHex());

        // ── 颜色预览 ──
        _colorPreview = container.Q<VisualElement>("colorPreview");
        UpdatePreview();

        // ── Add Color 按钮 ──
        container.Q<Button>("addColorBtn").clicked += AddColor;

        // ── 色块容器 ──
        _swatchNotice = container.Q<Label>("swatchNotice");
        _swatchContainer = container.Q<VisualElement>("swatchContainer");
        RefreshSwatches();

        // ── Save 按钮 ──
        container.Q<Button>("saveBtn").clicked += TrySave;
    }

    // 封装单条滑条的绑定，返回 Slider 引用
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

    // ─────────────────────────────────────────────────────
    //  事件处理
    // ─────────────────────────────────────────────────────

    void OnSliderChanged()
    {
        // 滑条改变 → 同步 HEX 输入框
        _hexField.SetValueWithoutNotify(RgbToHex(_r, _g, _b));
        HideError(_hexError);
        _addNotice.style.display = DisplayStyle.Flex;
        UpdatePreview();
    }

    void ValidateAndApplyHex()
    {
        string hex = _hexField.value.Trim().TrimStart('#').ToUpper();
        if (!Regex.IsMatch(hex, @"^[0-9A-F]{6}$"))
        {
            _addNotice.style.display = DisplayStyle.None;
            ShowError(_hexError);
            return;
        }
        HideError(_hexError);

        _r = Convert.ToInt32(hex.Substring(0, 2), 16);
        _g = Convert.ToInt32(hex.Substring(2, 2), 16);
        _b = Convert.ToInt32(hex.Substring(4, 2), 16);

        // 同步回三条滑条（不触发 ValueChanged 回调，避免循环）
        _sliderR.SetValueWithoutNotify(_r);
        _sliderG.SetValueWithoutNotify(_g);
        _sliderB.SetValueWithoutNotify(_b);
        _sliderRValue.text = Mathf.RoundToInt(_r).ToString();
        _sliderGValue.text = Mathf.RoundToInt(_g).ToString();
        _sliderBValue.text = Mathf.RoundToInt(_b).ToString();

        UpdatePreview();
    }

    void UpdatePreview()
    {
        _colorPreview.style.backgroundColor = new Color(_r / 255f, _g / 255f, _b / 255f);
    }

    void AddColor()
    {
        // 如果 HEX 当前有错误，先验证一次
        if (_hexError.ClassListContains("error-label--visible"))
            return;

        _colors.Add(new ColorEntry { r = _r, g = _g, b = _b });
        RefreshSwatches();
    }

    void RefreshSwatches()
    {
        _swatchContainer.Clear();
        // 如果色板为空显示提示
        if (_colors.Count == 0)
        {
            _swatchNotice.style.display = DisplayStyle.Flex;
        }
        else
        {
            _swatchNotice.style.display = DisplayStyle.None;
        }
        for (int i = 0; i < _colors.Count; i++)
        {
            int index = i; // 闭包捕获索引
            var color = _colors[i];

            var swatch = new VisualElement();
            swatch.AddToClassList("added-swatch");
            swatch.style.backgroundColor = color.ToUnityColor();
            swatch.tooltip = $"#{color.ToHex()}\nRight Click to remove";

            swatch.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 1) 
                {
                    // 这里执行右键逻辑
                    _colors.RemoveAt(index);
                    RefreshSwatches();
                }
            });
            _swatchContainer.Add(swatch);
        }
    }

    void TrySave()
    {
        // 验证：名字不能为空
        if (string.IsNullOrWhiteSpace(_paletteName))
        {
            _nameError.text = "⚠ Palette name cannot be empty";
            ShowError(_nameError);
            _nameNotice.style.display = DisplayStyle.None;
            return;
        }

        // 验证：名字唯一（Edit 模式排除自身）
        bool nameConflict = _library.palettes.Exists(p =>
            p.name == _paletteName && p != _editingPalette);
        if (nameConflict)
        {
            _nameError.text = "⚠ A palette with this name already exists";
            ShowError(_nameError);
            _nameNotice.style.display = DisplayStyle.None;
            return;
        }

        if (IsEditMode)
        {
            // 写回原对象
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

    // ─────────────────────────────────────────────────────
    //  工具方法
    // ─────────────────────────────────────────────────────

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
