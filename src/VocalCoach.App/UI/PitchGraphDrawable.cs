using VocalCoach.Core.Audio;
using VocalCoach.Core.Music;
using VocalCoach.Core.Practice;

namespace VocalCoach.App.UI;

/// <summary>
/// The core visual-feedback display: a scrolling "piano roll". Target notes scroll from
/// right to left toward the "now" line; the singer's pitch is drawn as a coloured line
/// (green = on pitch, amber = close, red = off). Bar thickness equals the tolerance band,
/// so keeping the line inside the bar literally means "in tune".
/// </summary>
public sealed class PitchGraphDrawable : IDrawable
{
    private const float KeyStripWidth = 46;
    private double _viewCentre = 57;

    public IReadOnlyList<TimedNote> Notes { get; set; } = [];
    public Func<int, NoteResult?> ResultFor { get; set; } = _ => null;
    public List<PitchSample> Trail { get; } = new();
    public double Now { get; set; }
    public double ToleranceCents { get; set; } = 25;
    public double PastSeconds { get; set; } = 3;
    public double FutureSeconds { get; set; } = 5;
    /// <summary>An optional fixed reference pitch drawn across the whole graph (tuner match mode).</summary>
    public int? ReferenceMidi { get; set; }
    /// <summary>Where to centre the view when nothing else gives a hint.</summary>
    public double DefaultCentre { get; set; } = 57;
    public double VisibleSemitones { get; set; } = 14;

    public void ResetView() => _viewCentre = DefaultCentre;

    public void TrimTrail()
    {
        double cutoff = Now - PastSeconds - 1;
        int remove = Trail.FindIndex(s => s.Time >= cutoff);
        if (remove > 0) Trail.RemoveRange(0, remove);
        else if (remove < 0) Trail.Clear();
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        canvas.FillColor = Theme.Background;
        canvas.FillRectangle(rect);
        if (rect.Width < 50 || rect.Height < 50) return;

        double windowStart = Now - PastSeconds;
        double windowEnd = Now + FutureSeconds;
        var visibleNotes = Notes.Where(n => n.End >= windowStart && n.Start <= windowEnd).ToList();

        UpdateViewCentre(visibleNotes);
        double minMidi = _viewCentre - VisibleSemitones / 2;
        double maxMidi = _viewCentre + VisibleSemitones / 2;
        float rowH = (float)(rect.Height / (maxMidi - minMidi));
        float plotLeft = rect.Left + KeyStripWidth;
        float plotWidth = rect.Width - KeyStripWidth;

        float X(double t) => plotLeft + (float)((t - windowStart) / (windowEnd - windowStart) * plotWidth);
        float Y(double midi) => rect.Bottom - (float)((midi - minMidi) / (maxMidi - minMidi) * rect.Height);

        var latest = Trail.Count > 0 ? Trail[^1] : (PitchSample?)null;
        int? singingRow = latest is { IsVoiced: true } l && Now - l.Time < 0.3 ? (int)Math.Round(l.Midi!.Value) : null;

        DrawRowsAndKeys(canvas, rect, minMidi, maxMidi, rowH, plotLeft, Y, singingRow);

        if (ReferenceMidi is int refMidi)
        {
            float bandH = Math.Max(4, rowH * (float)(ToleranceCents * 2 / 100));
            canvas.FillColor = Theme.Sing.WithAlpha(0.25f);
            canvas.FillRectangle(plotLeft, Y(refMidi) - bandH / 2, plotWidth, bandH);
            canvas.StrokeColor = Theme.Sing.WithAlpha(0.7f);
            canvas.StrokeSize = 1;
            canvas.DrawLine(plotLeft, Y(refMidi), rect.Right, Y(refMidi));
        }

        foreach (var note in visibleNotes) DrawNote(canvas, note, rowH, X, Y);

        DrawTrail(canvas, X, Y, windowStart);

        // "Now" line.
        float nowX = X(Now);
        canvas.StrokeColor = Colors.White.WithAlpha(0.15f);
        canvas.StrokeSize = 8;
        canvas.DrawLine(nowX, rect.Top, nowX, rect.Bottom);
        canvas.StrokeColor = Colors.White.WithAlpha(0.8f);
        canvas.StrokeSize = 2;
        canvas.DrawLine(nowX, rect.Top, nowX, rect.Bottom);

        // Current pitch "ball" on the now line.
        if (latest is { IsVoiced: true } cur && Now - cur.Time < 0.3)
        {
            var color = ColorFor(cur);
            float y = Y(cur.Midi!.Value);
            canvas.FillColor = color.WithAlpha(0.25f);
            canvas.FillCircle(nowX, y, 14);
            canvas.FillColor = color;
            canvas.FillCircle(nowX, y, 7);
        }
    }

    private void UpdateViewCentre(List<TimedNote> visibleNotes)
    {
        double target;
        if (visibleNotes.Count > 0)
        {
            target = (visibleNotes.Min(n => n.Midi) + visibleNotes.Max(n => n.Midi)) / 2.0;
            VisibleSemitones = Math.Max(14, visibleNotes.Max(n => n.Midi) - visibleNotes.Min(n => n.Midi) + 6);
        }
        else if (ReferenceMidi is int r) target = r;
        else if (Trail.LastOrDefault(s => s.IsVoiced) is { IsVoiced: true } v && Now - v.Time < 1.5) target = v.Midi!.Value;
        else target = _viewCentre;

        if (Math.Abs(target - _viewCentre) > 30) _viewCentre = target; // jump when wildly off-screen
        _viewCentre += (target - _viewCentre) * 0.08; // otherwise glide smoothly
    }

    private static void DrawRowsAndKeys(ICanvas canvas, RectF rect, double minMidi, double maxMidi, float rowH, float plotLeft, Func<double, float> Y, int? singingRow)
    {
        canvas.FontSize = Math.Clamp(rowH * 0.6f, 9, 13);
        for (int m = (int)Math.Floor(minMidi); m <= (int)Math.Ceiling(maxMidi); m++)
        {
            float yTop = Y(m + 0.5), yBottom = Y(m - 0.5);
            bool black = NoteMath.IsBlackKey(m);

            // Plot background stripes for black-key rows.
            if (black)
            {
                canvas.FillColor = Colors.White.WithAlpha(0.025f);
                canvas.FillRectangle(plotLeft, yTop, rect.Right - plotLeft, yBottom - yTop);
            }
            // Octave lines on each C.
            if (m % 12 == 0)
            {
                canvas.StrokeColor = Theme.Grid;
                canvas.StrokeSize = 1;
                canvas.DrawLine(plotLeft, yBottom, rect.Right, yBottom);
            }

            // Piano key strip.
            canvas.FillColor = m == singingRow ? Theme.Accent : black ? Color.FromArgb("#2B2840") : Color.FromArgb("#D9D6E8");
            canvas.FillRectangle(rect.Left, yTop + 0.5f, KeyStripWidth - 2, yBottom - yTop - 1);

            if (m % 12 == 0 || rowH >= 18 || m == singingRow)
            {
                canvas.FontColor = m == singingRow ? Colors.White : black ? Theme.Muted : Color.FromArgb("#3B3360");
                canvas.DrawString(NoteMath.NoteName(m), rect.Left + 2, yTop, KeyStripWidth - 6, yBottom - yTop,
                    HorizontalAlignment.Right, VerticalAlignment.Center);
            }
        }
    }

    private void DrawNote(ICanvas canvas, TimedNote note, float rowH, Func<double, float> X, Func<double, float> Y)
    {
        float x1 = X(note.Start), x2 = X(note.End);
        float bandH = Math.Max(6, rowH * (float)(ToleranceCents * 2 / 100));
        float y = Y(note.Midi);
        var r = new RectF(x1, y - bandH / 2, Math.Max(2, x2 - x1), bandH);

        Color fill;
        if (note.Phase == NotePhase.Listen)
            fill = Theme.Listen.WithAlpha(note.Contains(Now) ? 0.65f : 0.3f);
        else if (ResultFor(note.Index) is { } result)
            fill = (result.Missed ? Theme.Muted : Theme.ForScore(result.Score)).WithAlpha(0.45f);
        else
            fill = Theme.Sing.WithAlpha(note.Contains(Now) ? 0.75f : 0.5f);

        canvas.FillColor = fill;
        canvas.FillRoundedRectangle(r, bandH / 2);

        if (note.Phase == NotePhase.Sing && note.Start > Now - 0.2)
        {
            canvas.StrokeColor = Theme.Sing;
            canvas.StrokeSize = 1.5f;
            canvas.DrawRoundedRectangle(r, bandH / 2);
        }

        // Note name on bars that are still ahead.
        if (note.End > Now && r.Width > 30)
        {
            canvas.FontColor = Colors.White;
            canvas.FontSize = 11;
            canvas.DrawString(NoteMath.NoteName(note.Midi), r.Left + 6, y - 18, 60, 14, HorizontalAlignment.Left, VerticalAlignment.Center);
        }
    }

    private void DrawTrail(ICanvas canvas, Func<double, float> X, Func<double, float> Y, double windowStart)
    {
        canvas.StrokeSize = 3.5f;
        canvas.StrokeLineCap = LineCap.Round;
        PitchSample? prev = null;
        foreach (var s in Trail)
        {
            if (s.Time < windowStart || !s.IsVoiced) { prev = null; continue; }
            // Join consecutive readings, but break the line at big jumps (voice cracks, new notes)
            // rather than drawing a misleading vertical stroke.
            if (prev is { } p && s.Time - p.Time < 0.12 && Math.Abs(s.Midi!.Value - p.Midi!.Value) < 1.5)
            {
                canvas.StrokeColor = ColorFor(s);
                canvas.DrawLine(X(p.Time), Y(p.Midi!.Value), X(s.Time), Y(s.Midi!.Value));
            }
            else
            {
                canvas.FillColor = ColorFor(s);
                canvas.FillCircle(X(s.Time), Y(s.Midi!.Value), 2);
            }
            prev = s;
        }
    }

    /// <summary>Colour a sample by its error against whatever it should be matching.</summary>
    private Color ColorFor(PitchSample s)
    {
        var target = Notes.FirstOrDefault(n => n.Phase == NotePhase.Sing && n.Contains(s.Time));
        // Moving between notes takes a moment; show the transition neutrally instead of as an error
        // (the scorer ignores this onset window too).
        if (target is not null && s.Time - target.Start < NoteScorer.OnsetGraceSeconds) return Theme.Muted;
        int? targetMidi = target?.Midi ?? ReferenceMidi;
        if (targetMidi is int t) return Theme.ForCents((s.Midi!.Value - t) * 100, ToleranceCents);
        // Free singing: colour by distance to the nearest semitone.
        double cents = (s.Midi!.Value - Math.Round(s.Midi.Value)) * 100;
        return Theme.ForCents(cents, ToleranceCents).WithAlpha(0.9f);
    }
}
