using VocalCoach.Core.Music;

namespace VocalCoach.Core.Audio;

/// <summary>
/// Renders reference tones (a soft, piano-like timbre) into a mono float buffer.
/// Rendering the whole session up front keeps playback sample-accurate and lets every
/// platform use the same tiny "play this buffer" audio service.
/// </summary>
public static class ToneSynth
{
    public readonly record struct Tone(double StartSeconds, double DurationSeconds, double Midi, double Volume = 0.5);

    public static float[] Render(IEnumerable<Tone> tones, int sampleRate, double totalSeconds)
    {
        var buffer = new float[(int)Math.Ceiling(totalSeconds * sampleRate)];
        foreach (var tone in tones) Mix(buffer, tone, sampleRate);

        // Soft clip so overlapping tones can't wrap around.
        for (int i = 0; i < buffer.Length; i++) buffer[i] = (float)Math.Tanh(buffer[i]);
        return buffer;
    }

    public static float[] RenderSingle(double midi, double seconds, int sampleRate, double volume = 0.5) =>
        Render([new Tone(0, seconds, midi, volume)], sampleRate, seconds + 0.05);

    private static void Mix(float[] buffer, Tone tone, int sampleRate)
    {
        double freq = NoteMath.MidiToFrequency(tone.Midi);
        int start = (int)(tone.StartSeconds * sampleRate);
        int length = (int)(tone.DurationSeconds * sampleRate);
        double attack = 0.015 * sampleRate;
        double release = Math.Min(0.08 * sampleRate, length / 3.0);

        // A few harmonics with falling amplitude give a warm tone that is easy to match.
        ReadOnlySpan<double> harmonics = [1.0, 0.45, 0.22, 0.12, 0.06];
        double twoPiF = 2 * Math.PI * freq / sampleRate;

        for (int i = 0; i < length && start + i < buffer.Length; i++)
        {
            double env = i < attack ? i / attack
                       : i > length - release ? (length - i) / release
                       : 1.0;
            env *= 0.75 + 0.25 * Math.Exp(-3.0 * i / sampleRate); // gentle decay after the attack

            double s = 0;
            for (int h = 0; h < harmonics.Length; h++) s += harmonics[h] * Math.Sin(twoPiF * (h + 1) * i);
            buffer[start + i] += (float)(s * env * tone.Volume * 0.5);
        }
    }
}
