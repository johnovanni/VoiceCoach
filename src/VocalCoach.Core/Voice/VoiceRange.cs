using VocalCoach.Core.Music;

namespace VocalCoach.Core.Voice;

public enum VoiceType { Bass, Baritone, Tenor, Alto, MezzoSoprano, Soprano }

/// <summary>A comfortable singing range in MIDI notes (inclusive).</summary>
public sealed record VoiceRange(int LowMidi, int HighMidi)
{
    public int Span => HighMidi - LowMidi;
    public override string ToString() => $"{NoteMath.NoteName(LowMidi)} – {NoteMath.NoteName(HighMidi)}";
}

public static class VoiceTypes
{
    /// <summary>
    /// Comfortable practice ranges — deliberately narrower than the full classical ranges so
    /// exercises never push a developing voice to its extremes.
    /// </summary>
    public static VoiceRange ComfortableRange(VoiceType type) => type switch
    {
        VoiceType.Bass => new(40, 60),          // E2 – C4
        VoiceType.Baritone => new(43, 64),      // G2 – E4
        VoiceType.Tenor => new(48, 69),         // C3 – A4
        VoiceType.Alto => new(53, 74),          // F3 – D5
        VoiceType.MezzoSoprano => new(57, 77),  // A3 – F5
        VoiceType.Soprano => new(60, 81),       // C4 – A5
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public static string DisplayName(VoiceType type) => type == VoiceType.MezzoSoprano ? "Mezzo-soprano" : type.ToString();

    /// <summary>Suggests the voice type whose comfortable range best overlaps a measured range.</summary>
    public static VoiceType Suggest(VoiceRange measured)
    {
        return Enum.GetValues<VoiceType>()
            .OrderByDescending(t => Overlap(ComfortableRange(t), measured))
            .ThenBy(t => Math.Abs(Centre(ComfortableRange(t)) - Centre(measured)))
            .First();

        static int Overlap(VoiceRange a, VoiceRange b) => Math.Max(0, Math.Min(a.HighMidi, b.HighMidi) - Math.Max(a.LowMidi, b.LowMidi));
        static double Centre(VoiceRange r) => (r.LowMidi + r.HighMidi) / 2.0;
    }
}
