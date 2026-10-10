namespace VocalCoach.App.UI;

/// <summary>Simple line chart of session scores (0–100) over time.</summary>
public sealed class ScoreChartDrawable : IDrawable
{
    public IReadOnlyList<double> Scores { get; set; } = [];

    public void Draw(ICanvas canvas, RectF rect)
    {
        float left = rect.Left + 30, right = rect.Right - 8, top = rect.Top + 8, bottom = rect.Bottom - 8;
        float Y(double s) => bottom - (float)(s / 100) * (bottom - top);

        canvas.FontSize = 10;
        canvas.FontColor = Theme.Muted;
        canvas.StrokeColor = Theme.Grid;
        canvas.StrokeSize = 1;
        foreach (int g in new[] { 0, 50, 100 })
        {
            canvas.DrawLine(left, Y(g), right, Y(g));
            canvas.DrawString(g.ToString(), rect.Left, Y(g) - 7, 24, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
        }

        if (Scores.Count == 0)
        {
            canvas.FontSize = 13;
            canvas.DrawString("Complete an exercise to see your progress", rect, HorizontalAlignment.Center, VerticalAlignment.Center);
            return;
        }

        float X(int i) => Scores.Count == 1 ? (left + right) / 2 : left + i * (right - left) / (Scores.Count - 1);

        var path = new PathF();
        for (int i = 0; i < Scores.Count; i++)
        {
            if (i == 0) path.MoveTo(X(i), Y(Scores[i])); else path.LineTo(X(i), Y(Scores[i]));
        }
        canvas.StrokeColor = Theme.Accent;
        canvas.StrokeSize = 2.5f;
        canvas.DrawPath(path);

        for (int i = 0; i < Scores.Count; i++)
        {
            canvas.FillColor = Theme.ForScore(Scores[i]);
            canvas.FillCircle(X(i), Y(Scores[i]), 3.5f);
        }
    }
}
