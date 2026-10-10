using System.Diagnostics;
using VocalCoach.App.Services;
using VocalCoach.App.UI;
using VocalCoach.Core.Audio;
using VocalCoach.Core.Coaching;
using VocalCoach.Core.Exercises;
using VocalCoach.Core.Music;
using VocalCoach.Core.Practice;
using VocalCoach.Core.Progress;

namespace VocalCoach.App.Pages;

/// <summary>
/// Runs one or more exercises in sequence. Each exercise goes:
/// intro (instructions) → live (listen/sing with visual feedback) → results (coaching).
/// Navigate with ?ids=hum-descend,long-tones
/// </summary>
public sealed class PracticePage : ContentPage, IQueryAttributable
{
    private readonly PitchEngine _engine;
    private readonly IAudioPlayer _player;
    private readonly ProgressStore _store;
    private readonly PitchGraphDrawable _graph = new();
    private readonly CentsMeterDrawable _meter = new();
    private readonly GraphicsView _graphView;
    private readonly GraphicsView _meterView;
    private readonly Stopwatch _clock = new();
    private IDispatcherTimer? _timer;

    private List<Exercise> _queue = [];
    private int _queueIndex;
    private PracticeSession? _session;
    private PitchSample? _latest;

    // UI parts
    private readonly Label _header = Ui.Title("", 22);
    private readonly Label _subHeader = Ui.Muted("");
    private readonly ContentView _intro = new();
    private readonly Grid _live;
    private readonly ContentView _results = new();
    private readonly Label _phase = new() { FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text, HorizontalTextAlignment = TextAlignment.Center };
    private readonly Label _repLabel = Ui.Muted("");
    private readonly Label _targetLabel = new() { FontSize = 15, TextColor = Theme.Sing, FontAttributes = FontAttributes.Bold };
    private readonly Label _youLabel = new() { FontSize = 15, TextColor = Theme.Text, HorizontalTextAlignment = TextAlignment.End };
    private readonly Label _cue = new() { FontSize = 16, HorizontalTextAlignment = TextAlignment.Center, TextColor = Theme.Muted };
    private readonly ProgressBar _progress = new() { ProgressColor = Theme.Accent, HeightRequest = 4 };

    public PracticePage(PitchEngine engine, IAudioPlayer player, ProgressStore store)
    {
        _engine = engine;
        _player = player;
        _store = store;
        Title = "Practice";
        BackgroundColor = Theme.Background;
        Shell.SetTabBarIsVisible(this, false); // full focus while singing

        _graphView = new GraphicsView { Drawable = _graph, BackgroundColor = Theme.Background };
        _meterView = new GraphicsView { Drawable = _meter, HeightRequest = 44 };

        _live = new Grid
        {
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
            RowSpacing = 8,
            Padding = new Thickness(0, 4, 0, 12),
        };
        _live.Add(_progress.Row(0));
        _live.Add(new VerticalStackLayout { Spacing = 2, Padding = new Thickness(16, 0), Children = { _phase, _repLabel } }.Row(1));
        _repLabel.HorizontalTextAlignment = TextAlignment.Center;
        _live.Add(_graphView.Row(2));
        _live.Add(new VerticalStackLayout
        {
            Padding = new Thickness(16, 0),
            Spacing = 6,
            Children =
            {
                new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], Children = { _targetLabel, _youLabel.Column(1) } },
                _meterView,
                _cue,
                Ui.Secondary("Stop", () => FinishExercise(stoppedEarly: true)),
            },
        }.Row(3));

        var root = new Grid { RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)] };
        root.Add(new VerticalStackLayout { Padding = new Thickness(16, 8, 16, 4), Children = { _header, _subHeader } }.Row(0));
        root.Add(_intro.Row(1));
        root.Add(_live.Row(1));
        root.Add(_results.Row(1));
        Content = root;
        ShowPanel(_intro);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var ids = (query.TryGetValue("ids", out var v) ? v?.ToString() : null) ?? "lip-trill-5";
        _queue = Uri.UnescapeDataString(ids).Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ExerciseLibrary.Get).ToList();
        _queueIndex = 0;
        ShowIntro();
    }

    private Exercise Current => _queue[_queueIndex];

    private void ShowPanel(View panel)
    {
        _intro.IsVisible = panel == _intro;
        _live.IsVisible = panel == _live;
        _results.IsVisible = panel == _results;
    }

    private void SetHeader()
    {
        _header.Text = Current.Name;
        _subHeader.Text = _queue.Count > 1
            ? $"Exercise {_queueIndex + 1} of {_queue.Count} · {ExerciseLibrary.CategoryName(Current.Category)}"
            : ExerciseLibrary.CategoryName(Current.Category);
    }

    private void ShowIntro()
    {
        SetHeader();
        var ex = Current;
        var profile = _store.Profile;
        _intro.Content = Ui.Page(
            Ui.Body(ex.Summary),
            new HorizontalStackLayout { Spacing = 8, Children = { Ui.Chip($"Sing: {ex.Syllable}", Theme.Sing), Ui.Chip(Ui.Stars(ex.Difficulty), Theme.Close) } },
            Ui.Card(new VerticalStackLayout { Spacing = 10, Children = { Ui.Heading("How to do it"), Ui.Bullets(ex.Instructions) } }),
            Ui.Card(new VerticalStackLayout { Spacing = 10, Children = { Ui.Heading("Coach's notes"), Ui.Bullets(ex.CoachTips, "♪") } }),
            Ui.Muted(profile.Demonstrate
                ? "You'll hear each pattern (purple bars), then sing it back (blue bars). Keep your line inside the bars."
                : "You'll hear a starting note, then sing the blue bars. Keep your line inside the bars."),
            profile.AccompanyWhileSinging ? Ui.Muted("🎧 Accompaniment is on — use headphones so the mic only hears you.") : new ContentView(),
            Ui.Primary("Start", async () => await StartExerciseAsync()));
        ShowPanel(_intro);
    }

    private async Task StartExerciseAsync()
    {
        var profile = _store.Profile;
        double tolerance = Difficulty.ToleranceCents(profile.Level);
        var plan = SessionPlan.Build(Current, profile.Range, profile.Demonstrate);

        _phase.Text = "Get ready…";
        _repLabel.Text = "";
        _cue.Text = "";
        ShowPanel(_live);

        // Render the reference audio off the UI thread (can be a few seconds of synthesis).
        var audio = await Task.Run(() => plan.RenderReferenceAudio(_player.SampleRate, profile.AccompanyWhileSinging));

        if (!await _engine.StartAsync())
        {
            await DisplayAlertAsync("Microphone", _engine.LastError ?? "The microphone couldn't be started.", "OK");
            ShowIntro();
            return;
        }

        _session = new PracticeSession(plan, tolerance);
        _graph.Notes = plan.Notes;
        _graph.ResultFor = i => _session?.Results.GetValueOrDefault(i);
        _graph.ToleranceCents = tolerance;
        _graph.ReferenceMidi = null;
        _graph.Trail.Clear();
        _graph.DefaultCentre = (profile.Range.LowMidi + profile.Range.HighMidi) / 2.0;
        _graph.ResetView();
        _meter.ToleranceCents = tolerance;
        _latest = null;

        _player.Play(audio);
        _clock.Restart();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += (_, _) => OnFrame();
        _timer.Start();
        DeviceDisplay.Current.KeepScreenOn = true;
    }

    private void OnFrame()
    {
        if (_session is null) return;
        double now = _clock.Elapsed.TotalSeconds;

        while (_engine.Samples.TryDequeue(out var s))
        {
            _session.AddSample(s);
            _graph.Trail.Add(s);
            _latest = s;
        }
        _session.Update(now);
        _graph.Now = now;
        _graph.TrimTrail();
        _graphView.Invalidate();

        var plan = _session.Plan;
        _progress.Progress = Math.Clamp(now / plan.TotalSeconds, 0, 1);

        var note = plan.NoteAt(now);
        var upcoming = note ?? plan.NextNoteAfter(now);
        if (upcoming is not null)
        {
            _repLabel.Text = $"Key {upcoming.Repetition + 1} of {plan.RepetitionCount} · starting on {NoteMath.NoteName(plan.Notes.First(n => n.Repetition == upcoming.Repetition).Midi)}";
        }
        _phase.Text = note?.Phase switch
        {
            NotePhase.Listen => "🎹 Listen…",
            NotePhase.Sing => $"🎤 Sing \"{plan.Exercise.Syllable}\"",
            _ when upcoming?.Phase == NotePhase.Sing => "Breathe… your turn",
            _ when now < plan.LeadInSeconds => "Get ready…",
            _ => "🎹 Listen…",
        };

        bool fresh = _latest is { IsVoiced: true } l && now - l.Time < 0.3;
        var singNote = note?.Phase == NotePhase.Sing ? note : null;
        _targetLabel.Text = singNote is null ? (upcoming?.Phase == NotePhase.Sing ? $"Next: {NoteMath.NoteName(upcoming.Midi)}" : "") : $"Target: {NoteMath.NoteName(singNote.Midi)}";

        if (fresh)
        {
            double midi = _latest!.Value.Midi!.Value;
            int reference = singNote?.Midi ?? (int)Math.Round(midi);
            double cents = (midi - reference) * 100;
            _youLabel.Text = $"You: {NoteMath.NoteName((int)Math.Round(midi))} {(midi - Math.Round(midi)) * 100:+0;-0;0}¢";
            _meter.Cents = cents;
        }
        else
        {
            _youLabel.Text = "";
            _meter.Cents = null;
        }
        _meterView.Invalidate();

        if (singNote is not null)
        {
            var cue = CoachingEngine.Live(fresh ? (_latest!.Value.Midi!.Value - singNote.Midi) * 100 : null, _session.ToleranceCents);
            _cue.Text = cue.Text;
            _cue.TextColor = cue.Level switch
            {
                CueLevel.OnPitch => Theme.Good,
                CueLevel.Close => Theme.Close,
                CueLevel.Off => Theme.Bad,
                _ => Theme.Muted,
            };
        }
        else
        {
            _cue.Text = note?.Phase == NotePhase.Listen ? "Hear the notes in your head as they play" : "";
            _cue.TextColor = Theme.Muted;
        }

        if (_session.IsComplete) FinishExercise(stoppedEarly: false);
    }

    private void StopAudio()
    {
        _timer?.Stop();
        _timer = null;
        _clock.Stop();
        _engine.Stop();
        _player.Stop();
        DeviceDisplay.Current.KeepScreenOn = false;
    }

    private void FinishExercise(bool stoppedEarly)
    {
        if (_session is null) return;
        StopAudio();
        var summary = _session.Finish(_clock.Elapsed.TotalSeconds);
        _session = null;

        // Don't record a stopped-early run with almost nothing sung.
        if (summary.NotesSung >= 3 || !stoppedEarly)
            _store.AddSession(SessionRecord.From(summary, DateTime.Now));

        ShowResults(summary);
    }

    private void ShowResults(SessionSummary s)
    {
        var tips = CoachingEngine.Review(s);
        var tipViews = new VerticalStackLayout { Spacing = 10 };
        foreach (var tip in tips)
        {
            var (icon, color) = tip.Kind switch
            {
                TipKind.Praise => ("🌟", Theme.Good),
                TipKind.Improve => ("🎯", Theme.Close),
                _ => ("🎵", Theme.Sing),
            };
            tipViews.Add(Ui.Card(new VerticalStackLayout
            {
                Spacing = 4,
                Children = { new Label { Text = $"{icon}  {tip.Title}", FontAttributes = FontAttributes.Bold, FontSize = 16, TextColor = color }, Ui.Body(tip.Message) },
            }));
        }

        bool hasNext = _queueIndex < _queue.Count - 1;
        var buttons = new VerticalStackLayout { Spacing = 8 };
        if (hasNext)
            buttons.Add(Ui.Primary($"Next: {_queue[_queueIndex + 1].Name}", () => { _queueIndex++; ShowIntro(); }));
        buttons.Add(Ui.Secondary("Try again", ShowIntro));
        buttons.Add(hasNext ? Ui.Secondary("End practice", async () => await Shell.Current.GoToAsync("..")) : Ui.Primary("Done", async () => await Shell.Current.GoToAsync("..")));

        _results.Content = Ui.Page(
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = $"{s.Score:0}", FontSize = 56, FontAttributes = FontAttributes.Bold, TextColor = Theme.ForScore(s.Score), HorizontalTextAlignment = TextAlignment.Center },
                    new Label { Text = s.Grade, FontSize = 20, TextColor = Theme.Text, HorizontalTextAlignment = TextAlignment.Center },
                    Ui.Muted($"Best: {_store.BestScore(s.ExerciseId) ?? s.Score:0}").Also(l => l.HorizontalTextAlignment = TextAlignment.Center),
                },
            }),
            StatsGrid(s),
            NoteStrip(s),
            Ui.Heading("Coach's feedback"),
            tipViews,
            buttons);
        ShowPanel(_results);
    }

    private static Grid StatsGrid(SessionSummary s)
    {
        var grid = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 8 };
        grid.Add(Stat("On target", $"{s.AccuracyPercent:0}%"));
        grid.Add(Stat("Avg offset", s.NotesSung == 0 ? "—" : $"{s.AverageCents:+0;-0;0}¢").Column(1));
        grid.Add(Stat("Steadiness", s.NotesSung == 0 ? "—" : $"±{s.AverageStability:0}¢").Column(2));
        return grid;

        static View Stat(string label, string value) => Ui.Card(new VerticalStackLayout
        {
            Children =
            {
                new Label { Text = value, FontSize = 20, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text, HorizontalTextAlignment = TextAlignment.Center },
                Ui.Muted(label, 12).Also(l => l.HorizontalTextAlignment = TextAlignment.Center),
            },
        }, padding: 10);
    }

    /// <summary>A row of coloured dots, one per sung note, so the singer sees where it went wrong.</summary>
    private static View NoteStrip(SessionSummary s)
    {
        var flex = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
        foreach (var n in s.Notes)
        {
            var color = n.Missed ? Theme.Muted : Theme.ForScore(n.Score);
            flex.Add(new Border
            {
                WidthRequest = 14, HeightRequest = 14, Margin = 2, StrokeThickness = 0,
                BackgroundColor = color,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 7 },
            });
        }
        return Ui.Card(new VerticalStackLayout { Spacing = 8, Children = { Ui.Muted("Every note you sang (green = on pitch, amber = close, red = off, grey = not heard)"), flex } });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopAudio();
        _session = null;
    }
}
