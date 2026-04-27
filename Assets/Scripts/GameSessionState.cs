using System;
using System.Collections.Generic;
using System.Globalization;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Government;
using FamilyBusiness.CityGen.Government.Windows;
using UnityEngine;

/// <summary>
/// Persistent state across planning and execution. Expand as systems grow.
/// </summary>
public static class GameSessionState
{
    public enum OrganizationStage
    {
        Gang = 0,
        Crew = 1,
        CrimeFamily = 2
    }

    public enum PrisonLegalPhase
    {
        BeforeTrial = 0,
        AfterTrial = 1
    }

    public enum AgencyId
    {
        Police = 0,
        FederalBureau = 1,
        TaxAuthority = 2
    }

    public enum PoliceFacilityType
    {
        HQ = 0,
        Precinct = 1
    }

    [Serializable]
    public class PoliceFacilityRecord
    {
        public PoliceFacilityType Type;
        public int FacilityNumber; // e.g., Precinct 7. For HQ keep 0.
        public string DisplayName;

        /// <summary>FOG: what the player knows about this station (0..5).</summary>
        public int DiscoveryLevel;

        /// <summary>FOG: how freely the player can approach characters here (0..5).</summary>
        public int AccessLevel;

        /// <summary>Last public mention (newspaper / gossip) that revealed or advanced it.</summary>
        public string LastPublicMention;
    }

    /// <summary>How clearly the player can infer hidden state via moles / contacts / intel.</summary>
    public enum IntelClarity
    {
        None = 0,
        Rumor = 1,
        Confirmed = 2,
        Detailed = 3
    }

    public enum FederalBureauEngagement
    {
        Dormant = 0,
        Watching = 1,
        Active = 2
    }

    /// <summary>Starting clean/account cash for a new crew. User requested: start with 0.</summary>
    public const int StartingCrewCash = 0;

    /// <summary>When true, <see cref="RebuildProceduralCityData"/> uses the sandbox block grid (default 3×4 blocks, 8 lots each).</summary>
    public static bool SingleBlockSandboxEnabled = true;

    /// <summary>
    /// Sandbox macro blocks the player has “seen” via travel (Scout / Surveillance / Collect path from home).
    /// Clears macro fog independently of Chebyshev distance from <see cref="MicroBlockWorldState.CrewHomeBlockId"/>.
    /// </summary>
    public static readonly HashSet<int> SandboxRevealedBlockIds = new HashSet<int>();

    /// <summary>Deterministic seed for procedural Ops city map. New value each <see cref="ResetForNewGame"/>.</summary>
    public static int CityMapSeed;

    /// <summary>Runtime city from <see cref="CityGenerator"/>; kept in sync with <see cref="CityMapSeed"/>.</summary>
    public static CityData ActiveCityData;

    /// <summary>Increments each time <see cref="RebuildProceduralCityData"/> completes so UI can detect regenerated cities at the same seed.</summary>
    public static int ActiveCityDataRevision { get; private set; }

    /// <summary>Clears generated city so the next <see cref="EnsureActiveCityData"/> rebuilds from <see cref="CityMapSeed"/>.</summary>
    public static void InvalidateActiveCityData()
    {
        ActiveCityData = null;
        GovernmentRuntimeCitySource.ActiveCity = null;
    }

    public static void EnsureActiveCityData()
    {
        if (ActiveCityData != null && CityMapSeed != 0 && ActiveCityData.Seed != CityMapSeed)
        {
            InvalidateActiveCityData();
            RebuildProceduralCityData();
            return;
        }

        if (ActiveCityData != null)
            return;

        RebuildProceduralCityData();
    }

    public static void RebuildProceduralCityData()
    {
        if (CityMapSeed == 0)
            CityMapSeed = UnityEngine.Random.Range(1, int.MaxValue);
        CityGenerationConfig cfg = ScriptableObject.CreateInstance<CityGenerationConfig>();
        cfg.singleBlockSandboxMap = SingleBlockSandboxEnabled;
        if (SingleBlockSandboxEnabled)
        {
            cfg.placeAnchorInstitutions = false;
            // Explicit 3×4 macro grid = 12 blocks (Ops big map); avoids any drift from edited defaults.
            cfg.singleBlockSandboxBlocksAlongX = 3;
            cfg.singleBlockSandboxBlocksAlongY = 4;
        }
        var gen = new CityGenerator();
        ActiveCityData = gen.Generate(cfg, CitySeed.FromExplicit(CityMapSeed));
        GovernmentDataExtractor.Refresh(ActiveCityData);
        GovernmentRuntimeCitySource.ActiveCity = ActiveCityData;
        ActiveCityDataRevision++;

        MicroBlockAnchorResolver.EnsureCrewHomeBlockAnchored(ActiveCityData);
        MicroBlockSpotLotBinder.BindSpotsToHomeBlockLots(ActiveCityData);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        LogCityGenPipelineSanity(ActiveCityData, CityMapSeed);
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    static void LogCityGenPipelineSanity(CityData city, int seed)
    {
        if (city == null)
        {
            Debug.LogWarning("[CityGen] ActiveCityData is null after RebuildProceduralCityData.");
            return;
        }

        if (city.Seed != seed)
            Debug.LogWarning("[CityGen] Seed mismatch: CityData.Seed=" + city.Seed + " CityMapSeed=" + seed);

        if (city.Lots.Count == 0)
            Debug.LogWarning("[CityGen] No lots — Ops CityGen map will show the empty fallback.");

        if (city.RoadNodes.Count > 0 && city.RoadEdges.Count == 0)
            Debug.LogWarning("[CityGen] Road nodes exist but no edges.");

        int unknownDistrictLots = 0;
        for (int i = 0; i < city.Lots.Count; i++)
        {
            if (city.Lots[i].DistrictKind == DistrictKind.Unknown)
                unknownDistrictLots++;
        }

        if (city.Lots.Count > 0 && unknownDistrictLots * 2 > city.Lots.Count)
        {
            Debug.LogWarning("[CityGen] Many lots still have DistrictKind.Unknown (" + unknownDistrictLots + "/" +
                city.Lots.Count + ").");
        }

        Debug.Log("[CityGen] Session city OK — seed=" + seed + " lots=" + city.Lots.Count + " roadEdges=" +
            city.RoadEdges.Count + " districts=" + city.Districts.Count + " blocks=" + city.Blocks.Count +
            " institutions=" + city.Institutions.Count + " buildings=" + city.Buildings.Count +
            " govData=" + (city.GovernmentData != null));
    }
#endif

    /// <summary>
    /// <para><b>Planning (Gangsters-style):</b> city map = CityGen lots + roads + landmark icons — no moving people/cars.</para>
    /// <para><b>Execution:</b> set this <c>true</c> from the live street map when you spawn agent dots; keep <c>false</c> on the planning shell.</para>
    /// </summary>
    public static bool CityMapAgentOverlayEnabled;

    /// <summary>Whether colored dots / vehicles should draw on the city map (never during Management / planning UI).</summary>
    public static bool ShouldRenderCityMapAgents()
    {
        if (!CityMapAgentOverlayEnabled)
            return false;
        if (GameModeManager.Instance == null)
            return false;
        return !GameModeManager.Instance.IsManagementMode();
    }

    /// <summary>Game day counter from 1 = July 1, 1924; each planning→execution cycle advances one calendar day.</summary>
    public static int CurrentDay = 1;

    /// <summary>Crew cash on hand (whole dollars). Shown in planning HUD.</summary>
    public static int CrewCash = StartingCrewCash;

    /// <summary>Dirty cash kept as liquid money (\"black money\").</summary>
    public const int StartingBlackCash = 350;

    /// <summary>Black cash kept off the books (liquid, not backed by receipts).</summary>
    public static int BlackCash = StartingBlackCash;

    /// <summary>Player organization progression tier (not a character status).</summary>
    public static OrganizationStage PlayerOrganizationStage = OrganizationStage.Gang;

    /// <summary>Hidden: how dangerous the organization appears to state agencies (0..100+). Not directly shown to player.</summary>
    public static int PlayerThreatScore;

    /// <summary>Hidden: when the federal bureau is engaged with the player.</summary>
    public static FederalBureauEngagement FederalBureauState = FederalBureauEngagement.Dormant;

    /// <summary>Revenue / tax case progression (Capone-style parallel track). Not yet persisted in save JSON.</summary>
    public static readonly TaxAuthorityCaseState TaxAuthorityCase = new TaxAuthorityCaseState();

    /// <summary>Hidden: how strong the player's intel network is (moles/contacts), 0..100. Controls <see cref="IntelClarity"/>.</summary>
    public static int PlayerIntelNetworkRating;

    /// <summary>
    /// Police facilities in the city. Generated per new game.
    /// This is used by FOG: discovery (what you know) vs access (how close you can get).
    /// </summary>
    public static readonly List<PoliceFacilityRecord> PoliceFacilities = new List<PoliceFacilityRecord>();

    /// <summary>Local newspaper snippet shown in the News tab.</summary>
    public static string LastLocalPaperBlurb = string.Empty;

    /// <summary>Day index when <see cref="LastLocalPaperBlurb"/> was last generated.</summary>
    public static int LastLocalPaperBlurbDay = -1;

    public static IntelClarity GetIntelClarity()
    {
        int r = Mathf.Clamp(PlayerIntelNetworkRating, 0, 100);
        if (r >= 70) return IntelClarity.Detailed;
        if (r >= 40) return IntelClarity.Confirmed;
        if (r >= 15) return IntelClarity.Rumor;
        return IntelClarity.None;
    }

    public static void EnsurePoliceFacilitiesInitialized(bool forceRebuild = false)
    {
        if (!forceRebuild && PoliceFacilities.Count > 0)
            return;

        PoliceFacilities.Clear();

        // Dynamic precinct count: HQ + (2..4) precincts depending on players.
        int precinctCount;
        if (TotalPlayers <= 4) precinctCount = 2;
        else if (TotalPlayers <= 6) precinctCount = 3;
        else precinctCount = 4;

        // HQ is always known as a public institution (but access can still be 0).
        PoliceFacilities.Add(new PoliceFacilityRecord
        {
            Type = PoliceFacilityType.HQ,
            FacilityNumber = 0,
            DisplayName = "Police HQ",
            DiscoveryLevel = 2,
            AccessLevel = 0,
            LastPublicMention = string.Empty
        });

        // Deterministic numbers per run seed (avoid "meaningful" numbering; user requested random IDs).
        var rng = new System.Random(CityMapSeed ^ unchecked((int)0x5EEDB01));
        var used = new HashSet<int>();
        for (int i = 0; i < precinctCount; i++)
        {
            int num = rng.Next(3, 98);
            int guard = 0;
            while (used.Contains(num) && guard++ < 256)
                num = rng.Next(3, 98);
            used.Add(num);

            PoliceFacilities.Add(new PoliceFacilityRecord
            {
                Type = PoliceFacilityType.Precinct,
                FacilityNumber = num,
                DisplayName = "Precinct " + num,
                DiscoveryLevel = 0,
                AccessLevel = 0,
                LastPublicMention = string.Empty
            });
        }
    }

    public static void GenerateLocalPaperBlurbIfNeeded(bool force = false)
    {
        if (!force && LastLocalPaperBlurbDay == CurrentDay && !string.IsNullOrWhiteSpace(LastLocalPaperBlurb))
            return;

        EnsurePoliceFacilitiesInitialized();

        // Pick an undiscovered precinct if possible; otherwise pick any precinct.
        PoliceFacilityRecord pick = null;
        for (int i = 0; i < PoliceFacilities.Count; i++)
        {
            var f = PoliceFacilities[i];
            if (f != null && f.Type == PoliceFacilityType.Precinct && f.DiscoveryLevel <= 0)
            {
                pick = f;
                break;
            }
        }
        if (pick == null)
        {
            for (int i = 0; i < PoliceFacilities.Count; i++)
            {
                var f = PoliceFacilities[i];
                if (f != null && f.Type == PoliceFacilityType.Precinct)
                {
                    pick = f;
                    break;
                }
            }
        }

        if (pick == null)
        {
            LastLocalPaperBlurb = "Local paper: <i>quiet week in the precincts.</i>";
            LastLocalPaperBlurbDay = CurrentDay;
            return;
        }

        // Flavor: public mention that reveals the station to the player.
        // (We keep it 1920s-appropriate and low detail.)
        var rng = new System.Random((CityMapSeed * 31) ^ (CurrentDay * 997) ^ unchecked((int)0xB10773));
        string[] names =
        {
            "Capt. O'Rourke", "Lt. Harlan", "Sgt. Marconi", "Det. Wexler", "Officer Kowalski", "Inspector Graves"
        };
        string who = names[rng.Next(0, names.Length)];
        int n = pick.FacilityNumber;

        string[] templates =
        {
            "Local paper: <b>Precinct {0}</b> welcomes a new shift commander, {1}.",
            "Local paper: <b>Precinct {0}</b> announces a neighborhood foot patrol push under {1}.",
            "Local paper: <b>Precinct {0}</b> rotates desk sergeants; {1} takes the front office.",
            "Local paper: <b>Precinct {0}</b> reports a commendation ceremony led by {1}."
        };
        string blurb = string.Format(CultureInfo.InvariantCulture, templates[rng.Next(0, templates.Length)], n, who);

        // Reveal: bump discovery so the player can "see it on the map" / access its page.
        pick.DiscoveryLevel = Mathf.Clamp(pick.DiscoveryLevel < 1 ? 1 : pick.DiscoveryLevel, 0, 5);
        pick.LastPublicMention = blurb;

        LastLocalPaperBlurb = blurb;
        LastLocalPaperBlurbDay = CurrentDay;
    }

    /// <summary>Player-facing hint (no numbers) derived from hidden metrics + <see cref="IntelClarity"/>.</summary>
    public static string GetAgencyIntelHint(AgencyId agency)
    {
        IntelClarity c = GetIntelClarity();
        int threat = Mathf.Max(0, PlayerThreatScore);

        switch (agency)
        {
            case AgencyId.Police:
                if (c == IntelClarity.None)
                    return "Police posture: unclear.";
                if (c == IntelClarity.Rumor)
                    return threat >= 60 ? "Street talk: cops are tightening the net." : "Street talk: patrols feel routine.";
                if (c == IntelClarity.Confirmed)
                    return threat >= 80 ? "Confirmed: active case-building and pressure spikes." :
                           threat >= 50 ? "Confirmed: increased attention and targeted checks." :
                           "Confirmed: low-profile monitoring only.";
                return threat >= 80 ? "Detailed: detectives compiling warrants; raids likely soon." :
                       threat >= 50 ? "Detailed: watchlists updated; patrol patterns shifted." :
                       "Detailed: minimal attention; paperwork only.";

            case AgencyId.FederalBureau:
                if (FederalBureauState == FederalBureauEngagement.Dormant)
                {
                    if (c == IntelClarity.Detailed)
                        return "Federal Bureau: dormant (no active file).";
                    if (c == IntelClarity.Confirmed)
                        return "Federal Bureau: no active involvement detected.";
                    return "Federal Bureau: quiet.";
                }

                if (FederalBureauState == FederalBureauEngagement.Watching)
                {
                    if (c == IntelClarity.None)
                        return "Federal Bureau: quiet.";
                    if (c == IntelClarity.Rumor)
                        return "Rumor: a federal unit is sniffing around.";
                    if (c == IntelClarity.Confirmed)
                        return "Confirmed: federal watchers flagged your name.";
                    return "Detailed: surveillance interest rising; avoid loud moves.";
                }

                // Active
                if (c == IntelClarity.None)
                    return "Federal Bureau: quiet.";
                if (c == IntelClarity.Rumor)
                    return "Rumor: federal pressure is real — someone above the precinct.";
                if (c == IntelClarity.Confirmed)
                    return "Confirmed: a federal case is active. Expect sophisticated moves.";
                return "Detailed: federal tasking active — wiretaps, assets, and deep investigation.";

            case AgencyId.TaxAuthority:
                if (c == IntelClarity.None)
                    return "Tax authority: unclear.";
                if (c == IntelClarity.Rumor)
                    return threat >= 65 ? "Rumor: tax men are asking questions." : "Rumor: filings look normal.";
                if (c == IntelClarity.Confirmed)
                    return threat >= 75 ? "Confirmed: audit risk rising; paper trail is hot." :
                           threat >= 45 ? "Confirmed: mild scrutiny — keep books clean." :
                           "Confirmed: no unusual scrutiny.";
                return threat >= 75 ? "Detailed: audit preparation underway; expect seizures if sloppy." :
                       threat >= 45 ? "Detailed: compliance checks increasing." :
                       "Detailed: low interest — routine only.";

            default:
                return string.Empty;
        }
    }

    public static string FormatCrewCashDisplay()
    {
        return "$" + CrewCash.ToString("N0", CultureInfo.InvariantCulture);
    }

    public static string FormatBlackCashDisplay()
    {
        return "$" + BlackCash.ToString("N0", CultureInfo.InvariantCulture);
    }

    public static bool ScoutMissionOrdered;

    /// <summary>
    /// Operations ordered for execution this turn. Filled from Ops tab.
    /// </summary>
    public static readonly List<OperationType> OrderedOperations = new List<OperationType>();

    /// <summary>Roster index (<see cref="PersonnelRegistry.Members"/>) leading each queued operation; cleared with <see cref="OrderedOperations"/>.</summary>
    public static readonly Dictionary<OperationType, int> OperationAssigneeMemberIndex = new Dictionary<OperationType, int>();

    /// <summary><see cref="FamilyBusiness.CityGen.Data.BlockData.Id"/> for the op target (from selected spot’s lot); used with <see cref="OperationTimingSystem"/>.</summary>
    public static readonly Dictionary<OperationType, int> OperationTargetBlockId = new Dictionary<OperationType, int>();

    /// <summary>Optional stable id of the façade / spot the op was queued against (Ops selection).</summary>
    public static readonly Dictionary<OperationType, string> OperationTargetSpotStableId = new Dictionary<OperationType, string>();

    /// <summary>People counted for execution-time scaling (lead + available helpers, capped when confirming).</summary>
    public static readonly Dictionary<OperationType, int> OperationSquadSize = new Dictionary<OperationType, int>();

    /// <summary>Roster indices on this mission besides the lead (pooled under lead’s command).</summary>
    public static readonly Dictionary<OperationType, List<int>> OperationExtraMemberIndices = new Dictionary<OperationType, List<int>>();

    /// <summary>Per-mission vehicle for travel speed (distinct from personal “take the car today”).</summary>
    public static readonly Dictionary<OperationType, bool> OperationMissionUsesCrewVehicle = new Dictionary<OperationType, bool>();

    /// <summary>Roster index or −1 = none. Must be in squad list when set.</summary>
    public static readonly Dictionary<OperationType, int> OperationLookoutMemberIndex = new Dictionary<OperationType, int>();

    /// <summary>Driver stays in vehicle; −1 = none (everyone dismounts).</summary>
    public static readonly Dictionary<OperationType, int> OperationDriverMemberIndex = new Dictionary<OperationType, int>();

    /// <summary>Travel+execution wall hours last committed for this op (for undo when removed from queue).</summary>
    public static readonly Dictionary<OperationType, float> OperationPlannedWallHours = new Dictionary<OperationType, float>();

    /// <summary>When true and no coop car owner is set, travel still uses vehicle speed (single-player default).</summary>
    public static bool CrewOpsHasVehicle = true;

    /// <summary>Vehicle access for travel-time estimates: coop car owner implies a crew car; else <see cref="CrewOpsHasVehicle"/>.</summary>
    public static bool ResolveCrewOpsVehicle()
    {
        if (CooperativeCarOwnerChoice >= 0)
            return true;
        return CrewOpsHasVehicle;
    }

    public static void ClearOperationAssignees()
    {
        OperationAssigneeMemberIndex.Clear();
        OperationTargetBlockId.Clear();
        OperationTargetSpotStableId.Clear();
        OperationSquadSize.Clear();
        OperationExtraMemberIndices.Clear();
        OperationMissionUsesCrewVehicle.Clear();
        OperationLookoutMemberIndex.Clear();
        OperationDriverMemberIndex.Clear();
        OperationPlannedWallHours.Clear();
    }

    public static void RemoveOperationMissionMeta(OperationType op)
    {
        OperationTargetBlockId.Remove(op);
        OperationTargetSpotStableId.Remove(op);
        OperationSquadSize.Remove(op);
        OperationExtraMemberIndices.Remove(op);
        OperationMissionUsesCrewVehicle.Remove(op);
        OperationLookoutMemberIndex.Remove(op);
        OperationDriverMemberIndex.Remove(op);
        OperationPlannedWallHours.Remove(op);
    }

    /// <summary>Lead first, then extras (unique).</summary>
    public static List<int> GetSquadRosterIndicesForOperation(OperationType op)
    {
        var list = new List<int>(8);
        if (!OperationAssigneeMemberIndex.TryGetValue(op, out int lead) || lead < 0)
            return list;
        list.Add(lead);
        if (OperationExtraMemberIndices.TryGetValue(op, out var extras) && extras != null)
        {
            for (int i = 0; i < extras.Count; i++)
            {
                int x = extras[i];
                if (x < 0 || x == lead)
                    continue;
                bool dup = false;
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j] == x)
                    {
                        dup = true;
                        break;
                    }
                }

                if (!dup)
                    list.Add(x);
            }
        }

        return list;
    }

    public static void SwapQueuedOperationsOrder(int indexA, int indexB)
    {
        if (indexA == indexB)
            return;
        if (indexA < 0 || indexB < 0 || indexA >= OrderedOperations.Count || indexB >= OrderedOperations.Count)
            return;
        OperationType t = OrderedOperations[indexA];
        OrderedOperations[indexA] = OrderedOperations[indexB];
        OrderedOperations[indexB] = t;
    }

    /// <summary>Undo wall-hour load for a queued op before removing or re-confirming.</summary>
    public static void RollbackOperationPlanningHoursIfAny(OperationType op)
    {
        if (!OperationPlannedWallHours.TryGetValue(op, out float h) || h <= 0f)
            return;
        List<int> squad = GetSquadRosterIndicesForOperation(op);
        OpsPlanningRhythmState.SubtractMissionWallHoursForMembers(squad, h);
    }

    public static void SetOperationMissionMeta(OperationType op, int targetBlockId, string spotStableId, int squadSize)
    {
        if (targetBlockId >= 0)
            OperationTargetBlockId[op] = targetBlockId;
        else
            OperationTargetBlockId.Remove(op);

        if (!string.IsNullOrEmpty(spotStableId))
            OperationTargetSpotStableId[op] = spotStableId;
        else
            OperationTargetSpotStableId.Remove(op);

        OperationSquadSize[op] = Mathf.Clamp(squadSize, 1, 8);
    }

    public static int GetOperationTargetBlockId(OperationType op)
    {
        if (OperationTargetBlockId.TryGetValue(op, out int b) && b >= 0)
            return b;
        return MicroBlockWorldState.CrewHomeBlockId;
    }

    public static int GetOperationSquadSizeForDisplay(OperationType op, int leadMemberIndex)
    {
        if (OperationSquadSize.TryGetValue(op, out int s) && s >= 1)
            return s;
        if (leadMemberIndex >= 0)
            return OperationTimingSystem.ComputeDeployableSquadSize(leadMemberIndex);
        return 1;
    }

    public static void SetOperationAssignee(OperationType op, int memberIndex)
    {
        OperationAssigneeMemberIndex[op] = memberIndex;
    }

    public static int GetOperationAssignee(OperationType op)
    {
        return OperationAssigneeMemberIndex.TryGetValue(op, out int i) ? i : -1;
    }

    public static void RemoveOperationAssignee(OperationType op)
    {
        OperationAssigneeMemberIndex.Remove(op);
    }

    /// <summary>
    /// How many human/AI players must finish planning before execution starts together.
    /// </summary>
    public static int TotalPlayers = 1;

    /// <summary>
    /// How many players have locked their planning (submitted for this round).
    /// </summary>
    public static int ReadyPlayersCount;

    /// <summary>
    /// Local player has pressed "End Turn" and is waiting for others.
    /// </summary>
    public static bool LocalPlayerPlanningReady;

    /// <summary>
    /// While true, execution UI (day summary) blocks the map.
    /// </summary>
    public static bool IsDaySummaryShowing;

    public static int ExecutionDayDurationSeconds = 10;

    public static int LastDayMissionsCompleted;
    public static int LastDayMissionsFailed;
    public static int LastDaySoldiersReleased;

    /// <summary>How much police attention the family starts with. Can be negative only from the setup interrogation; gameplay deltas should floor at 0.</summary>
    public static int PolicePressure = 35;

    /// <summary>When true, positive police-pressure gains during play are damped (cooperative bar alibi buffer).</summary>
    public static bool CoopPoliceHeatRisesSlowly;

    /// <summary>Last police investigation update (player-facing, no numbers).</summary>
    public static string LastPoliceInvestigationUpdate = string.Empty;

    /// <summary>Boss agreed to inform for detectives after the bar incident thread.</summary>
    public static bool BossIsPoliceInformant;

    /// <summary>Underworld / rival crews believe the boss is a snitch (shown in boss profile status).</summary>
    public static bool BossSnitchKnownToRivalGangs;

    /// <summary>First game day when street rumor locks in; -1 if not applicable.</summary>
    public static int BossSnitchStreetRevealAtDay = -1;

    /// <summary>Days after informant flag before rivals reliably treat the boss as a snitch.</summary>
    public const int BossSnitchStreetRumorDelayDays = 2;

    /// <summary>After underworld learns the boss is a police informant: open war from rival families (narrative / systems hook).</summary>
    public static bool UnderworldWarDeclaredOnPlayerFamily;

    /// <summary>One-time morale/rep penalties when street exposure triggers; persisted so reload does not re-apply.</summary>
    public static bool PoliceInformantStreetFalloutApplied;

    /// <summary>USD posted as bail after certain interrogation paths (deducted from starting black cash).</summary>
    public static int InterrogationBailUsd;

    /// <summary>Police-named bar used in cooperative "where were you" follow-up (session fiction).</summary>
    public static string CoopInventedBarName = string.Empty;

    /// <summary>Cooperative evening alibi: 0 bar, 1 restaurant, 2 park, 3 gym; -1 unset.</summary>
    public static int CoopEveningWhereChoice = -1;

    /// <summary>After a non-bar alibi: 0 firm deny (may continue to knowledge trap), 1 admit bar, 2 "what is this place?"; -1 unset.</summary>
    public static int CoopBarContradictionChoice = -1;

    /// <summary>Assigned after co-op bar-trap "demand lawyer" path (shown on Legal tab).</summary>
    public static string InterrogationPublicDefenderName = string.Empty;

    /// <summary>Bar incident follow-up: 0 truth, 1 fabricate; -1 unset / N/A.</summary>
    public static int CoopBarWhatHappenedChoice = -1;

    /// <summary>Detective snitch offer: 0 accept, 1 refuse; -1 N/A.</summary>
    public static int CoopSnitchChoice = -1;

    /// <summary>Hostile family slot 0..AccentColorCount-1 (matches accent); -1 unset.</summary>
    public static int RivalCrimeFamilyAccentIndex = -1;

    /// <summary>Apply a police pressure change during planning/execution (cannot push below zero).</summary>
    public static void ApplyGameplayPolicePressureDelta(int delta)
    {
        if (delta > 0 && PolicePressure < 0 && CoopPoliceHeatRisesSlowly)
            delta = Mathf.Max(1, Mathf.RoundToInt(delta * 0.65f));
        PolicePressure = Mathf.Max(0, PolicePressure + delta);
    }

    /// <summary>True when the boss is personally known to police because of the stabbing case.</summary>
    public static bool BossKnownToPolice;

    /// <summary>True when the boss starts the run from prison (short term).</summary>
    public static bool BossStartsInPrison;

    /// <summary>Name of detained character at start (boss or one associate).</summary>
    public static string InitialDetainedCharacterName = string.Empty;

    /// <summary>One-line narrative summary of interrogation consequences.</summary>
    public static string InterrogationCaseNote = string.Empty;

    /// <summary>Interrogation path custody tension; feeds daily street-stop probability with <see cref="PolicePressure"/>.</summary>
    public static int InterrogationCustodyRisk;

    /// <summary>True when player disclosed being with three associates.</summary>
    public static bool AssociatesDisclosedInInterview = true;

    /// <summary>Cooperative route reason choice. -1 when not selected.</summary>
    public static int CooperativeReasonChoice = -1;

    /// <summary>Co-op "try our luck": rolled practice XP copied from profile at game start.</summary>
    public static int CoopTryLuckGrantedXp;

    /// <summary>Co-op "try our luck": <see cref="CoreTrait"/> ordinal 0..5, or -1 if N/A.</summary>
    public static int CoopTryLuckTraitIndex = -1;

    /// <summary>Co-op "try our luck" partners 0..2: rolled practice XP (mirrors profile).</summary>
    public static readonly int[] CoopTryLuckPartnerGrantedXp = new int[3];

    /// <summary>Co-op "try our luck" partners: <see cref="CoreTrait"/> ordinal 0..5, or -1 if N/A.</summary>
    public static readonly int[] CoopTryLuckPartnerTraitIndex = new int[] { -1, -1, -1 };

    /// <summary>Cooperative route car owner choice. -1 when not selected (0=me, 1..3=friends).</summary>
    public static int CooperativeCarOwnerChoice = -1;

    /// <summary>Derived display name for the cooperative route car owner.</summary>
    public static string CooperativeCarOwnerName = string.Empty;

    /// <summary>Vehicle color from cooperative interrogation (0..AccentColorCount-1). -1 if not chosen.</summary>
    public static int CooperativeVehicleColorIndex = -1;

    /// <summary>Current legal phase for the boss while in prison (derived — do not toggle by UI).</summary>
    public static PrisonLegalPhase BossPrisonPhase = PrisonLegalPhase.BeforeTrial;

    /// <summary>True after a trial verdict while the boss remains incarcerated (post-sentencing prison). Until then, only pre-trial detention applies.</summary>
    public static bool BossCustodyTrialCompleted;

    /// <summary>
    /// Resolved custody for the <i>player boss</i> (profile + session + roster slot 0). Avoids showing "Available"
    /// when slot 0 is still template crew or when session says the boss is in pre/post-trial custody.
    /// </summary>
    public static CharacterStatus GetPlayerBossResolvedCustodyStatus()
    {
        if (PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return CharacterStatus.Unknown;

        CrewMember slot0 = PersonnelRegistry.Members[0];
        CharacterStatus roster = slot0.GetResolvedStatus();

        string dn = "Boss";
        if (PlayerRunState.HasCharacter && PlayerRunState.Character != null &&
            !string.IsNullOrWhiteSpace(PlayerRunState.Character.DisplayName))
            dn = PlayerRunState.Character.DisplayName.Trim();
        string bossRosterName = dn + " (Boss)";
        string initial = (InitialDetainedCharacterName ?? string.Empty).Trim();

        bool rosterSlotLooksLikePlayerBoss =
            string.Equals(slot0.Role, "Boss", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(slot0.Name, bossRosterName, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(slot0.Name) &&
             slot0.Name.StartsWith(dn, StringComparison.OrdinalIgnoreCase) &&
             slot0.Name.IndexOf("(Boss)", StringComparison.OrdinalIgnoreCase) >= 0);

        // Session flags: exact roster tag, plain display name, or BossStartsInPrison (saves / routes where the string drifted).
        bool initialRefersToBoss =
            !string.IsNullOrEmpty(initial) &&
            (string.Equals(initial, bossRosterName, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(initial, dn, StringComparison.OrdinalIgnoreCase));

        // Session custody must win even if roster slot 0 is temporarily not synced to the boss
        // (e.g. template crew, load order, or UI opened before SyncBossSlotFromProfileAndCustody).
        bool bossInCustodyBySession = initialRefersToBoss || BossStartsInPrison;

        if (bossInCustodyBySession)
            return BossCustodyTrialCompleted ? CharacterStatus.Imprisoned : CharacterStatus.Detained;

        if (rosterSlotLooksLikePlayerBoss)
            return roster;

        return roster;
    }

    public static bool AllPlayersSubmittedPlanning()
    {
        return ReadyPlayersCount >= TotalPlayers && TotalPlayers > 0;
    }

    public static bool IsWaitingForOtherPlayers()
    {
        return LocalPlayerPlanningReady && !AllPlayersSubmittedPlanning();
    }

    /// <summary>
    /// While waiting on others, you stay in planning but cannot "jump ahead" to armament/execution preview.
    /// Hook future equip/arm UI to this.
    /// </summary>
    public static bool CanAccessArmamentAndExecutionPrep()
    {
        return !LocalPlayerPlanningReady;
    }

    /// <summary>Call from planning tick (or after loading) so snitch street status can flip on schedule.</summary>
    public static void RefreshBossSnitchStreetRumor()
    {
        if (!BossIsPoliceInformant)
        {
            BossSnitchKnownToRivalGangs = false;
            BossSnitchStreetRevealAtDay = -1;
            return;
        }

        if (!BossSnitchKnownToRivalGangs && BossSnitchStreetRevealAtDay <= 0)
            BossSnitchStreetRevealAtDay = Mathf.Max(1, CurrentDay) + BossSnitchStreetRumorDelayDays;

        if (BossSnitchStreetRevealAtDay >= 0 && CurrentDay >= BossSnitchStreetRevealAtDay)
            BossSnitchKnownToRivalGangs = true;

        ApplyPoliceInformantStreetFalloutIfNeeded();
    }

    /// <summary>Narrative hooks (missions, intel leaks) can force immediate underworld knowledge.</summary>
    public static void ExposeBossSnitchToRivalGangsImmediately()
    {
        if (!BossIsPoliceInformant)
            return;
        BossSnitchKnownToRivalGangs = true;
        BossSnitchStreetRevealAtDay = -1;
        ApplyPoliceInformantStreetFalloutIfNeeded();
    }

    /// <summary>When rival crews learn the boss is a police informant: war flag, crew morale crash, reputation hit (once).</summary>
    public static void ApplyPoliceInformantStreetFalloutIfNeeded()
    {
        if (!BossSnitchKnownToRivalGangs || PoliceInformantStreetFalloutApplied)
            return;

        PoliceInformantStreetFalloutApplied = true;
        UnderworldWarDeclaredOnPlayerFamily = true;

        if (PlayerRunState.Character != null)
        {
            PlayerRunState.Character.PublicReputation =
                Mathf.Clamp(PlayerRunState.Character.PublicReputation - 45, -100, 100);
        }

        if (PersonnelRegistry.Members != null)
        {
            for (int i = 0; i < PersonnelRegistry.Members.Count; i++)
                DemoteCrewMemberSatisfactionOneStep(PersonnelRegistry.Members[i]);
        }

        const string fallout =
            "Underworld reaction: rival crime families declared war on your organization; crew morale collapsed; your street reputation cratered.";
        if (string.IsNullOrEmpty(InterrogationCaseNote))
            InterrogationCaseNote = fallout;
        else if (!InterrogationCaseNote.Contains("Underworld reaction:"))
            InterrogationCaseNote = InterrogationCaseNote.Trim() + " " + fallout;
    }

    private static void DemoteCrewMemberSatisfactionOneStep(CrewMember member)
    {
        if (member == null)
            return;
        switch (member.Satisfaction)
        {
            case CrewSatisfactionLevel.VerySatisfied:
                member.Satisfaction = CrewSatisfactionLevel.Satisfied;
                break;
            case CrewSatisfactionLevel.Satisfied:
                member.Satisfaction = CrewSatisfactionLevel.Neutral;
                break;
            case CrewSatisfactionLevel.Neutral:
                member.Satisfaction = CrewSatisfactionLevel.Unsatisfied;
                break;
            case CrewSatisfactionLevel.Unsatisfied:
            case CrewSatisfactionLevel.VeryUnsatisfied:
                member.Satisfaction = CrewSatisfactionLevel.VeryUnsatisfied;
                break;
        }
    }

    /// <summary>
    /// Derives <see cref="BossPrisonPhase"/> and boss custody status from <see cref="BossCustodyTrialCompleted"/>.
    /// Pre-trial detention and post-trial prison are mutually exclusive lanes — never both for the same character.
    /// </summary>
    public static void ApplyBossCustodyLegalPhaseFromTrialFlag()
    {
        if (PersonnelRegistry.Members != null && PersonnelRegistry.Members.Count > 0)
        {
            CrewMember boss = PersonnelRegistry.Members[0];
            CharacterStatus s = boss.GetResolvedStatus();
            if (CharacterStatusUtility.IsIncarcerated(s) && s == CharacterStatus.Imprisoned)
                BossCustodyTrialCompleted = true;
        }

        BossPrisonPhase = BossCustodyTrialCompleted ? PrisonLegalPhase.AfterTrial : PrisonLegalPhase.BeforeTrial;

        if (PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return;
        CrewMember b = PersonnelRegistry.Members[0];
        CharacterStatus st = b.GetResolvedStatus();
        if (!CharacterStatusUtility.IsIncarcerated(st))
            return;

        if (BossCustodyTrialCompleted && st == CharacterStatus.Detained)
            b.SetStatus(CharacterStatus.Imprisoned);
    }

    /// <summary>Call from court / trial resolution when the boss is sentenced while still in custody.</summary>
    public static void AdvanceBossCustodyAfterTrialVerdict()
    {
        if (PersonnelRegistry.Members == null || PersonnelRegistry.Members.Count == 0)
            return;
        CrewMember boss = PersonnelRegistry.Members[0];
        if (!CharacterStatusUtility.IsIncarcerated(boss.GetResolvedStatus()))
            return;

        BossCustodyTrialCompleted = true;
        ApplyBossCustodyLegalPhaseFromTrialFlag();
    }

    public static string FormatPolicePressureLabel()
    {
        if (PolicePressure < 0) return "Under the radar";
        if (PolicePressure >= 80) return "Maximum scrutiny";
        if (PolicePressure >= 60) return "High watch";
        if (PolicePressure >= 40) return "Noticeable";
        return "Low profile";
    }

    /// <summary>HUD numeric display: negatives from setup interrogation show as 0 for the bar scale.</summary>
    public static int PolicePressureDisplayValue()
    {
        return Mathf.Max(0, PolicePressure);
    }

    /// <summary>0–100 HUD scale derived from the same inputs as the daily street-stop probability.</summary>
    public static int StreetStopRiskDisplayValue()
    {
        return Mathf.RoundToInt(Mathf.Clamp01(PoliceStreetPressureDaily.ComputeStreetStopProbability01()) * 100f);
    }

    public static string FormatStreetStopRiskLabel()
    {
        int v = StreetStopRiskDisplayValue();
        if (v >= 72) return "Severe";
        if (v >= 48) return "Elevated";
        if (v >= 24) return "Moderate";
        return "Low";
    }

    public static void ResetPlanningSubmissionState()
    {
        LocalPlayerPlanningReady = false;
        ReadyPlayersCount = 0;
    }

    public static void FillDaySummaryStub()
    {
        LastDayMissionsCompleted = 2;
        LastDayMissionsFailed = 0;
        LastDaySoldiersReleased = 1;
    }

    /// <summary>
    /// Full session reset for "New Game". Scene reload should follow so scene singletons re-init.
    /// </summary>
    public static void SyncScoutToOrderedOps()
    {
        if (ScoutMissionOrdered && !OrderedOperations.Contains(OperationType.Scout))
            OrderedOperations.Add(OperationType.Scout);
        else if (!ScoutMissionOrdered)
            OrderedOperations.RemoveAll(o => o == OperationType.Scout);
    }

    public static void RevealSandboxBlock(int blockId)
    {
        if (blockId < 0 || !SingleBlockSandboxEnabled)
            return;
        SandboxRevealedBlockIds.Add(blockId);
    }

    public static void ClearSandboxRevealedBlocks()
    {
        SandboxRevealedBlockIds.Clear();
    }

    public static bool IsSandboxBlockRevealed(int blockId)
    {
        return blockId >= 0 && SandboxRevealedBlockIds.Contains(blockId);
    }

    public static int[] GetSandboxRevealedBlockIdsSnapshot()
    {
        if (SandboxRevealedBlockIds.Count == 0)
            return null;
        var arr = new int[SandboxRevealedBlockIds.Count];
        int i = 0;
        foreach (int id in SandboxRevealedBlockIds)
            arr[i++] = id;
        return arr;
    }

    public static void ResetForNewGame()
    {
        CityMapSeed = UnityEngine.Random.Range(1, int.MaxValue);
        CityMapAgentOverlayEnabled = false;
        CurrentDay = 1;
        CrewCash = StartingCrewCash;
        BlackCash = StartingBlackCash;
        PlayerOrganizationStage = OrganizationStage.Gang;
        PlayerThreatScore = 0;
        FederalBureauState = FederalBureauEngagement.Dormant;
        TaxAuthorityCase.Reset();
        PlayerIntelNetworkRating = 0;
        ScoutMissionOrdered = false;
        OrderedOperations.Clear();
        ClearOperationAssignees();
        ClearSandboxRevealedBlocks();
        OpsPlanningRhythmState.ResetForNewGame();
        CrewOpsHasVehicle = true;
        ReadyPlayersCount = 0;
        LocalPlayerPlanningReady = false;
        IsDaySummaryShowing = false;
        LastDayMissionsCompleted = 0;
        LastDayMissionsFailed = 0;
        LastDaySoldiersReleased = 0;
        PolicePressure = 35;
        CoopPoliceHeatRisesSlowly = false;
        LastPoliceInvestigationUpdate = string.Empty;
        BossIsPoliceInformant = false;
        BossSnitchKnownToRivalGangs = false;
        BossSnitchStreetRevealAtDay = -1;
        UnderworldWarDeclaredOnPlayerFamily = false;
        PoliceInformantStreetFalloutApplied = false;
        InterrogationBailUsd = 0;
        BossKnownToPolice = false;
        BossStartsInPrison = false;
        InitialDetainedCharacterName = string.Empty;
        InterrogationCaseNote = string.Empty;
        InterrogationCustodyRisk = 0;
        AssociatesDisclosedInInterview = true;
        BossPrisonPhase = PrisonLegalPhase.BeforeTrial;
        BossCustodyTrialCompleted = false;

        CooperativeReasonChoice = -1;
        CoopTryLuckGrantedXp = 0;
        CoopTryLuckTraitIndex = -1;
        for (int i = 0; i < 3; i++)
        {
            CoopTryLuckPartnerGrantedXp[i] = 0;
            CoopTryLuckPartnerTraitIndex[i] = -1;
        }
        CooperativeCarOwnerChoice = -1;
        CooperativeCarOwnerName = string.Empty;
        CooperativeVehicleColorIndex = -1;
        CoopInventedBarName = string.Empty;
        CoopEveningWhereChoice = -1;
        CoopBarContradictionChoice = -1;
        InterrogationPublicDefenderName = string.Empty;
        CoopBarWhatHappenedChoice = -1;
        CoopSnitchChoice = -1;
        RivalCrimeFamilyAccentIndex = -1;

        EnsurePoliceFacilitiesInitialized(forceRebuild: true);
        LastLocalPaperBlurb = string.Empty;
        LastLocalPaperBlurbDay = -1;
        GenerateLocalPaperBlurbIfNeeded(force: true);

        InvalidateActiveCityData();
        RebuildProceduralCityData();

        MicroBlockBootstrap.ResetAndBuildForNewGame(CityMapSeed);
        PoliceWorldState.ResetForNewGame(CityMapSeed);
        BureauWorldState.ResetForNewGame(CityMapSeed);
    }

    /// <summary>
    /// Poor single-block simulation (separate from full CityGen). Rebuilt on New Game / load from seed until save deltas exist.
    /// </summary>
    public static void EnsureMicroBlockReady()
    {
        if (MicroBlockWorldState.Spots.Count > 0)
            return;
        if (CityMapSeed == 0)
            CityMapSeed = UnityEngine.Random.Range(1, int.MaxValue);
        MicroBlockBootstrap.ResetAndBuildForNewGame(CityMapSeed);
    }
}
