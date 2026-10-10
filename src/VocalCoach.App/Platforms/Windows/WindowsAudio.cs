using NAudio.Wave;
using VocalCoach.App.Services;

namespace VocalCoach.App.Platforms.Windows;

public sealed class WindowsAudioInput : IAudioInput
{
    private WaveInEvent? _waveIn;
    private float[] _buffer = [];

    public int SampleRate => 44100;
    public event AudioSamplesHandler? SamplesAvailable;

    public Task<bool> StartAsync()
    {
        if (WaveInEvent.DeviceCount == 0) return Task.FromResult(false);
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(SampleRate, 16, 1),
            BufferMilliseconds = 20, // small buffers keep visual feedback responsive
            NumberOfBuffers = 4,
        };
        _waveIn.DataAvailable += OnData;
        _waveIn.StartRecording();
        return Task.FromResult(true);
    }

    public void Stop()
    {
        if (_waveIn is null) return;
        _waveIn.DataAvailable -= OnData;
        _waveIn.StopRecording();
        _waveIn.Dispose();
        _waveIn = null;
    }

    private void OnData(object? sender, WaveInEventArgs e)
    {
        int count = e.BytesRecorded / 2;
        if (_buffer.Length < count) _buffer = new float[count];
        for (int i = 0; i < count; i++) _buffer[i] = BitConverter.ToInt16(e.Buffer, i * 2) / 32768f;
        SamplesAvailable?.Invoke(_buffer.AsSpan(0, count));
    }
}

public sealed class WindowsAudioPlayer : IAudioPlayer
{
    private WaveOutEvent? _output;

    public int SampleRate => 44100;

    public void Play(float[] samples)
    {
        Stop();
        _output = new WaveOutEvent { DesiredLatency = 100 };
        _output.Init(new BufferSampleProvider(samples, SampleRate));
        _output.Play();
    }

    public void Stop()
    {
        _output?.Stop();
        _output?.Dispose();
        _output = null;
    }

    private sealed class BufferSampleProvider(float[] data, int sampleRate) : ISampleProvider
    {
        private int _position;
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);

        public int Read(float[] buffer, int offset, int count)
        {
            int n = Math.Min(count, data.Length - _position);
            // Element-by-element on purpose: NAudio passes a byte[] reinterpreted as float[]
            // (WaveBuffer), so Array.Copy's runtime type check throws ArrayTypeMismatchException.
            for (int i = 0; i < n; i++) buffer[offset + i] = data[_position + i];
            _position += n;
            return n;
        }
    }
}
