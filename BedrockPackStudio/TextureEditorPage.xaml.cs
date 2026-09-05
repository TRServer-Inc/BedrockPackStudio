using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Maui.Graphics;

namespace BedrockPackStudio;

public partial class TextureEditorPage : ContentPage
{
    private readonly PixelData _pixelData;

    private readonly Stack<Color[,]> _undo =
        new();

    private readonly Stack<Color[,]> _redo =
        new();

    private Color _currentColor =
        Colors.White;

    private EditTool _tool =
        EditTool.Pen;

    public TextureEditorPage()
    {
        InitializeComponent();

        _pixelData =
            new PixelData(
                16,
                16);

        PixelCanvas.Drawable =
            new PixelDrawable(
                _pixelData);

        PixelCanvas.StartInteraction +=
            OnCanvasInteraction;

        LoadTextureInfo();
    }

    // =========================================================
    // LOAD
    // =========================================================

    private void LoadTextureInfo()
    {
        string? path =
            ProjectContext.CurrentTexturePath;

        if (string.IsNullOrWhiteSpace(path))
        {
            FileNameLabel.Text =
                "Yeni Texture";

            return;
        }

        FileNameLabel.Text =
            System.IO.Path.GetFileName(path);
    }

    // =========================================================
    // CANVAS
    // =========================================================

    private void OnCanvasInteraction(
        object? sender,
        TouchEventArgs e)
    {
        if (e.Touches == null ||
            !e.Touches.Any())
        {
            return;
        }

        var touch =
            e.Touches.First();

        float canvasX =
            touch.X;

        float canvasY =
            touch.Y;

        float width =
            (float)PixelCanvas.Width;

        float height =
            (float)PixelCanvas.Height;

        if (width <= 0 ||
            height <= 0)
        {
            return;
        }

        float pixelSize =
            Math.Min(
                width / _pixelData.Width,
                height / _pixelData.Height);

        float canvasWidth =
            pixelSize *
            _pixelData.Width;

        float canvasHeight =
            pixelSize *
            _pixelData.Height;

        float startX =
            (width -
             canvasWidth) /
            2f;

        float startY =
            (height -
             canvasHeight) /
            2f;

        int x =
            (int)
            ((canvasX - startX) /
             pixelSize);

        int y =
            (int)
            ((canvasY - startY) /
             pixelSize);

        if (x < 0 ||
            y < 0 ||
            x >= _pixelData.Width ||
            y >= _pixelData.Height)
        {
            return;
        }

        if (_tool == EditTool.Picker)
        {
            _currentColor =
                _pixelData.Get(
                    x,
                    y);

            ColorEntry.Text =
                ToHex(
                    _currentColor);

            _tool =
                EditTool.Pen;

            PixelCanvas.Invalidate();

            return;
        }

        SaveUndo();

        switch (_tool)
        {
            case EditTool.Pen:

                _pixelData.Set(
                    x,
                    y,
                    _currentColor);

                break;

            case EditTool.Eraser:

                _pixelData.Set(
                    x,
                    y,
                    Colors.Transparent);

                break;

            case EditTool.Fill:

                FillCanvas();

                break;
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

    private void OnFillClicked(
        object sender,
        EventArgs e)
    {
        SaveUndo();

        FillCanvas();

        PixelCanvas.Invalidate();
    }

    private void FillCanvas()
    {
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
    }

    private void OnPickerClicked(
        object sender,
        EventArgs e)
    {
        _tool =
            EditTool.Picker;
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
        string text =
            ColorEntry.Text?
                .Trim() ?? "";

        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            _currentColor =
                Color.FromArgb(
                    text);

            await DisplayAlert(
                "Renk",
                $"Seçilen renk: {text}",
                "Tamam");
        }
        catch
        {
            await DisplayAlert(
                "Hatalı Renk",
                "Örnek: #FF0000 veya #FFFFFFFF",
                "Tamam");
        }
    }

    // =========================================================
    // UNDO
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

        Color[,] previous =
            _undo.Pop();

        _pixelData.CopyFrom(
            previous);

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

        Color[,] next =
            _redo.Pop();

        _pixelData.CopyFrom(
            next);

        PixelCanvas.Invalidate();
    }

    // =========================================================
    // GRID
    // =========================================================

    private void OnGridClicked(
        object sender,
        EventArgs e)
    {
        if (PixelCanvas.Drawable
            is PixelDrawable drawable)
        {
            drawable.ShowGrid =
                !drawable.ShowGrid;

            PixelCanvas.Invalidate();
        }
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
                "Kaydedilecek texture seçilmedi.",
                "Tamam");

            return;
        }

        await DisplayAlert(
            "Texture",
            "Pixel düzenlemeleri bellekte tutuldu.",
            "Tamam");
    }

    // =========================================================
    // BACK
    // =========================================================

    private async void OnBackClicked(
        object sender,
        EventArgs e)
    {
        await Navigation.PopAsync();
    }

    // =========================================================
    // COLOR
    // =========================================================

    private static string ToHex(
        Color color)
    {
        int r =
            (int)
            (color.Red * 255);

        int g =
            (int)
            (color.Green * 255);

        int b =
            (int)
            (color.Blue * 255);

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
