#if DEBUG
namespace VocalCoach.App.Services;

/// <summary>
/// Debug-only test rig: the "microphone" hears whatever the player plays, slightly detuned
/// to mimic a singer, with no sound card involved. Enable with VOCALCOACH_LOOPBACK=1 and turn on
/// "Play along while I sing" so there's audio during the sing phases.
/// With VOCALCOACH_LOOPBACK=range, silence is replaced by a singer holding A2 then A3 for
/// 3 s each, which is enough to drive the Range Finder end to end.
/// </summary>
public sealed class LoopbackAudio : IAudioInput, IAudioPlayer
{
    private const double DetuneCents = -12; // sing a little flat, like a real beginner
    private readonly object _gate = new();
    private float[] _playing = [];
    private double _position;
    private Timer? _timer;
    private readonly Random _noise = new(1);
    private readonly float[] _chunk = new float[441];
    private readonly System.Diagnostics.Stopwatch _clock = new();
    private long _produced;
    private readonly object _pumpGate = new();
    private readonly bool _simulateRange = Environment.GetEnvironmentVariable("VOCALCOACH_LOOPBACK") == "range";

    public int SampleRate => 44100;
    public event AudioSamplesHandler? SamplesAvailable;

    public void Play(float[] samples)
    {
        lock (_gate) { _playing = samples; _position = 0; }
    }

    public void Stop()
    {
        lock (_gate) { _playing = []; }
        _timer?.Dispose();
        _timer = null;
    }

    public Task<bool> StartAsync()
    {
        _produced = 0;
        _clock.Restart();
        _timer = new Timer(_ => Pump(), null, 0, 10);
        return Task.FromResult(true);
    }

    /// <summary>Deliver as many 10 ms chunks as wall-clock time says are due, so timestamps don't drift.</summary>
    private void Pump()
    {
        // Timer callbacks can overlap; real audio drivers deliver buffers one at a time.
        if (!Monitor.TryEnter(_pumpGate)) return;
        try
        {
            while (_produced + _chunk.Length <= _clock.Elapsed.TotalSeconds * SampleRate)
            {
                PumpChunk();
                _produced += _chunk.Length;
            }
        }
        finally
        {
            Monitor.Exit(_pumpGate);
        }
    }

    private void PumpChunk()
    {
        double rate = Math.Pow(2, DetuneCents / 1200);
        lock (_gate)
        {
            for (int i = 0; i < _chunk.Length; i++)
            {
                int idx = (int)_position;
                float s = idx < _playing.Length ? _playing[idx] * 3f // boost: accompaniment is quiet
                        : _simulateRange ? SimulatedSinger(_produced + i) : 0f;
                _chunk[i] = s + (float)(_noise.NextDouble() - 0.5) * 0.004f;
                _position += rate;
            }
        }
        SamplesAvailable?.Invoke(_chunk);
    }

    /// <summary>Alternates 3 s on A2 (MIDI 45) with 3 s on A3 (MIDI 57), with harmonics like a voice.</summary>
    private float SimulatedSinger(long sample)
    {
        double t = (double)sample / SampleRate;
        double freq = (int)(t / 3) % 2 == 0 ? 110.0 : 220.0;
        double s = 0;
        for (int h = 1; h <= 4; h++) s += Math.Sin(2 * Math.PI * freq * h * t) / h;
        return (float)(0.2 * s);
    }
}
#endif
