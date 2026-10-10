using System.Collections.Concurrent;
using VocalCoach.Core.Audio;

namespace VocalCoach.App.Services;

/// <summary>
/// Connects the platform microphone to the Core pitch tracker. Pitch samples are
/// produced on the audio thread and queued; the UI drains the queue each frame.
/// </summary>
public sealed class PitchEngine
{
    private readonly IAudioInput _input;
    private PitchTracker? _tracker;

    public ConcurrentQueue<PitchSample> Samples { get; } = new();
    public bool IsRunning { get; private set; }
    public string? LastError { get; private set; }

    public PitchEngine(IAudioInput input)
    {
        _input = input;
        _input.SamplesAvailable += OnSamples;
    }

    public async Task<bool> StartAsync()
    {
        if (IsRunning) Stop();
        LastError = null;

#if IOS
        // iOS needs an explicit prompt (text comes from NSMicrophoneUsageDescription in Info.plist).
        // Unpackaged Windows apps have no AppxManifest for MAUI to check; Windows privacy settings
        // govern mic access there and a refusal surfaces as a capture failure below.
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
            status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted)
        {
            LastError = "Microphone access was denied. Enable it in Settings › Privacy › Microphone.";
            return false;
        }
#else
        await Task.CompletedTask;
#endif

        Samples.Clear();
        _tracker = null; // new tracker = timestamps restart at zero
        try
        {
            IsRunning = await _input.StartAsync();
            if (!IsRunning) LastError = "No microphone was found.";
        }
        catch (Exception ex)
        {
            LastError = $"Couldn't start the microphone: {ex.Message}";
            IsRunning = false;
        }
        return IsRunning;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        _input.Stop();
        IsRunning = false;
    }

    private void OnSamples(ReadOnlySpan<float> samples)
    {
        if (_tracker is null)
        {
            _tracker = new PitchTracker(_input.SampleRate);
            _tracker.PitchDetected += Samples.Enqueue;
        }
        _tracker.Process(samples);
    }
}
