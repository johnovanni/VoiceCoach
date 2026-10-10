using VocalCoach.App.Pages;

namespace VocalCoach.App;

/// <summary>Bottom tab bar navigation — the standard iPhone pattern, and fine on desktop too.</summary>
public sealed class AppShell : Shell
{
    public AppShell()
    {
        FlyoutBehavior = FlyoutBehavior.Disabled;
        var tabs = new TabBar();
        tabs.Items.Add(Tab<HomePage>("Home", "home"));
        tabs.Items.Add(Tab<ExercisesPage>("Exercises", "exercises"));
        tabs.Items.Add(Tab<TunerPage>("Tuner", "tuner"));
        tabs.Items.Add(Tab<ProgressPage>("Progress", "progress"));
        tabs.Items.Add(Tab<LearnPage>("Learn", "learn"));
        Items.Add(tabs);

        Routing.RegisterRoute("exercise", typeof(ExerciseDetailPage));
        Routing.RegisterRoute("practice", typeof(PracticePage));
        Routing.RegisterRoute("rangefinder", typeof(RangeFinderPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
    }

    private static ShellContent Tab<T>(string title, string route) where T : Page => new()
    {
        Title = title,
        Route = route,
        ContentTemplate = new DataTemplate(typeof(T)),
    };
}
