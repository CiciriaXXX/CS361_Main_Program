using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

/// <summary>
/// Editor popup for creating a palette from an image URL.
/// </summary>
public class ImagePaletteWindow : EditorWindow
{
    const string UssPath = "Assets/Editor/ColorPaletteTool/UI/USS/ColorPaletteTool.uss";
    const int PaletteColorCount = 5;
    const int MaxSamples = 4096;

    ColorPaletteTool _owner;
    TextField _urlField;
    Label _errorLabel;
    Button _fetchButton;
    Button _createButton;
    Image _preview;
    Texture2D _sourceTexture;

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
        rootVisualElement.Add(_urlField);

        _errorLabel = new Label();
        _errorLabel.AddToClassList("error-label");
        rootVisualElement.Add(_errorLabel);

        _fetchButton = new Button(() => _ = FetchPreviewAsync()) { text = "Fetch Image" };
        rootVisualElement.Add(_fetchButton);

        _preview = new Image();
        _preview.AddToClassList("image-preview");
        rootVisualElement.Add(_preview);

        _createButton = new Button(CreatePalette) { text = "Create Palette" };
        _createButton.style.display = DisplayStyle.None;
        rootVisualElement.Add(_createButton);
    }

    async Task FetchPreviewAsync()
    {
        HideError();
        _createButton.style.display = DisplayStyle.None;
        SetSourceTexture(null);

        string imageUrl = _urlField.value?.Trim();
        if (string.IsNullOrEmpty(imageUrl))
        {
            ShowError("Image URL cannot be empty.");
            return;
        }

        if (!imageUrl.StartsWith("http://") && !imageUrl.StartsWith("https://"))
        {
            ShowError("Image URL must start with http:// or https://.");
            return;
        }

        SetBusy(true);

        using (var request = UnityWebRequest.Get(imageUrl))
        {
            request.timeout = 10;
            await SendAsync(request);

            if (request.result != UnityWebRequest.Result.Success)
            {
                ShowError(string.IsNullOrEmpty(request.error) ? "Could not fetch image." : request.error);
                SetBusy(false);
                return;
            }

            var texture = new Texture2D(2, 2);
            if (!texture.LoadImage(request.downloadHandler.data))
            {
                DestroyImmediate(texture);
                ShowError("Fetched data is not a valid image.");
                SetBusy(false);
                return;
            }

            SetSourceTexture(texture);
            _preview.image = _sourceTexture;
            _createButton.style.display = DisplayStyle.Flex;
        }

        SetBusy(false);
    }

    void CreatePalette()
    {
        if (_sourceTexture == null)
        {
            ShowError("Fetch an image before creating a palette.");
            return;
        }

        HideError();

        var colors = ExtractDominantColors(_sourceTexture, PaletteColorCount);
        if (colors.Count == 0)
        {
            ShowError("No visible colors were found in this image.");
            return;
        }

        _owner.ShowCreatePaletteFromColors("", colors);
        Close();
    }

    static async Task SendAsync(UnityWebRequest request)
    {
        var operation = request.SendWebRequest();
        while (!operation.isDone)
            await Task.Yield();
    }

    static List<ColorEntry> ExtractDominantColors(Texture2D texture, int count)
    {
        Color32[] pixels = texture.GetPixels32();
        int stride = Mathf.Max(1, pixels.Length / MaxSamples);
        var buckets = new Dictionary<int, ColorBucket>();

        for (int i = 0; i < pixels.Length; i += stride)
        {
            Color32 pixel = pixels[i];
            if (pixel.a < 16)
                continue;

            int r = pixel.r >> 4;
            int g = pixel.g >> 4;
            int b = pixel.b >> 4;
            int key = (r << 8) | (g << 4) | b;

            if (!buckets.TryGetValue(key, out var bucket))
                bucket = new ColorBucket();

            bucket.Add(pixel);
            buckets[key] = bucket;
        }

        var ordered = buckets.Values.OrderByDescending(bucket => bucket.Count).ToList();
        var selected = new List<ColorEntry>();

        foreach (var bucket in ordered)
        {
            var candidate = bucket.ToColorEntry();
            if (selected.All(existing => ColorDistance(existing, candidate) > 35f))
                selected.Add(candidate);

            if (selected.Count == count)
                return selected;
        }

        foreach (var bucket in ordered)
        {
            if (selected.Count == count)
                break;

            var candidate = bucket.ToColorEntry();
            if (!selected.Any(existing => existing.ToHex() == candidate.ToHex()))
                selected.Add(candidate);
        }

        return selected;
    }

    static float ColorDistance(ColorEntry a, ColorEntry b)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return Mathf.Sqrt(dr * dr + dg * dg + db * db);
    }

    void SetSourceTexture(Texture2D texture)
    {
        if (_sourceTexture != null)
            DestroyImmediate(_sourceTexture);

        _sourceTexture = texture;
        if (_preview != null)
            _preview.image = texture;
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

    void OnDisable()
    {
        SetSourceTexture(null);
    }

    class ColorBucket
    {
        public int Count { get; private set; }
        int _r;
        int _g;
        int _b;

        public void Add(Color32 color)
        {
            Count++;
            _r += color.r;
            _g += color.g;
            _b += color.b;
        }

        public ColorEntry ToColorEntry()
        {
            return new ColorEntry
            {
                r = _r / (float)Count,
                g = _g / (float)Count,
                b = _b / (float)Count
            };
        }
    }
}
