[System.Serializable]
public class CrewMember
{
    /// <summary>First calendar day this person counts as “in the family” — anchors personal week for rest rules.</summary>
    public int JoinedFamilyOnDay = 1;

    public string Name;
    public string Role;
    public string Status;
    public CharacterStatus StatusType = CharacterStatus.Unknown;
    public string Loyalty;
    public string Skills;
    public int PersonalReputation;

    /// <summary>−50..+50 — design §4; affects performance via <see cref="CrewMoraleSystem"/>.</summary>
    public int PersonalMorale;

    /// <summary>0..100 — operational state (stub for future decay/tick).</summary>
    public int Fatigue;

    /// <summary>0..100 — operational state (stub).</summary>
    public int Stress;

    /// <summary>0 none, 1 light .. 4 critical — stub for injury pipeline.</summary>
    public int InjuryTier;
    /// <summary>Rubric XP from character-creation interrogation (6 traits, same order as <see cref="CoreTrait"/>).</summary>
    public int[] InterrogationRubricBonusXp;
    /// <summary>Accumulated XP earned from prison time (monthly tick).</summary>
    public int PrisonTrainingXp;

    /// <summary>When incarcerated, trains without player input (default for non-boss until player chooses otherwise).</summary>
    public bool PrisonTrainingAuto = true;

    /// <summary>
    /// 0..3 focus index: Strength, Agility, Intelligence, Charisma. Interpreted differently for Detained vs Imprisoned.
    /// -1 means unset (auto-pick when <see cref="PrisonTrainingAuto"/> is true).
    /// </summary>
    public int PrisonTrainingFocusIndex = -1;

    /// <summary>Last known arrest/detention record (when Detained).</summary>
    public ArrestRecord Arrest;
    public CrewSatisfactionLevel Satisfaction = CrewSatisfactionLevel.Neutral;

    public CharacterStatus GetResolvedStatus()
    {
        if (StatusType != CharacterStatus.Unknown)
            return StatusType;
        return CharacterStatusUtility.Parse(Status);
    }

    public void SetStatus(CharacterStatus status)
    {
        StatusType = status;
        Status = CharacterStatusUtility.ToDisplayLabel(status);
    }
}
