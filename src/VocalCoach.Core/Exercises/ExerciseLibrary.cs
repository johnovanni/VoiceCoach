namespace VocalCoach.Core.Exercises;

/// <summary>The built-in exercise catalogue, ordered roughly as a lesson would run.</summary>
public static class ExerciseLibrary
{
    private static PatternStep N(int offset, double beats = 1) => new(offset, beats);
    private static PatternStep R(double beats = 1) => PatternStep.Rest(beats);

    public static IReadOnlyList<Exercise> All { get; } =
    [
        new()
        {
            Id = "hum-descend",
            Name = "Descending Hum",
            Category = ExerciseCategory.WarmUp,
            Difficulty = 1,
            Syllable = "Mm (hum)",
            Tempo = 72,
            Summary = "Wake the voice up gently with a hum sliding down five notes.",
            Pattern = [N(7), N(5), N(4), N(2), N(0, 2)],
            Repetitions = 6,
            StartAboveLow = 3,
            Instructions =
            [
                "Close your lips lightly, teeth apart, tongue relaxed.",
                "Hum each note — you should feel a buzz on your lips and nose.",
                "Keep the volume at a comfortable speaking level, no pushing.",
            ],
            CoachTips =
            [
                "If you don't feel a buzz, smile slightly and bring the sound forward.",
                "A hum is the safest warm-up: it lines up your vocal folds with minimal effort.",
            ],
        },
        new()
        {
            Id = "lip-trill-5",
            Name = "Lip Trill Five-Tone Scale",
            Category = ExerciseCategory.WarmUp,
            Difficulty = 1,
            Syllable = "Brr (lip trill)",
            Tempo = 100,
            Summary = "Balance breath and voice by trilling up and down a five-note scale.",
            Pattern = [N(0, 0.5), N(2, 0.5), N(4, 0.5), N(5, 0.5), N(7, 0.5), N(5, 0.5), N(4, 0.5), N(2, 0.5), N(0, 1.5)],
            Repetitions = 7,
            Instructions =
            [
                "Relax your lips and blow so they flutter like a motorboat — 'brrr'.",
                "Add voice to the trill and follow the notes up and back down.",
                "If your lips stop, press lightly on your cheeks with your fingertips.",
            ],
            CoachTips =
            [
                "Steady airflow keeps the trill going — that same steady air is what keeps pitch steady.",
                "Lip trills take strain off the throat, so they're perfect for exploring higher notes.",
            ],
        },
        new()
        {
            Id = "long-tones",
            Name = "Long Tones",
            Category = ExerciseCategory.Stability,
            Difficulty = 1,
            Syllable = "Ah",
            Tempo = 60,
            Summary = "Hold single notes perfectly still to build pitch stability and breath control.",
            Pattern = [N(0, 4), R(1), N(4, 4), R(1), N(7, 4)],
            Repetitions = 4,
            Instructions =
            [
                "Take a low, quiet breath — belly and lower ribs expand, shoulders stay down.",
                "Start each note cleanly, right in the centre of the target bar.",
                "Keep your line flat and green for the full length of the note.",
            ],
            CoachTips =
            [
                "Pitch that drifts flat at the end of a note usually means the breath is running out — breathe earlier.",
                "Wobbly line? Imagine the sound travelling forward on a smooth, slow stream of air.",
            ],
        },
        new()
        {
            Id = "major-arpeggio",
            Name = "Major Arpeggio",
            Category = ExerciseCategory.PitchAccuracy,
            Difficulty = 2,
            Syllable = "Ah / Ee",
            Tempo = 90,
            Summary = "Leap 1-3-5-8 and back to train your ear for the notes of a chord.",
            Pattern = [N(0), N(4), N(7), N(12), N(7), N(4), N(0, 2)],
            Repetitions = 5,
            Instructions =
            [
                "Hear each note in your head before you sing it.",
                "Aim for the centre of each bar — don't slide into the note.",
                "Keep the top note light; don't reach up with your chin.",
            ],
            CoachTips =
            [
                "Bigger leaps tend to land flat — think of the top note as 'higher and lighter' than you expect.",
                "Keep the 'Ah' tall and open, like the start of a yawn.",
            ],
        },
        new()
        {
            Id = "interval-leaps",
            Name = "Interval Leaps",
            Category = ExerciseCategory.PitchAccuracy,
            Difficulty = 2,
            Syllable = "Nah",
            Tempo = 80,
            Summary = "Jump from home to a third, fourth, fifth and octave — the building blocks of melody.",
            Pattern = [N(0), N(4), N(0), N(5), N(0), N(7), N(0), N(12), N(0, 2)],
            Repetitions = 4,
            Instructions =
            [
                "Always come back to the same home note — keep it identical every time.",
                "Use the 'N' to start each note cleanly.",
                "Listen carefully during the demonstration, then match it.",
            ],
            CoachTips =
            [
                "If the home note creeps upward, you're carrying tension from the leaps — reset your jaw.",
                "Associate intervals with songs: a 4th starts 'Here Comes the Bride', an octave starts 'Somewhere Over the Rainbow'.",
            ],
        },
        new()
        {
            Id = "staccato-arpeggio",
            Name = "Staccato Arpeggio",
            Category = ExerciseCategory.Agility,
            Difficulty = 2,
            Syllable = "Ha",
            Tempo = 96,
            Summary = "Short bouncy notes that train clean onsets and an active breath.",
            Pattern = [N(0, 0.5), R(0.5), N(4, 0.5), R(0.5), N(7, 0.5), R(0.5), N(12, 0.5), R(0.5), N(7, 0.5), R(0.5), N(4, 0.5), R(0.5), N(0, 1)],
            Repetitions = 5,
            Instructions =
            [
                "Put a hand on your belly: each 'Ha' should give a small inward bounce.",
                "Notes are short and separate — no sliding between them.",
                "Hit the pitch immediately; there's no time to adjust.",
            ],
            CoachTips =
            [
                "Clean staccato means your folds and breath are coordinating — the payoff is cleaner starts to every phrase.",
                "Don't squeeze the throat to stop each note; let the breath bounce stop it.",
            ],
        },
        new()
        {
            Id = "octave-siren",
            Name = "Octave Siren",
            Category = ExerciseCategory.Range,
            Difficulty = 2,
            Syllable = "Ng / Woo",
            Tempo = 70,
            Summary = "Glide smoothly through an octave to blend chest and head voice.",
            Pattern = [N(0, 2), N(12, 2), N(0, 3)],
            Repetitions = 5,
            Instructions =
            [
                "Start on the low note, then slide up like a siren to the top note and back.",
                "Make the slide smooth — no breaks, cracks or jumps in the line.",
                "Let the voice get lighter as it rises; don't drag chest voice up.",
            ],
            CoachTips =
            [
                "A crack in the middle is normal while your registers learn to blend — go lighter there.",
                "The 'ng' (as in 'sing') keeps the back of the tongue up and makes blending easier.",
            ],
        },
        new()
        {
            Id = "minor-scale",
            Name = "Minor Five-Tone Scale",
            Category = ExerciseCategory.PitchAccuracy,
            Difficulty = 2,
            Syllable = "Oh",
            Tempo = 88,
            Summary = "Train your ear on the minor third — a note singers often sing too high.",
            Pattern = [N(0), N(2), N(3), N(5), N(7), N(5), N(3), N(2), N(0, 2)],
            Repetitions = 5,
            Instructions =
            [
                "Listen for the darker colour of the third note.",
                "Keep the 'Oh' round, lips slightly forward.",
                "Watch the third: is your dot sitting on the bar, or above it?",
            ],
            CoachTips =
            [
                "Minor thirds feel 'sad' — let that colour guide your ear down to the correct pitch.",
            ],
        },
        new()
        {
            Id = "rossini-scale",
            Name = "Nine-Tone Scale",
            Category = ExerciseCategory.Agility,
            Difficulty = 3,
            Syllable = "Ah",
            Tempo = 112,
            Summary = "A fast scale over a ninth for agility, breath stamina and accuracy at speed.",
            Pattern =
            [
                N(0, 0.5), N(2, 0.5), N(4, 0.5), N(5, 0.5), N(7, 0.5), N(9, 0.5), N(11, 0.5), N(12, 0.5),
                N(14, 0.5), N(12, 0.5), N(11, 0.5), N(9, 0.5), N(7, 0.5), N(5, 0.5), N(4, 0.5), N(2, 0.5), N(0, 2),
            ],
            Repetitions = 4,
            Instructions =
            [
                "Take a full breath — the whole run is sung on one breath.",
                "Keep every note distinct; aim for a string of pearls, not a smear.",
                "If it falls apart, learn it at an easier difficulty level first.",
            ],
            CoachTips =
            [
                "Speed comes from accuracy, not effort. Slow and clean beats fast and messy.",
                "Think of the top of the run as the easiest part — don't brace for it.",
            ],
        },
        new()
        {
            Id = "cooldown-sigh",
            Name = "Cool-down Slides",
            Category = ExerciseCategory.CoolDown,
            Difficulty = 1,
            Syllable = "Hoo (sigh)",
            Tempo = 60,
            Summary = "Relaxed downward slides to release tension after practice.",
            Pattern = [N(7, 2), N(0, 3)],
            Repetitions = 4,
            StartAboveLow = 1,
            Instructions =
            [
                "Sing a soft, breathy 'Hoo' and let it fall like a gentle sigh.",
                "Volume stays low; nothing should feel effortful.",
                "Finish with a few slow breaths and a sip of water.",
            ],
            CoachTips =
            [
                "Cooling down helps your voice recover — especially after high or loud practice.",
            ],
        },
    ];

    public static Exercise Get(string id) =>
        All.FirstOrDefault(e => e.Id == id) ?? throw new KeyNotFoundException($"No exercise with id '{id}'.");

    public static string CategoryName(ExerciseCategory c) => c switch
    {
        ExerciseCategory.WarmUp => "Warm-up",
        ExerciseCategory.PitchAccuracy => "Pitch Accuracy",
        ExerciseCategory.Stability => "Stability",
        ExerciseCategory.Agility => "Agility",
        ExerciseCategory.Range => "Range",
        ExerciseCategory.CoolDown => "Cool-down",
        _ => c.ToString(),
    };
}
