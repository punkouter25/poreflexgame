using Godot;
using System;
using System.Linq;

public partial class Graph : Control
{
    private const float PADDING = 20.0f;
    private const float POINT_RADIUS = 4.0f;
    private const float LINE_WIDTH = 2.0f;

    private float[] _values = Array.Empty<float>();
    private float _minValue = float.MaxValue;
    private float _maxValue = float.MinValue;

    public void SetData(float[] values)
    {
        _values = values;
        if (values.Length > 0)
        {
            var validValues = values.Where(v => v < 99999.0f);
            if (validValues.Any())
            {
                _minValue = validValues.Min();
                _maxValue = validValues.Max();
            }
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_values.Length < 2) return;

        var graphRect = GetRect();
        var graphWidth = graphRect.Size.X - (2 * PADDING);
        var graphHeight = graphRect.Size.Y - (2 * PADDING);

        // Draw axes
        DrawLine(
            new Vector2(PADDING, PADDING),
            new Vector2(PADDING, graphRect.Size.Y - PADDING),
            Colors.White,
            LINE_WIDTH
        );
        DrawLine(
            new Vector2(PADDING, graphRect.Size.Y - PADDING),
            new Vector2(graphRect.Size.X - PADDING, graphRect.Size.Y - PADDING),
            Colors.White,
            LINE_WIDTH
        );

        // Draw data points and lines
        var xStep = graphWidth / (_values.Length - 1);
        var yScale = graphHeight / (_maxValue - _minValue);

        for (int i = 0; i < _values.Length; i++)
        {
            var x = PADDING + (i * xStep);
            var y = graphRect.Size.Y - PADDING;

            if (_values[i] < 99999.0f)
            {
                y -= (_values[i] - _minValue) * yScale;
                var point = new Vector2(x, y);

                // Draw point
                DrawCircle(point, POINT_RADIUS, Colors.Green);

                // Draw line to next point
                if (i < _values.Length - 1 && _values[i + 1] < 99999.0f)
                {
                    var nextX = PADDING + ((i + 1) * xStep);
                    var nextY = graphRect.Size.Y - PADDING - (_values[i + 1] - _minValue) * yScale;
                    DrawLine(point, new Vector2(nextX, nextY), Colors.Green, LINE_WIDTH);
                }
            }
        }

        // Draw labels
        var font = ThemeDB.FallbackFont;
        var fontSize = ThemeDB.FallbackFontSize;

        // Y-axis labels
        DrawString(font, new Vector2(0, PADDING), $"{_maxValue:F2}s", HorizontalAlignment.Left, -1, fontSize);
        DrawString(font, new Vector2(0, graphRect.Size.Y - PADDING), $"{_minValue:F2}s", HorizontalAlignment.Left, -1, fontSize);

        // X-axis labels
        DrawString(font, new Vector2(PADDING, graphRect.Size.Y), "First", HorizontalAlignment.Left, -1, fontSize);
        DrawString(font, new Vector2(graphRect.Size.X - PADDING, graphRect.Size.Y), "Latest", HorizontalAlignment.Right, -1, fontSize);
    }
} 