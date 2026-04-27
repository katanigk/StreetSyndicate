using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Picks a single primary target id for the day from candidates (deterministic tie-break).</summary>
public static class FederalTargetPriorityResolver
{
    public static string PickPrimaryTarget(
        IReadOnlyList<string> candidateIds,
        int federalInterestScore,
        int casePriority,
        int recentIntelValue,
        int directorPolicyWeight,
        int unitChiefPreference,
        int exposureRisk,
        int politicalRisk,
        int resourceLoad,
        int dayIndex,
        int citySeed)
    {
        if (candidateIds == null || candidateIds.Count == 0)
            return string.Empty;

        int best = int.MinValue;
        string bestId = candidateIds[0];
        for (int i = 0; i < candidateIds.Count; i++)
        {
            string id = candidateIds[i];
            if (string.IsNullOrEmpty(id)) continue;
            int h = StringHash(id);
            int score = federalInterestScore + casePriority + recentIntelValue + directorPolicyWeight
                + unitChiefPreference - exposureRisk - politicalRisk - resourceLoad
                + (h & 0x1F) + (Mathf.Abs(Mix(h, dayIndex, citySeed)) % 7);
            if (score > best)
            {
                best = score;
                bestId = id;
            }
        }
        return bestId;
    }

    static int StringHash(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        unchecked
        {
            int h = 17;
            for (int i = 0; i < s.Length; i++)
                h = h * 31 + s[i];
            return h;
        }
    }

    static int Mix(int a, int b, int c)
    {
        unchecked
        {
            return a * 0x1B873593 ^ b * 0x1D2E0E7 ^ c * 0x27D4EB2D;
        }
    }
}
