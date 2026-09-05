using Microsoft.Maui.Graphics;

namespace BedrockPackStudio;

public sealed class PixelDrawable : IDrawable
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
        canvas.SaveState();

        float pixelSize =
            Math.Min(
                dirtyRect.Width / _data.Width,
                dirtyRect.Height / _data.Height);

        float totalWidth =
            pixelSize * _data.Width;

        float totalHeight =
            pixelSize * _data.Height;

        float startX =
            dirtyRect.X +
            (dirtyRect.Width - totalWidth) / 2f;

        float startY =
            dirtyRect.Y +
            (dirtyRect.Height - totalHeight) / 2f;

        canvas.FillColor =
            Color.FromArgb("#202329");

        canvas.FillRectangle(
            dirtyRect);

        for (int y = 0;
             y < _data.Height;
             y++)
        {
            for (int x = 0;
                 x < _data.Width;
                 x++)
            {
                Color color =
                    _data.Get(x, y);

                float px =
                    startX +
                    x * pixelSize;

                float py =
                    startY +
                    y * pixelSize;

                canvas.FillColor =
                    color;

                canvas.FillRectangle(
                    px,
                    py,
                    pixelSize,
                    pixelSize);
            }
        }

        if (ShowGrid)
        {
            canvas.StrokeColor =
                Color.FromArgb("#555B65");

            canvas.StrokeSize =
                Math.Max(
                    0.5f,
                    pixelSize / 16f);

            for (int x = 0;
                 x <= _data.Width;
                 x++)
            {
                float px =
                    startX +
                    x * pixelSize;

                canvas.DrawLine(
                    px,
                    startY,
                    px,
                    startY + totalHeight);
            }

            for (int y = 0;
                 y <= _data.Height;
                 y++)
            {
                float py =
                    startY +
                    y * pixelSize;

                canvas.DrawLine(
                    startX,
                    py,
                    startX + totalWidth,
                    py);
            }
        }

        canvas.RestoreState();
    }
}
