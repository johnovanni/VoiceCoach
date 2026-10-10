namespace VocalCoach.Core.Coaching;

public sealed record Lesson(string Title, string Icon, string Summary, IReadOnlyList<string> Points);

/// <summary>Short foundational lessons — the theory behind the exercises.</summary>
public static class Lessons
{
    public static IReadOnlyList<Lesson> All { get; } =
    [
        new("Reading the pitch graph", "📈",
            "How to use the visual feedback to sing in tune.",
            [
                "Each coloured bar is a target note. Time moves right to left; the vertical line is 'now'.",
                "Your voice draws a line. Higher on screen = higher pitch.",
                "Green means you're within tolerance, amber is close, red is off.",
                "Listen first (purple bars are demonstrations), then sing the blue bars.",
                "Above the bar? You're sharp — relax down. Below? You're flat — lift with energy, not force.",
            ]),
        new("Breathing for singing", "🌬️",
            "Breath is the engine of the voice. Good pitch starts here.",
            [
                "Inhale low and silently: belly and lower ribs expand outward, shoulders stay relaxed.",
                "Exhale slowly on a 'sss' for as long as you comfortably can. Aim for 20–30 seconds over time.",
                "While singing, keep the ribs gently open — resist the urge to collapse. This is 'support'.",
                "Running out of air causes flat, wobbly endings. Breathe before you need to.",
            ]),
        new("Posture", "🧍",
            "An aligned body lets the voice work freely.",
            [
                "Feet hip-width, weight balanced, knees unlocked.",
                "Imagine a string lifting the crown of your head; chin parallel to the floor.",
                "Shoulders down and back, chest comfortably open.",
                "Don't lift your chin for high notes or tuck it for low ones.",
            ]),
        new("Hearing pitch (ear training)", "👂",
            "Most pitch problems are listening problems.",
            [
                "Hear the note in your head before you open your mouth (audiation).",
                "Hum the reference note quietly, then open into the vowel.",
                "Use the Tuner tab: play a note, match it, and watch where your line lands.",
                "Practise intervals by linking them to songs you know.",
            ]),
        new("Resonance and tone", "🔔",
            "A ringing, forward tone is easier to tune and carries further.",
            [
                "Hum and feel the buzz on your lips and nose — that's forward resonance.",
                "Keep the back of the throat open, like the start of a yawn.",
                "Vowels shape tone: tall 'Ah', round 'Oh', bright but relaxed 'Ee'.",
            ]),
        new("Chest voice, head voice and mix", "🎚️",
            "Your voice has registers. Training them to blend removes cracks.",
            [
                "Chest voice: speech-like, rich, lower notes. Head voice: lighter, higher, more 'floaty'.",
                "The 'break' is where they switch. Sirens and lip trills smooth it out.",
                "Going up: use less weight, not more force. Going down: don't press.",
            ]),
        new("Vocal health", "💧",
            "Look after the instrument you can't replace.",
            [
                "Drink water throughout the day; avoid excess caffeine and alcohol before singing.",
                "Always warm up before demanding singing and cool down afterwards.",
                "Pain, hoarseness lasting more than two weeks, or loss of range: see an ENT or voice specialist.",
                "Rest your voice when sick. Whispering is more straining than gentle speech.",
            ]),
    ];
}
