using System;
using System.Collections.Generic;
using UnityEngine;

public static class FederalSourceHandlingResolver
{
    public static void EnsureBaselineSources(int day, int citySeed)
    {
        if (BureauWorldState.federalSourceProfiles.Count > 0) return;
        string handler = "fbu_chief_03";
        BureauWorldState.federalSourceProfiles.Add(new FederalSourceProfile
        {
            sourceId = "src_city_001",
            sourceTypeInt = (int)FederalIntelSourceType.BusinessRecord,
            realCharacterId = "char_city_clerk_01",
            codeName = "Ledger Finch",
            coverIdentityId = string.Empty,
            originalOrganizationId = string.Empty,
            handlerAgentId = handler,
            currentStatusInt = (int)FederalSourceStatus.Active,
            motivationInt = (int)FederalSourceMotivation.Money,
            accessLevel = 45,
            reliability = 55,
            loyaltyToBureau = 35,
            loyaltyToOriginalOrg = 20,
            fearLevel = 25,
            greedLevel = 50,
            blackmailPressure = 0,
            exposureRisk = 30,
            counterIntelRisk = 25,
            usefulness = 50,
            lastContactAt = DateTime.UtcNow.Ticks,
            origin = "City records office",
            background = "Accounting assistant",
            classificationLevelInt = (int)FederalIntelClassification.LevelB
        });
        BureauWorldState.federalSourceProfiles.Add(new FederalSourceProfile
        {
            sourceId = "src_uc_001",
            sourceTypeInt = (int)FederalIntelSourceType.UndercoverAgent,
            realCharacterId = "char_fbu_field_12",
            codeName = "Quiet Ember",
            coverIdentityId = "cover_dock_laborer_77",
            originalOrganizationId = string.Empty,
            handlerAgentId = handler,
            currentStatusInt = (int)FederalSourceStatus.Active,
            motivationInt = (int)FederalSourceMotivation.Ideology,
            accessLevel = 62,
            reliability = 68,
            loyaltyToBureau = 75,
            loyaltyToOriginalOrg = 0,
            fearLevel = 28,
            greedLevel = 8,
            blackmailPressure = 0,
            exposureRisk = 44,
            counterIntelRisk = 40,
            usefulness = 72,
            lastContactAt = DateTime.UtcNow.Ticks,
            origin = "Transferred from other district",
            background = "Deep street cover training",
            classificationLevelInt = (int)FederalIntelClassification.LevelC
        });
        // Baseline personality hints for known seeded characters.
        PersonalityProgressionResolver.ApplyMajorEvent("char_city_clerk_01", PersonalityTraitType.MoneyGreedy, 1, PersonalityTraitSource.Environment);
        PersonalityProgressionResolver.ApplyBehaviorPattern("char_city_clerk_01", PersonalityTraitType.Calculated, 1, PersonalityTraitSource.Behavior);
        PersonalityProgressionResolver.ApplyMajorEvent("char_fbu_field_12", PersonalityTraitType.Disciplined, 2, PersonalityTraitSource.Training);
        PersonalityProgressionResolver.ApplyBehaviorPattern("char_fbu_field_12", PersonalityTraitType.Calm, 1, PersonalityTraitSource.Training);
    }
}

public static class FederalIntelCollectionResolver
{
    public static void CollectDaily(int day, int citySeed, int interest)
    {
        int maxNew = Mathf.Clamp(1 + interest / 35, 1, 4);
        for (int i = 0; i < BureauWorldState.federalSourceProfiles.Count && maxNew > 0; i++)
        {
            var src = BureauWorldState.federalSourceProfiles[i];
            if (src == null) continue;
            if (!FederalIntelInvariants.ValidateSource(src, out _)) continue;
            if ((FederalSourceStatus)src.currentStatusInt == FederalSourceStatus.Burned
                || (FederalSourceStatus)src.currentStatusInt == FederalSourceStatus.Dead)
                continue;

            int h = Math.Abs((day * 131 + citySeed * 17 + src.sourceId.GetHashCode()) ^ (i * 97)) % 100;
            if (h > Mathf.Clamp(src.usefulness, 20, 85)) continue;

            var intel = new FederalIntelItem
            {
                intelId = "intel_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                sourceId = src.sourceId,
                sourceTypeInt = src.sourceTypeInt,
                collectionMethodInt = PickMethodBySource(src),
                targetTypeInt = (int)FederalTargetType.Organization,
                targetId = PickTarget(day, citySeed, i),
                contentSummary = "Routine feed from source " + src.codeName,
                rawContent = string.Empty,
                receivedAt = DateTime.UtcNow.Ticks,
                collectedByAgentId = src.handlerAgentId,
                handlerAgentId = src.handlerAgentId,
                reliability = Mathf.Clamp(src.reliability + (h % 7) - 3, 5, 95),
                specificity = Mathf.Clamp(35 + src.accessLevel / 2 + (h % 11) - 5, 0, 100),
                freshness = Mathf.Clamp(60 + (h % 20) - 10, 0, 100),
                verificationStatusInt = (int)FederalIntelVerificationStatus.Unverified,
                deceptionRisk = Mathf.Clamp(src.counterIntelRisk / 2 + (h % 15), 0, 100),
                counterIntelRisk = Mathf.Clamp(src.counterIntelRisk + (h % 10), 0, 100),
                legalRisk = EstimateLegalRisk(PickMethodBySource(src)),
                pressRisk = 5,
                politicalRisk = 5,
                exposureRisk = src.exposureRisk,
                nationalSecurityRisk = Mathf.Clamp(interest / 2 + (h % 20), 0, 100),
                actionabilityInt = (int)FederalIntelActionability.WatchTarget,
                truthStateInt = (int)FederalIntelTruthState.PartialView,
                classificationLevelInt = src.classificationLevelInt
            };
            if (FederalIntelInvariants.ValidateIntel(intel, out _))
            {
                BureauWorldState.federalIntelItems.Add(intel);
                maxNew--;
            }
        }
        while (BureauWorldState.federalIntelItems.Count > 500)
            BureauWorldState.federalIntelItems.RemoveAt(0);
    }

    static int PickMethodBySource(FederalSourceProfile src)
    {
        if (src == null) return (int)FederalIntelCollectionMethod.VoluntaryReport;
        return (FederalIntelSourceType)src.sourceTypeInt switch
        {
            FederalIntelSourceType.UndercoverAgent => (int)FederalIntelCollectionMethod.UndercoverContact,
            FederalIntelSourceType.DoubleAgent => (int)FederalIntelCollectionMethod.UndercoverContact,
            FederalIntelSourceType.Press => (int)FederalIntelCollectionMethod.PressScan,
            FederalIntelSourceType.PoliceFile => (int)FederalIntelCollectionMethod.PoliceCaseAccess,
            FederalIntelSourceType.TaxFile => (int)FederalIntelCollectionMethod.DocumentAccess,
            _ => (int)FederalIntelCollectionMethod.VoluntaryReport
        };
    }

    static string PickTarget(int day, int citySeed, int idx)
    {
        string[] t = { "player_org", "city_smuggling_ring", "city_black_market_cell", "corrupt:port_authority_chain" };
        int k = Math.Abs((day * 31 + citySeed * 13 + idx * 7)) % t.Length;
        return t[k];
    }

    static int EstimateLegalRisk(int methodInt)
    {
        if (methodInt == (int)FederalIntelCollectionMethod.Torture) return 95;
        if (methodInt == (int)FederalIntelCollectionMethod.IllegalEntry) return 85;
        if (methodInt == (int)FederalIntelCollectionMethod.CoercedInterrogation) return 80;
        if (methodInt == (int)FederalIntelCollectionMethod.BlackmailThreat) return 72;
        if (methodInt == (int)FederalIntelCollectionMethod.BlackCashPurchase) return 70;
        return 20;
    }
}

public static class FederalIntelVerificationResolver
{
    public static void RunDailyVerification(int day, int citySeed)
    {
        for (int i = 0; i < BureauWorldState.federalIntelItems.Count; i++)
        {
            var x = BureauWorldState.federalIntelItems[i];
            if (x == null) continue;
            int h = Mathf.Abs((day * 97 + citySeed * 19 + x.intelId.GetHashCode()) ^ (i * 43)) % 100;
            if (h < 18) x.verificationStatusInt = (int)FederalIntelVerificationStatus.Contradicted;
            else if (h < 45) x.verificationStatusInt = (int)FederalIntelVerificationStatus.PartiallyVerified;
            else if (h < 80) x.verificationStatusInt = (int)FederalIntelVerificationStatus.Corroborated;
            else x.verificationStatusInt = (int)FederalIntelVerificationStatus.OperationallyConfirmed;
        }
    }
}

public static class FederalIntelToCaseResolver
{
    public static void LinkIntelToCases(int day)
    {
        for (int i = 0; i < BureauWorldState.federalIntelItems.Count; i++)
        {
            var x = BureauWorldState.federalIntelItems[i];
            if (x == null) continue;
            x.linkedFederalCaseIds.Clear();
            for (int c = 0; c < BureauWorldState.FederalCases.Count; c++)
            {
                var cf = BureauWorldState.FederalCases[c];
                if (cf == null || cf.isDestroyed) continue;
                if (!string.IsNullOrEmpty(cf.targetId) &&
                    string.Equals(cf.targetId, x.targetId, StringComparison.OrdinalIgnoreCase))
                {
                    x.linkedFederalCaseIds.Add(cf.caseId);
                    cf.intelStrength = Mathf.Clamp(cf.intelStrength + 1, 0, 100);
                }
            }
        }
        _ = day;
    }
}

public static class FederalIntelToActionResolver
{
    public static void DeriveActionability(int day, int citySeed)
    {
        for (int i = 0; i < BureauWorldState.federalIntelItems.Count; i++)
        {
            var x = BureauWorldState.federalIntelItems[i];
            if (x == null) continue;
            var source = FindSource(x.sourceId);
            int score = FederalIntelAssessmentResolver.ComputeIntelValue(x, source);
            x.actionabilityInt = FederalIntelAssessmentResolver.DeriveActionabilityFromScore(score, x.nationalSecurityRisk, x.verificationStatusInt);
            if (!FederalIntelInvariants.ValidateIntel(x, out _))
                x.actionabilityInt = (int)FederalIntelActionability.ArchiveOnly;
        }
        _ = day; _ = citySeed;
    }

    static FederalSourceProfile FindSource(string id)
    {
        for (int i = 0; i < BureauWorldState.federalSourceProfiles.Count; i++)
        {
            var s = BureauWorldState.federalSourceProfiles[i];
            if (s != null && string.Equals(s.sourceId, id, StringComparison.OrdinalIgnoreCase))
                return s;
        }
        return null;
    }
}

public static class FederalSourceExposureResolver
{
    public static void TickExposure(int day, int citySeed, int bureauExposure0to100)
    {
        for (int i = 0; i < BureauWorldState.federalSourceProfiles.Count; i++)
        {
            var s = BureauWorldState.federalSourceProfiles[i];
            if (s == null) continue;
            int pressure = Mathf.Clamp(s.exposureRisk + bureauExposure0to100 / 2 + s.counterIntelRisk / 2, 0, 100);
            int h = Mathf.Abs((day * 59 + citySeed * 29 + s.sourceId.GetHashCode()) ^ (i * 11)) % 100;
            if (h < pressure / 8)
            {
                s.currentStatusInt = (int)FederalSourceStatus.Compromised;
                if (string.IsNullOrEmpty(s.exposureReason))
                    s.exposureReason = "Counter-intel pressure and exposure accumulation";
            }
            TryForceDoubleAgentShift(s, day, citySeed, h);
        }
    }

    static void TryForceDoubleAgentShift(FederalSourceProfile s, int day, int citySeed, int seedRoll)
    {
        if (s == null) return;
        if ((FederalSourceStatus)s.currentStatusInt != FederalSourceStatus.Compromised
            && (FederalSourceStatus)s.currentStatusInt != FederalSourceStatus.Unstable)
            return;

        int bureauHold = Mathf.Clamp(s.loyaltyToBureau + s.reliability / 2 - s.fearLevel / 3, 0, 100);
        int counterPressure = Mathf.Clamp(
            s.loyaltyToOriginalOrg + s.fearLevel + s.blackmailPressure + s.counterIntelRisk / 2, 0, 140);
        if (!string.IsNullOrEmpty(s.realCharacterId))
        {
            // Personality influence in universal system: loyal/calm resist turn; cowardly/treacherous increase turn chance.
            var loyal = PersonalityResolverUtil.GetTraitIntensity(s.realCharacterId, PersonalityTraitType.Loyal);
            var coward = PersonalityResolverUtil.GetTraitIntensity(s.realCharacterId, PersonalityTraitType.Cowardly);
            var treach = PersonalityResolverUtil.GetTraitIntensity(s.realCharacterId, PersonalityTraitType.Treacherous);
            var calc = PersonalityResolverUtil.GetTraitIntensity(s.realCharacterId, PersonalityTraitType.Calculated);
            bureauHold += loyal * 7 + calc * 3;
            counterPressure += coward * 8 + treach * 10;
        }
        int roll = Mathf.Abs((day * 83 + citySeed * 41 + s.sourceId.GetHashCode()) ^ seedRoll) % 100;
        bool forced = counterPressure > bureauHold + 12 && roll < Mathf.Clamp(counterPressure - bureauHold, 5, 80);
        if (!forced) return;

        s.sourceTypeInt = (int)FederalIntelSourceType.DoubleAgent;
        if (string.IsNullOrEmpty(s.originalOrganizationId))
            s.originalOrganizationId = InferOrganizationFromSource(s);
        s.currentStatusInt = (int)FederalSourceStatus.TurnedAgainstBureau;
        s.exposureReason = "Forced double-agent shift under severe external pressure";
        s.reliability = Mathf.Clamp(s.reliability - 20, 0, 100);
        s.loyaltyToBureau = Mathf.Clamp(s.loyaltyToBureau - 25, 0, 100);
        s.loyaltyToOriginalOrg = Mathf.Clamp(s.loyaltyToOriginalOrg + 20, 0, 100);
        if (!string.IsNullOrEmpty(s.realCharacterId))
        {
            PersonalityProgressionResolver.ApplyMajorEvent(
                s.realCharacterId,
                PersonalityTraitType.Cowardly,
                2,
                PersonalityTraitSource.Trauma);
            PersonalityProgressionResolver.ApplyMajorEvent(
                s.realCharacterId,
                PersonalityTraitType.Treacherous,
                2,
                PersonalityTraitSource.Trauma);
        }
    }

    static string InferOrganizationFromSource(FederalSourceProfile s)
    {
        if (s == null || string.IsNullOrEmpty(s.sourceId)) return "org_unknown";
        if (s.sourceId.Contains("uc")) return "city_smuggling_ring";
        return "city_black_market_cell";
    }
}

static class PersonalityResolverUtil
{
    public static int GetTraitIntensity(string characterId, PersonalityTraitType traitType)
    {
        if (string.IsNullOrEmpty(characterId)) return 0;
        for (int i = 0; i < PersonalityWorldState.Profiles.Count; i++)
        {
            var p = PersonalityWorldState.Profiles[i];
            if (p == null || !string.Equals(p.characterId, characterId, StringComparison.OrdinalIgnoreCase)) continue;
            if (p.traits == null) return 0;
            for (int t = 0; t < p.traits.Count; t++)
            {
                var tr = p.traits[t];
                if (tr != null && tr.traitTypeInt == (int)traitType)
                    return Mathf.Clamp(tr.intensity, 1, 3);
            }
        }
        return 0;
    }
}

