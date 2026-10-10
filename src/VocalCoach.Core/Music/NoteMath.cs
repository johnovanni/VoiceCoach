namespace VocalCoach.Core.Music;

/// <summary>
/// Conversions between frequency, MIDI note numbers, cents and note names.
/// MIDI 69 = A4 = 440 Hz; MIDI 60 = middle C (C4).
/// </summary>
public static class NoteMath
{
    public const double A4Frequency = 440.0;
    public const int A4Midi = 69;

    private static readonly string[] SharpNames = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];

    public static double MidiToFrequency(double midi) => A4Frequency * Math.Pow(2, (midi - A4Midi) / 12.0);

    /// <summary>Fractional MIDI number, e.g. 69.25 is A4 plus 25 cents.</summary>
    public static double FrequencyToMidi(double frequency) => A4Midi + 12.0 * Math.Log2(frequency / A4Frequency);

    /// <summary>Signed distance in cents from <paramref name="targetMidi"/> to <paramref name="midi"/>.</summary>
    public static double CentsFrom(double midi, double targetMidi) => (midi - targetMidi) * 100.0;

    public static string NoteName(int midi) => $"{SharpNames[Mod(midi, 12)]}{midi / 12 - 1}";

    /// <summary>Pitch class only, e.g. "F#".</summary>
    public static string PitchClass(int midi) => SharpNames[Mod(midi, 12)];

    public static bool IsBlackKey(int midi) => SharpNames[Mod(midi, 12)].Length == 2;

    /// <summary>Parses names like "C4", "F#3", "Bb2".</summary>
    public static int ParseNote(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        name = name.Trim();
        int i = 1;
        int pc = Array.IndexOf(SharpNames, name[..1].ToUpperInvariant());
        if (pc < 0) throw new FormatException($"Invalid note name '{name}'.");
        while (i < name.Length && (name[i] == '#' || name[i] == 'b'))
        {
            pc += name[i] == '#' ? 1 : -1;
            i++;
        }
        if (!int.TryParse(name[i..], out int octave)) throw new FormatException($"Invalid note name '{name}'.");
        return (octave + 1) * 12 + pc;
    }

    private static int Mod(int a, int n) => ((a % n) + n) % n;
}
