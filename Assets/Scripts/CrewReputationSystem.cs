using UnityEngine;

/// <summary>
/// Crew-wide reputation pressure model + multi-channel rep stubs (design §8).
/// </summary>
public static class CrewReputationSystem
{
    public static void ApplyOperationOutcome(OperationType op, in OperationResolution res)
    {
        if (PlayerRunState.Character == null)
            return;

        PlayerCharacterProfile boss = PlayerRunState.Character;
        int leaderBefore = boss.PublicReputation;
        int leaderDelta = GetLeaderOutcomeDelta(op, res.Tier);
        int contributorDelta = 0;

        for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
        {
            CrewMember member = PersonnelRegistry.Members[i];
            if (member == null)
                continue;

            bool onMission = string.Equals(member.Status, "On Mission");
            int personalDelta = GetMemberOutcomeDelta(op, res.Tier, onMission);
            member.PersonalReputation = Mathf.Clamp(member.PersonalReputation + personalDelta, -100, 100);

            if (onMission)
                contributorDelta += OutcomeTierMapper.MeetsObjectiveLine(res.Tier) ? 1 : -1;
        }

        int leaderAfter = Mathf.Clamp(leaderBefore + leaderDelta + contributorDelta, -100, 100);
        boss.PublicReputation = leaderAfter;

        ApplyChannelReputationDeltas(boss, op, res.Tier);
        CrewMoraleSystem.ApplyMissionOutcomeDelta(res.Tier);
    }

    private static void ApplyChannelReputationDeltas(PlayerCharacterProfile boss, OperationType op, OutcomeTier tier)
    {
        bool ok = OutcomeTierMapper.MeetsObjectiveLine(tier);
        int mag = tier switch
        {
            OutcomeTier.CriticalSuccess => 3,
            OutcomeTier.Success => 2,
            OutcomeTier.PartialSuccess => 1,
            OutcomeTier.CleanFailure => -1,
            OutcomeTier.Failure => -2,
            _ => -3
        };

        if (!ok)
            mag -= 1;

        boss.UnderworldRespect = Clamp50(boss.UnderworldRespect + (ok ? mag : mag - 1));
        boss.StreetFear = Clamp50(boss.StreetFear + (op == OperationType.Collect ? mag : mag / 2));
        boss.PoliceAttentionChannel = Clamp50(boss.PoliceAttentionChannel + (op == OperationType.Collect ? mag + 1 : mag / 3));
        boss.BusinessCredibility = Clamp50(boss.BusinessCredibility + (op == OperationType.Collect ? mag : 0));
    }

    private static int Clamp50(int v) => Mathf.Clamp(v, -50, 50);

    public static int GetOrganizationReputation()
    {
        if (PlayerRunState.Character == null)
            return 0;
        return Mathf.Clamp(PlayerRunState.Character.PublicReputation, -100, 100);
    }

    public static int GetOrganizationInfluenceOnMember()
    {
        int org = GetOrganizationReputation();
        return Mathf.Clamp(Mathf.RoundToInt(org * 0.35f), -20, 20);
    }

    public static int GetEffectiveReputation(CrewMember member)
    {
        if (member == null)
            return 0;
        int eff = member.PersonalReputation + GetOrganizationInfluenceOnMember();
        return Mathf.Clamp(eff, -100, 100);
    }

    public static float GetPoachAcceptanceChance(
        CrewMember member,
        int targetOrgReputation,
        int offerQualityBonus = 0,
        int ladderStepUps = 0,
        bool majorStep = false,
        int loyaltyGuard = 0)
    {
        if (member == null)
            return 0f;

        float baseMood = GetBaseChanceBySatisfaction(member.Satisfaction);
        float stepBonus = ladderStepUps * (majorStep ? 13f : 7f);
        float repDeltaBonus = Mathf.Clamp((targetOrgReputation - GetOrganizationReputation()) * 0.6f, -20f, 35f);

        float chance = baseMood + stepBonus + repDeltaBonus + offerQualityBonus - loyaltyGuard;
        return Mathf.Clamp(chance, 0f, 95f);
    }

    public static float GetBaseChanceBySatisfaction(CrewSatisfactionLevel satisfaction)
    {
        switch (satisfaction)
        {
            case CrewSatisfactionLevel.VerySatisfied: return 2f;
            case CrewSatisfactionLevel.Satisfied: return 6f;
            case CrewSatisfactionLevel.Neutral: return 15f;
            case CrewSatisfactionLevel.Unsatisfied: return 32f;
            case CrewSatisfactionLevel.VeryUnsatisfied: return 55f;
            default: return 15f;
        }
    }

    public static string GetSatisfactionLabel(CrewSatisfactionLevel level)
    {
        switch (level)
        {
            case CrewSatisfactionLevel.VerySatisfied: return "Very satisfied";
            case CrewSatisfactionLevel.Satisfied: return "Satisfied";
            case CrewSatisfactionLevel.Neutral: return "Neutral";
            case CrewSatisfactionLevel.Unsatisfied: return "Unsatisfied";
            case CrewSatisfactionLevel.VeryUnsatisfied: return "Very unsatisfied";
            default: return "Neutral";
        }
    }

    private static int GetLeaderOutcomeDelta(OperationType op, OutcomeTier tier)
    {
        bool line = OutcomeTierMapper.MeetsObjectiveLine(tier);
        int baseDelta = op switch
        {
            OperationType.Collect => line ? 4 : -5,
            OperationType.Surveillance => line ? 3 : -3,
            _ => line ? 2 : -2
        };

        return tier switch
        {
            OutcomeTier.CriticalSuccess => baseDelta + 2,
            OutcomeTier.PartialSuccess => baseDelta - 1,
            OutcomeTier.Failure => baseDelta - 2,
            OutcomeTier.DisastrousFailure => baseDelta - 5,
            _ => baseDelta
        };
    }

    private static int GetMemberOutcomeDelta(OperationType op, OutcomeTier tier, bool onMission)
    {
        bool line = OutcomeTierMapper.MeetsObjectiveLine(tier);
        int baseDelta = line ? 1 : -1;
        switch (op)
        {
            case OperationType.Collect:
                baseDelta += line ? 1 : -1;
                break;
            case OperationType.Surveillance:
                baseDelta += line ? 1 : 0;
                break;
        }

        if (onMission)
            baseDelta += line ? 1 : -1;

        return baseDelta;
    }
}
