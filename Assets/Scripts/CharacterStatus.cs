public enum CharacterStatus
{
    Unknown = 0,
    Available = 1,
    OnMission = 2,
    Training = 3,
    WantedForQuestioning = 4,
    Detained = 5,
    Imprisoned = 6,
    Wounded = 7,
    Hospitalized = 8,
    Dead = 9
}

public static class CharacterStatusUtility
{
    public static CharacterStatus Parse(string rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
            return CharacterStatus.Unknown;

        string s = rawStatus.Trim().ToLowerInvariant();
        if (s.Contains("detained") || s.Contains("awaiting trial"))
            return CharacterStatus.Detained;
        if (s.Contains("imprisoned") || s.Contains("sentenced") || (s.Contains("prison") && !s.Contains("awaiting")))
            return CharacterStatus.Imprisoned;
        if (s.Contains("hospital") || s.Contains("hospitalized"))
            return CharacterStatus.Hospitalized;
        if (s.Contains("wounded") || s.Contains("injured"))
            return CharacterStatus.Wounded;
        if (s.Contains("arrest"))
            return CharacterStatus.Detained;
        if (s.Contains("investigation") || s.Contains("questioning") || s.Contains("wanted"))
            return CharacterStatus.WantedForQuestioning;
        if (s.Contains("mission"))
            return CharacterStatus.OnMission;
        if (s.Contains("training"))
            return CharacterStatus.Training;
        if (s.Contains("dead") || s.Contains("killed"))
            return CharacterStatus.Dead;
        if (s.Contains("available"))
            return CharacterStatus.Available;
        return CharacterStatus.Unknown;
    }

    public static bool IsIncarcerated(CharacterStatus status)
    {
        return status == CharacterStatus.Detained || status == CharacterStatus.Imprisoned;
    }

    public static string ToDisplayLabel(CharacterStatus status)
    {
        if (status == CharacterStatus.Imprisoned) return "In prison (sentenced)";
        if (status == CharacterStatus.Hospitalized) return "Hospitalized";
        if (status == CharacterStatus.Wounded) return "Wounded";
        if (status == CharacterStatus.Detained) return "Detained — awaiting trial";
        if (status == CharacterStatus.WantedForQuestioning) return "Wanted for questioning";
        if (status == CharacterStatus.OnMission) return "On Mission";
        if (status == CharacterStatus.Training) return "Training";
        if (status == CharacterStatus.Dead) return "Dead";
        if (status == CharacterStatus.Available) return "Available";
        return "Unknown";
    }

    public static bool IsNegative(CharacterStatus status)
    {
        return status == CharacterStatus.WantedForQuestioning ||
               status == CharacterStatus.Detained ||
               status == CharacterStatus.Imprisoned ||
               status == CharacterStatus.Wounded ||
               status == CharacterStatus.Hospitalized ||
               status == CharacterStatus.Dead;
    }
}
