using System;
using UnityEngine;

/// <summary>Deterministic compliance band from agent/operation context (no free RNG).</summary>
public static class FederalComplianceResolver
{
    public static FederalComplianceBand Resolve(
        FederalAgentProfile agent,
        string operationId,
        int dayIndex,
        int citySeed,
        int stress01 = 0,
        int aggression01 = 0)
    {
        if (string.IsNullOrEmpty(operationId) || agent == null)
            return FederalComplianceBand.CompliantInterpretation;

        int h = MixHash(
            citySeed,
            dayIndex,
            StringHash(operationId),
            StringHash(agent != null ? agent.agentId : "none"),
            agent != null ? (int)agent.rank : 0,
            agent != null ? (int)agent.deputyPortfolio : 0,
            stress01,
            aggression01);

        int t = Math.Abs(h) % 100;
        // 0-45 Full, 45-70 CompliantInterpretation, 70-90 Soft, 90-98 Hard, 98+ Open
        if (t < 45) return FederalComplianceBand.FullCompliance;
        if (t < 70) return FederalComplianceBand.CompliantInterpretation;
        if (t < 90) return FederalComplianceBand.SoftDeviation;
        if (t < 98) return FederalComplianceBand.HardDeviation;
        return FederalComplianceBand.OpenViolation;
    }

    static int StringHash(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        unchecked
        {
            int h = 23;
            for (int i = 0; i < s.Length; i++)
                h = h * 31 + s[i];
            return h;
        }
    }

    static int MixHash(int a, int b, int c, int d, int e, int f, int g, int h)
    {
        unchecked
        {
            int x = a * 0x1B873593 ^ b * 0x1D2E0E7 ^ c * 0x27D4EB2D;
            x ^= d * 0x45B9C27F ^ e * 0x5A8F8E11;
            x ^= f * 0x6A7E3C29 ^ g * 0x7B2E1A0D;
            return x ^ h;
        }
    }
}
