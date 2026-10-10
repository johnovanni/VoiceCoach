using VocalCoach.App.UI;
using VocalCoach.Core.Coaching;
using VocalCoach.Core.Exercises;
using VocalCoach.Core.Progress;
using VocalCoach.Core.Voice;

namespace VocalCoach.App.Pages;

public sealed class HomePage : ContentPage
{
    private readonly ProgressStore _store;

    public HomePage(ProgressStore store)
    {
        _store = store;
        Title = "Vocal Coach";
        BackgroundColor = Theme.Background;
        ToolbarItems.Add(new ToolbarItem("Settings", null, async () => await Shell.Current.GoToAsync("settings")));
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Build();
    }

    private void Build()
    {
        var profile = _store.Profile;
        var today = DateTime.Now;
        int streak = _store.CurrentStreak(today);
        var routine = CoachingEngine.TodaysRoutine(_store.History, profile.Level, today);
        string greeting = today.Hour < 12 ? "Good morning" : today.Hour < 18 ? "Good afternoon" : "Good evening";

        var views = new List<View>
        {
            Ui.Title($"{greeting} 🎶"),
            Ui.Muted($"{VoiceTypes.DisplayName(profile.VoiceType)} · {profile.Range} · {profile.Level}"),
        };

        if (!profile.HasCompletedSetup)
        {
            views.Add(Ui.Card(new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    Ui.Heading("Let's find your voice"),
                    Ui.Body("Sing your lowest and highest comfortable notes and I'll set every exercise to fit your range."),
                    Ui.Primary("Find my range", async () => await Shell.Current.GoToAsync("rangefinder")),
                    Ui.Secondary("Choose voice type manually", async () => await Shell.Current.GoToAsync("settings")),
                },
            }, Theme.SurfaceRaised));
        }

        var stats = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 10 };
        stats.Add(Stat($"{streak}", "day streak 🔥"));
        stats.Add(Stat($"{_store.SessionsOn(today)}", "exercises today").Column(1));
        views.Add(stats);

        var routineList = new VerticalStackLayout { Spacing = 8 };
        foreach (var (ex, i) in routine.Select((e, i) => (e, i)))
        {
            routineList.Add(new Grid
            {
                ColumnDefinitions = [new(28), new(GridLength.Star), new(GridLength.Auto)],
                Children =
                {
                    new Label { Text = $"{i + 1}", TextColor = Theme.Accent, FontAttributes = FontAttributes.Bold, FontSize = 16 },
                    new VerticalStackLayout { Children = { Ui.Body(ex.Name), Ui.Muted(ExerciseLibrary.CategoryName(ex.Category), 12) } }.Column(1),
                    Ui.Muted(_store.BestScore(ex.Id) is double b ? $"best {b:0}" : "new", 12).Column(2),
                },
            }.Tap(async () => await Shell.Current.GoToAsync($"exercise?id={ex.Id}")));
        }

        views.Add(Ui.Card(new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                Ui.Heading("Today's practice"),
                Ui.Muted($"About {routine.Sum(EstimateMinutes):0} minutes · warm-up, focus work, cool-down"),
                routineList,
                Ui.Primary("▶  Start today's practice", async () =>
                    await Shell.Current.GoToAsync($"practice?ids={string.Join(',', routine.Select(r => r.Id))}")),
            },
        }));

        views.Add(Ui.Card(new VerticalStackLayout
        {
            Spacing = 6,
            Children = { Ui.Heading("💡 Tip of the day"), Ui.Body(CoachingEngine.TipOfTheDay(today)) },
        }));

        views.Add(Ui.Secondary("🎚  Re-measure my range", async () => await Shell.Current.GoToAsync("rangefinder")));

        Content = Ui.Page(views.ToArray());
    }

    private static double EstimateMinutes(Exercise e) => e.Repetitions * (e.PatternBeats * 2 + 2.5) * e.SecondsPerBeat / 60 + 0.5;

    private static View Stat(string value, string label) => Ui.Card(new VerticalStackLayout
    {
        Children =
        {
            new Label { Text = value, FontSize = 30, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
            Ui.Muted(label),
        },
    });
}
