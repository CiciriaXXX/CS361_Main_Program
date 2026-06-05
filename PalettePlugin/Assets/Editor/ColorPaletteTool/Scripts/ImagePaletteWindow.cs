using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

/// <summary>
/// Editor popup for creating a palette from an image URL.
/// It fetches a preview image, then sends the image bytes to the extractor service.
/// </summary>
public class ImagePaletteWindow : EditorWindow
{
    const string FetchUrl = "http://localhost:5100/fetch";
    const string ExtractUrl = "http://localhost:5002/extract";
    const string UssPath = "Assets/Editor/ColorPaletteTool/UI/USS/ColorPaletteTool.uss";

    ColorPaletteTool _owner;
    TextField _urlField;
    Label _errorLabel;
    Button _fetchButton;
    Button _createButton;
    Image _preview;
    byte[] _imageBytes;

    public static void Open(ColorPaletteTool owner)
    {
        var window = CreateInstance<ImagePaletteWindow>();
        window._owner = owner;
        window.titleContent = new GUIContent("Create From Image");
        window.minSize = new Vector2(360, 260);
        window.ShowUtility();
    }

    void CreateGUI()
    {
        var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        if (uss != null)
            rootVisualElement.styleSheets.Add(uss);

        rootVisualElement.AddToClassList("view-root");

        var title = new Label("Create Palette From Image");
        title.AddToClassList("page-title");
        rootVisualElement.Add(title);

        var label = new Label("IMAGE URL");
        label.AddToClassList("section-label");
        rootVisualElement.Add(label);

        _urlField = new TextField();
        _urlField.AddToClassList("input-field");
        rootVisualElement.Add(_urlField);

        _errorLabel = new Label();
        _errorLabel.AddToClassList("error-label");
        rootVisualElement.Add(_errorLabel);

        _fetchButton = new Button(() => _ = FetchPreviewAsync()) { text = "Fetch Image" };
        _fetchButton.AddToClassList("btn-secondary");
        rootVisualElement.Add(_fetchButton);

        _preview = new Image();
        _preview.AddToClassList("image-preview");
        rootVisualElement.Add(_preview);

        _createButton = new Button(() => _ = CreatePaletteAsync()) { text = "Create Palette" };
        _createButton.AddToClassList("btn-primary");
        _createButton.style.display = DisplayStyle.None;
        rootVisualElement.Add(_createButton);
    }

    async Task FetchPreviewAsync()
    {
        HideError();
        _createButton.style.display = DisplayStyle.None;
        _imageBytes = null;

        string imageUrl = _urlField.value?.Trim();
        if (string.IsNullOrEmpty(imageUrl))
        {
            ShowError("Image URL cannot be empty.");
            return;
        }

        SetBusy(true);

        string requestUrl = $"{FetchUrl}?image_url={UnityWebRequest.EscapeURL(imageUrl)}&width=300&fit=inside";
        using (var request = UnityWebRequest.Get(requestUrl))
        {
            await PaletteMicroserviceClient.SendAsync(request);

            if (request.result != UnityWebRequest.Result.Success)
            {
                ShowError(PaletteMicroserviceClient.ReadError(request));
                SetBusy(false);
                return;
            }

            _imageBytes = request.downloadHandler.data;
            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(_imageBytes))
            {
                ShowError("Fetched data is not a valid image.");
                SetBusy(false);
                return;
            }

            _preview.image = texture;
            _createButton.style.display = DisplayStyle.Flex;
        }

        SetBusy(false);
    }

    async Task CreatePaletteAsync()
    {
        if (_imageBytes == null)
        {
            ShowError("Fetch an image before creating a palette.");
            return;
        }

        HideError();
        SetBusy(true);

        var form = new WWWForm();
        form.AddBinaryData("image", _imageBytes, "palette_source.png", "image/png");
        form.AddField("count", "5");

        using (var request = UnityWebRequest.Post(ExtractUrl, form))
        {
            await PaletteMicroserviceClient.SendAsync(request);

            if (request.result != UnityWebRequest.Result.Success)
            {
                ShowError(PaletteMicroserviceClient.ReadError(request));
                SetBusy(false);
                return;
            }

            var response = JsonUtility.FromJson<ExtractColorsResponse>(request.downloadHandler.text);
            if (response == null || response.colors == null || response.colors.Length == 0)
            {
                ShowError("Image Extractor returned no colors.");
                SetBusy(false);
                return;
            }

            var colors = new List<ColorEntry>();
            foreach (var hex in response.colors)
                colors.Add(ColorEntry.FromHex(hex));

            _owner.ShowCreatePaletteFromColors("", colors);
            Close();
        }

        SetBusy(false);
    }

    void SetBusy(bool busy)
    {
        _fetchButton.SetEnabled(!busy);
        _createButton.SetEnabled(!busy);
    }

    void ShowError(string message)
    {
        _errorLabel.text = message;
        _errorLabel.AddToClassList("error-label--visible");
    }

    void HideError()
    {
        _errorLabel.RemoveFromClassList("error-label--visible");
    }

    [Serializable]
    class ExtractColorsResponse
    {
        public string[] colors;
    }
}
