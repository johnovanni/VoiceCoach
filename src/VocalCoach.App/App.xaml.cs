namespace VocalCoach.App;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark; // the pitch graph is designed for a dark stage
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new AppShell())
    {
        Title = "Vocal Coach",
#if WINDOWS
        // Phone-like proportions on desktop so the layout matches what will ship on iPhone.
        Width = 520,
        Height = Math.Min(900, DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density - 80),
#endif
    };
}
