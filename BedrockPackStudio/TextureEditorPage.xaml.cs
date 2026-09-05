using System;
using Microsoft.Maui.Graphics;

namespace BedrockPackStudio;

public partial class TextureEditorPage : ContentPage
{
    private readonly PixelData _pixelData;

    private readonly Stack<Color[,]> _undo =
        new();

    private readonly Stack<Color[,]> _redo =
        new();

    private Color _currentColor = Colors.White;

    private EditTool _tool = EditTool.Pen;

    public TextureEditorPage()
    {
        InitializeComponent();

        _pixelData = new PixelData(16, 16);

        PixelCanvas.Drawable =
            new PixelDrawable(_pixelData);

        PixelCanvas.StartInteraction +=
            OnCanvasInteraction;
    }

    // =====================================================
    // CANVAS
    // =====================================================

    private void OnCanvasInteraction(
        object? sender,
        TouchEventArgs e)
    {
        if (e.Touches.Count == 0)
            return;

        var touch = e.Touches[0];

        float canvasX = touch.X;
        float canvasY = touch.Y;

        float pixelSize =
            Math.Min(
                (float)PixelCanvas.Width / 16f,
                (float)PixelCanvas.Height / 16f
            );

        float startX =
            (float)(PixelCanvas.Width - pixelSize * 16) / 2;

        float startY =
            (float)(PixelCanvas.Height - pixelSize * 16) / 2;

        int x =
            (int)((canvasX - startX) / pixelSize);

        int y =
            (int)((canvasY - startY) / pixelSize);

        if (x < 0 || x >= 16 ||
            y < 0 || y >= 16)
            return;

        SaveUndo();

        switch (_tool)
        {
            case EditTool.Pen:
                _pixelData.Set(
                    x,
                    y,
                    _currentColor
                );
                break;

            case EditTool.Eraser:
                _pixelData.Set(
                    x,
                    y,
                    Colors.Transparent
                );
                break;

            case EditTool.Picker:

                _currentColor =
                    _pixelData.Get(x, y);

                ColorEntry.Text =
                    ToHex(_currentColor);

                _tool = EditTool.Pen;

                break;
        }

        PixelCanvas.Invalidate();
    }

    // =====================================================
    // TOOLS
    // =====================================================

    private void OnPenClicked(
        object sender,
        EventArgs e)
    {
        _tool = EditTool.Pen;
    }

    private void OnEraserClicked(
        object sender,
        EventArgs e)
    {
        _tool = EditTool.Eraser;
    }

    private void OnFillClicked(
        object sender,
        EventArgs e)
    {
        SaveUndo();

        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                _pixelData.Set(
                    x,
                    y,
                    _currentColor
                );
            }
        }

        PixelCanvas.Invalidate();
    }

    private void OnPickerClicked(
        object sender,
        EventArgs e)
    {
        _tool = EditTool.Picker;
    }

    private void OnColorClicked(
        object sender,
        EventArgs e)
    {
        ColorEntry.Focus();
    }

    private void OnApplyColorClicked(
        object sender,
        EventArgs e)
    {
        try
        {
            _currentColor =
                Color.FromArgb(
                    ColorEntry.Text.Trim()
                );
        }
        catch
        {
            DisplayAlert(
                "Hatalı Renk",
                "Örnek: #FF0000",
                "Tamam"
            );
        }
    }

    // =====================================================
    // UNDO
    // =====================================================

    private void SaveUndo()
    {
        _undo.Push(
            _pixelData.Clone()
        );

        _redo.Clear();
    }

    private void OnUndoClicked(
        object sender,
        EventArgs e)
    {
        if (_undo.Count == 0)
            return;

        _redo.Push(
            _pixelData.Clone()
        );

        _pixelData.CopyFrom(
            _undo.Pop()
        );

        PixelCanvas.Invalidate();
    }

    private void OnRedoClicked(
        object sender,
        EventArgs e)
    {
        if (_redo.Count == 0)
            return;

        _undo.Push(
            _pixelData.Clone()
        );

        _pixelData.CopyFrom(
            _redo.Pop()
        );

        PixelCanvas.Invalidate();
    }

    // =====================================================
    // GRID
    // =====================================================

    private void OnGridClicked(
        object sender,
        EventArgs e)
    {
        if (PixelCanvas.Drawable is PixelDrawable drawable)
        {
            drawable.ShowGrid =
                !drawable.ShowGrid;

            PixelCanvas.Invalidate();
        }
    }

    // =====================================================
    // SAVE
    // =====================================================

    private async void OnSaveClicked(
        object sender,
        EventArgs e)
    {
        await DisplayAlert(
            "Kaydedildi",
            "Texture bellekte kaydedildi. PNG export sonraki aşamada eklenecek.",
            "Tamam"
        );
    }

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // =====================================================
    // COLOR → HEX
    // =====================================================

    private static string ToHex(Color color)
    {
        int r = (int)(color.Red * 255);
        int g = (int)(color.Green * 255);
        int b = (int)(color.Blue * 255);

        return $"#{r:X2}{g:X2}{b:X2}";
    }
}

public enum EditTool
{
    Pen,
    Eraser,
    Fill,
    Picker
}
