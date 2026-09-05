using Microsoft.Maui.Graphics;

namespace BedrockPackStudio;

public class PixelDrawable : IDrawable
{
    private readonly PixelData _data;

    public bool ShowGrid { get; set; } = true;

    public PixelDrawable(
        PixelData data)
    {
        _data = data;
    }

    public void Draw(
        ICanvas canvas,
        RectF dirtyRect)
    {
        canvas.FillColor =
            Color.FromArgb("#1B1D22");

        canvas.FillRectangle(
            dirtyRect
        );

        float pixelSize =
            Math.Min(
                dirtyRect.Width / _data.Width,
                dirtyRect.Height / _data.Height
            );

        float totalWidth =
            pixelSize * _data.Width;

        float totalHeight =
            pixelSize * _data.Height;

        float startX =
            (dirtyRect.Width - totalWidth) / 2;

        float startY =
            (dirtyRect.Height - totalHeight) / 2;


        // PIXELS
        for (int y = 0; y < _data.Height; y++)
        {
            for (int x = 0; x < _data.Width; x++)
            {
                Color color =
                    _data.Get(x, y);

                float px =
                    startX + x * pixelSize;

                float py =
                    startY + y * pixelSize;

                if (color != Colors.Transparent)
                {
                    canvas.FillColor =
                        color;

                    canvas.FillRectangle(
                        px,
                        py,
                        pixelSize,
                        pixelSize
                    );
                }
                else
                {
                    // Şeffaf pixel checkerboard
                    canvas.FillColor =
                        ((x + y) % 2 == 0)
                            ? Color.FromArgb("#3A3D43")
                            : Color.FromArgb("#303339");

                    canvas.FillRectangle(
                        px,
                        py,
                        pixelSize,
                        pixelSize
                    );
                }
            }
        }


        // GRID
        if (ShowGrid)
        {
            canvas.StrokeColor =
                Color.FromArgb("#555A63");

            canvas.StrokeSize = 0.7f;

            for (int x = 0; x <= _data.Width; x++)
            {
                float px =
                    startX + x * pixelSize;

                canvas.DrawLine(
                    px,
                    startY,
                    px,
                    startY + totalHeight
                );
            }

            for (int y = 0; y <= _data.Height; y++)
            {
                float py =
                    startY + y * pixelSize;

                canvas.DrawLine(
                    startX,
                    py,
                    startX + totalWidth,
                    py
                );
            }
        }
    }
}
