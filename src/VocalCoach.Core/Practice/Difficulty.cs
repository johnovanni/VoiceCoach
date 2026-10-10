namespace VocalCoach.Core.Practice;

public enum SkillLevel { Beginner, Intermediate, Advanced }

public static class Difficulty
{
    /// <summary>
    /// How many cents either side of the target still counts as "on pitch".
    /// 100 cents = one semitone. Trained ears notice errors of ~10–20 cents.
    /// </summary>
    public static double ToleranceCents(SkillLevel level) => level switch
    {
        SkillLevel.Beginner => 40,
        SkillLevel.Intermediate => 25,
        SkillLevel.Advanced => 15,
        _ => 25,
    };
}
