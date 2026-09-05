using Microsoft.Maui.Graphics;

namespace BedrockPackStudio;

public sealed class PixelData
{
    private Color[,] _pixels;

    public int Width { get; private set; }
    public int Height { get; private set; }

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
        if (x < 0 ||
            y < 0 ||
            x >= Width ||
            y >= Height)
        {
            return Colors.Transparent;
        }

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

    public void Resize(
        int width,
        int height)
    {
        var next =
            new Color[width, height];

        for (int y = 0;
             y < Math.Min(height, Height);
             y++)
        {
            for (int x = 0;
                 x < Math.Min(width, Width);
                 x++)
            {
                next[x, y] =
                    _pixels[x, y];
            }
        }

        Width = width;
        Height = height;
        _pixels = next;
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
        int width =
            source.GetLength(0);

        int height =
            source.GetLength(1);

        Width = width;
        Height = height;

        _pixels =
            new Color[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                _pixels[x, y] =
                    source[x, y];
            }
        }
    }
}
