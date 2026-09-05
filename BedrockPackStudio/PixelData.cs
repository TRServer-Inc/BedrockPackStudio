using Microsoft.Maui.Graphics;

namespace BedrockPackStudio;

public class PixelData
{
    private readonly Color[,] _pixels;

    public int Width { get; }

    public int Height { get; }

    public PixelData(
        int width,
        int height)
    {
        Width = width;
        Height = height;

        _pixels =
            new Color[width, height];

        Clear();
    }

    public Color Get(
        int x,
        int y)
    {
        return _pixels[x, y];
    }

    public void Set(
        int x,
        int y,
        Color color)
    {
        if (x < 0 ||
            y < 0 ||
            x >= Width ||
            y >= Height)
            return;

        _pixels[x, y] = color;
    }

    public void Clear()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _pixels[x, y] =
                    Colors.Transparent;
            }
        }
    }

    public Color[,] Clone()
    {
        var copy =
            new Color[Width, Height];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                copy[x, y] =
                    _pixels[x, y];
            }
        }

        return copy;
    }

    public void CopyFrom(
        Color[,] source)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                _pixels[x, y] =
                    source[x, y];
            }
        }
    }
}
