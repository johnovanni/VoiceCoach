using VocalCoach.Core.Exercises;
using VocalCoach.Core.Practice;
using VocalCoach.Core.Progress;

namespace VocalCoach.Core.Coaching;

public enum TipKind { Praise, Improve, Technique }

public sealed record CoachingTip(TipKind Kind, string Title, string Message);

public enum CueLevel { OnPitch, Close, Off, Silent }

/// <summary>A short instruction shown while singing.</summary>
public sealed record LiveCue(CueLevel Level, string Text);

/// <summary>
/// Turns measurements into the kind of feedback a voice teacher would give:
/// short in-the-moment cues while singing, and specific, prioritised advice afterwards.
/// </summary>
public static class CoachingEngine
{
    public static LiveCue Live(double? centsOff, double tolerance)
    {
        if (centsOff is not double c) return new(CueLevel.Silent, "Sing when the bar reaches the line");
        double a = Math.Abs(c);
        if (a > 600) return new(CueLevel.Off, c > 0 ? "That's far too high — listen again" : "That's far too low — listen again");
        if (a <= tolerance) return new(CueLevel.OnPitch, "On pitch — hold it there!");
        if (a <= tolerance * 2) return new(CueLevel.Close, c > 0 ? "A touch sharp — relax down" : "A touch flat — lift slightly");
        return new(CueLevel.Off, c > 0 ? "Sharp — bring it down" : "Flat — aim higher");
    }

    /// <summary>Post-exercise feedback, most important first. Always 2–4 tips.</summary>
    public static IReadOnlyList<CoachingTip> Review(SessionSummary s)
    {
        var tips = new List<CoachingTip>();
        if (s.Notes.Count == 0) return [new(TipKind.Improve, "No notes sung", "Start the exercise and sing along when the bars reach the line.")];

        if (s.MissedNotes > s.Notes.Count / 2)
        {
            tips.Add(new(TipKind.Improve, "I couldn't hear you",
                "Most notes weren't picked up. Move closer to the microphone, sing a little louder, or try a quieter room. " +
                "Use headphones if the reference tones are drowning you out."));
        }

        if (s.OctaveOffNotes > s.Notes.Count / 3)
        {
            tips.Add(new(TipKind.Improve, "Right notes, different octave",
                "You're matching the note names but in another octave. That's a great ear! Check your voice type in Settings " +
                "so exercises sit in your range, or run the Range Finder."));
        }

        if (s.Score >= 85)
            tips.Add(new(TipKind.Praise, "Beautifully in tune", $"{s.AccuracyPercent:0}% of your singing was on target. Try a harder level or a more advanced exercise."));
        else if (s.Score >= 60)
            tips.Add(new(TipKind.Praise, "Solid work", $"You hit the target {s.AccuracyPercent:0}% of the time. Consistency comes with repetition — keep going."));

        bool subtle = Math.Abs(s.AverageCents) <= s.ToleranceCents;
        if (subtle && s.Tendency == PitchTendency.Flat)
            tips.Add(new(TipKind.Technique, "A slight flat lean",
                $"You're on target, but sitting about {Math.Abs(s.AverageCents):0} cents low. Think 'bright and forward' and keep the breath moving to centre each note."));
        else if (subtle && s.Tendency == PitchTendency.Sharp)
            tips.Add(new(TipKind.Technique, "A slight sharp lean",
                $"You're on target, but sitting about {s.AverageCents:0} cents high. Ease off the volume a touch and let the notes settle."));
        else switch (s.Tendency)
        {
            case PitchTendency.Flat:
                tips.Add(new(TipKind.Improve, "You tend to sing flat",
                    $"On average you were {Math.Abs(s.AverageCents):0} cents under the note. Flatness usually comes from low energy " +
                    "or running out of breath. Breathe low before each phrase, keep your eyebrows lifted and think of every note as 'bright and forward'."));
                break;
            case PitchTendency.Sharp:
                tips.Add(new(TipKind.Improve, "You tend to sing sharp",
                    $"On average you were {s.AverageCents:0} cents over the note. Sharpness often comes from pushing or tension. " +
                    "Sing a little softer, relax your jaw and neck, and let the note 'sit' rather than reaching for it."));
                break;
        }

        if (s.AverageStability > 25)
        {
            tips.Add(new(TipKind.Technique, "Steady the line",
                $"Your pitch wavered by about ±{s.AverageStability:0} cents within notes. Aim for a flat, smooth line on the graph. " +
                "Long Tones is the best exercise for this: slow, steady airflow from the belly, not the chest."));
        }

        if (s.ScoopedNotes >= Math.Max(2, s.NotesSung / 4))
        {
            tips.Add(new(TipKind.Technique, "Avoid scooping",
                $"{s.ScoopedNotes} notes started below pitch and slid up. Hear the note in your head first, then start it right " +
                "on target — picture landing on top of the note, not climbing up to it."));
        }

        if (s.LowerHalfAccuracy is double lo && s.UpperHalfAccuracy is double hi)
        {
            if (lo - hi > 20)
                tips.Add(new(TipKind.Technique, "Higher notes need attention",
                    "Your accuracy drops on the higher notes. As you rise, use less weight and more breath — let the voice get lighter. " +
                    "Lip trills and sirens help the upper range settle."));
            else if (hi - lo > 20)
                tips.Add(new(TipKind.Technique, "Lower notes need attention",
                    "Your accuracy drops on the lower notes. Don't press down with the chin; keep the sound forward and let low notes be quieter."));
        }

        if (tips.Count < 2)
            tips.Add(new(TipKind.Technique, "Tip", "Record yourself occasionally — hearing your own voice back trains your ear faster than anything else."));

        return tips.Take(4).ToList();
    }

    /// <summary>Picks a practice routine for today: warm-up, focus work, cool-down.</summary>
    public static IReadOnlyList<Exercise> TodaysRoutine(IReadOnlyList<SessionRecord> history, SkillLevel level, DateTime today)
    {
        int maxDifficulty = level switch { SkillLevel.Beginner => 1, SkillLevel.Intermediate => 2, _ => 3 };
        var pool = ExerciseLibrary.All.Where(e => e.Difficulty <= maxDifficulty + 1).ToList();

        var routine = new List<Exercise>
        {
            ExerciseLibrary.Get(today.DayOfYear % 2 == 0 ? "hum-descend" : "lip-trill-5"),
        };

        // Focus: the two weakest (or never tried) main exercises, favouring ones at the right level.
        var focus = pool
            .Where(e => e.Category is not (ExerciseCategory.WarmUp or ExerciseCategory.CoolDown))
            .OrderBy(e => RecentAverage(history, e.Id) ?? -1) // untried first
            .ThenBy(e => Math.Abs(e.Difficulty - maxDifficulty))
            .Take(2);
        routine.AddRange(focus);
        routine.Add(ExerciseLibrary.Get("cooldown-sigh"));
        return routine;
    }

    private static double? RecentAverage(IReadOnlyList<SessionRecord> history, string exerciseId)
    {
        var recent = history.Where(h => h.ExerciseId == exerciseId).OrderByDescending(h => h.Date).Take(3).ToList();
        return recent.Count == 0 ? null : recent.Average(r => r.Score);
    }

    private static readonly string[] DailyTips =
    [
        "Hydrate early: water you drink now reaches your vocal folds in a few hours, not minutes.",
        "Posture check: feet hip-width apart, knees soft, ribs open, chin level.",
        "Breathe low. Your shoulders shouldn't rise when you inhale to sing.",
        "Short, daily practice beats one long session a week. 15 minutes is plenty.",
        "If it hurts, stop. Singing should never feel scratchy or painful.",
        "Yawning opens the throat — use that open feeling on 'Ah' vowels.",
        "Hear it before you sing it. Audiate each note in your head first.",
        "Speak your lyrics in rhythm before singing them — it frees up your attention for pitch.",
        "Steam, sleep and rest are the best vocal medicines.",
        "Smile with your eyes on high notes: it lifts the soft palate and brightens the tone.",
    ];

    public static string TipOfTheDay(DateTime date) => DailyTips[date.DayOfYear % DailyTips.Length];
}
