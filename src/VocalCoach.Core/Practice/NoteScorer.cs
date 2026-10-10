using VocalCoach.Core.Audio;

namespace VocalCoach.Core.Practice;

/// <summary>How well one target note was sung.</summary>
public sealed record NoteResult(
    TimedNote Note,
    double VoicedFraction,
    double HitFraction,
    double MeanCents,
    double StdDevCents,
    bool Scooped,
    bool OctaveOff,
    double Score)
{
    public bool Missed => VoicedFraction < NoteScorer.MinVoicedFraction;
}

public static class NoteScorer
{
    /// <summary>Time allowed at the start of a note to find the pitch before accuracy counts.</summary>
    public const double OnsetGraceSeconds = 0.15;
    public const double MinVoicedFraction = 0.25;

    public static NoteResult Score(TimedNote note, IReadOnlyList<PitchSample> samples, double toleranceCents)
    {
        // Samples are expected in time order and roughly within the note's span.
        var inNote = samples.Where(s => s.Time >= note.Start && s.Time < note.End).ToList();
        var sustain = inNote.Where(s => s.Time >= note.Start + OnsetGraceSeconds).ToList();
        if (sustain.Count == 0) sustain = inNote;

        if (inNote.Count == 0)
            return new NoteResult(note, 0, 0, 0, 0, false, false, 0);

        var voicedCents = sustain.Where(s => s.IsVoiced).Select(s => (s.Midi!.Value - note.Midi) * 100).ToList();
        double voicedFraction = (double)voicedCents.Count / sustain.Count;
        if (voicedCents.Count == 0 || voicedFraction < MinVoicedFraction)
            return new NoteResult(note, voicedFraction, 0, 0, 0, false, false, 0);

        double mean = voicedCents.Average();
        double std = Math.Sqrt(voicedCents.Sum(c => (c - mean) * (c - mean)) / voicedCents.Count);
        double hit = voicedCents.Count(c => Math.Abs(c) <= toleranceCents) / (double)sustain.Count;

        double median = voicedCents.Order().ElementAt(voicedCents.Count / 2);
        bool octaveOff = Math.Abs(Math.Abs(median) - 1200) < 150;

        // Scoop: the onset starts well below pitch and then settles onto it.
        var onset = inNote.Where(s => s.IsVoiced && s.Time < note.Start + 0.25).Select(s => (s.Midi!.Value - note.Midi) * 100).ToList();
        bool scooped = onset.Count >= 2 && onset.Take(2).Average() < -Math.Max(50, toleranceCents * 1.5) && Math.Abs(mean) <= toleranceCents * 1.5;

        double stability = Math.Clamp(1 - std / 50.0, 0, 1);
        double coverage = Math.Min(1, voicedFraction / 0.6);
        double score = 100 * (0.75 * hit + 0.25 * stability * coverage);
        if (octaveOff) score = Math.Min(score, 50); // right note name, wrong octave: half credit

        return new NoteResult(note, voicedFraction, hit, mean, std, scooped, octaveOff, Math.Round(score, 1));
    }
}
