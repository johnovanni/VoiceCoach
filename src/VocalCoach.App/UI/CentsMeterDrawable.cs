namespace VocalCoach.App.UI;

/// <summary>A tuner-style needle: -50¢ (flat) … 0 … +50¢ (sharp), with the on-pitch zone highlighted.</summary>
public sealed class CentsMeterDrawable : IDrawable
{
    private double _shown;

    public double? Cents { get; set; }
    public double ToleranceCents { get; set; } = 25;

    public void Draw(ICanvas canvas, RectF rect)
    {
        const double range = 50;
        float pad = 18;
        float left = rect.Left + pad, right = rect.Right - pad, mid = rect.Center.X;
        float trackY = rect.Center.Y + 4;
        float X(double c) => mid + (float)(Math.Clamp(c, -range, range) / range) * (right - mid);

        canvas.FillColor = Theme.SurfaceRaised;
        canvas.FillRoundedRectangle(left, trackY - 5, right - left, 10, 5);

        float zone = X(ToleranceCents) - mid;
        canvas.FillColor = Theme.Good.WithAlpha(0.35f);
        canvas.FillRoundedRectangle(mid - zone, trackY - 5, zone * 2, 10, 5);

        canvas.StrokeColor = Theme.Muted;
        canvas.StrokeSize = 1;
        foreach (int c in new[] { -50, -25, 0, 25, 50 })
            canvas.DrawLine(X(c), trackY + 8, X(c), trackY + (c == 0 ? 14 : 11));

        canvas.FontColor = Theme.Muted;
        canvas.FontSize = 12;
        canvas.DrawString("♭ flat", rect.Left, rect.Top, 80, 16, HorizontalAlignment.Left, VerticalAlignment.Top);
        canvas.DrawString("sharp ♯", rect.Right - 80, rect.Top, 80, 16, HorizontalAlignment.Right, VerticalAlignment.Top);

        if (Cents is not double target) return;
        _shown += (target - _shown) * 0.35; // ease the needle so it doesn't jitter
        var color = Theme.ForCents(_shown, ToleranceCents);
        float x = X(_shown);
        canvas.FillColor = color.WithAlpha(0.3f);
        canvas.FillCircle(x, trackY, 13);
        canvas.FillColor = color;
        canvas.FillCircle(x, trackY, 8);
        canvas.FontColor = color;
        canvas.FontSize = 13;
        canvas.DrawString($"{_shown:+0;-0;0}¢", mid - 40, rect.Top, 80, 16, HorizontalAlignment.Center, VerticalAlignment.Top);
    }
}
