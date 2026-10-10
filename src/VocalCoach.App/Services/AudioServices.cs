namespace VocalCoach.App.Services;

public delegate void AudioSamplesHandler(ReadOnlySpan<float> samples);

/// <summary>Microphone capture. Implemented per platform under Platforms/.</summary>
public interface IAudioInput
{
    /// <summary>Valid once <see cref="StartAsync"/> has returned true.</summary>
    int SampleRate { get; }

    /// <summary>Raised on an audio thread with mono samples in -1..1.</summary>
    event AudioSamplesHandler? SamplesAvailable;

    Task<bool> StartAsync();
    void Stop();
}

/// <summary>Plays a pre-rendered mono buffer (reference tones). Implemented per platform.</summary>
public interface IAudioPlayer
{
    int SampleRate { get; }
    void Play(float[] samples);
    void Stop();
}
