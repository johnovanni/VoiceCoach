using VocalCoach.Core.Audio;

namespace VocalCoach.Core.Practice;

/// <summary>
/// Runs one exercise: collects pitch samples against the plan and scores each sung note
/// as soon as it ends. Purely time-driven, so the UI just feeds it samples and the clock.
/// Not thread-safe: use from the UI thread.
/// </summary>
public sealed class PracticeSession
{
    private readonly List<PitchSample> _samples = new();
    private readonly Dictionary<int, NoteResult> _results = new();
    private int _nextToScore;
    private readonly List<TimedNote> _singNotes;

    public SessionPlan Plan { get; }
    public double ToleranceCents { get; }
    public IReadOnlyList<PitchSample> Samples => _samples;
    public IReadOnlyDictionary<int, NoteResult> Results => _results;
    public bool IsComplete { get; private set; }

    /// <summary>Raised when a sung note has finished and been scored.</summary>
    public event Action<NoteResult>? NoteScored;

    public PracticeSession(SessionPlan plan, double toleranceCents)
    {
        Plan = plan;
        ToleranceCents = toleranceCents;
        _singNotes = plan.SingNotes.ToList();
    }

    public void AddSample(PitchSample sample) => _samples.Add(sample);

    /// <summary>Advance the clock; scores any sung notes that have ended.</summary>
    public void Update(double now)
    {
        while (_nextToScore < _singNotes.Count && now >= _singNotes[_nextToScore].End + 0.1)
        {
            var note = _singNotes[_nextToScore++];
            var window = SamplesBetween(note.Start, note.End);
            var result = NoteScorer.Score(note, window, ToleranceCents);
            _results[note.Index] = result;
            NoteScored?.Invoke(result);
        }
        if (now >= Plan.TotalSeconds && _nextToScore >= _singNotes.Count) IsComplete = true;
    }

    /// <summary>Scores any remaining notes (e.g. when the user stops early) and builds the summary.</summary>
    public SessionSummary Finish(double now)
    {
        Update(now);
        return SessionSummary.From(Plan, _results.Values.OrderBy(r => r.Note.Index).ToList(), ToleranceCents);
    }

    private List<PitchSample> SamplesBetween(double start, double end)
    {
        // Samples arrive in time order — binary search for the first one.
        int lo = 0, hi = _samples.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_samples[mid].Time < start) lo = mid + 1; else hi = mid;
        }
        var list = new List<PitchSample>();
        for (int i = lo; i < _samples.Count && _samples[i].Time < end; i++) list.Add(_samples[i]);
        return list;
    }

    /// <summary>Signed cents from the given target for a live reading, or null if unvoiced.</summary>
    public static double? CentsOff(PitchSample sample, int targetMidi) =>
        sample.Midi is double m ? (m - targetMidi) * 100 : null;
}
