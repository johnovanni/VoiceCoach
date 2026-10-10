using System.Runtime.InteropServices;
using AVFoundation;
using VocalCoach.App.Services;

namespace VocalCoach.App.Platforms.iOS;

internal static class IosAudioSession
{
    private static bool _configured;

    /// <summary>Play-and-record lets reference tones and the microphone run together.</summary>
    public static void Activate()
    {
        if (_configured) return;
        var session = AVAudioSession.SharedInstance();
        session.SetCategory(
            AVAudioSessionCategory.PlayAndRecord,
            AVAudioSessionCategoryOptions.DefaultToSpeaker | AVAudioSessionCategoryOptions.AllowBluetoothA2DP,
            out _);
        session.SetPreferredIOBufferDuration(0.01, out _); // ~10 ms for responsive feedback
        session.SetActive(true, out _);
        _configured = true;
    }
}

public sealed class IosAudioInput : IAudioInput
{
    private AVAudioEngine? _engine;
    private float[] _buffer = [];

    public int SampleRate { get; private set; } = 48000;
    public event AudioSamplesHandler? SamplesAvailable;

    public Task<bool> StartAsync()
    {
        IosAudioSession.Activate();
        _engine = new AVAudioEngine();
        var input = _engine.InputNode;
        var format = input.GetBusOutputFormat(0);
        if (format.ChannelCount == 0) return Task.FromResult(false);
        SampleRate = (int)format.SampleRate;

        input.InstallTapOnBus(0, 1024, format, (buffer, _) =>
        {
            int frames = (int)buffer.FrameLength;
            if (_buffer.Length < frames) _buffer = new float[frames];
            // FloatChannelData is float**; channel 0 is all we need for pitch.
            IntPtr channel0 = Marshal.ReadIntPtr(buffer.FloatChannelData);
            Marshal.Copy(channel0, _buffer, 0, frames);
            SamplesAvailable?.Invoke(_buffer.AsSpan(0, frames));
        });

        _engine.Prepare();
        return Task.FromResult(_engine.StartAndReturnError(out _));
    }

    public void Stop()
    {
        if (_engine is null) return;
        _engine.InputNode.RemoveTapOnBus(0);
        _engine.Stop();
        _engine.Dispose();
        _engine = null;
    }
}

public sealed class IosAudioPlayer : IAudioPlayer
{
    private AVAudioEngine? _engine;
    private AVAudioPlayerNode? _player;

    public int SampleRate => 44100;

    public void Play(float[] samples)
    {
        Stop();
        IosAudioSession.Activate();

        var format = new AVAudioFormat(SampleRate, 1);
        var buffer = new AVAudioPcmBuffer(format, (uint)samples.Length) { FrameLength = (uint)samples.Length };
        Marshal.Copy(samples, 0, Marshal.ReadIntPtr(buffer.FloatChannelData), samples.Length);

        _engine = new AVAudioEngine();
        _player = new AVAudioPlayerNode();
        _engine.AttachNode(_player);
        _engine.Connect(_player, _engine.MainMixerNode, format);
        _engine.Prepare();
        if (!_engine.StartAndReturnError(out _)) return;

        _player.ScheduleBuffer(buffer, () => { });
        _player.Play();
    }

    public void Stop()
    {
        _player?.Stop();
        _engine?.Stop();
        _player?.Dispose();
        _engine?.Dispose();
        _player = null;
        _engine = null;
    }
}
