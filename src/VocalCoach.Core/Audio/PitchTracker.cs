using VocalCoach.Core.Music;

namespace VocalCoach.Core.Audio;

/// <summary>A pitch measurement at a moment in time. <see cref="Midi"/> is null when nothing is being sung.</summary>
/// <param name="Time">Seconds since the tracker was reset (audio-clock time of the window centre).</param>
/// <param name="Midi">Fractional MIDI note number, e.g. 60.12 = C4 + 12 cents.</param>
/// <param name="Level">RMS level of the window, 0..1.</param>
public readonly record struct PitchSample(double Time, double? Midi, double Level)
{
    public bool IsVoiced => Midi.HasValue;
}

/// <summary>
/// Turns a stream of raw microphone samples into a stream of smoothed pitch samples.
/// Feed it whatever buffer sizes the platform delivers; it re-windows internally.
/// Not thread-safe: call <see cref="Process"/> from a single (audio) thread.
/// </summary>
public sealed class PitchTracker
{
    private readonly YinPitchDetector _detector;
    private readonly float[] _ring;
    private readonly float[] _window;
    private readonly int _hopSize;
    private readonly double[] _recent = new double[3];
    private int _ringFill;
    private int _sinceLastHop;
    private long _totalSamples;
    private int _recentCount;

    public int SampleRate { get; }

    /// <summary>Windows quieter than this RMS are treated as silence. Tune for noisy rooms.</summary>
    public double SilenceThreshold { get; set; } = 0.01;

    /// <summary>Readings with lower YIN clarity are discarded as unvoiced (breath, consonants).</summary>
    public double MinClarity { get; set; } = 0.80;

    public event Action<PitchSample>? PitchDetected;

    public PitchTracker(int sampleRate, int windowSize = 2048, int hopSize = 1024)
    {
        SampleRate = sampleRate;
        _detector = new YinPitchDetector(sampleRate, windowSize);
        _ring = new float[windowSize];
        _window = new float[windowSize];
        _hopSize = hopSize;
    }

    public void Reset()
    {
        _ringFill = 0;
        _sinceLastHop = 0;
        _totalSamples = 0;
        _recentCount = 0;
    }

    public void Process(ReadOnlySpan<float> samples)
    {
        int n = _ring.Length;
        foreach (float s in samples)
        {
            // Shift-free ring: write at (_totalSamples % n), unroll into _window when analysing.
            _ring[(int)(_totalSamples % n)] = s;
            _totalSamples++;
            if (_ringFill < n) _ringFill++;
            _sinceLastHop++;

            if (_ringFill == n && _sinceLastHop >= _hopSize)
            {
                _sinceLastHop = 0;
                Analyse();
            }
        }
    }

    private void Analyse()
    {
        int n = _ring.Length;
        int start = (int)(_totalSamples % n);
        _ring.AsSpan(start).CopyTo(_window);
        _ring.AsSpan(0, start).CopyTo(_window.AsSpan(n - start));

        double sumSq = 0;
        foreach (float s in _window) sumSq += s * s;
        double rms = Math.Sqrt(sumSq / n);
        double time = (_totalSamples - n / 2.0) / SampleRate;

        double? midi = null;
        if (rms >= SilenceThreshold && _detector.Detect(_window) is { } reading && reading.Clarity >= MinClarity)
        {
            midi = Smooth(NoteMath.FrequencyToMidi(reading.Frequency));
        }
        else
        {
            _recentCount = 0;
        }

        PitchDetected?.Invoke(new PitchSample(time, midi, rms));
    }

    /// <summary>
    /// Median-of-3 smoothing removes single-frame octave glitches without adding
    /// noticeable lag. The very first readings pass through unchanged.
    /// </summary>
    private double Smooth(double midi)
    {
        _recent[_recentCount % 3] = midi;
        _recentCount++;
        if (_recentCount < 3) return midi;
        double a = _recent[0], b = _recent[1], c = _recent[2];
        return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
    }
}
