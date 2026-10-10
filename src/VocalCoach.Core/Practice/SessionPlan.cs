using VocalCoach.Core.Audio;
using VocalCoach.Core.Exercises;
using VocalCoach.Core.Voice;

namespace VocalCoach.Core.Practice;

public enum NotePhase
{
    /// <summary>The app plays the note; the singer listens.</summary>
    Listen,
    /// <summary>The singer sings the note and is scored.</summary>
    Sing,
}

/// <param name="Index">Position in the plan's note list.</param>
/// <param name="Repetition">0-based repetition (each a semitone higher).</param>
public sealed record TimedNote(int Index, int Midi, double Start, double Duration, NotePhase Phase, int Repetition)
{
    public double End => Start + Duration;
    public bool Contains(double time) => time >= Start && time < End;
}

/// <summary>
/// The full timeline of a practice run: for each repetition, the app demonstrates the
/// pattern ("Listen"), then the singer repeats it ("Sing"). Times are in seconds from start.
/// </summary>
public sealed class SessionPlan
{
    public Exercise Exercise { get; }
    public IReadOnlyList<TimedNote> Notes { get; }
    public double TotalSeconds { get; }
    public int RepetitionCount { get; }
    public double LeadInSeconds { get; }

    private SessionPlan(Exercise exercise, IReadOnlyList<TimedNote> notes, double total, int reps, double leadIn)
    {
        Exercise = exercise;
        Notes = notes;
        TotalSeconds = total;
        RepetitionCount = reps;
        LeadInSeconds = leadIn;
    }

    public IEnumerable<TimedNote> SingNotes => Notes.Where(n => n.Phase == NotePhase.Sing);

    /// <param name="demonstrate">If false, skip the "Listen" phrase and only cue the first note.</param>
    public static SessionPlan Build(Exercise exercise, VoiceRange range, bool demonstrate = true, double leadInSeconds = 2.0)
    {
        double beat = exercise.SecondsPerBeat;
        int firstRoot = range.LowMidi + exercise.StartAboveLow;
        // Never exceed the top of the comfortable range.
        int maxReps = range.HighMidi - exercise.HighestOffset - firstRoot + 1;
        int reps = Math.Max(1, Math.Min(exercise.Repetitions, maxReps));
        if (maxReps < 1) firstRoot = Math.Max(range.LowMidi, range.HighMidi - exercise.HighestOffset);

        var notes = new List<TimedNote>();
        double t = leadInSeconds;
        for (int rep = 0; rep < reps; rep++)
        {
            int root = firstRoot + rep;
            if (demonstrate)
            {
                t = AddPhrase(notes, exercise, root, t, beat, NotePhase.Listen, rep);
            }
            else
            {
                // A single cue note so the singer can find the key.
                notes.Add(new TimedNote(notes.Count, root + FirstOffset(exercise), t, beat, NotePhase.Listen, rep));
                t += beat;
            }
            t += beat; // breathe
            t = AddPhrase(notes, exercise, root, t, beat, NotePhase.Sing, rep);
            t += beat * 1.5; // reset before the next key
        }
        return new SessionPlan(exercise, notes, t, reps, leadInSeconds);
    }

    private static int FirstOffset(Exercise e) => e.Pattern.First(p => p.Offset.HasValue).Offset!.Value;

    private static double AddPhrase(List<TimedNote> notes, Exercise exercise, int root, double t, double beat, NotePhase phase, int rep)
    {
        foreach (var step in exercise.Pattern)
        {
            double dur = step.Beats * beat;
            if (step.Offset is int offset)
            {
                // Tiny gap between notes so the targets read as separate bars.
                notes.Add(new TimedNote(notes.Count, root + offset, t, dur * 0.94, phase, rep));
            }
            t += dur;
        }
        return t;
    }

    public TimedNote? NoteAt(double time) => Notes.FirstOrDefault(n => n.Contains(time));

    /// <summary>The next note that hasn't started yet (useful for "get ready" cues).</summary>
    public TimedNote? NextNoteAfter(double time) => Notes.FirstOrDefault(n => n.Start > time);

    /// <summary>Renders the reference audio for the whole run.</summary>
    /// <param name="accompanySinging">Also play the notes softly while the singer sings (use headphones!).</param>
    public float[] RenderReferenceAudio(int sampleRate, bool accompanySinging, double volume = 0.5)
    {
        var tones = Notes
            .Where(n => n.Phase == NotePhase.Listen || accompanySinging)
            .Select(n => new ToneSynth.Tone(n.Start, n.Duration, n.Midi, n.Phase == NotePhase.Listen ? volume : volume * 0.35));
        return ToneSynth.Render(tones, sampleRate, TotalSeconds);
    }
}
