namespace VocalCoach.App.UI;

/// <summary>App palette. One place to retheme the whole app.</summary>
public static class Theme
{
    public static readonly Color Background = Color.FromArgb("#12101F");
    public static readonly Color Surface = Color.FromArgb("#1E1A33");
    public static readonly Color SurfaceRaised = Color.FromArgb("#2A2547");
    public static readonly Color Accent = Color.FromArgb("#8B5CF6");
    public static readonly Color Listen = Color.FromArgb("#A78BFA");
    public static readonly Color Sing = Color.FromArgb("#38BDF8");
    public static readonly Color Good = Color.FromArgb("#34D399");
    public static readonly Color Close = Color.FromArgb("#FBBF24");
    public static readonly Color Bad = Color.FromArgb("#F87171");
    public static readonly Color Text = Color.FromArgb("#EDE9FE");
    public static readonly Color Muted = Color.FromArgb("#A39FC0");
    public static readonly Color Grid = Color.FromArgb("#2E2950");

    /// <summary>Colour for a pitch error, given the on-pitch tolerance.</summary>
    public static Color ForCents(double cents, double tolerance)
    {
        double a = Math.Abs(cents);
        return a <= tolerance ? Good : a <= tolerance * 2 ? Close : Bad;
    }

    public static Color ForScore(double score) => score >= 75 ? Good : score >= 45 ? Close : Bad;
}
