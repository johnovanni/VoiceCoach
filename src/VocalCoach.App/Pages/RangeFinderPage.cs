using VocalCoach.App.Services;
using VocalCoach.App.UI;
using VocalCoach.Core.Music;
using VocalCoach.Core.Progress;
using VocalCoach.Core.Voice;

namespace VocalCoach.App.Pages;

/// <summary>Measures the singer's lowest and highest comfortable notes and suggests a voice type.</summary>
public sealed class RangeFinderPage : ContentPage
{
    private enum Step { Intro, Low, High, Result }

    private readonly PitchEngine _engine;
    private readonly ProgressStore _store;
    private readonly SteadyNoteCapture _capture = new();
    private IDispatcherTimer? _timer;
    private Step _step;
    private int? _low, _high;

    // Recreated for each capture screen: a MAUI view can only have one parent, so reusing these
    // in a new layout while the old one still holds them crashes on Windows.
    private Label _live = new();
    private ProgressBar _hold = new();

    public RangeFinderPage(PitchEngine engine, ProgressStore store)
    {
        _engine = engine;
        _store = store;
        Title = "Find my range";
        BackgroundColor = Theme.Background;
        Shell.SetTabBarIsVisible(this, false); // full focus while singing
        ShowIntro();
    }

    private void ShowIntro()
    {
        StopListening();
        _step = Step.Intro;
        _low = _high = null;
        Content = Ui.Page(
            Ui.Title("Find your range"),
            Ui.Body("We'll measure the lowest and highest notes you can sing comfortably — not the extremes. Every exercise will then be fitted to your voice."),
            Ui.Card(Ui.Bullets(
            [
                "Sing on a relaxed \"Ah\" at a medium volume.",
                "Hold each note steadily until the bar fills (about 1.5 seconds).",
                "Comfortable means no strain, no squeezing, no cracking. If it hurts, it's too far.",
            ])),
            Ui.Primary("Start", async () => await BeginAsync()));
    }

    private async Task BeginAsync()
    {
        if (!await _engine.StartAsync())
        {
            await DisplayAlertAsync("Microphone", _engine.LastError ?? "Microphone unavailable", "OK");
            return;
        }
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(30);
        _timer.Tick += (_, _) => OnFrame();
        _timer.Start();
        ShowCapture(Step.Low);
    }

    private void ShowCapture(Step step)
    {
        _step = step;
        _capture.Reset();
        _engine.Samples.Clear();
        _live = new Label { Text = "—", FontSize = 56, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text, HorizontalTextAlignment = TextAlignment.Center };
        _hold = new ProgressBar { ProgressColor = Theme.Good, HeightRequest = 8 };
        bool low = step == Step.Low;
        Content = Ui.Page(
            Ui.Muted(low ? "Step 1 of 2" : "Step 2 of 2"),
            Ui.Title(low ? "Sing your lowest comfortable note" : "Sing your highest comfortable note"),
            Ui.Body(low
                ? "Start in your speaking voice and slide down until it feels like the bottom of your easy range. Then hold it."
                : "Slide up on \"Ah\" until it's as high as feels easy and clear — use your lighter head voice if you need to. Then hold it."),
            Ui.Card(new VerticalStackLayout { Spacing = 12, Children = { _live, _hold, Ui.Muted("Hold steady to capture").Also(l => l.HorizontalTextAlignment = TextAlignment.Center) } }),
            low ? new ContentView() : Ui.Muted($"Lowest note: {NoteMath.NoteName(_low!.Value)}"),
            Ui.Secondary("Cancel", ShowIntro));
    }

    private void OnFrame()
    {
        while (_engine.Samples.TryDequeue(out var s))
        {
            if (s.IsVoiced) _live.Text = NoteMath.NoteName((int)Math.Round(s.Midi!.Value));
            if (_capture.Add(s) is int midi)
            {
                if (_step == Step.Low)
                {
                    _low = midi;
                    ShowCapture(Step.High);
                }
                else if (_step == Step.High)
                {
                    if (midi < _low!.Value + 7)
                    {
                        _live.Text = "Higher!";
                        continue; // keep listening — that's too close to the low note to be a top note
                    }
                    _high = midi;
                    ShowResult();
                }
                return;
            }
        }
        _hold.Progress = _capture.Progress;
    }

    private void ShowResult()
    {
        StopListening();
        _step = Step.Result;
        var range = new VoiceRange(_low!.Value, _high!.Value);
        var type = VoiceTypes.Suggest(range);

        Content = Ui.Page(
            Ui.Title("Your range"),
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label { Text = range.ToString(), FontSize = 36, FontAttributes = FontAttributes.Bold, TextColor = Theme.Good, HorizontalTextAlignment = TextAlignment.Center },
                    Ui.Muted($"{range.Span} semitones ({range.Span / 12.0:0.0} octaves)").Also(l => l.HorizontalTextAlignment = TextAlignment.Center),
                },
            }),
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    Ui.Heading($"You're probably a {VoiceTypes.DisplayName(type)}"),
                    Ui.Muted($"Typical comfortable {VoiceTypes.DisplayName(type)} range: {VoiceTypes.ComfortableRange(type)}. " +
                             "Voice type is about more than range, so treat this as a starting point. Your range will grow with practice!"),
                },
            }),
            Ui.Primary("Save and use this range", async () =>
            {
                _store.SaveProfile(_store.Profile with { VoiceType = type, CustomRange = range, HasCompletedSetup = true });
                await Shell.Current.GoToAsync("..");
            }),
            Ui.Secondary("Try again", ShowIntro));
    }

    private void StopListening()
    {
        _timer?.Stop();
        _timer = null;
        _engine.Stop();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopListening();
    }
}
