/// <summary>
/// Descriptions and execution behavior for operation types.
/// </summary>
public static class OperationRegistry
{
    public static string GetName(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout: return "Scout";
            case OperationType.Surveillance: return "Surveillance";
            case OperationType.Collect: return "Collect";
            default: return op.ToString();
        }
    }

    public static string GetDescription(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout: return "Crew moves to scout point to recon the docks.";
            case OperationType.Surveillance: return "Watch a location and report back.";
            case OperationType.Collect: return "Pick up a package or payment.";
            default: return "";
        }
    }

    public static CoreTrait GetPrimaryTrait(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout: return CoreTrait.Agility;
            case OperationType.Surveillance: return CoreTrait.Intelligence;
            case OperationType.Collect: return CoreTrait.Charisma;
            default: return CoreTrait.Determination;
        }
    }

    public static CoreTrait GetSecondaryTrait(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout: return CoreTrait.Intelligence;
            case OperationType.Surveillance: return CoreTrait.MentalResilience;
            case OperationType.Collect: return CoreTrait.Determination;
            default: return CoreTrait.MentalResilience;
        }
    }

    public static float GetBaseChance(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout: return 0.45f;
            case OperationType.Surveillance: return 0.40f;
            case OperationType.Collect: return 0.42f;
            default: return 0.35f;
        }
    }

    /// <summary>Which skill drives XP / resolution for this operation type.</summary>
    public static DerivedSkill GetDerivedSkill(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout: return DerivedSkill.Stealth;
            case OperationType.Surveillance: return DerivedSkill.Surveillance;
            case OperationType.Collect: return DerivedSkill.Negotiation;
            default: return DerivedSkill.Leadership;
        }
    }

    /// <summary>Notional star requirement for “standard” op difficulty — for mismatch penalties.</summary>
    public static int GetSuggestedSkillRequirement(OperationType op)
    {
        switch (op)
        {
            case OperationType.Scout: return 3;
            case OperationType.Surveillance: return 4;
            case OperationType.Collect: return 4;
            default: return 3;
        }
    }

    public static string GetTraitName(CoreTrait trait)
    {
        switch (trait)
        {
            case CoreTrait.Strength: return "Strength";
            case CoreTrait.Agility: return "Agility";
            case CoreTrait.Intelligence: return "Intelligence";
            case CoreTrait.Charisma: return "Charisma";
            case CoreTrait.MentalResilience: return "Mental resilience";
            case CoreTrait.Determination: return "Determination";
            default: return trait.ToString();
        }
    }

    public static bool IsOrdered(OperationType op)
    {
        return GameSessionState.OrderedOperations.Contains(op);
    }

    public static void ToggleOrdered(OperationType op)
    {
        if (GameSessionState.OrderedOperations.Contains(op))
        {
            GameSessionState.OrderedOperations.Remove(op);
            GameSessionState.RemoveOperationAssignee(op);
            GameSessionState.RemoveOperationMissionMeta(op);
        }
        else
            GameSessionState.OrderedOperations.Add(op);

        if (op == OperationType.Scout)
            GameSessionState.ScoutMissionOrdered = GameSessionState.OrderedOperations.Contains(op);
    }
}
