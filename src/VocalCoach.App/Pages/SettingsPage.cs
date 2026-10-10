using VocalCoach.App.UI;
using VocalCoach.Core.Practice;
using VocalCoach.Core.Progress;
using VocalCoach.Core.Voice;

namespace VocalCoach.App.Pages;

public sealed class SettingsPage : ContentPage
{
    private readonly ProgressStore _store;

    public SettingsPage(ProgressStore store)
    {
        _store = store;
        Title = "Settings";
        BackgroundColor = Theme.Background;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Build();
    }

    private void Build()
    {
        var p = _store.Profile;
        var voiceTypes = Enum.GetValues<VoiceType>();
        var levels = Enum.GetValues<SkillLevel>();

        var voicePicker = new Picker
        {
            Title = "Voice type",
            ItemsSource = voiceTypes.Select(t => $"{VoiceTypes.DisplayName(t)}  ({VoiceTypes.ComfortableRange(t)})").ToList(),
            SelectedIndex = Array.IndexOf(voiceTypes, p.VoiceType),
            TextColor = Theme.Text,
        };
        voicePicker.SelectedIndexChanged += (_, _) =>
        {
            // Choosing a type manually replaces any measured range.
            Save(_store.Profile with { VoiceType = voiceTypes[voicePicker.SelectedIndex], CustomRange = null, HasCompletedSetup = true });
            Build();
        };

        var levelPicker = new Picker
        {
            Title = "Level",
            ItemsSource = levels.Select(l => $"{l} (±{Difficulty.ToleranceCents(l):0}¢ counts as in tune)").ToList(),
            SelectedIndex = Array.IndexOf(levels, p.Level),
            TextColor = Theme.Text,
        };
        levelPicker.SelectedIndexChanged += (_, _) => Save(_store.Profile with { Level = levels[levelPicker.SelectedIndex] });

        Content = Ui.Page(
            Ui.Title("Settings"),
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Ui.Heading("Your voice"),
                    voicePicker,
                    Ui.Muted(p.CustomRange is { } r ? $"Using your measured range: {r}" : $"Using the default {VoiceTypes.DisplayName(p.VoiceType)} range: {p.Range}"),
                    Ui.Secondary("🎚  Measure my range", async () => await Shell.Current.GoToAsync("rangefinder")),
                },
            }),
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 8,
                Children = { Ui.Heading("Difficulty"), levelPicker, Ui.Muted("Higher levels need you to be closer to the exact pitch. 100¢ = one semitone.") },
            }),
            Ui.Card(new VerticalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    Ui.Heading("Practice"),
                    Toggle("Demonstrate each pattern first", "Hear the whole pattern, then sing it back.", p.Demonstrate,
                        v => Save(_store.Profile with { Demonstrate = v })),
                    Toggle("Play along while I sing", "Soft piano under your voice. Use headphones, or the mic will hear the piano.", p.AccompanyWhileSinging,
                        v => Save(_store.Profile with { AccompanyWhileSinging = v })),
                },
            }));
    }

    private void Save(UserProfile profile) => _store.SaveProfile(profile);

    private static View Toggle(string title, string detail, bool value, Action<bool> changed)
    {
        var sw = new Switch { IsToggled = value, OnColor = Theme.Accent, VerticalOptions = LayoutOptions.Center };
        sw.Toggled += (_, e) => changed(e.Value);
        return new Grid
        {
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)],
            ColumnSpacing = 10,
            Children = { new VerticalStackLayout { Children = { Ui.Body(title), Ui.Muted(detail, 12) } }, sw.Column(1) },
        };
    }
}
