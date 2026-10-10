using System.Diagnostics;
using VocalCoach.App.Services;
using VocalCoach.App.UI;
using VocalCoach.Core.Audio;
using VocalCoach.Core.Music;
using VocalCoach.Core.Practice;
using VocalCoach.Core.Progress;

namespace VocalCoach.App.Pages;

/// <summary>
/// Free singing with live pitch display. Optionally pick a reference note, hear it,
/// and match it — the simplest and most effective ear-training drill.
/// </summary>
public sealed class TunerPage : ContentPage
{
    private readonly PitchEngine _engine;
    private readonly IAudioPlayer _player;
    private readonly ProgressStore _store;
    private readonly PitchGraphDrawable _graph = new() { PastSeconds = 6, FutureSeconds = 1 };
    private readonly CentsMeterDrawable _meter = new();
    private readonly GraphicsView _graphView;
    private readonly GraphicsView _meterView;
    private readonly Stopwatch _clock = new();
    private IDispatcherTimer? _timer;
    private PitchSample? _latest;
    private int _referenceMidi;
    private bool _matchMode;

    private readonly Label _note = new() { FontSize = 64, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _freq = Ui.Muted("Sing any note");
    private readonly Label _refLabel = new() { FontSize = 18, FontAttributes = FontAttributes.Bold, TextColor = Theme.Sing, VerticalTextAlignment = TextAlignment.Center, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Button _matchButton;

    public TunerPage(PitchEngine engine, IAudioPlayer player, ProgressStore store)
    {
        _engine = engine;
        _player = player;
        _store = store;
        Title = "Tuner";
        BackgroundColor = Theme.Background;
        _freq.HorizontalTextAlignment = TextAlignment.Center;

        _graphView = new GraphicsView { Drawable = _graph };
        _meterView = new GraphicsView { Drawable = _meter, HeightRequest = 44 };
        _matchButton = Ui.Secondary("Match mode: off", ToggleMatch);

        var refRow = new Grid
        {
            ColumnDefinitions = [new(56), new(GridLength.Star), new(56), new(GridLength.Star)],
            ColumnSpacing = 8,
            Children =
            {
                Ui.Secondary("◀", () => ChangeReference(-1)),
                _refLabel.Column(1),
                Ui.Secondary("▶", () => ChangeReference(1)).Column(2),
                Ui.Primary("🔊 Play", PlayReference).Column(3),
            },
        };

        var root = new Grid
        {
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            Padding = new Thickness(0, 8, 0, 12),
            RowSpacing = 8,
        };
        root.Add(new VerticalStackLayout { Padding = new Thickness(16, 0), Children = { _note, _freq, _meterView } }.Row(0));
        root.Add(_graphView.Row(1));
        root.Add(new VerticalStackLayout
        {
            Padding = new Thickness(16, 0),
            Spacing = 8,
            Children = { Ui.Muted("Reference note"), refRow, _matchButton },
        }.Row(2));
        Content = root;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var range = _store.Profile.Range;
        _referenceMidi = (range.LowMidi + range.HighMidi) / 2;
        _graph.DefaultCentre = _referenceMidi;
        _graph.ResetView();
        _graph.ToleranceCents = Difficulty.ToleranceCents(_store.Profile.Level);
        _meter.ToleranceCents = _graph.ToleranceCents;
        UpdateReference();

        if (!await _engine.StartAsync())
        {
            _freq.Text = _engine.LastError ?? "Microphone unavailable";
            return;
        }
        _graph.Trail.Clear();
        _clock.Restart();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += (_, _) => OnFrame();
        _timer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer?.Stop();
        _timer = null;
        _engine.Stop();
        _player.Stop();
    }

    private void OnFrame()
    {
        double now = _clock.Elapsed.TotalSeconds;
        while (_engine.Samples.TryDequeue(out var s))
        {
            _graph.Trail.Add(s);
            _latest = s;
        }
        _graph.Now = now;
        _graph.TrimTrail();
        _graphView.Invalidate();

        if (_latest is { IsVoiced: true } l && now - l.Time < 0.3)
        {
            double midi = l.Midi!.Value;
            int nearest = (int)Math.Round(midi);
            _note.Text = NoteMath.NoteName(nearest);
            _freq.Text = $"{NoteMath.MidiToFrequency(midi):0.0} Hz";
            int target = _matchMode ? _referenceMidi : nearest;
            double cents = (midi - target) * 100;
            _meter.Cents = Math.Abs(cents) <= 600 ? cents : null;
            _note.TextColor = Theme.ForCents(cents, _graph.ToleranceCents);
            if (_matchMode)
                _freq.Text += Math.Abs(cents) <= _graph.ToleranceCents ? " · matched! ✅" : cents < 0 ? " · go higher ↑" : " · go lower ↓";
        }
        else
        {
            _meter.Cents = null;
            _note.TextColor = Theme.Muted;
        }
        _meterView.Invalidate();
    }

    private void ChangeReference(int delta)
    {
        _referenceMidi = Math.Clamp(_referenceMidi + delta, 36, 84);
        UpdateReference();
    }

    private void UpdateReference()
    {
        _refLabel.Text = NoteMath.NoteName(_referenceMidi);
        _graph.ReferenceMidi = _matchMode ? _referenceMidi : null;
    }

    private void PlayReference() => _player.Play(ToneSynth.RenderSingle(_referenceMidi, 1.5, _player.SampleRate));

    private void ToggleMatch()
    {
        _matchMode = !_matchMode;
        _matchButton.Text = _matchMode ? "Match mode: on — sing the reference note" : "Match mode: off";
        UpdateReference();
        if (_matchMode) PlayReference();
    }
}
