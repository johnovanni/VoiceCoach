using VocalCoach.App.UI;
using VocalCoach.Core.Progress;

namespace VocalCoach.App.Pages;

public sealed class ProgressPage : ContentPage
{
    private readonly ProgressStore _store;
    private readonly ScoreChartDrawable _chart = new();

    public ProgressPage(ProgressStore store)
    {
        _store = store;
        Title = "Progress";
        BackgroundColor = Theme.Background;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var history = _store.History.OrderBy(h => h.Date).ToList();
        var lastWeek = history.Where(h => h.Date >= DateTime.Now.AddDays(-7)).ToList();
        var prevWeek = history.Where(h => h.Date >= DateTime.Now.AddDays(-14) && h.Date < DateTime.Now.AddDays(-7)).ToList();
        _chart.Scores = history.TakeLast(30).Select(h => h.Score).ToList();

        var stats = new Grid { ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)], ColumnSpacing = 8 };
        stats.Add(Stat($"{_store.CurrentStreak(DateTime.Now)}", "day streak"));
        stats.Add(Stat($"{lastWeek.Count}", "this week").Column(1));
        stats.Add(Stat(lastWeek.Count == 0 ? "—" : $"{lastWeek.Average(h => h.Score):0}", "avg score (7d)").Column(2));

        var views = new List<View>
        {
            Ui.Title("Your progress"),
            stats,
        };

        if (lastWeek.Count > 0 && prevWeek.Count > 0)
        {
            double delta = lastWeek.Average(h => h.AccuracyPercent) - prevWeek.Average(h => h.AccuracyPercent);
            views.Add(Ui.Card(Ui.Body(delta >= 0
                ? $"📈 Your pitch accuracy is up {delta:0} points on last week. Keep it up!"
                : $"📉 Accuracy dipped {-delta:0} points from last week. Shorter, more frequent sessions help — and remember to warm up.")));
        }

        views.Add(Ui.Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children = { Ui.Heading("Scores (last 30 exercises)"), new GraphicsView { Drawable = _chart, HeightRequest = 180 } },
        }));

        var recent = new VerticalStackLayout { Spacing = 10 };
        foreach (var h in history.AsEnumerable().Reverse().Take(25))
        {
            recent.Add(new Grid
            {
                ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)],
                Children =
                {
                    new VerticalStackLayout
                    {
                        Children =
                        {
                            Ui.Body(h.ExerciseName),
                            Ui.Muted($"{h.Date:ddd d MMM, HH:mm} · {h.AccuracyPercent:0}% on target · {h.AverageCents:+0;-0;0}¢", 12),
                        },
                    },
                    new Label { Text = $"{h.Score:0}", FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Theme.ForScore(h.Score), VerticalTextAlignment = TextAlignment.Center }.Column(1),
                },
            });
        }
        views.Add(Ui.Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children = { Ui.Heading("Recent sessions"), history.Count == 0 ? Ui.Muted("Nothing yet — your sessions will appear here.") : recent },
        }));

        Content = Ui.Page(views.ToArray());
    }

    private static View Stat(string value, string label) => Ui.Card(new VerticalStackLayout
    {
        Children =
        {
            new Label { Text = value, FontSize = 24, FontAttributes = FontAttributes.Bold, TextColor = Theme.Text, HorizontalTextAlignment = TextAlignment.Center },
            Ui.Muted(label, 12).Also(l => l.HorizontalTextAlignment = TextAlignment.Center),
        },
    }, padding: 10);
}
