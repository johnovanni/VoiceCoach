using VocalCoach.App.UI;
using VocalCoach.Core.Coaching;

namespace VocalCoach.App.Pages;

/// <summary>Bite-sized singing lessons; tap a card to expand it.</summary>
public sealed class LearnPage : ContentPage
{
    public LearnPage()
    {
        Title = "Learn";
        BackgroundColor = Theme.Background;

        var views = new List<View> { Ui.Title("Singing basics"), Ui.Muted("Tap a topic to open it.") };
        foreach (var lesson in Lessons.All)
        {
            var details = Ui.Bullets(lesson.Points);
            details.IsVisible = false;
            var chevron = Ui.Muted("▸", 16);
            var header = new Grid
            {
                ColumnDefinitions = [new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto)],
                ColumnSpacing = 12,
                Children =
                {
                    new Label { Text = lesson.Icon, FontSize = 26, VerticalTextAlignment = TextAlignment.Center },
                    new VerticalStackLayout { Children = { Ui.Heading(lesson.Title), Ui.Muted(lesson.Summary) } }.Column(1),
                    chevron.Column(2),
                },
            };
            views.Add(Ui.Card(new VerticalStackLayout { Spacing = 12, Children = { header, details } }).Tap(() =>
            {
                details.IsVisible = !details.IsVisible;
                chevron.Text = details.IsVisible ? "▾" : "▸";
            }));
        }
        views.Add(Ui.Muted("This app is a practice aid, not a substitute for a qualified voice teacher. " +
                           "If you feel pain or persistent hoarseness, stop and consult a professional."));
        Content = Ui.Page(views.ToArray());
    }
}
