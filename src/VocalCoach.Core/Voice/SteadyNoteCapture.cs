using VocalCoach.Core.Audio;

namespace VocalCoach.Core.Voice;

/// <summary>
/// Captures a held note: waits until the singer has sustained a steady pitch for
/// <see cref="HoldSeconds"/>, then reports the nearest semitone. Used to measure the
/// lowest and highest comfortable notes.
/// </summary>
public sealed class SteadyNoteCapture
{
    private readonly List<PitchSample> _run = new();

    public double HoldSeconds { get; init; } = 1.5;
    /// <summary>How far the pitch may wander (in cents) and still count as steady.</summary>
    public double MaxSpreadCents { get; init; } = 60;

    /// <summary>0..1 progress of the current steady hold.</summary>
    public double Progress { get; private set; }

    /// <summary>Feed a sample; returns the captured MIDI note once a steady hold completes.</summary>
    public int? Add(PitchSample sample)
    {
        if (!sample.IsVoiced)
        {
            _run.Clear();
            Progress = 0;
            return null;
        }

        _run.Add(sample);
        // Drop leading samples until the run fits within the spread limit.
        while (_run.Count > 1 && (_run.Max(s => s.Midi!.Value) - _run.Min(s => s.Midi!.Value)) * 100 > MaxSpreadCents)
            _run.RemoveAt(0);

        double held = _run[^1].Time - _run[0].Time;
        Progress = Math.Clamp(held / HoldSeconds, 0, 1);
        if (held < HoldSeconds) return null;

        var sorted = _run.Select(s => s.Midi!.Value).Order().ToList();
        int midi = (int)Math.Round(sorted[sorted.Count / 2]);
        _run.Clear();
        Progress = 0;
        return midi;
    }

    public void Reset()
    {
        _run.Clear();
        Progress = 0;
    }
}
