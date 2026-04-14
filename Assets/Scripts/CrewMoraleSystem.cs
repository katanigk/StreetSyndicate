using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Personal morale per member (-50..50) and simple org aggregates (design §4).
/// </summary>
public static class CrewMoraleSystem
{
    public const int MoraleMin = -50;
    public const int MoraleMax = 50;

    public static int ClampMorale(int v) => Mathf.Clamp(v, MoraleMin, MoraleMax);

    public static float GetMoralePerformanceMultiplier(int personalMorale)
    {
        return personalMorale * 0.003f;
    }

    public static float GetOrgMoraleMean()
    {
        List<CrewMember> members = PersonnelRegistry.Members;
        if (members == null || members.Count == 0)
            return 0f;
        float s = 0f;
        int n = 0;
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] == null)
                continue;
            s += members[i].PersonalMorale;
            n++;
        }

        return n > 0 ? s / n : 0f;
    }

    public static float GetOrgMoraleMedian()
    {
        List<CrewMember> members = PersonnelRegistry.Members;
        if (members == null || members.Count == 0)
            return 0f;
        List<int> vals = new List<int>(members.Count);
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] != null)
                vals.Add(members[i].PersonalMorale);
        }

        if (vals.Count == 0)
            return 0f;
        vals.Sort();
        int mid = vals.Count / 2;
        if (vals.Count % 2 == 0)
            return (vals[mid - 1] + vals[mid]) * 0.5f;
        return vals[mid];
    }

    /// <summary>Apply mission result deltas to members on-mission; boss optional index 0 boost.</summary>
    public static void ApplyMissionOutcomeDelta(OutcomeTier tier, bool bossParticipates = true)
    {
        List<CrewMember> members = PersonnelRegistry.Members;
        if (members == null)
            return;

        int delta = tier switch
        {
            OutcomeTier.CriticalSuccess => Random.Range(8, 15),
            OutcomeTier.Success => Random.Range(3, 8),
            OutcomeTier.PartialSuccess => Random.Range(-2, 4),
            OutcomeTier.CleanFailure => Random.Range(-6, -2),
            OutcomeTier.Failure => Random.Range(-10, -4),
            _ => Random.Range(-18, -8)
        };

        for (int i = 0; i < members.Count; i++)
        {
            CrewMember m = members[i];
            if (m == null)
                continue;
            bool onMission = string.Equals(m.Status, "On Mission");
            if (!onMission && !(bossParticipates && i == 0))
                continue;
            int d = onMission ? delta : delta / 2;
            m.PersonalMorale = ClampMorale(m.PersonalMorale + d);
        }
    }
}
