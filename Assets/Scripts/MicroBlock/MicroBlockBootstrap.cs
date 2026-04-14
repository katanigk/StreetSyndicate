using System;
using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

/// <summary>
/// Builds one coherent Prohibition-era poor block: façade knowledge vs concealed simulation fields.
/// </summary>
public static class MicroBlockBootstrap
{
    public static void ResetAndBuildForNewGame(int citySeed)
    {
        MicroBlockWorldState.Clear();
        var rng = new System.Random(unchecked(citySeed * 31) ^ 0x50D2E);

        if (GameSessionState.SingleBlockSandboxEnabled)
            BuildSingleBlockSandboxSpots(rng);
        else
            BuildFullDefaultSpots(rng);

        MicroBlockWorldState.CrewWeeklyRentUsd = 8;
        MicroBlockWorldState.CrewRentPrepaidThroughDay = GameSessionState.CurrentDay;

        MicroBlockKnowledgeStore.SeedAmbientFromSpots(MicroBlockWorldState.Spots);

        if (GameSessionState.ActiveCityData != null)
        {
            MicroBlockAnchorResolver.EnsureCrewHomeBlockAnchored(GameSessionState.ActiveCityData);
            AssignSandboxMacroZones(GameSessionState.ActiveCityData, rng);
            MicroBlockSpotLotBinder.BindSpotsToHomeBlockLots(GameSessionState.ActiveCityData);
        }
    }

    /// <summary>
    /// Big map: crew home block is always <see cref="OpsBigMapLotZoneResolver.ZoneKind.Residential"/>.
    /// 12-block sandbox: 3 warehouse, 1 police, 3 residential total (home + 2), 5 commercial — shuffled among non-home blocks.
    /// Smaller grids: legacy mix (one each + commercial padding).
    /// </summary>
    static void AssignSandboxMacroZones(CityData city, System.Random rng)
    {
        MicroBlockWorldState.SandboxMacroZoneByBlockId.Clear();
        if (!GameSessionState.SingleBlockSandboxEnabled || city?.Blocks == null || city.Blocks.Count == 0)
            return;

        int home = MicroBlockWorldState.CrewHomeBlockId;
        if (home < 0)
            return;

        MicroBlockWorldState.SandboxMacroZoneByBlockId[home] = OpsBigMapLotZoneResolver.ZoneKind.Residential;

        var otherBlocks = new List<int>(city.Blocks.Count);
        for (int i = 0; i < city.Blocks.Count; i++)
        {
            int id = city.Blocks[i].Id;
            if (id != home)
                otherBlocks.Add(id);
        }

        if (otherBlocks.Count == 0)
            return;

        ShuffleList(otherBlocks, rng);

        var pool = new List<OpsBigMapLotZoneResolver.ZoneKind>();
        if (city.Blocks.Count == 12 && otherBlocks.Count == 11)
        {
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Warehouse);
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Warehouse);
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Warehouse);
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Police);
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Residential);
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Residential);
            for (int c = 0; c < 5; c++)
                pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Commercial);
        }
        else
        {
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Warehouse);
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Police);
            pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Commercial);
            while (pool.Count < otherBlocks.Count)
                pool.Add(OpsBigMapLotZoneResolver.ZoneKind.Commercial);
        }

        ShuffleList(pool, rng);

        for (int i = 0; i < otherBlocks.Count; i++)
            MicroBlockWorldState.SandboxMacroZoneByBlockId[otherBlocks[i]] = pool[i];
    }

    static void ShuffleList<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// One spot per lot: blocks × 8 ring cells (3×3 grid minus center courtyard), block index then lot creation order
    /// (matches <see cref="MicroBlockSpotLotBinder"/> sort by block + lot id).
    /// </summary>
    static void BuildSingleBlockSandboxSpots(System.Random rng)
    {
        const int spotsPerBlock = 8;
        int nLots = 12 * spotsPerBlock;
        if (GameSessionState.ActiveCityData != null && GameSessionState.ActiveCityData.Lots.Count > 0)
            nLots = GameSessionState.ActiveCityData.Lots.Count;

        MicroBlockSpotKind[][] templates =
        {
            new[]
            {
                MicroBlockSpotKind.CrewSharedRoom, MicroBlockSpotKind.BarberShop, MicroBlockSpotKind.Laundromat,
                MicroBlockSpotKind.RoomingHouse, MicroBlockSpotKind.PrintShop, MicroBlockSpotKind.SodaLunchCounter,
                MicroBlockSpotKind.PoolHall, MicroBlockSpotKind.SmallClinic
            },
            new[]
            {
                MicroBlockSpotKind.Warehouse, MicroBlockSpotKind.AutoGarage, MicroBlockSpotKind.PoliceBeatOffice,
                MicroBlockSpotKind.PostOfficeBranch, MicroBlockSpotKind.SpeakeasyFront, MicroBlockSpotKind.MissionHall,
                MicroBlockSpotKind.PawnShop, MicroBlockSpotKind.TelegraphDesk
            },
            new[]
            {
                MicroBlockSpotKind.NeighborhoodPark, MicroBlockSpotKind.FirehouseSmall, MicroBlockSpotKind.Newsstand,
                MicroBlockSpotKind.BarberShop, MicroBlockSpotKind.Laundromat, MicroBlockSpotKind.PoolHall,
                MicroBlockSpotKind.SmallClinic, MicroBlockSpotKind.SodaLunchCounter
            },
            new[]
            {
                MicroBlockSpotKind.Newsstand, MicroBlockSpotKind.PrintShop, MicroBlockSpotKind.CornerGrocery,
                MicroBlockSpotKind.SpeakeasyFront, MicroBlockSpotKind.Warehouse, MicroBlockSpotKind.AutoGarage,
                MicroBlockSpotKind.ChurchParish, MicroBlockSpotKind.MissionHall
            }
        };

        const string mark = "\u00B7";
        var spots = new List<MicroBlockSpotRuntime>(nLots);
        for (int i = 0; i < nLots; i++)
        {
            int b = i / spotsPerBlock;
            int s = i % spotsPerBlock;
            MicroBlockSpotKind kind = templates[b % templates.Length][s];
            string id = "sb_" + b + "_" + s;
            MicroBlockConcealedFacts truth = SandboxTruthForKind(rng, kind);
            spots.Add(Spot(id, kind, mark, string.Empty, truth));
        }

        AssignSandboxRoofMergeKeys(spots, spotsPerBlock);

        MicroBlockWorldState.Spots.AddRange(spots);
        for (int i = 0; i < MicroBlockWorldState.Spots.Count; i++)
            MicroBlockWorldState.Spots[i].KnownOnBlockMap = true;
    }

    /// <summary>
    /// Marks adjacent ring slots that share one merged roof visual (see <see cref="MicroBlockSpotRuntime.RoofClusterKey"/>).
    /// Slot order matches <see cref="SingleBlockSandboxCityBuilder"/> lot creation: (0,0)…(2,2) minus center.
    /// </summary>
    static void AssignSandboxRoofMergeKeys(List<MicroBlockSpotRuntime> spots, int spotsPerBlock)
    {
        if (spots == null || spots.Count == 0 || spotsPerBlock <= 0)
            return;

        (int, int)[][] mergeSchemes =
        {
            new[] { (1, 2) },
            new[] { (5, 6) },
            new[] { (0, 1) },
            new[] { (6, 7) }
        };

        int nBlocks = (spots.Count + spotsPerBlock - 1) / spotsPerBlock;
        for (int b = 0; b < nBlocks; b++)
        {
            (int, int)[] pairs = mergeSchemes[b % mergeSchemes.Length];
            string clusterKey = "merge_" + b;
            for (int p = 0; p < pairs.Length; p++)
            {
                int i0 = b * spotsPerBlock + pairs[p].Item1;
                int i1 = b * spotsPerBlock + pairs[p].Item2;
                if (i0 >= spots.Count || i1 >= spots.Count)
                    continue;
                spots[i0].RoofClusterKey = clusterKey;
                spots[i1].RoofClusterKey = clusterKey;
            }
        }
    }

    static MicroBlockConcealedFacts SandboxTruthForKind(System.Random rng, MicroBlockSpotKind kind)
    {
        switch (kind)
        {
            case MicroBlockSpotKind.CrewSharedRoom:
                return Truth(rng, MicroBlockWorldState.LandlordDisplayName, new[] { "Crew" }, 0, false, false, 18, 22, false, false,
                    "Rent tracked on MicroBlockWorldState.");
            case MicroBlockSpotKind.PoliceBeatOffice:
                return Truth(rng, "City", new[] { "Desk sergeant" }, 0, false, true, 96, 90, Maybe(rng, 0.06f), false, "Beat paperwork.");
            case MicroBlockSpotKind.NeighborhoodPark:
                return Truth(rng, "Parks", Array.Empty<string>(), 0, false, false, 24, 8, Maybe(rng, 0.15f), Maybe(rng, 0.12f), "Open air.");
            case MicroBlockSpotKind.ChurchParish:
                return Truth(rng, "Parish", new[] { "Priest" }, rng.Next(4, 14), false, false, 22, 28, false, false, "Charity register.");
            case MicroBlockSpotKind.MissionHall:
                return Truth(rng, "Mission board", new[] { "Deacon" }, rng.Next(6, 18), false, false, 20, 24, false, false, "Soup line.");
            case MicroBlockSpotKind.FirehouseSmall:
                return Truth(rng, "Fire dept", new[] { "Captain" }, 0, false, true, 48, 55, Maybe(rng, 0.08f), false, "Volunteers.");
            case MicroBlockSpotKind.PostOfficeBranch:
                return Truth(rng, "Post office", new[] { "Clerk" }, 0, false, false, 52, 70, false, false, "Parcels.");
            case MicroBlockSpotKind.SpeakeasyFront:
                return Truth(rng, "Front", new[] { "Door" }, rng.Next(180, 380), Maybe(rng, 0.65f), Maybe(rng, 0.3f), 28, 68, true, Maybe(rng, 0.15f), "Wet nights.");
            default:
                return Truth(rng, "Proprietor", new[] { "Staff" }, rng.Next(28, 95), Maybe(rng, 0.18f), Maybe(rng, 0.06f),
                    rng.Next(28, 52), rng.Next(32, 55), Maybe(rng, 0.25f), Maybe(rng, 0.12f), "Neighborhood unit.");
        }
    }

    static void BuildFullDefaultSpots(System.Random rng)
    {
        var spots = new List<MicroBlockSpotRuntime>
        {
            Spot("spot_crew_room", MicroBlockSpotKind.CrewSharedRoom,
                "Your shared room (rear hall, 3rd floor)",
                "Four of you split the rent; stairs smell of coal smoke and cheap soap.",
                Truth(rng, owner: MicroBlockWorldState.LandlordDisplayName, workers: new[] { "You lot" },
                    weekly: 0, protection: false, policeTie: false, patrol: 15, intrusion: 20,
                    booze: false, dope: false, note: "Weekly rent tracked on MicroBlockWorldState; not shop income.")),

            Spot("spot_rooming_landlord", MicroBlockSpotKind.RoomingHouse,
                "O'Brien Rooming House — office window by the stoop",
                "Landlady collects rent Saturdays. She knows everyone's business.",
                Truth(rng, owner: MicroBlockWorldState.LandlordDisplayName, workers: new[] { "Mrs. O'Brien", "Charlotte (charwoman)" },
                    weekly: rng.Next(45, 90), protection: false, policeTie: false, patrol: 40, intrusion: 55,
                    booze: false, dope: false, note: "Legit income mostly rent; may hold letters for tenants.")),

            Spot("spot_barber", MicroBlockSpotKind.BarberShop,
                "Vincent's Barber Shop",
                "Striped pole, two chairs. Vincent talks politics and baseball.",
                Truth(rng, owner: "Vincent Gallo", workers: new[] { "Vincent", "Eddie (Saturday boy)" },
                    weekly: rng.Next(28, 55), protection: Maybe(rng, 0.12f), policeTie: false, patrol: 35, intrusion: 40,
                    booze: false, dope: false, note: "Barbers hear everything; good rumor funnel.")),

            Spot("spot_grocery", MicroBlockSpotKind.CornerGrocery,
                "Kowalski Groceries",
                "Shelves of tins, sack flour, loose tobacco. Credit notebook by the till.",
                Truth(rng, owner: "Helena Kowalski", workers: new[] { "Helena", "Stefan" },
                    weekly: rng.Next(60, 120), protection: Maybe(rng, 0.18f), policeTie: Maybe(rng, 0.06f), patrol: 45, intrusion: 50,
                    booze: false, dope: false, note: "May fence small goods under counter if pressured.")),

            Spot("spot_laundry", MicroBlockSpotKind.Laundromat,
                "Red Brick Washhouse",
                "Steam in the windows Tuesdays. Women wait with baskets.",
                Truth(rng, owner: "Chen & Sons Laundry", workers: new[] { "Old Man Chen", "Mei" },
                    weekly: rng.Next(40, 85), protection: Maybe(rng, 0.22f), policeTie: false, patrol: 25, intrusion: 35,
                    booze: false, dope: false, note: "Back room sometimes stores other people's bundles.")),

            Spot("spot_police", MicroBlockSpotKind.PoliceBeatOffice,
                "12th Ward Beat Office",
                "Blue lamp, wanted notices, smell of coffee gone sour.",
                Truth(rng, owner: "City of New Temperance", workers: new[] { "Sgt. Marconi", "Officer Kowalski" },
                    weekly: 0, protection: false, policeTie: true, patrol: 100, intrusion: 95,
                    booze: Maybe(rng, 0.08f), dope: false, note: "Dry enforcement paperwork; some take squeeze.")),

            Spot("spot_post", MicroBlockSpotKind.PostOfficeBranch,
                "Neighborhood Post & Telegraph Window",
                "Brass grille, rubber stamps, gossip in line.",
                Truth(rng, owner: "U.S. Post Office", workers: new[] { "Clerk Dunne" },
                    weekly: 0, protection: false, policeTie: false, patrol: 55, intrusion: 75,
                    booze: false, dope: false, note: "Inspectors watch parcel weight; useful for paper trails.")),

            Spot("spot_church", MicroBlockSpotKind.ChurchParish,
                "St. Malachy's Parish (side door on the alley)",
                "Soup smell Thursdays. Father Burke keeps the poor register.",
                Truth(rng, owner: "Diocese", workers: new[] { "Father Burke", "Sister Agnes" },
                    weekly: rng.Next(5, 15), protection: false, policeTie: false, patrol: 20, intrusion: 30,
                    booze: false, dope: false, note: "Charity ledger; moral pressure on locals.")),

            Spot("spot_warehouse", MicroBlockSpotKind.Warehouse,
                "Murray Storage — corrugated door",
                "You know crates go in and out. No hours posted.",
                Truth(rng, owner: "Murray Storage LLC", workers: new[] { "Night watchman Doyle" },
                    weekly: rng.Next(70, 140), protection: Maybe(rng, 0.35f), policeTie: Maybe(rng, 0.1f), patrol: 30, intrusion: 65,
                    booze: Maybe(rng, 0.45f), dope: Maybe(rng, 0.25f), note: "Classic blind pig stash risk.")),

            Spot("spot_garage", MicroBlockSpotKind.AutoGarage,
                "Russo Garage & Tire",
                "Lift pit, smell of oil. Radio plays static jazz.",
                Truth(rng, owner: "Tony Russo", workers: new[] { "Tony", "Nicky" },
                    weekly: rng.Next(55, 100), protection: Maybe(rng, 0.28f), policeTie: Maybe(rng, 0.05f), patrol: 28, intrusion: 45,
                    booze: false, dope: Maybe(rng, 0.15f), note: "Cars moved after hours; mufflers hide runs.")),

            Spot("spot_park", MicroBlockSpotKind.NeighborhoodPark,
                "Duffy's Patch (iron fence, mud center)",
                "Kids, drunks, soapboxers. Deals happen on the benches.",
                Truth(rng, owner: "City Parks", workers: Array.Empty<string>(),
                    weekly: 0, protection: false, policeTie: false, patrol: 22, intrusion: 5,
                    booze: Maybe(rng, 0.2f), dope: Maybe(rng, 0.3f), note: "Open air; low privacy, high rumor.")),

            Spot("spot_pool", MicroBlockSpotKind.PoolHall,
                "Star Billiards",
                "Green felt, chalk dust, cigarette haze behind the bead curtain.",
                Truth(rng, owner: "Leo 'Stars' Moretti", workers: new[] { "Leo", "Rack boy Sal" },
                    weekly: rng.Next(35, 75), protection: Maybe(rng, 0.55f), policeTie: Maybe(rng, 0.15f), patrol: 38, intrusion: 42,
                    booze: Maybe(rng, 0.65f), dope: Maybe(rng, 0.2f), note: "Back room whispers; fixed games.")),

            Spot("spot_soda", MicroBlockSpotKind.SodaLunchCounter,
                "Marie's Lunch — stools and pie case",
                "Coffee five cents. Ham on rye. Truck drivers at noon.",
                Truth(rng, owner: "Marie Lefèvre", workers: new[] { "Marie", "Dish girl Iris" },
                    weekly: rng.Next(50, 95), protection: Maybe(rng, 0.15f), policeTie: false, patrol: 42, intrusion: 38,
                    booze: Maybe(rng, 0.25f), dope: false, note: "Coffee cup may carry something stronger for regulars.")),

            Spot("spot_clinic", MicroBlockSpotKind.SmallClinic,
                "Dr. Weiss — shingle by the stairs",
                "Two rooms. iodine smell. Cash on the barrelhead.",
                Truth(rng, owner: "Dr. Nathan Weiss", workers: new[] { "Dr. Weiss", "Nurse Pauline" },
                    weekly: rng.Next(40, 90), protection: false, policeTie: Maybe(rng, 0.12f), patrol: 33, intrusion: 48,
                    booze: false, dope: Maybe(rng, 0.18f), note: "Treats bullet holes without questions.")),

            Spot("spot_news", MicroBlockSpotKind.Newsstand,
                "Hegarty's News & Smokes",
                "Stacked Tribunes, Racing Form, cough drops in jars.",
                Truth(rng, owner: "Patrick Hegarty", workers: new[] { "Patrick", "Tommy (evenings)" },
                    weekly: rng.Next(22, 48), protection: Maybe(rng, 0.1f), policeTie: false, patrol: 40, intrusion: 25,
                    booze: false, dope: false, note: "Papers hide messages between pages.")),

            Spot("spot_speakeasy", MicroBlockSpotKind.SpeakeasyFront,
                "Blind Tiger Tailors (closed shutters)",
                "Looks empty. Piano faintly on wet nights.",
                Truth(rng, owner: "Unknown", workers: new[] { "Door man", "Bartender (unknown)" },
                    weekly: rng.Next(200, 450), protection: Maybe(rng, 0.7f), policeTie: Maybe(rng, 0.35f), patrol: 25, intrusion: 70,
                    booze: true, dope: Maybe(rng, 0.2f), note: "Core Prohibition pressure magnet.")),

            Spot("spot_mission", MicroBlockSpotKind.MissionHall,
                "Bethesda Mission Hall",
                "Soup line, hymn boards, English night classes.",
                Truth(rng, owner: "Methodist Board", workers: new[] { "Deacon Holt" },
                    weekly: rng.Next(8, 20), protection: false, policeTie: false, patrol: 18, intrusion: 22,
                    booze: false, dope: false, note: "Reformers watch who shows up.")),

            Spot("spot_fire", MicroBlockSpotKind.FirehouseSmall,
                "Engine House No. 47 (single bay)",
                "Red doors, brass pole, bored volunteers playing checkers.",
                Truth(rng, owner: "Fire Department", workers: new[] { "Captain Roy", "Driver Walsh" },
                    weekly: 0, protection: false, policeTie: true, patrol: 50, intrusion: 60,
                    booze: Maybe(rng, 0.1f), dope: false, note: "Saw everything on the block last big fire.")),

            Spot("spot_pawn", MicroBlockSpotKind.PawnShop,
                "Gold & Garnet Pawnbrokers",
                "Three balls sign, mesh cage. Pledges in tin tags.",
                Truth(rng, owner: "Isaac Goldfarb", workers: new[] { "Isaac", "Morris" },
                    weekly: rng.Next(45, 110), protection: Maybe(rng, 0.2f), policeTie: Maybe(rng, 0.08f), patrol: 44, intrusion: 72,
                    booze: false, dope: false, note: "Holds hot goods; ledger is leverage.")),

            Spot("spot_telegraph", MicroBlockSpotKind.TelegraphDesk,
                "Western Union at the druggist",
                "Brass instrument on the counter; boy runners on the corner.",
                Truth(rng, owner: "Western Union", workers: new[] { "Operator Finch" },
                    weekly: rng.Next(15, 35), protection: false, policeTie: Maybe(rng, 0.2f), patrol: 48, intrusion: 55,
                    booze: false, dope: false, note: "Messages can be 'lost' for a price.")),
        };

        MicroBlockWorldState.Spots.AddRange(spots);

        for (int i = 0; i < MicroBlockWorldState.Spots.Count; i++)
            MicroBlockWorldState.Spots[i].KnownOnBlockMap = i < 4;
    }

    static bool Maybe(System.Random rng, float probability) => rng.NextDouble() < probability;

    static MicroBlockSpotRuntime Spot(string id, MicroBlockSpotKind kind, string surfaceName, string blurb, MicroBlockConcealedFacts truth)
    {
        return new MicroBlockSpotRuntime
        {
            StableId = id,
            Kind = kind,
            SurfacePublicName = surfaceName,
            SurfaceShortBlurb = blurb,
            Truth = truth
        };
    }

    static MicroBlockConcealedFacts Truth(System.Random rng, string owner, string[] workers, int weekly,
        bool protection, bool policeTie, int patrol, int intrusion, bool booze, bool dope, string note)
    {
        return new MicroBlockConcealedFacts
        {
            OwnerDisplayName = owner,
            WorkerDisplayNames = workers ?? Array.Empty<string>(),
            WeeklyNetIncomeEstimateUsd = weekly,
            PaysProtectionRacket = protection,
            PoliceLookoutOrInformantTie = policeTie,
            PolicePatrolInterest = Mathf.Clamp(patrol, 0, 100),
            IntrusionDifficulty = Mathf.Clamp(intrusion, 0, 100),
            SellsAlcoholIllicitly = booze,
            SellsNarcoticsIllicitly = dope,
            DesignerNote = note
        };
    }
}
