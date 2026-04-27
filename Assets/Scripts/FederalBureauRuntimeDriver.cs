using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-calendar-day federal Bureau brain: interest → status → strategy → workflow requests → stub execution.
/// Wires to <see cref="MicroBlockDayAdvanceHooks"/>. No UI required.
/// </summary>
public static class FederalBureauRuntimeDriver
{
    public static void OnCalendarDayAdvanced(int previousDay, int newDay)
    {
        _ = previousDay;
        if (newDay < 1) return;
        if (!BureauWorldState.IsBootstrapped)
            BureauWorldState.EnsureBootstrappedForSession(GameSessionState.CityMapSeed, newDay);
        if (!BureauWorldState.IsBootstrapped) return;
        if (BureauWorldState.lastRuntimeDay == newDay) return; // one pass per day

        RunFullDay(newDay, GameSessionState.CityMapSeed);
        BureauWorldState.lastRuntimeDay = newDay;
    }

    static void RunFullDay(int day, int citySeed)
    {
        int statusBefore = (int)BureauWorldState.bureauStatus;
        int heatBefore = BureauWorldState.currentHeat;
        int polBefore = BureauWorldState.politicalPressure;
        var scan = ScanWorldThreats(day, citySeed);
        int interest = FederalInterestScoreResolver.Compute(scan, citySeed);
        BureauWorldState.federalRuntimeInterest01 = interest;
        BureauWorldState.federalRuntimeExposureEvents01 = Math.Min(10, BureauWorldState.FederalDeviationRecords != null ? BureauWorldState.FederalDeviationRecords.Count : 0);

        FederalPolicyResolver.EnsureSeeded(citySeed);
        _ = FederalDirectorPolicyCalibrationEngine.CalibrateForDay(day, citySeed, interest, scan);
        int polInfl = FederalPolicyInfluenceResolver.ComputeAggregate(day, citySeed, out int fRes);
        BureauWorldState.lastAggregatedPolicyInfluence0to100 = polInfl;
        BureauWorldState.lastFieldCultureResistance0to100 = fRes;
        FederalPolicyConflictResolver.RunDaily(day, citySeed);
        // Federal intelligence loop: collect -> verify -> assess -> link to cases -> derive actionability.
        FederalSourceHandlingResolver.EnsureBaselineSources(day, citySeed);
        FederalIntelCollectionResolver.CollectDaily(day, citySeed, interest);
        FederalIntelVerificationResolver.RunDailyVerification(day, citySeed);
        RunCounterIntelAndDisinformation(day, citySeed);
        FederalIntelToCaseResolver.LinkIntelToCases(day);
        FederalIntelToActionResolver.DeriveActionability(day, citySeed);
        FederalSourceExposureResolver.TickExposure(day, citySeed, BureauWorldState.publicExposure);

        var report = new FederalDailyReport
        {
            day = day,
            bureauStatusBeforeInt = statusBefore,
            selectedStrategyInt = 0,
            federalInterestScore = interest,
            policyInfluence0to100 = polInfl,
            fieldCultureResistance0to100 = fRes,
            directorPolicyModeInt = BureauWorldState.currentDirectorPolicyModeInt,
        };
        int expBefore = BureauWorldState.publicExposure;
        OpenOrUpdateFederalCases(day, interest, citySeed, report);
        UpdateCaseDynamicControlAndAwareness(day, interest, citySeed);
        DecideBureauStatusAndStrategy(day, interest, citySeed, out var strategy);
        var dMode = (FederalDirectorPolicyMode)BureauWorldState.currentDirectorPolicyModeInt;
        strategy = FederalStrategyBiasResolver.BiasStrategy(
            strategy, dMode, polInfl, day, citySeed, interest, BureauWorldState.publicExposure);
        if (polInfl < 32)
            DirectorBias(day, citySeed, ref strategy);
        report.selectedStrategyInt = (int)strategy;
        report.federalInterestScore = interest;

        report.selectedTargets = SelectTargetsForDay(day, interest, citySeed, out var primary);
        report.notes = string.Empty;

        GenerateWorkflowRequestsForStrategy(day, strategy, primary, interest, report);
        RunApprovedOperationsForActiveStub(day, report);
        RunStubPipelineOnGeneratedRequestsIfAny(day, report);
        UpdateHeatExposurePolitics(day, interest, strategy, expBefore, heatBefore, polBefore, report);

        report.bureauStatusAfterInt = (int)BureauWorldState.bureauStatus;

        BureauWorldState.lastSelectedStrategyInt = (int)strategy;
        BureauWorldState.lastPrimaryTargetId = primary ?? string.Empty;
        BureauWorldState.dailyReports.Add(report);
        while (BureauWorldState.dailyReports.Count > 100)
            BureauWorldState.dailyReports.RemoveAt(0);

        Log("[FederalRuntime] day=" + day + " interest=" + interest + " strategy=" + strategy + " status=" + BureauWorldState.bureauStatus);
        SaveBureauState();
    }

    public static void SaveBureauState()
    {
        // Persisted on next <see cref="GameSave.CaptureFromSession"/> / manual save; keep hook for auto-save if added later.
    }

    public static FederalBureauWorldScan ScanWorldThreats(int day, int citySeed)
    {
        int policeActive = 0;
        if (PoliceWorldState.IsBootstrapped && PoliceWorldState.CaseFiles != null)
        {
            for (int i = 0; i < PoliceWorldState.CaseFiles.Count; i++)
            {
                if (PoliceWorldState.CaseFiles[i] == null) continue;
                if (PoliceWorldState.CaseFiles[i].status == PoliceCaseStatus.Active
                    || PoliceWorldState.CaseFiles[i].status == PoliceCaseStatus.Operational)
                    policeActive++;
            }
        }
        int stuck = Mathf.Max(0, policeActive - 1);
        int speakeasy = (Mathf.Abs(Mix(day, citySeed)) % 20) + (GameSessionState.BlackCash / 2000);
        int dry = (Mathf.Abs(Mix2(day, citySeed)) % 15) + 3;

        return new FederalBureauWorldScan
        {
            dayIndex = day,
            organizationStageInt = (int)GameSessionState.PlayerOrganizationStage,
            playerThreatScore = GameSessionState.PlayerThreatScore,
            blackCash = GameSessionState.BlackCash,
            publicReputationHint = PlayerRunState.Character?.PublicReputation ?? 0,
            activePoliceCaseCount = policeActive,
            activeFederalCaseCount = BureauWorldState.FederalCases != null ? BureauWorldState.FederalCases.Count : 0,
            speakeasyPressureHint = Mathf.Min(20, speakeasy),
            dryEnforcementHint = dry,
            policeStuckCasesHint = Mathf.Min(15, stuck * 3),
            publicExposureHint = BureauWorldState.publicExposure,
            priorBureauHeat = BureauWorldState.currentHeat,
            priorPoliticalPressure = BureauWorldState.politicalPressure,
            priorFederalAggression = BureauWorldState.federalAggressionLevel,
        };
    }

    static int Mix(int a, int b)
    {
        unchecked { return a * 0x45D9F3B ^ b * 0x27D4EB2D; }
    }
    static int Mix2(int a, int b)
    {
        unchecked { return a * 0x6A7E3C29 + b; }
    }

    static void OpenOrUpdateFederalCases(int day, int interest, int citySeed, FederalDailyReport rep)
    {
        if (BureauWorldState.FederalCases == null) return;
        if (interest < 32) return;

        var potentialTargets = BuildPotentialTargetList();
        int maxOpen = Mathf.Clamp(1 + (interest / 30), 1, 4);
        int created = 0;
        for (int i = 0; i < potentialTargets.Count && created < maxOpen; i++)
        {
            string t = potentialTargets[i];
            if (string.IsNullOrEmpty(t) || ExistingCaseForTarget(t) != null) continue;
            string cid = "fbu_case_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            bool policeTarget = t.StartsWith("police:", StringComparison.OrdinalIgnoreCase);
            bool corruptionTarget = t.StartsWith("corrupt:", StringComparison.OrdinalIgnoreCase);
            var cf = new FederalCaseFileBureau
            {
                caseId = cid,
                caseType = corruptionTarget ? FederalCaseTypeBureau.Corruption : (policeTarget ? FederalCaseTypeBureau.PoliceFailure : FederalCaseTypeBureau.OrganizedCrime),
                targetTypeInt = policeTarget ? (int)FederalTargetType.PoliceCase : (int)FederalTargetType.Organization,
                targetId = t,
                owningDivisionId = corruptionTarget ? FederalBureauStructure.FederalBureauDivisionIds.InternalControl : FederalBureauStructure.FederalBureauDivisionIds.OrganizedCrime,
                leadAgentId = PickUnitChiefId(),
                status = FederalCaseStatusBureau.Active,
                priority = Mathf.Clamp(interest - i * 8, 25, 100),
                legalIntegrity = 45 + (interest / 4),
                evidenceStrength = Mathf.Clamp(30 + interest / 3, 0, 100),
                intelStrength = Mathf.Clamp(35 + interest / 3, 0, 100),
                secrecyLevel = corruptionTarget ? 70 : 40,
                politicalRisk = BureauWorldState.politicalPressure,
                nationalSecurityThreat = Mathf.Clamp((interest / 2) + (corruptionTarget ? 20 : 0), 0, 100),
                targetBehaviorArchetypeInt = (int)ResolveTargetBehaviorArchetype(t, day, citySeed),
                targetLeaderId = "leader:" + t,
                targetLeaderStatusInt = (int)FederalLeaderStatus.Active,
                dynamicControlLeverage0to100 = Mathf.Clamp(20 + interest / 4, 0, 100),
                currentlyControlledByBureau = false,
                targetAwareOfBureauAttention = false,
                lastAwarenessReason = string.Empty,
                leadershipTransitionReasonInt = (int)FederalLeadershipTransitionReason.None,
                leadershipTransitionDayIndex = 0,
                isDestroyed = false
            };
            if (policeTarget) cf.linkedPoliceCaseIds = LinkPoliceCaseSample();
            BureauWorldState.FederalCases.Add(cf);
            if (rep != null) rep.newFederalCaseIds.Add(cid);
            created++;
        }

        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            var c = BureauWorldState.FederalCases[i];
            if (c == null) continue;
            if (c.status == FederalCaseStatusBureau.Destroyed || c.isDestroyed)
                continue;
            if (interest < 28 && c.status == FederalCaseStatusBureau.Active)
            {
                c.status = FederalCaseStatusBureau.Archived;
                continue;
            }
            if (interest > 55 && c.status == FederalCaseStatusBureau.Archived)
                c.status = FederalCaseStatusBureau.Active;
            c.priority = Mathf.Clamp(Mathf.Max(c.priority, interest / 2), 0, 100);
            c.nationalSecurityThreat = Mathf.Clamp(Mathf.Max(c.nationalSecurityThreat, interest / 2), 0, 100);
            c.evidenceStrength = Mathf.Clamp(c.evidenceStrength + Mathf.Max(1, interest / 20), 0, 100);
            c.intelStrength = Mathf.Clamp(c.intelStrength + Mathf.Max(1, interest / 18), 0, 100);
            if (rep != null) rep.updatedFederalCaseIds.Add(c.caseId);
        }
    }

    static void UpdateCaseDynamicControlAndAwareness(int day, int interest, int citySeed)
    {
        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            var c = BureauWorldState.FederalCases[i];
            if (c == null || c.isDestroyed || c.status != FederalCaseStatusBureau.Active) continue;
            var behavior = (FederalTargetBehaviorArchetype)c.targetBehaviorArchetypeInt;
            ApplyLeaderPersonalityEnvironment(c, behavior);
            int leverage = c.evidenceStrength / 2 + c.intelStrength / 3 + c.legalIntegrity / 4;
            if (BureauWorldState.currentDirectorPolicyModeInt == (int)FederalDirectorPolicyMode.PoliceOverride) leverage += 6;
            if (BureauWorldState.currentDirectorPolicyModeInt == (int)FederalDirectorPolicyMode.PublicLegitimacy) leverage -= 4;
            leverage -= c.politicalRisk / 8;
            ApplyBehaviorLeverageModifiers(behavior, ref leverage, c, day, citySeed);
            c.dynamicControlLeverage0to100 = Mathf.Clamp(leverage, 0, 100);
            c.currentlyControlledByBureau = c.dynamicControlLeverage0to100 >= 65 && c.intelStrength >= 50;

            // Awareness is dynamic and event-driven, not a fixed spectrum.
            bool aware = false;
            string why = string.Empty;
            if (BureauWorldState.publicExposure >= 55)
            {
                aware = true;
                why = "Public exposure wave";
            }
            else if (c.targetId != null && c.targetId.StartsWith("corrupt:", StringComparison.OrdinalIgnoreCase) && c.intelStrength >= 45)
            {
                aware = true;
                why = "Insider counter-intelligence signal";
            }
            else if ((Math.Abs(Mix(day, citySeed + i)) % 100) < Mathf.Clamp(c.priority / 2, 8, 45))
            {
                aware = true;
                why = "Pattern detection by target network";
            }
            ApplyBehaviorAwarenessModifiers(behavior, c, day, citySeed, ref aware, ref why);
            c.targetAwareOfBureauAttention = aware;
            c.lastAwarenessReason = aware ? why : string.Empty;
            MaybeEmitBehaviorEvent(c, behavior, day, citySeed, aware);
        }
    }

    static void ApplyLeaderPersonalityEnvironment(FederalCaseFileBureau c, FederalTargetBehaviorArchetype behavior)
    {
        if (c == null || string.IsNullOrEmpty(c.targetLeaderId)) return;
        switch (behavior)
        {
            case FederalTargetBehaviorArchetype.Paranoid:
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Paranoid, 2);
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Suspicious, 1);
                break;
            case FederalTargetBehaviorArchetype.Conspiratorial:
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Calculated, 1);
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Suspicious, 2);
                break;
            case FederalTargetBehaviorArchetype.RecklessEgo:
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Proud, 2);
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Impulsive, 1);
                break;
            case FederalTargetBehaviorArchetype.Indifferent:
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Calm, 1);
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Disciplined, -1);
                break;
            case FederalTargetBehaviorArchetype.Cautious:
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Patient, 2);
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Methodical, 1);
                break;
            default:
                PersonalityProgressionResolver.ApplyEnvironmentPressure(c.targetLeaderId, PersonalityTraitType.Calculated, 1);
                break;
        }
    }

    static void RunCounterIntelAndDisinformation(int day, int citySeed)
    {
        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            var c = BureauWorldState.FederalCases[i];
            if (c == null || c.isDestroyed) continue;
            var behavior = (FederalTargetBehaviorArchetype)c.targetBehaviorArchetypeInt;
            int orgLevelHint = c.priority >= 70 ? 4 : (c.priority >= 45 ? 3 : 2);
            int recentLeaksHint = c.targetAwareOfBureauAttention ? 15 : 0;
            int ci = FederalCounterIntelResolver.ComputeCounterIntelPressure(
                c, behavior, orgLevelHint, BureauWorldState.publicExposure, recentLeaksHint);
            for (int n = 0; n < BureauWorldState.federalIntelItems.Count; n++)
            {
                var intel = BureauWorldState.federalIntelItems[n];
                if (intel == null) continue;
                if (!string.Equals(intel.targetId, c.targetId, StringComparison.OrdinalIgnoreCase)) continue;
                bool mark = FederalDisinformationResolver.ShouldMarkDisinformation(intel, ci, day, citySeed);
                if (!mark) continue;
                intel.truthStateInt = (int)FederalIntelTruthState.Disinformation;
                intel.verificationStatusInt = (int)FederalIntelVerificationStatus.PartiallyVerified;
                intel.deceptionRisk = Mathf.Clamp(intel.deceptionRisk + 20, 0, 100);
                BureauWorldState.federalDisinformationEvents.Add(new FederalDisinformationEvent
                {
                    eventId = "disinfo_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                    intelId = intel.intelId,
                    targetId = intel.targetId,
                    dayIndex = day,
                    severity = Mathf.Clamp(ci / 10, 1, 10),
                    narrative = "Counter-intel planted misleading stream."
                });
                AppendCaseActivityByTarget(c.targetId, day, string.Empty, "DisinformationInjected", "Target fed disinformation to federal channel", "Counter-intel action", false);
                if (BureauWorldState.federalDisinformationEvents.Count > 200)
                    BureauWorldState.federalDisinformationEvents.RemoveAt(0);
            }
        }
    }

    static FederalTargetBehaviorArchetype ResolveTargetBehaviorArchetype(string targetId, int day, int citySeed)
    {
        int h = Math.Abs(Mix(day ^ 0x4D2, citySeed ^ (targetId != null ? targetId.GetHashCode() : 17)));
        int roll = h % 100;
        if (roll < 20) return FederalTargetBehaviorArchetype.Paranoid;
        if (roll < 35) return FederalTargetBehaviorArchetype.Conspiratorial;
        if (roll < 50) return FederalTargetBehaviorArchetype.RecklessEgo;
        if (roll < 65) return FederalTargetBehaviorArchetype.Indifferent;
        if (roll < 80) return FederalTargetBehaviorArchetype.Cautious;
        return FederalTargetBehaviorArchetype.Standard;
    }

    static void ApplyBehaviorLeverageModifiers(FederalTargetBehaviorArchetype behavior, ref int leverage, FederalCaseFileBureau c, int day, int citySeed)
    {
        switch (behavior)
        {
            case FederalTargetBehaviorArchetype.Paranoid:
                leverage -= 6; // harder to hold stable control
                break;
            case FederalTargetBehaviorArchetype.Conspiratorial:
                leverage -= 8;
                if ((Math.Abs(Mix2(day, citySeed)) % 5) == 0) leverage -= 4;
                break;
            case FederalTargetBehaviorArchetype.RecklessEgo:
                leverage += 3; // easier to bait / overplay
                break;
            case FederalTargetBehaviorArchetype.Indifferent:
                leverage -= 2;
                break;
            case FederalTargetBehaviorArchetype.Cautious:
                leverage += 2;
                break;
        }
        if (c.targetAwareOfBureauAttention && behavior == FederalTargetBehaviorArchetype.Paranoid)
            leverage -= 5;
    }

    static void ApplyBehaviorAwarenessModifiers(
        FederalTargetBehaviorArchetype behavior,
        FederalCaseFileBureau c,
        int day,
        int citySeed,
        ref bool aware,
        ref string why)
    {
        // IMPORTANT: behavior alone must never create certain awareness.
        // Paranoid/conspiratorial targets can *suspect*, but awareness=true requires supporting signals.
        if ((behavior == FederalTargetBehaviorArchetype.Paranoid || behavior == FederalTargetBehaviorArchetype.Conspiratorial) && aware)
        {
            if (string.IsNullOrEmpty(why))
                why = "Behavior amplified existing warning signal";
        }
        if (behavior == FederalTargetBehaviorArchetype.Indifferent && aware)
        {
            // May ignore warning signs despite awareness.
            if ((Math.Abs(Mix(day + 17, citySeed + 31)) % 100) < 35)
            {
                aware = false;
                why = string.Empty;
            }
        }
    }

    static void MaybeEmitBehaviorEvent(FederalCaseFileBureau c, FederalTargetBehaviorArchetype behavior, int day, int citySeed, bool aware)
    {
        if (c == null || c.activityLog == null) return;
        int roll = Math.Abs(Mix(day + 71, citySeed + (c.caseId != null ? c.caseId.GetHashCode() : 0))) % 100;
        if (behavior == FederalTargetBehaviorArchetype.Paranoid && aware && roll < 18)
        {
            AppendCaseActivityByTarget(c.targetId, day, string.Empty, "CounterSurveillanceSpike", "Target paranoia triggers erratic counter-surveillance", "Behavior event", false);
            c.politicalRisk = Mathf.Clamp(c.politicalRisk + 2, 0, 100);
        }
        else if (behavior == FederalTargetBehaviorArchetype.Paranoid && !aware && roll < 12)
        {
            AppendCaseActivityByTarget(c.targetId, day, string.Empty, "ParanoidFalseAlarm", "Target behaves as if watched despite no confirmed signal", "Behavior suspicion only", false);
        }
        else if (behavior == FederalTargetBehaviorArchetype.Conspiratorial && aware && roll < 16)
        {
            AppendCaseActivityByTarget(c.targetId, day, string.Empty, "InternalPurge", "Conspiratorial cell purge causes volatility", "Behavior event", false);
            c.intelStrength = Mathf.Clamp(c.intelStrength - 2, 0, 100);
        }
        else if (behavior == FederalTargetBehaviorArchetype.Conspiratorial && !aware && roll < 10)
        {
            AppendCaseActivityByTarget(c.targetId, day, string.Empty, "ConspiratorialNoise", "Cell interprets random noise as hostile pressure", "Behavior suspicion only", false);
        }
        else if (behavior == FederalTargetBehaviorArchetype.RecklessEgo && roll < 20)
        {
            AppendCaseActivityByTarget(c.targetId, day, string.Empty, "EgoDrivenExposure", "Reckless ego move creates exploitable exposure", "Behavior event", false);
            c.evidenceStrength = Mathf.Clamp(c.evidenceStrength + 3, 0, 100);
        }
        else if (behavior == FederalTargetBehaviorArchetype.Indifferent && roll < 14)
        {
            AppendCaseActivityByTarget(c.targetId, day, string.Empty, "ComplacencyWindow", "Complacency window opens in target routine", "Behavior event", false);
            c.dynamicControlLeverage0to100 = Mathf.Clamp(c.dynamicControlLeverage0to100 + 2, 0, 100);
        }
    }

    static List<string> BuildPotentialTargetList()
    {
        var list = new List<string> { "player_org", "city_smuggling_ring", "city_black_market_cell" };
        if (PoliceWorldState.IsBootstrapped && PoliceWorldState.CaseFiles != null)
        {
            for (int i = 0; i < PoliceWorldState.CaseFiles.Count && i < 6; i++)
            {
                var pc = PoliceWorldState.CaseFiles[i];
                if (pc == null || string.IsNullOrEmpty(pc.caseId)) continue;
                list.Add("police:" + pc.caseId);
            }
        }
        // Corruption watchlist stubs (future: replace by real city actors / relations graph)
        list.Add("corrupt:municipal_procurement_office");
        list.Add("corrupt:port_authority_chain");
        return list;
    }

    static FederalCaseFileBureau ExistingCaseForTarget(string targetId)
    {
        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            var c = BureauWorldState.FederalCases[i];
            if (c == null || c.isDestroyed) continue;
            if (string.Equals(c.targetId, targetId, StringComparison.OrdinalIgnoreCase))
                return c;
        }
        return null;
    }

    static string PickUnitChiefId()
    {
        for (int i = 0; i < BureauWorldState.Roster.Count; i++)
        {
            var a = BureauWorldState.Roster[i];
            if (a != null && a.rank == FederalBureauRank.UnitChief)
                return a.agentId;
        }
        return "fbu_dir_01";
    }

    static void DecideBureauStatusAndStrategy(
        int day, int interest, int citySeed,
        out FederalDailyStrategy strategy)
    {
        bool crisis = BureauWorldState.publicExposure >= 70 && BureauWorldState.currentHeat >= 50;
        if (!crisis && (BureauWorldState.publicExposure + BureauWorldState.currentHeat) > 150)
            crisis = true;
        if (crisis)
        {
            BureauWorldState.bureauStatus = BureauOperationalStatus.Crisis;
            BureauWorldState.activeStrategy = "crisis_mitigation";
            strategy = (Mathf.Abs(Mix(day, citySeed)) % 2 == 0) ? FederalDailyStrategy.LayLow : FederalDailyStrategy.CoverUp;
            return;
        }

        if (BureauWorldState.politicalPressure >= 70)
        {
            strategy = FederalDailyStrategy.Observe;
            if (BureauWorldState.federalRuntimeInterest01 >= 45) strategy = FederalDailyStrategy.BuildCase;
            if (BureauWorldState.federalRuntimeInterest01 >= 65) strategy = FederalDailyStrategy.Pressure;
        }
        else
        {
            if (interest < 25)
                strategy = FederalDailyStrategy.Observe;
            else if (interest < 45)
                strategy = FederalDailyStrategy.Observe;
            else if (interest < 55)
                strategy = FederalDailyStrategy.BuildCase;
            else if (interest < 70)
                strategy = FederalDailyStrategy.Pressure;
            else if (interest < 85)
                strategy = (Mathf.Abs(Mix2(day, citySeed)) % 2 == 0) ? FederalDailyStrategy.Infiltrate : FederalDailyStrategy.TakeOverPoliceCase;
            else
                strategy = FederalDailyStrategy.Strike;
        }

        if (BureauWorldState.federalRuntimeInterest01 < 20)
        {
            BureauWorldState.bureauStatus = BureauOperationalStatus.Dormant;
            strategy = FederalDailyStrategy.Observe;
        }
        else if (BureauWorldState.federalRuntimeInterest01 < 45)
            BureauWorldState.bureauStatus = BureauOperationalStatus.Watching;
        else if (BureauWorldState.federalRuntimeInterest01 < 80)
        {
            BureauWorldState.bureauStatus = FederalCaseCountActive() > 0 ? BureauOperationalStatus.Active : BureauOperationalStatus.Watching;
        }
        else
        {
            BureauWorldState.bureauStatus = interest >= 90 || BureauWorldState.federalRuntimeInterest01 >= 85
                ? BureauOperationalStatus.Aggressive
                : BureauOperationalStatus.Active;
        }

        BureauWorldState.activeStrategy = strategy.ToString();
        BureauWorldState.SyncToLegacySessionEngagement();
    }

    static int FederalCaseCountActive()
    {
        int n = 0;
        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            if (BureauWorldState.FederalCases[i] != null
                && BureauWorldState.FederalCases[i].status == FederalCaseStatusBureau.Active
                && !BureauWorldState.FederalCases[i].isDestroyed)
                n++;
        }
        return n;
    }

    static void DirectorBias(int day, int citySeed, ref FederalDailyStrategy s)
    {
        var dir = BureauWorldState.GetAgent("fbu_dir_01");
        int h = dir != null ? dir.agentId.GetHashCode() ^ day ^ citySeed : Mix(day, citySeed);
        bool aggressive = (h & 1) == 0;
        if (aggressive && s == FederalDailyStrategy.Observe)
            s = FederalDailyStrategy.Pressure;
        if (!aggressive && s == FederalDailyStrategy.Strike)
            s = FederalDailyStrategy.BuildCase;
    }

    static List<string> SelectTargetsForDay(int day, int interest, int citySeed, out string primary)
    {
        var list = new List<string> { "player_org", "city_core" };
        if (PoliceWorldState.IsBootstrapped && PoliceWorldState.CaseFiles != null)
        {
            for (int i = 0; i < PoliceWorldState.CaseFiles.Count && i < 4; i++)
            {
                if (PoliceWorldState.CaseFiles[i] != null && !string.IsNullOrEmpty(PoliceWorldState.CaseFiles[i].caseId))
                    list.Add("police:" + PoliceWorldState.CaseFiles[i].caseId);
            }
        }
        if (BureauWorldState.FederalCases != null)
        {
            for (int i = 0; i < BureauWorldState.FederalCases.Count && i < 3; i++)
            {
                if (BureauWorldState.FederalCases[i] != null && !string.IsNullOrEmpty(BureauWorldState.FederalCases[i].caseId))
                    list.Add("federal:" + BureauWorldState.FederalCases[i].caseId);
            }
        }
        string pid = list[0];
        if (BureauWorldState.FederalCases != null && BureauWorldState.FederalCases.Count > 0 && BureauWorldState.FederalCases[0] != null)
            pid = "federal:" + BureauWorldState.FederalCases[0].caseId;
        else if (PoliceWorldState.IsBootstrapped && PoliceWorldState.CaseFiles != null && PoliceWorldState.CaseFiles.Count > 0 && PoliceWorldState.CaseFiles[0] != null)
            pid = "police:" + PoliceWorldState.CaseFiles[0].caseId;
        string pr = FederalTargetPriorityResolver.PickPrimaryTarget(
            list, interest, 5, 0, 2, 2, BureauWorldState.publicExposure, BureauWorldState.politicalPressure, 0, day, citySeed);
        primary = string.IsNullOrEmpty(pr) ? pid : pr;
        return list;
    }

    static void GenerateWorkflowRequestsForStrategy(
        int day, FederalDailyStrategy st, string primary, int interest, FederalDailyReport report)
    {
        if (BureauWorldState.federalRuntimeInterest01 < 25) return; // track only
        int maxReq = 2;
        if (BureauWorldState.federalRuntimeInterest01 >= 65) maxReq = 3;
        if (BureauWorldState.federalRuntimeInterest01 >= 85) maxReq = 4;
        if (BureauWorldState.federalRuntimeInterest01 >= 90) maxReq = 3;

        string requester = PickRequestingAgent();
        if (string.IsNullOrEmpty(requester)) return;

        var plan = new List<FederalActionType>();
        bool coverOk = (BureauWorldState.federalRuntimeExposureEvents01 > 0) || (BureauWorldState.publicExposure > 40);
        if (st == FederalDailyStrategy.CoverUp)
        {
            if (!FederalBureauRuntimeInvariants.CoverUpAllowance(coverOk) && st == FederalDailyStrategy.CoverUp)
            {
                report.notes = "CoverUp: no prior exposure in snapshot — policy notes only; no action requests.\n" + (report.notes ?? string.Empty);
            }
        }

        switch (st)
        {
            case FederalDailyStrategy.Observe: plan.Add(FederalActionType.ShortSurveillance); plan.Add(FederalActionType.AccessPoliceCase); plan.Add(FederalActionType.HandleSource); break;
            case FederalDailyStrategy.Infiltrate: plan.Add(FederalActionType.RecruitSource); plan.Add(FederalActionType.UseSafeHouse); break;
            case FederalDailyStrategy.Pressure: plan.Add(FederalActionType.FederalSearch); plan.Add(FederalActionType.FederalArrest); break;
            case FederalDailyStrategy.BuildCase: plan.Add(FederalActionType.OpenFederalCase); plan.Add(FederalActionType.ExtendedSurveillance); break;
            case FederalDailyStrategy.TakeOverPoliceCase: plan.Add(FederalActionType.TakeOverPoliceCase); plan.Add(FederalActionType.AccessPoliceCase); break;
            case FederalDailyStrategy.Strike: plan.Add(FederalActionType.FederalRaid); plan.Add(FederalActionType.FederalArrest); break;
            case FederalDailyStrategy.LayLow: return;
            case FederalDailyStrategy.CoverUp:
                if (FederalBureauRuntimeInvariants.CoverUpAllowance(coverOk))
                {
                    plan.Add(FederalActionType.AccessPoliceEvidence);
                    plan.Add(FederalActionType.PoliticalSensitiveAction);
                }
                break;
        }
        if (BureauWorldState.federalRuntimeInterest01 >= 75 && (st == FederalDailyStrategy.Pressure || st == FederalDailyStrategy.BuildCase))
        {
            if (plan.Count < 4) plan.Add(FederalActionType.DeepCoverInsertion);
        }
        if (ShouldConsiderSecurityThreatRemoval(interest, st))
        {
            plan.Add(FederalActionType.SecurityThreatRemoval);
        }
        string intelDrivenTarget = PickIntelDrivenTargetHint(primary, day, st, interest, out bool disinfoDominated);
        if (!string.IsNullOrEmpty(intelDrivenTarget))
            primary = intelDrivenTarget;
        if (disinfoDominated && report != null)
            report.notes = (report.notes ?? string.Empty) + " Intel stream likely disinformation-dominated; targeting may be skewed.";
        for (int i = 0; i < plan.Count && report.generatedRequestIds.Count < maxReq; i++)
        {
            if (!ShouldEmitActionForStatus(plan[i], BureauWorldState.bureauStatus, st))
                continue;
            if (!TryBuildAndEnqueueRequest(plan[i], day, requester, primary, interest, out var rid, out var n))
            {
                report.notes = (report.notes ?? string.Empty) + n + " ";
                AppendCaseActivityByTarget(primary, day, null, plan[i].ToString(), "Skipped action", n, false);
                continue;
            }
            if (!string.IsNullOrEmpty(rid)) report.generatedRequestIds.Add(rid);
            AppendCaseActivityByTarget(primary, day, null, plan[i].ToString(), "Planned action", "Daily strategy " + st, false);
        }
    }

    static bool ShouldEmitActionForStatus(FederalActionType at, BureauOperationalStatus s, FederalDailyStrategy st)
    {
        if (at == FederalActionType.FederalRaid)
            return s == BureauOperationalStatus.Active || s == BureauOperationalStatus.Aggressive || s == BureauOperationalStatus.Crisis;
        if (at == FederalActionType.TakeOverPoliceCase)
            return FirstPoliceCaseId() != null;
        if (at == FederalActionType.AccessPoliceCase || at == FederalActionType.AccessPoliceEvidence)
            return PoliceWorldState.IsBootstrapped && PoliceWorldState.CaseFiles != null && PoliceWorldState.CaseFiles.Count > 0;
        return true;
    }

    static string FirstPoliceCaseId()
    {
        if (!PoliceWorldState.IsBootstrapped || PoliceWorldState.CaseFiles == null) return null;
        for (int i = 0; i < PoliceWorldState.CaseFiles.Count; i++)
        {
            if (PoliceWorldState.CaseFiles[i] == null) continue;
            if (!string.IsNullOrEmpty(PoliceWorldState.CaseFiles[i].caseId))
                return PoliceWorldState.CaseFiles[i].caseId;
        }
        return null;
    }

    static string FirstFederalCaseId()
    {
        if (BureauWorldState.FederalCases == null) return null;
        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            if (BureauWorldState.FederalCases[i] == null) continue;
            if (!string.IsNullOrEmpty(BureauWorldState.FederalCases[i].caseId))
                return BureauWorldState.FederalCases[i].caseId;
        }
        return null;
    }

    static List<string> LinkPoliceCaseSample()
    {
        var l = new List<string>();
        string a = FirstPoliceCaseId();
        if (!string.IsNullOrEmpty(a)) l.Add(a);
        return l;
    }

    static bool TryBuildAndEnqueueRequest(
        FederalActionType at, int day, string requester, string targetHint, int interest, out string requestId, out string errorNote)
    {
        requestId = null; errorNote = null;
        if (at == FederalActionType.TakeOverPoliceCase && string.IsNullOrEmpty(FirstPoliceCaseId()))
        {
            errorNote = "TakeOver: no police case. ";
            return false;
        }
        if (at == FederalActionType.FederalRaid)
        {
            var s = BureauWorldState.bureauStatus;
            if (s != BureauOperationalStatus.Active && s != BureauOperationalStatus.Aggressive && s != BureauOperationalStatus.Crisis)
            {
                errorNote = "FederalRaid: bureau not Active/Aggressive/Crisis. ";
                return false;
            }
        }

        var r = new FederalWorkflowRequest
        {
            requestId = string.Empty,
            actionType = at,
            requestedByAgentId = requester,
            targetType = FederalTargetType.Organization,
            targetId = string.IsNullOrEmpty(targetHint) ? "player_org" : targetHint,
            relatedCaseId = FirstFederalCaseId() ?? string.Empty,
            relatedPoliceCaseId = FirstPoliceCaseId() ?? string.Empty,
            reason = "FederalBureauRuntime day " + day + " interest " + interest,
            hasPoliceAccessLog = at == FederalActionType.AccessPoliceCase || at == FederalActionType.AccessPoliceEvidence,
            hasTakeoverLog = at == FederalActionType.TakeOverPoliceCase,
            hasWarrant = at == FederalActionType.SecurityThreatRemoval && HasSoftJudicialPath(interest),
        };
        if (at == FederalActionType.AccessPoliceCase) r.hasPoliceAccessLog = true;
        if (at == FederalActionType.TakeOverPoliceCase) r.hasTakeoverLog = true;

        FederalWorkflowOrchestrator.CreateRequest(r);
        requestId = r.requestId;
        FederalWorkflowOrchestrator.TryResolveAuthority(requestId, out _);
        FederalWorkflowOrchestrator.TryFindApproverForRequest(requestId, out _);
        return true;
    }

    static bool ShouldConsiderSecurityThreatRemoval(int interest, FederalDailyStrategy st)
    {
        // Option C (soft thresholds): raise likelihood under severe conditions, but don't hard-block by score alone.
        if (st != FederalDailyStrategy.Strike && st != FederalDailyStrategy.Pressure) return false;
        int soft = 0;
        if (interest >= 80) soft += 2;
        if (BureauWorldState.publicExposure >= 60) soft += 1;
        if (BureauWorldState.lastAggregatedPolicyInfluence0to100 >= 45) soft += 1;
        if (BureauWorldState.currentDirectorPolicyModeInt == (int)FederalDirectorPolicyMode.StrategicDecapitation) soft += 2;
        return soft >= 3;
    }

    static bool HasSoftJudicialPath(int interest)
    {
        // Represents "strong case + no reasonable alternative" in soft model.
        int soft = 0;
        if (interest >= 85) soft++;
        if (BureauWorldState.lastAggregatedPolicyInfluence0to100 >= 50) soft++;
        if (BureauWorldState.FederalCases != null && BureauWorldState.FederalCases.Count > 0) soft++;
        return soft >= 2;
    }

    static string PickIntelDrivenTargetHint(string fallbackPrimary, int day, FederalDailyStrategy st, int interest, out bool disinfoDominated)
    {
        disinfoDominated = false;
        if (BureauWorldState.federalIntelItems == null || BureauWorldState.federalIntelItems.Count == 0)
            return fallbackPrimary;

        int bestScore = -1;
        string bestTarget = fallbackPrimary;
        int disinfoWeight = 0;
        int totalWeight = 0;
        for (int i = 0; i < BureauWorldState.federalIntelItems.Count; i++)
        {
            var x = BureauWorldState.federalIntelItems[i];
            if (x == null || string.IsNullOrEmpty(x.targetId)) continue;
            int act = x.actionabilityInt;
            if (act < (int)FederalIntelActionability.StartSurveillance) continue;
            int w = Mathf.Clamp(x.reliability + x.specificity + x.freshness - x.deceptionRisk, 0, 220);
            totalWeight += w;
            if (x.truthStateInt == (int)FederalIntelTruthState.Disinformation)
                disinfoWeight += w;
            if (w > bestScore)
            {
                bestScore = w;
                bestTarget = x.targetId;
            }
        }
        if (totalWeight > 0 && disinfoWeight * 100 / totalWeight >= 45)
        {
            disinfoDominated = true;
            // Under heavy disinfo, targeting can drift to wrong direction.
            if (Mathf.Abs(Mix(day, BureauWorldState.BootstrapSeed + interest)) % 100 < 60)
            {
                return "city_core";
            }
        }
        return bestTarget ?? fallbackPrimary;
    }

    static void RunStubPipelineOnGeneratedRequestsIfAny(int day, FederalDailyReport report)
    {
        if (report == null || report.generatedRequestIds == null || report.generatedRequestIds.Count == 0)
            return;
        string last = report.generatedRequestIds[report.generatedRequestIds.Count - 1];
        if (string.IsNullOrEmpty(last)) return;
        if (FederalWorkflowOrchestrator.RunStubFullPipeline(last, day, out var op, out _) && op != null)
        {
            report.completedOperationIds.Add(op.operationId);
            AppendCaseActivityByTarget(op.targetId, day, op.operationId, op.actionType.ToString(), "Operation completed", op.summaryLine, true);
            ApplyTargetReactionFromOperation(op, day, report);
        }
    }

    static void RunApprovedOperationsForActiveStub(int day, FederalDailyReport report)
    {
        for (int i = 0; i < BureauWorldState.ActiveWorkflowOperations.Count && i < 1; i++)
        {
            var op = BureauWorldState.ActiveWorkflowOperations[i];
            if (op == null) continue;
            if (FederalWorkflowOrchestrator.RunStubExecution(op.operationId, day, out _))
            {
                report.completedOperationIds.Add(op.operationId);
                AppendCaseActivityByTarget(op.targetId, day, op.operationId, op.actionType.ToString(), "Operation executed", "Active pipeline execution", true);
                ApplyTargetReactionFromOperation(op, day, report);
            }
        }
    }

    static void ApplyTargetReactionFromOperation(FederalWorkflowOperation op, int day, FederalDailyReport report)
    {
        if (op == null) return;
        bool tacticalCapture = op.actionType == FederalActionType.FederalArrest || op.actionType == FederalActionType.FederalRaid;
        bool failureLike = op.outcomeInt == (int)FederalOperationOutcome.Failure || op.outcomeInt == (int)FederalOperationOutcome.SevereFailure;
        if (!tacticalCapture || !failureLike) return;

        string reason = "Escaped federal capture attempt";
        bool touchedAny = false;
        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            var c = BureauWorldState.FederalCases[i];
            if (c == null || c.isDestroyed || c.status != FederalCaseStatusBureau.Active) continue;
            bool direct = string.Equals(c.targetId, op.targetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.caseId, op.relatedCaseId, StringComparison.OrdinalIgnoreCase);
            if (direct)
            {
                c.targetAwareOfBureauAttention = true;
                c.lastAwarenessReason = reason;
                c.politicalRisk = Mathf.Clamp(c.politicalRisk + 4, 0, 100);
                c.dynamicControlLeverage0to100 = Mathf.Clamp(c.dynamicControlLeverage0to100 - 6, 0, 100);
                AppendCaseActivityByTarget(c.targetId, day, op.operationId, "EscapeReaction", "Target confirmed federal pursuit after escape", reason, true);
                if (!string.IsNullOrEmpty(c.targetLeaderId))
                {
                    var ctx = new PersonalityContext(true, false, false, true, false, false);
                    PersonalityProgressionResolver.ApplyObservedEvent(c.targetLeaderId, PersonalityObservedEventType.EgoEscalation, ctx, 1);
                    PersonalityProgressionResolver.ApplyObservedEvent(c.targetLeaderId, PersonalityObservedEventType.RanCounterIntelChecks, ctx, 1);
                    TraitConflictResolver.NormalizeProfile(c.targetLeaderId);
                }
                touchedAny = true;
            }
        }

        // Propagate to close-circle organizational peers (not guaranteed certainty for all).
        if (touchedAny)
        {
            for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
            {
                var c = BureauWorldState.FederalCases[i];
                if (c == null || c.isDestroyed || c.status != FederalCaseStatusBureau.Active) continue;
                bool sameOrgClass = c.caseType == FederalCaseTypeBureau.OrganizedCrime;
                bool notDirect = !string.Equals(c.targetId, op.targetId, StringComparison.OrdinalIgnoreCase);
                if (!sameOrgClass || !notDirect) continue;
                int spreadRoll = Mathf.Abs(Mix(day + i, BureauWorldState.BootstrapSeed + op.operationId.GetHashCode())) % 100;
                if (spreadRoll < 45)
                {
                    c.targetAwareOfBureauAttention = true;
                    c.lastAwarenessReason = "Network learned of failed federal capture";
                    c.politicalRisk = Mathf.Clamp(c.politicalRisk + 2, 0, 100);
                    AppendCaseActivityByTarget(c.targetId, day, op.operationId, "NetworkAwarenessSpread", "Peer network adapts after failed capture attempt", c.lastAwarenessReason, false);
                }
            }
            BureauWorldState.federalAggressionLevel = Mathf.Clamp(BureauWorldState.federalAggressionLevel + 3, 0, 100);
            BureauWorldState.currentHeat = Mathf.Clamp(BureauWorldState.currentHeat + 2, 0, 100);
            if (report != null)
                report.notes = (report.notes ?? string.Empty) + " Capture failure triggered target/network awareness spread.";
        }
    }

    static void AppendCaseActivityByTarget(
        string targetId,
        int day,
        string operationId,
        string actionType,
        string narrative,
        string reason,
        bool executed)
    {
        if (string.IsNullOrEmpty(targetId)) return;
        string normalizedTarget = targetId;
        string byCaseId = null;
        if (targetId.StartsWith("federal:", StringComparison.OrdinalIgnoreCase))
            byCaseId = targetId.Substring("federal:".Length);
        else if (targetId.StartsWith("police:", StringComparison.OrdinalIgnoreCase))
            normalizedTarget = targetId;
        for (int i = 0; i < BureauWorldState.FederalCases.Count; i++)
        {
            var c = BureauWorldState.FederalCases[i];
            if (c == null || c.isDestroyed) continue;
            bool matchesTarget = string.Equals(c.targetId, normalizedTarget, StringComparison.OrdinalIgnoreCase);
            bool matchesCase = !string.IsNullOrEmpty(byCaseId) && string.Equals(c.caseId, byCaseId, StringComparison.OrdinalIgnoreCase);
            if (!matchesTarget && !matchesCase) continue;
            if (c.activityLog == null) c.activityLog = new List<FederalCaseActivityRecord>();
            c.activityLog.Add(new FederalCaseActivityRecord
            {
                activityId = "fcase_act_" + Guid.NewGuid().ToString("N").Substring(0, 10),
                dayIndex = day,
                operationId = operationId ?? string.Empty,
                actionType = actionType ?? string.Empty,
                narrative = narrative ?? string.Empty,
                reason = reason ?? string.Empty,
                wasExecuted = executed,
                createdAtTicks = DateTime.UtcNow.Ticks
            });
            while (c.activityLog.Count > 120) c.activityLog.RemoveAt(0);
            return;
        }
    }

    static void UpdateHeatExposurePolitics(
        int day, int interest, FederalDailyStrategy strategy, int expBefore, int heatBefore, int polBefore, FederalDailyReport rep)
    {
        int dHeat = 0, dEx = 0, dPol = 0;
        if (strategy == FederalDailyStrategy.LayLow)
        {
            dHeat = -2; dEx = -3;
        }
        if (BureauWorldState.federalRuntimeInterest01 >= 75) { dPol += 1; dHeat += 1; }
        if (BureauWorldState.federalRuntimeInterest01 >= 90) dHeat += 1;
        if (BureauWorldState.politicalPressure >= 70) dEx -= 1;
        BureauWorldState.currentHeat = Mathf.Clamp(BureauWorldState.currentHeat + dHeat, 0, 100);
        BureauWorldState.publicExposure = Mathf.Clamp(BureauWorldState.publicExposure + dEx, 0, 100);
        BureauWorldState.politicalPressure = Mathf.Clamp(BureauWorldState.politicalPressure + dPol, 0, 100);
        if (rep != null)
        {
            rep.exposureChange = BureauWorldState.publicExposure - expBefore;
            rep.politicalPressureChange = BureauWorldState.politicalPressure - polBefore;
        }
        _ = day; _ = interest; _ = heatBefore; // available for future tuning
    }

    static string PickRequestingAgent()
    {
        for (int i = 0; i < BureauWorldState.Roster.Count; i++)
        {
            if (BureauWorldState.Roster[i] == null) continue;
            if (BureauWorldState.Roster[i].rank >= FederalBureauRank.SupervisingSpecialAgent)
                return BureauWorldState.Roster[i].agentId;
        }
        return "fbu_dir_01";
    }

    public static void Log(string line)
    {
        BureauWorldState.federalRuntimeLog.Add(line);
        if (BureauWorldState.federalRuntimeLog.Count > 48) BureauWorldState.federalRuntimeLog.RemoveAt(0);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log(line);
#endif
    }
}
