using System.Numerics;

namespace VocalCoach.Core.Audio;

/// <summary>Result of analysing one window of audio.</summary>
/// <param name="Frequency">Detected fundamental in Hz.</param>
/// <param name="Clarity">0..1, how periodic the signal is (1 = perfectly periodic).</param>
public readonly record struct PitchReading(double Frequency, double Clarity);

/// <summary>
/// YIN fundamental-frequency estimator (de Cheveigné &amp; Kawahara, 2002).
/// Well suited to the human voice: robust against strong harmonics and cheap enough
/// to run in real time on a phone. Allocation-free after construction.
/// </summary>
public sealed class YinPitchDetector
{
    private readonly int _sampleRate;
    private readonly int _minTau;
    private readonly int _maxTau;
    private readonly double _threshold;
    private readonly double[] _diff;

    public int WindowSize { get; }

    /// <param name="minFrequency">Lowest pitch to detect. 65 Hz (C2) covers basses.</param>
    /// <param name="maxFrequency">Highest pitch to detect. 1400 Hz covers sopranos' whistle-free range.</param>
    public YinPitchDetector(int sampleRate, int windowSize = 2048, double minFrequency = 65, double maxFrequency = 1400, double threshold = 0.15)
    {
        _sampleRate = sampleRate;
        WindowSize = windowSize;
        _threshold = threshold;
        _minTau = Math.Max(2, (int)(sampleRate / maxFrequency));
        _maxTau = Math.Min(windowSize / 2, (int)Math.Ceiling(sampleRate / minFrequency));
        _diff = new double[_maxTau + 1];
    }

    /// <summary>Returns a reading, or null if the window holds no clear pitch (silence, breath, noise).</summary>
    public PitchReading? Detect(ReadOnlySpan<float> window)
    {
        if (window.Length < WindowSize) throw new ArgumentException($"Window must have at least {WindowSize} samples.", nameof(window));
        int w = WindowSize - _maxTau;

        // Step 1+2: difference function d(tau).
        var head = window[..w];
        for (int tau = _minTau; tau <= _maxTau; tau++) _diff[tau] = SumSquaredDiff(head, window.Slice(tau, w));

        // Step 3: cumulative mean normalised difference d'(tau).
        double running = 0;
        for (int tau = 1; tau < _minTau; tau++) running += SumSquaredDiff(head, window.Slice(tau, w));
        for (int tau = _minTau; tau <= _maxTau; tau++)
        {
            running += _diff[tau];
            _diff[tau] = running == 0 ? 1 : _diff[tau] * tau / running;
        }

        // Step 4: absolute threshold — first dip below threshold, then walk to its local minimum.
        int best = -1;
        for (int tau = _minTau; tau <= _maxTau; tau++)
        {
            if (_diff[tau] < _threshold)
            {
                while (tau + 1 <= _maxTau && _diff[tau + 1] < _diff[tau]) tau++;
                best = tau;
                break;
            }
        }
        if (best < 0) return null;

        // Step 5: parabolic interpolation for sub-sample accuracy.
        double refined = best;
        if (best > _minTau && best < _maxTau)
        {
            double s0 = _diff[best - 1], s1 = _diff[best], s2 = _diff[best + 1];
            double denom = 2 * (2 * s1 - s2 - s0);
            if (Math.Abs(denom) > 1e-12) refined = best + (s2 - s0) / denom;
        }

        double frequency = _sampleRate / refined;
        double clarity = Math.Clamp(1 - _diff[best], 0, 1);
        return new PitchReading(frequency, clarity);
    }

    /// <summary>Σ(a[i] − b[i])², SIMD-accelerated — this is >95% of YIN's cost.</summary>
    private static double SumSquaredDiff(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        int i = 0;
        var acc = Vector<float>.Zero;
        for (; i <= a.Length - Vector<float>.Count; i += Vector<float>.Count)
        {
            var d = new Vector<float>(a[i..]) - new Vector<float>(b[i..]);
            acc += d * d;
        }
        double sum = Vector.Sum(acc);
        for (; i < a.Length; i++)
        {
            double d = a[i] - b[i];
            sum += d * d;
        }
        return sum;
    }
}
