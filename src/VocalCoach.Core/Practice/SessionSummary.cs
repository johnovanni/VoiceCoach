namespace VocalCoach.Core.Practice;

public enum PitchTendency { OnPitch, Flat, Sharp }

/// <summary>Aggregate results for a practice run, used for coaching and history.</summary>
public sealed record SessionSummary(
    string ExerciseId,
    string ExerciseName,
    IReadOnlyList<NoteResult> Notes,
    double Score,
    double AccuracyPercent,
    double AverageCents,
    double AverageAbsCents,
    double AverageStability,
    int MissedNotes,
    int ScoopedNotes,
    int OctaveOffNotes,
    PitchTendency Tendency,
    double? LowerHalfAccuracy,
    double? UpperHalfAccuracy,
    double ToleranceCents)
{
    public int NotesSung => Notes.Count - MissedNotes;

    public string Grade => Score switch
    {
        >= 90 => "Excellent",
        >= 75 => "Great",
        >= 60 => "Good",
        >= 40 => "Getting there",
        _ => "Keep practising",
    };

    public static SessionSummary From(SessionPlan plan, IReadOnlyList<NoteResult> notes, double tolerance)
    {
        var sung = notes.Where(n => !n.Missed).ToList();
        double score = notes.Count == 0 ? 0 : notes.Average(n => n.Score);
        double accuracy = notes.Count == 0 ? 0 : notes.Average(n => n.HitFraction) * 100;
        // Octave errors would swamp the cents averages, so they're reported separately.
        var inOctave = sung.Where(n => !n.OctaveOff).ToList();
        double avgCents = inOctave.Count == 0 ? 0 : inOctave.Average(n => n.MeanCents);
        double avgAbs = inOctave.Count == 0 ? 0 : inOctave.Average(n => Math.Abs(n.MeanCents));
        double stability = sung.Count == 0 ? 0 : sung.Average(n => n.StdDevCents);

        var tendency = Math.Abs(avgCents) < tolerance * 0.4 ? PitchTendency.OnPitch
                     : avgCents < 0 ? PitchTendency.Flat : PitchTendency.Sharp;

        // Compare the singer's lower vs upper notes to spot register-specific problems.
        double? lower = null, upper = null;
        if (sung.Count >= 4)
        {
            double median = sung.Select(n => (double)n.Note.Midi).Order().ElementAt(sung.Count / 2);
            var lo = sung.Where(n => n.Note.Midi < median).ToList();
            var hi = sung.Where(n => n.Note.Midi > median).ToList();
            if (lo.Count > 0 && hi.Count > 0)
            {
                lower = lo.Average(n => n.HitFraction) * 100;
                upper = hi.Average(n => n.HitFraction) * 100;
            }
        }

        return new SessionSummary(
            plan.Exercise.Id, plan.Exercise.Name, notes,
            Math.Round(score, 1), Math.Round(accuracy, 1),
            Math.Round(avgCents, 1), Math.Round(avgAbs, 1), Math.Round(stability, 1),
            notes.Count(n => n.Missed), notes.Count(n => n.Scooped), notes.Count(n => n.OctaveOff),
            tendency, lower, upper, tolerance);
    }
}
