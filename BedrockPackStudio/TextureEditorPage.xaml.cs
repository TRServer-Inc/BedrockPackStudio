using Microsoft.Maui.Graphics;

namespace BedrockPackStudio;

public partial class TextureEditorPage : ContentPage
{
    private PixelData _pixelData;

    private readonly Stack<Color[,]> _undo = new();
    private readonly Stack<Color[,]> _redo = new();

    private Color _currentColor =
        Colors.White;

    private EditTool _tool =
        EditTool.Pen;

    private bool _grid = true;

    public TextureEditorPage()
    {
        InitializeComponent();

        _pixelData =
            new PixelData(16, 16);

        PixelCanvas.Drawable =
            new PixelDrawable(
                _pixelData);

        PixelCanvas.StartInteraction +=
            OnCanvasInteraction;

        LoadTexture();
    }

    private async void LoadTexture()
    {
        string? path =
            ProjectContext.CurrentTexturePath;

        if (string.IsNullOrWhiteSpace(path) ||
            !File.Exists(path))
        {
            return;
        }

        FileNameLabel.Text =
            Path.GetFileName(path);

        await Task.CompletedTask;
    }

    // =========================================================
    // CANVAS
    // =========================================================

    private void OnCanvasInteraction(
        object? sender,
        TouchEventArgs e)
    {
        if (e.Touches == null ||
            e.Touches.Count == 0)
            return;

        var touch =
            e.Touches[0];

        float xPosition =
            touch.X;

        float yPosition =
            touch.Y;

        float pixelSize =
            Math.Min(
                (float)PixelCanvas.Width / _pixelData.Width,
                (float)PixelCanvas.Height / _pixelData.Height);

        float startX =
            ((float)PixelCanvas.Width -
             pixelSize * _pixelData.Width) / 2f;

        float startY =
            ((float)PixelCanvas.Height -
             pixelSize * _pixelData.Height) / 2f;

        int x =
            (int)((xPosition - startX) /
                  pixelSize);

        int y =
            (int)((yPosition - startY) /
                  pixelSize);

        if (x < 0 ||
            y < 0 ||
            x >= _pixelData.Width ||
            y >= _pixelData.Height)
            return;

        if (_tool == EditTool.Picker)
        {
            _currentColor =
                _pixelData.Get(
                    x,
                    y);

            ColorEntry.Text =
                ToHex(_currentColor);

            _tool =
                EditTool.Pen;

            PixelCanvas.Invalidate();

            return;
        }

        SaveUndo();

        if (_tool == EditTool.Pen)
        {
            _pixelData.Set(
                x,
                y,
                _currentColor);
        }
        else if (_tool == EditTool.Eraser)
        {
            _pixelData.Set(
                x,
                y,
                Colors.Transparent);
        }

        PixelCanvas.Invalidate();
    }

    // =========================================================
    // TOOLS
    // =========================================================

    private void OnPenClicked(
        object sender,
        EventArgs e)
    {
        _tool =
            EditTool.Pen;
    }

    private void OnEraserClicked(
        object sender,
        EventArgs e)
    {
        _tool =
            EditTool.Eraser;
    }

    private void OnPickerClicked(
        object sender,
        EventArgs e)
    {
        _tool =
            EditTool.Picker;
    }

    private void OnFillClicked(
        object sender,
        EventArgs e)
    {
        SaveUndo();

        for (int y = 0;
             y < _pixelData.Height;
             y++)
        {
            for (int x = 0;
                 x < _pixelData.Width;
                 x++)
            {
                _pixelData.Set(
                    x,
                    y,
                    _currentColor);
            }
        }

        PixelCanvas.Invalidate();
    }

    private void OnColorClicked(
        object sender,
        EventArgs e)
    {
        ColorEntry.Focus();
    }

    private async void OnApplyColorClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            _currentColor =
                Color.FromArgb(
                    ColorEntry.Text
                        .Trim());

            await DisplayAlert(
                "Renk",
                $"Seçilen renk: {ColorEntry.Text}",
                "Tamam");
        }
        catch
        {
            await DisplayAlert(
                "Hatalı Renk",
                "Örnek: #FF0000 veya #FF0000FF",
                "Tamam");
        }
    }

    // =========================================================
    // UNDO / REDO
    // =========================================================

    private void SaveUndo()
    {
        _undo.Push(
            _pixelData.Clone());

        _redo.Clear();
    }

    private void OnUndoClicked(
        object sender,
        EventArgs e)
    {
        if (_undo.Count == 0)
            return;

        _redo.Push(
            _pixelData.Clone());

        _pixelData.CopyFrom(
            _undo.Pop());

        PixelCanvas.Invalidate();
    }

    private void OnRedoClicked(
        object sender,
        EventArgs e)
    {
        if (_redo.Count == 0)
            return;

        _undo.Push(
            _pixelData.Clone());

        _pixelData.CopyFrom(
            _redo.Pop());

        PixelCanvas.Invalidate();
    }

    // =========================================================
    // GRID
    // =========================================================

    private void OnGridClicked(
        object sender,
        EventArgs e)
    {
        _grid = !_grid;

        if (PixelCanvas.Drawable
            is PixelDrawable drawable)
        {
            drawable.ShowGrid =
                _grid;
        }

        PixelCanvas.Invalidate();
    }

    // =========================================================
    // SAVE
    // =========================================================

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        string? path =
            ProjectContext.CurrentTexturePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            await DisplayAlert(
                "Texture",
                "Kaydedilecek texture seçilmemiş.",
                "Tamam");

            return;
        }

        /*
         * Pixel buffer bellekte tutuluyor.
         * PNG export için Android tarafında
         * native bitmap kullanılması gerekiyor.
         *
         * Şimdilik editör durumunu kaybetmemek
         * için kullanıcıya bilgi veriyoruz.
         */

        await DisplayAlert(
            "Texture",
            "Pixel düzenlemeleri bellekte tutuluyor. PNG export katmanı bir sonraki adımda native Android Bitmap ile bağlanabilir.",
            "Tamam");
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private static string ToHex(
        Color color)
    {
        int r =
            (int)(color.Red * 255);

        int g =
            (int)(color.Green * 255);

        int b =
            (int)(color.Blue * 255);

        return
            $"#{r:X2}{g:X2}{b:X2}";
    }
}

public enum EditTool
{
    Pen,
    Eraser,
    Fill,
    Picker
}
