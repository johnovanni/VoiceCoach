namespace VocalCoach.Core.Exercises;

public enum ExerciseCategory { WarmUp, PitchAccuracy, Stability, Agility, Range, CoolDown }

/// <summary>One step of an exercise pattern.</summary>
/// <param name="Offset">Semitones above the exercise root, or null for a rest.</param>
/// <param name="Beats">Length in beats.</param>
public readonly record struct PatternStep(int? Offset, double Beats)
{
    public static PatternStep Rest(double beats) => new(null, beats);
}

/// <summary>
/// A vocal exercise: a short melodic pattern that is repeated, rising a semitone each
/// time, through the singer's comfortable range — exactly how a voice teacher runs
/// warm-ups at the piano.
/// </summary>
public sealed record Exercise
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ExerciseCategory Category { get; init; }
    /// <summary>1 = beginner, 2 = intermediate, 3 = advanced.</summary>
    public required int Difficulty { get; init; }
    /// <summary>What to sing, e.g. "Ah", "Mum", "Lip trill".</summary>
    public required string Syllable { get; init; }
    public required string Summary { get; init; }
    public required IReadOnlyList<string> Instructions { get; init; }
    public required IReadOnlyList<string> CoachTips { get; init; }
    public required IReadOnlyList<PatternStep> Pattern { get; init; }
    public required double Tempo { get; init; }
    /// <summary>How many times the pattern is repeated (each a semitone higher).</summary>
    public int Repetitions { get; init; } = 5;
    /// <summary>Semitones above the bottom of the singer's range where the first repetition starts.</summary>
    public int StartAboveLow { get; init; } = 2;

    public int HighestOffset => Pattern.Where(p => p.Offset.HasValue).Max(p => p.Offset!.Value);
    public double PatternBeats => Pattern.Sum(p => p.Beats);
    public double SecondsPerBeat => 60.0 / Tempo;
}
