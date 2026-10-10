using VocalCoach.Core.Audio;
using VocalCoach.Core.Coaching;
using VocalCoach.Core.Exercises;
using VocalCoach.Core.Music;
using VocalCoach.Core.Practice;
using VocalCoach.Core.Progress;
using VocalCoach.Core.Voice;

namespace VocalCoach.Core.Tests;

public class NoteMathTests
{
    [Theory]
    [InlineData("A4", 69)]
    [InlineData("C4", 60)]
    [InlineData("F#3", 54)]
    [InlineData("Bb2", 46)]
    [InlineData("E2", 40)]
    public void ParsesNoteNames(string name, int midi) => Assert.Equal(midi, NoteMath.ParseNote(name));

    [Fact]
    public void RoundTripsFrequency()
    {
        Assert.Equal(440, NoteMath.MidiToFrequency(69), 6);
        Assert.Equal(261.6256, NoteMath.MidiToFrequency(60), 3);
        Assert.Equal(60.0, NoteMath.FrequencyToMidi(261.6256), 3);
        Assert.Equal("C#5", NoteMath.NoteName(73));
    }
}

public class PitchDetectionTests
{
    private const int Rate = 44100;

    /// <summary>A voice-like test signal: fundamental plus strong harmonics plus noise.</summary>
    private static float[] Voice(double freq, double seconds, double noise = 0.02, int seed = 1)
    {
        var rng = new Random(seed);
        var buf = new float[(int)(seconds * Rate)];
        double[] harm = [1.0, 0.8, 0.6, 0.4, 0.3, 0.2];
        for (int i = 0; i < buf.Length; i++)
        {
            double s = 0;
            for (int h = 0; h < harm.Length; h++) s += harm[h] * Math.Sin(2 * Math.PI * freq * (h + 1) * i / Rate);
            buf[i] = (float)(0.15 * s + noise * (rng.NextDouble() * 2 - 1));
        }
        return buf;
    }

    [Theory]
    [InlineData(82.41)]   // E2 — low bass
    [InlineData(130.81)]  // C3
    [InlineData(220.0)]   // A3
    [InlineData(440.0)]   // A4
    [InlineData(880.0)]   // A5 — high soprano
    [InlineData(1046.5)]  // C6
    public void DetectsVoicePitchWithinFiveCents(double freq)
    {
        var detector = new YinPitchDetector(Rate);
        var reading = detector.Detect(Voice(freq, 0.1));
        Assert.NotNull(reading);
        double cents = 1200 * Math.Log2(reading.Value.Frequency / freq);
        Assert.InRange(cents, -5, 5);
    }

    [Fact]
    public void IgnoresNoise()
    {
        var rng = new Random(3);
        var noise = Enumerable.Range(0, 4096).Select(_ => (float)(rng.NextDouble() * 2 - 1) * 0.3f).ToArray();
        var tracker = new PitchTracker(Rate);
        var samples = new List<PitchSample>();
        tracker.PitchDetected += samples.Add;
        tracker.Process(noise);
        Assert.NotEmpty(samples);
        Assert.All(samples, s => Assert.False(s.IsVoiced));
    }

    [Fact]
    public void TrackerReportsTimestampsAndSilence()
    {
        var tracker = new PitchTracker(Rate);
        var samples = new List<PitchSample>();
        tracker.PitchDetected += samples.Add;

        var audio = new float[Rate]; // 0.5 s silence then 0.5 s of A3
        Voice(220, 0.5).CopyTo(audio, Rate / 2);
        // Feed in odd-sized chunks like a real audio callback.
        for (int i = 0; i < audio.Length; i += 441) tracker.Process(audio.AsSpan(i, Math.Min(441, audio.Length - i)));

        Assert.All(samples.Where(s => s.Time < 0.45), s => Assert.False(s.IsVoiced));
        var voiced = samples.Where(s => s.Time > 0.6).ToList();
        Assert.NotEmpty(voiced);
        Assert.All(voiced, s => Assert.InRange(s.Midi!.Value, 56.9, 57.1)); // A3 = 57
    }

    [Fact]
    public void SynthesisedReferenceToneIsDetectedAsItsNote()
    {
        var tone = ToneSynth.RenderSingle(64, 0.5, Rate); // E4
        var reading = new YinPitchDetector(Rate).Detect(tone.AsSpan(4000));
        Assert.NotNull(reading);
        Assert.Equal(64, NoteMath.FrequencyToMidi(reading.Value.Frequency), 1);
    }
}

public class SessionTests
{
    private static readonly VoiceRange Tenor = VoiceTypes.ComfortableRange(VoiceType.Tenor);

    [Fact]
    public void PlanStaysInsideComfortableRange()
    {
        foreach (var ex in ExerciseLibrary.All)
        {
            foreach (var type in Enum.GetValues<VoiceType>())
            {
                var range = VoiceTypes.ComfortableRange(type);
                var plan = SessionPlan.Build(ex, range);
                Assert.All(plan.Notes, n => Assert.InRange(n.Midi, range.LowMidi, range.HighMidi));
                Assert.Equal(plan.Notes.Count(n => n.Phase == NotePhase.Listen), plan.SingNotes.Count());
            }
        }
    }

    [Fact]
    public void PlanAscendsBySemitoneEachRepetition()
    {
        var plan = SessionPlan.Build(ExerciseLibrary.Get("hum-descend"), Tenor);
        var lastNotes = plan.SingNotes.GroupBy(n => n.Repetition).Select(g => g.Last().Midi).ToList();
        for (int i = 1; i < lastNotes.Count; i++) Assert.Equal(lastNotes[i - 1] + 1, lastNotes[i]);
        // Listen phrase precedes the sing phrase for each repetition.
        var rep0 = plan.Notes.Where(n => n.Repetition == 0).ToList();
        Assert.True(rep0.Where(n => n.Phase == NotePhase.Listen).Max(n => n.End) <= rep0.Where(n => n.Phase == NotePhase.Sing).Min(n => n.Start));
    }

    private static List<PitchSample> Sing(TimedNote note, double cents, double wobble = 0, double step = 0.023)
    {
        var list = new List<PitchSample>();
        int i = 0;
        for (double t = note.Start; t < note.End; t += step, i++)
            list.Add(new PitchSample(t, note.Midi + (cents + wobble * Math.Sin(i)) / 100, 0.1));
        return list;
    }

    [Fact]
    public void ReferenceAudioIsTrackedWithoutOutliers()
    {
        // End-to-end: synthesise a whole session, run it through the tracker, and check every
        // voiced reading lands within a quarter-tone of a note that was playing.
        var plan = SessionPlan.Build(ExerciseLibrary.Get("major-arpeggio"), Tenor);
        var audio = plan.RenderReferenceAudio(44100, accompanySinging: true).AsSpan(0, 44100 * 15).ToArray(); // first two keys
        var tracker = new PitchTracker(44100);
        var readings = new List<PitchSample>();
        tracker.PitchDetected += readings.Add;
        for (int i = 0; i < audio.Length; i += 441) tracker.Process(audio.AsSpan(i, Math.Min(441, audio.Length - i)));

        var voiced = readings.Where(r => r.IsVoiced).ToList();
        Assert.True(voiced.Count > 100);
        Assert.All(voiced, r =>
        {
            var nearby = plan.Notes.Where(n => r.Time >= n.Start - 0.05 && r.Time < n.End + 0.1);
            Assert.Contains(nearby, n => Math.Abs((r.Midi!.Value - n.Midi) * 100) < 50);
        });
    }

    [Fact]
    public void ScoresOnPitchHighAndOffPitchLow()
    {
        var note = new TimedNote(0, 60, 1, 1.5, NotePhase.Sing, 0);
        var good = NoteScorer.Score(note, Sing(note, 5), 25);
        var flat = NoteScorer.Score(note, Sing(note, -60), 25);
        var silent = NoteScorer.Score(note, [new PitchSample(1.5, null, 0)], 25);

        Assert.True(good.Score > 90, $"good {good.Score}");
        Assert.True(flat.Score < 30, $"flat {flat.Score}");
        Assert.True(silent.Missed);
        Assert.Equal(-60, flat.MeanCents, 1);
    }

    [Fact]
    public void DetectsOctaveErrorsAndScoops()
    {
        var note = new TimedNote(0, 60, 0, 1.0, NotePhase.Sing, 0);
        var octave = NoteScorer.Score(note, Sing(note, -1200), 25);
        Assert.True(octave.OctaveOff);

        var scoop = Sing(note, 0);
        scoop[0] = scoop[0] with { Midi = 59.0 };
        scoop[1] = scoop[1] with { Midi = 59.3 };
        Assert.True(NoteScorer.Score(note, scoop, 25).Scooped);
    }

    [Fact]
    public void SessionScoresNotesAsTheyEndAndCoachesFlatSinger()
    {
        var plan = SessionPlan.Build(ExerciseLibrary.Get("long-tones"), Tenor);
        var session = new PracticeSession(plan, 25);
        var scored = new List<NoteResult>();
        session.NoteScored += scored.Add;

        foreach (var note in plan.SingNotes)
            foreach (var s in Sing(note, -45)) session.AddSample(s);

        session.Update(plan.SingNotes.First().End + 0.2);
        Assert.Single(scored);

        var summary = session.Finish(plan.TotalSeconds);
        Assert.True(session.IsComplete);
        Assert.Equal(plan.SingNotes.Count(), summary.Notes.Count);
        Assert.Equal(PitchTendency.Flat, summary.Tendency);

        var tips = CoachingEngine.Review(summary);
        Assert.Contains(tips, t => t.Title.Contains("flat", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void LiveCueGuidesDirection()
    {
        Assert.Equal(CueLevel.OnPitch, CoachingEngine.Live(10, 25).Level);
        Assert.Contains("flat", CoachingEngine.Live(-40, 25).Text);
        Assert.Contains("Sharp", CoachingEngine.Live(80, 25).Text);
        Assert.Equal(CueLevel.Silent, CoachingEngine.Live(null, 25).Level);
    }
}

public class VoiceAndProgressTests
{
    [Fact]
    public void SuggestsVoiceTypeFromMeasuredRange()
    {
        Assert.Equal(VoiceType.Bass, VoiceTypes.Suggest(new VoiceRange(39, 58)));
        Assert.Equal(VoiceType.Tenor, VoiceTypes.Suggest(new VoiceRange(48, 70)));
        Assert.Equal(VoiceType.Soprano, VoiceTypes.Suggest(new VoiceRange(60, 83)));
    }

    [Fact]
    public void SteadyNoteCaptureWaitsForAHeldNote()
    {
        var capture = new SteadyNoteCapture { HoldSeconds = 1.0 };
        int? result = null;
        for (double t = 0; t < 2 && result is null; t += 0.02)
            result = capture.Add(new PitchSample(t, t < 0.5 ? 50 + t * 10 : 52.1, 0.1)); // slide, then hold
        Assert.Equal(52, result);
    }

    [Fact]
    public void PersistsProfileAndHistoryWithStreak()
    {
        var dir = Path.Combine(Path.GetTempPath(), "vc-test-" + Guid.NewGuid());
        try
        {
            var store = new ProgressStore(dir);
            store.SaveProfile(store.Profile with { VoiceType = VoiceType.Alto, Level = SkillLevel.Intermediate });
            var today = new DateTime(2026, 10, 2, 18, 0, 0);
            store.AddSession(new SessionRecord(today.AddDays(-2), "long-tones", "Long Tones", 70, 60, -10, 12, 12));
            store.AddSession(new SessionRecord(today.AddDays(-1), "long-tones", "Long Tones", 80, 70, -5, 10, 12));
            store.AddSession(new SessionRecord(today, "major-arpeggio", "Major Arpeggio", 65, 55, 3, 15, 30));

            var reloaded = new ProgressStore(dir);
            Assert.Equal(VoiceType.Alto, reloaded.Profile.VoiceType);
            Assert.Equal(3, reloaded.History.Count);
            Assert.Equal(3, reloaded.CurrentStreak(today));
            Assert.Equal(80, reloaded.BestScore("long-tones"));

            var routine = CoachingEngine.TodaysRoutine(reloaded.History, SkillLevel.Intermediate, today);
            Assert.Equal(ExerciseCategory.WarmUp, routine[0].Category);
            Assert.Equal(ExerciseCategory.CoolDown, routine[^1].Category);
            Assert.DoesNotContain(routine.Skip(1).SkipLast(1), e => e.Id == "long-tones"); // already practised; untried come first
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
