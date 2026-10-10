using System.Text.Json;
using System.Text.Json.Serialization;
using VocalCoach.Core.Practice;
using VocalCoach.Core.Voice;

namespace VocalCoach.Core.Progress;

public sealed record SessionRecord(
    DateTime Date,
    string ExerciseId,
    string ExerciseName,
    double Score,
    double AccuracyPercent,
    double AverageCents,
    double Stability,
    int NotesSung)
{
    public static SessionRecord From(SessionSummary s, DateTime when) =>
        new(when, s.ExerciseId, s.ExerciseName, s.Score, s.AccuracyPercent, s.AverageCents, s.AverageStability, s.NotesSung);
}

public sealed record UserProfile
{
    public VoiceType VoiceType { get; init; } = VoiceType.Baritone;
    /// <summary>Measured by the Range Finder; overrides the voice type's default range when set.</summary>
    public VoiceRange? CustomRange { get; init; }
    public SkillLevel Level { get; init; } = SkillLevel.Beginner;
    /// <summary>Demonstrate each pattern before the singer repeats it.</summary>
    public bool Demonstrate { get; init; } = true;
    /// <summary>Play the notes softly while singing (needs headphones to avoid feedback).</summary>
    public bool AccompanyWhileSinging { get; init; }
    public bool HasCompletedSetup { get; init; }

    [JsonIgnore]
    public VoiceRange Range => CustomRange ?? VoiceTypes.ComfortableRange(VoiceType);
}

/// <summary>
/// Persists the profile and practice history as JSON in an app-data folder.
/// Plain files keep it portable across Windows and iOS with no database dependency.
/// </summary>
public sealed class ProgressStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _historyPath;
    private readonly string _profilePath;
    private List<SessionRecord> _history;

    public UserProfile Profile { get; private set; }
    public IReadOnlyList<SessionRecord> History => _history;

    public ProgressStore(string directory)
    {
        Directory.CreateDirectory(directory);
        _historyPath = Path.Combine(directory, "history.json");
        _profilePath = Path.Combine(directory, "profile.json");
        _history = Load<List<SessionRecord>>(_historyPath) ?? [];
        Profile = Load<UserProfile>(_profilePath) ?? new UserProfile();
    }

    public void SaveProfile(UserProfile profile)
    {
        Profile = profile;
        Save(_profilePath, profile);
    }

    public void AddSession(SessionRecord record)
    {
        _history.Add(record);
        Save(_historyPath, _history);
    }

    /// <summary>Consecutive days (ending today or yesterday) with at least one session.</summary>
    public int CurrentStreak(DateTime today)
    {
        var days = _history.Select(h => h.Date.Date).ToHashSet();
        var day = today.Date;
        if (!days.Contains(day)) day = day.AddDays(-1);
        int streak = 0;
        while (days.Contains(day)) { streak++; day = day.AddDays(-1); }
        return streak;
    }

    public double? BestScore(string exerciseId) =>
        _history.Where(h => h.ExerciseId == exerciseId).Select(h => (double?)h.Score).Max();

    public int SessionsOn(DateTime day) => _history.Count(h => h.Date.Date == day.Date);

    private static T? Load<T>(string path)
    {
        try
        {
            return File.Exists(path) ? JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) : default;
        }
        catch (JsonException)
        {
            // A corrupt file shouldn't stop the app from starting; keep a copy for inspection.
            File.Copy(path, path + ".corrupt", overwrite: true);
            return default;
        }
    }

    private static void Save<T>(string path, T value)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(value, Json));
        File.Move(tmp, path, overwrite: true);
    }
}
