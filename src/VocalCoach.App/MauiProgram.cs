using Microsoft.Extensions.Logging;
using VocalCoach.App.Pages;
using VocalCoach.App.Services;
using VocalCoach.Core.Progress;

namespace VocalCoach.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        CrashLog.Install();
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Platform audio — the only platform-specific code in the app.
#if WINDOWS
        builder.Services.AddSingleton<IAudioInput, Platforms.Windows.WindowsAudioInput>();
        builder.Services.AddSingleton<IAudioPlayer, Platforms.Windows.WindowsAudioPlayer>();
#elif IOS
        builder.Services.AddSingleton<IAudioInput, Platforms.iOS.IosAudioInput>();
        builder.Services.AddSingleton<IAudioPlayer, Platforms.iOS.IosAudioPlayer>();
#endif
#if DEBUG
        if (Environment.GetEnvironmentVariable("VOCALCOACH_LOOPBACK") is "1" or "range")
        {
            var loopback = new LoopbackAudio();
            builder.Services.AddSingleton<IAudioInput>(loopback);
            builder.Services.AddSingleton<IAudioPlayer>(loopback);
        }
#endif
        builder.Services.AddSingleton<PitchEngine>();
        builder.Services.AddSingleton(_ => new ProgressStore(Path.Combine(FileSystem.AppDataDirectory, "VocalCoach")));

        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<ExercisesPage>();
        builder.Services.AddTransient<ExerciseDetailPage>();
        builder.Services.AddTransient<PracticePage>();
        builder.Services.AddTransient<TunerPage>();
        builder.Services.AddTransient<ProgressPage>();
        builder.Services.AddTransient<LearnPage>();
        builder.Services.AddTransient<RangeFinderPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }
}
