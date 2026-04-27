using System;
using UnityEngine;

[Serializable]
public struct PersonToOrgBehaviorSnapshot
{
    public int BondScore;
    public int ComplianceScore;
    public int RetentionScore;
    public int ComplianceProbability;
    public int BetrayalProbability;
    public int DefectionProbability;
    public int InformantProbability;
    public int StayProbability;
}

[Serializable]
public struct OrgToOrgBehaviorSnapshot
{
    public int InterOrgScore;
    public bool NegotiationOpen;
    public int EscalationProbability;
    public int ProxyConflictProbability;
    public int TemporaryAllianceProbability;
    public int AgreementBreakProbability;
}

public static class PersonToOrgBehaviorResolver
{
    public static int ComputeBondScore(PersonToOrgRelation relation)
    {
        if (relation == null)
            return 0;
        relation.ClampAxes();
        return SocialRelationMath.ComputeBondScore(relation.Affinity, relation.Respect, relation.Interest);
    }

    public static int ComputeComplianceScore(PersonToOrgRelation relation)
    {
        if (relation == null)
            return 0;

        relation.ClampAxes();
        float compliance =
            (relation.Fear * 0.55f) +
            (Mathf.Max(relation.Respect, 0) * 0.20f) +
            (Mathf.Max(relation.Interest, 0) * 0.15f) +
            (Mathf.Max(relation.Affinity, 0) * 0.10f);
        return Mathf.Clamp(Mathf.RoundToInt(compliance), 0, 100);
    }

    public static int ComputeRetentionScore(PersonToOrgRelation relation)
    {
        int bond = ComputeBondScore(relation);
        int compliance = ComputeComplianceScore(relation);
        float bond01 = (bond + 100f) * 0.5f;
        return Mathf.Clamp(Mathf.RoundToInt((bond01 * 0.6f) + (compliance * 0.4f)), 0, 100);
    }

    public static PersonToOrgSpectrumLabel ResolveSpectrum(PersonToOrgRelation relation)
    {
        int score = ComputeBondScore(relation);
        if (score >= SocialRelationThresholds.PersonOrgDevotedMin) return PersonToOrgSpectrumLabel.Devoted;
        if (score >= SocialRelationThresholds.PersonOrgLoyalMin) return PersonToOrgSpectrumLabel.Loyal;
        if (score >= SocialRelationThresholds.PersonOrgAttachedMin) return PersonToOrgSpectrumLabel.Attached;
        if (score >= SocialRelationThresholds.PersonOrgCooperativeMin) return PersonToOrgSpectrumLabel.Cooperative;
        if (score >= SocialRelationThresholds.PersonOrgNeutralMin) return PersonToOrgSpectrumLabel.Neutral;
        if (score >= SocialRelationThresholds.PersonOrgDetachedMin) return PersonToOrgSpectrumLabel.Detached;
        if (score >= SocialRelationThresholds.PersonOrgResentfulMin) return PersonToOrgSpectrumLabel.Resentful;
        if (score >= SocialRelationThresholds.PersonOrgDisloyalMin) return PersonToOrgSpectrumLabel.Disloyal;
        return PersonToOrgSpectrumLabel.ReadyToFlip;
    }

    public static PersonToOrgBehaviorSnapshot ResolveBehavior(
        PersonToOrgRelation relation,
        int alternativeOrgInterest = 0)
    {
        if (relation == null)
            return default;

        relation.ClampAxes();
        int bond = ComputeBondScore(relation);
        int compliance = ComputeComplianceScore(relation);
        int retention = ComputeRetentionScore(relation);

        int complianceProbability = Mathf.Clamp(35 + (compliance / 2), 0, 100);
        int betrayalProbability = Mathf.Clamp(10 + Mathf.Max(0, -bond / 2), 0, 100);
        int defectionProbability = Mathf.Clamp(5 + Mathf.Max(0, alternativeOrgInterest - relation.Interest), 0, 100);
        int informantProbability = Mathf.Clamp(5 + Mathf.Max(0, -relation.Affinity / 2), 0, 100);
        int stayProbability = Mathf.Clamp(20 + retention / 2, 0, 100);

        // If Fear >= 55 or Affinity >= 55 then ComplianceProbability += high.
        if (relation.Fear >= 55 || relation.Affinity >= 55)
            complianceProbability = Mathf.Clamp(complianceProbability + 30, 0, 100);

        // If Affinity <= -30 and Interest <= 0 and Fear < 55 then BetrayalProbability += high.
        if (relation.Affinity <= -30 && relation.Interest <= 0 && relation.Fear < 55)
            betrayalProbability = Mathf.Clamp(betrayalProbability + 35, 0, 100);

        // If Spectrum == ReadyToFlip and AlternativeOrgInterest > current Interest then DefectionProbability += high.
        PersonToOrgSpectrumLabel spectrum = ResolveSpectrum(relation);
        if (spectrum == PersonToOrgSpectrumLabel.ReadyToFlip && alternativeOrgInterest > relation.Interest)
            defectionProbability = Mathf.Clamp(defectionProbability + 35, 0, 100);

        // If Affinity <= -55 and Fear < 30 then InformantProbability += high.
        if (relation.Affinity <= -55 && relation.Fear < 30)
            informantProbability = Mathf.Clamp(informantProbability + 40, 0, 100);

        // If Fear >= 55 and Interest >= 10 then StayProbability += high.
        if (relation.Fear >= 55 && relation.Interest >= 10)
            stayProbability = Mathf.Clamp(stayProbability + 30, 0, 100);

        return new PersonToOrgBehaviorSnapshot
        {
            BondScore = bond,
            ComplianceScore = compliance,
            RetentionScore = retention,
            ComplianceProbability = complianceProbability,
            BetrayalProbability = betrayalProbability,
            DefectionProbability = defectionProbability,
            InformantProbability = informantProbability,
            StayProbability = stayProbability
        };
    }
}

public static class OrgToOrgBehaviorResolver
{
    public static int ComputeInterOrgScore(OrgToOrgRelation relation)
    {
        if (relation == null)
            return 0;

        relation.ClampAxes();
        float weighted =
            (relation.Affinity * 0.25f) +
            (relation.Respect * 0.25f) +
            (relation.Interest * 0.35f) -
            (relation.Fear * 0.15f);
        return Mathf.Clamp(Mathf.RoundToInt(weighted), -100, 100);
    }

    public static OrgToOrgSpectrumLabel ResolveSpectrum(OrgToOrgRelation relation)
    {
        int score = ComputeInterOrgScore(relation);
        if (score >= SocialRelationThresholds.OrgOrgAlliedMin) return OrgToOrgSpectrumLabel.Allied;
        if (score >= SocialRelationThresholds.OrgOrgFriendlyMin) return OrgToOrgSpectrumLabel.Friendly;
        if (score >= SocialRelationThresholds.OrgOrgCooperativeMin) return OrgToOrgSpectrumLabel.Cooperative;
        if (score >= SocialRelationThresholds.OrgOrgNeutralMin) return OrgToOrgSpectrumLabel.Neutral;
        if (score >= SocialRelationThresholds.OrgOrgSuspiciousMin) return OrgToOrgSpectrumLabel.Suspicious;
        if (score >= SocialRelationThresholds.OrgOrgHostileMin) return OrgToOrgSpectrumLabel.Hostile;
        return OrgToOrgSpectrumLabel.AtWar;
    }

    public static OrgToOrgBehaviorSnapshot ResolveBehavior(
        OrgToOrgRelation relation,
        bool sharedEnemy,
        bool allowWarNegotiation = false)
    {
        if (relation == null)
            return default;

        relation.ClampAxes();
        int score = ComputeInterOrgScore(relation);
        OrgToOrgSpectrumLabel spectrum = ResolveSpectrum(relation);

        bool negotiationOpen = relation.Interest >= 30 && spectrum != OrgToOrgSpectrumLabel.AtWar;
        if (spectrum == OrgToOrgSpectrumLabel.AtWar && !allowWarNegotiation)
            negotiationOpen = false;

        int escalation = Mathf.Clamp(5 + Mathf.Max(0, -score / 2), 0, 100);
        int proxyConflict = spectrum == OrgToOrgSpectrumLabel.Hostile && relation.Fear >= 30 ? 65 : 20;
        int tempAlliance = sharedEnemy && relation.Interest >= 55 ? 70 : 10;
        int agreementBreak = relation.Interest < 10 && relation.Affinity < 0 ? 55 : 15;

        // If Affinity <= -30 and Fear < 30 and Respect < 30 then EscalationProbability += high.
        if (relation.Affinity <= -30 && relation.Fear < 30 && relation.Respect < 30)
            escalation = Mathf.Clamp(escalation + 35, 0, 100);

        // If Hostile and Fear >= 30 then ProxyConflictProbability += high.
        if (spectrum == OrgToOrgSpectrumLabel.Hostile && relation.Fear >= 30)
            proxyConflict = Mathf.Clamp(proxyConflict + 20, 0, 100);

        // If SharedEnemy == true and Interest >= 55 then TemporaryAllianceProbability += high.
        if (sharedEnemy && relation.Interest >= 55)
            tempAlliance = Mathf.Clamp(tempAlliance + 20, 0, 100);

        // If Interest < 10 and Affinity < 0 then AgreementBreakProbability += medium.
        if (relation.Interest < 10 && relation.Affinity < 0)
            agreementBreak = Mathf.Clamp(agreementBreak + 15, 0, 100);

        return new OrgToOrgBehaviorSnapshot
        {
            InterOrgScore = score,
            NegotiationOpen = negotiationOpen,
            EscalationProbability = escalation,
            ProxyConflictProbability = proxyConflict,
            TemporaryAllianceProbability = tempAlliance,
            AgreementBreakProbability = agreementBreak
        };
    }
}

public static class CrossLayerInfluenceResolver
{
    // Deliberately low influence factors for v1 to avoid automatic large cross-layer drift.
    public const float PersonToOrgWeight = 0.08f;
    public const float OrgToPersonWeight = 0.08f;

    public static void ApplyPersonToOrgInfluence(
        PersonToPersonRelation personalRelation,
        PersonToOrgRelation personToOrgRelation,
        bool eventPerceivedAsOrganizational,
        bool actorRepresentsOrganization,
        bool actionIsPublicOrExplicit)
    {
        if (personalRelation == null || personToOrgRelation == null)
            return;

        if (!eventPerceivedAsOrganizational && !actorRepresentsOrganization && !actionIsPublicOrExplicit)
            return;

        personalRelation.ClampAxes();
        personToOrgRelation.ClampAxes();

        int dAffinity = Mathf.RoundToInt(personalRelation.Affinity * PersonToOrgWeight);
        int dRespect = Mathf.RoundToInt(personalRelation.Respect * PersonToOrgWeight);
        int dInterest = Mathf.RoundToInt(personalRelation.Interest * (PersonToOrgWeight * 0.5f));

        personToOrgRelation.Affinity = SocialRelationMath.ClampSignedAxis(personToOrgRelation.Affinity + dAffinity);
        personToOrgRelation.Respect = SocialRelationMath.ClampSignedAxis(personToOrgRelation.Respect + dRespect);
        personToOrgRelation.Interest = SocialRelationMath.ClampSignedAxis(personToOrgRelation.Interest + dInterest);
    }

    public static void ApplyOrgToPersonInfluence(
        OrgToOrgRelation orgToOrgRelation,
        PersonToPersonRelation personalRelation,
        bool directlyImpactsCharacterLife,
        bool clearOrganizationAttribution)
    {
        if (orgToOrgRelation == null || personalRelation == null)
            return;

        if (!directlyImpactsCharacterLife || !clearOrganizationAttribution)
            return;

        orgToOrgRelation.ClampAxes();
        personalRelation.ClampAxes();

        int dAffinity = Mathf.RoundToInt(orgToOrgRelation.Affinity * OrgToPersonWeight);
        int dRespect = Mathf.RoundToInt(orgToOrgRelation.Respect * OrgToPersonWeight);
        int dFear = Mathf.RoundToInt(orgToOrgRelation.Fear * (OrgToPersonWeight * 0.5f));

        personalRelation.Affinity = SocialRelationMath.ClampSignedAxis(personalRelation.Affinity + dAffinity);
        personalRelation.Respect = SocialRelationMath.ClampSignedAxis(personalRelation.Respect + dRespect);
        personalRelation.Fear = SocialRelationMath.ClampFearAxis(personalRelation.Fear + dFear);
    }
}
