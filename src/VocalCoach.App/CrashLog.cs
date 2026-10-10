namespace VocalCoach.App;

/// <summary>Appends unhandled exceptions to crash.log in app data so failures can be diagnosed after the fact.</summary>
public static class CrashLog
{
    public static string PathName => Path.Combine(FileSystem.AppDataDirectory, "crash.log");

    public static void Install()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Write(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, e) => Write(e.Exception);
    }

    public static void Write(Exception? ex)
    {
        try
        {
            File.AppendAllText(PathName, $"[{DateTime.Now:O}] {ex}\n\n");
        }
        catch
        {
            // Never let logging itself crash the app.
        }
    }
}
