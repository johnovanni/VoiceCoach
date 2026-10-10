using VocalCoach.App.UI;
using VocalCoach.Core.Exercises;
using VocalCoach.Core.Music;
using VocalCoach.Core.Practice;
using VocalCoach.Core.Progress;

namespace VocalCoach.App.Pages;

public sealed class ExercisesPage : ContentPage
{
    private readonly ProgressStore _store;

    public ExercisesPage(ProgressStore store)
    {
        _store = store;
        Title = "Exercises";
        BackgroundColor = Theme.Background;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var views = new List<View>();
        foreach (var group in ExerciseLibrary.All.GroupBy(e => e.Category))
        {
            views.Add(Ui.Heading(ExerciseLibrary.CategoryName(group.Key)));
            foreach (var ex in group)
            {
                double? best = _store.BestScore(ex.Id);
                views.Add(Ui.Card(new VerticalStackLayout
                {
                    Spacing = 4,
                    Children =
                    {
                        new Grid
                        {
                            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)],
                            Children =
                            {
                                new Label { Text = ex.Name, FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text },
                                new Label { Text = Ui.Stars(ex.Difficulty), TextColor = Theme.Close, FontSize = 12 }.Column(1),
                            },
                        },
                        Ui.Muted(ex.Summary),
                        Ui.Muted(best is double b ? $"Best score: {b:0}" : "Not tried yet", 12)
                            .Also(l => l.TextColor = best is double s ? Theme.ForScore(s) : Theme.Muted),
                    },
                }).Tap(async () => await Shell.Current.GoToAsync($"exercise?id={ex.Id}")));
            }
        }
        Content = Ui.Page(views.ToArray());
    }
}

public sealed class ExerciseDetailPage : ContentPage, IQueryAttributable
{
    private readonly ProgressStore _store;

    public ExerciseDetailPage(ProgressStore store)
    {
        _store = store;
        BackgroundColor = Theme.Background;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        var ex = ExerciseLibrary.Get(query["id"].ToString()!);
        Title = ex.Name;
        var plan = SessionPlan.Build(ex, _store.Profile.Range);
        var firstPhrase = plan.SingNotes.Where(n => n.Repetition == 0).Select(n => NoteMath.NoteName(n.Midi));
        double minutes = plan.TotalSeconds / 60;

        Content = Ui.Page(
            Ui.Title(ex.Name),
            new HorizontalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Ui.Chip(ExerciseLibrary.CategoryName(ex.Category), Theme.Accent),
                    Ui.Chip(Ui.Stars(ex.Difficulty), Theme.Close),
                    Ui.Chip($"~{Math.Ceiling(minutes)} min", Theme.Muted),
                },
            },
            Ui.Body(ex.Summary),
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    Ui.Heading("Your pattern"),
                    Ui.Body($"Sing on \"{ex.Syllable}\": {string.Join(" – ", firstPhrase)}"),
                    Ui.Muted($"Repeated {plan.RepetitionCount} times, rising a semitone each time. Fitted to your range ({_store.Profile.Range})."),
                },
            }),
            Ui.Card(new VerticalStackLayout { Spacing = 10, Children = { Ui.Heading("How to do it"), Ui.Bullets(ex.Instructions) } }),
            Ui.Card(new VerticalStackLayout { Spacing = 10, Children = { Ui.Heading("Coach's notes"), Ui.Bullets(ex.CoachTips, "♪") } }),
            Ui.Primary("▶  Practise this", async () => await Shell.Current.GoToAsync($"practice?ids={ex.Id}")));
    }
}
