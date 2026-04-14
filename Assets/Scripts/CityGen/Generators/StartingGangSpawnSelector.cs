using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 11: deterministic gang start on a believable lot (working-class / residential / fringe bias, building-aware).
    /// </summary>
    public static class StartingGangSpawnSelector
    {
        enum CollectPass
        {
            StrictPreferred,
            RelaxedDistricts,
            EmergencyAnyLot
        }

        struct Candidate
        {
            public LotData Lot;
            public BuildingData Building;
            public float Score;
            public int TieBreak;
        }

        public static StartingGangSpawnData Select(CityData city, CityGenerationConfig config, float metersPerPlanUnit,
            float worldY)
        {
            var result = new StartingGangSpawnData();
            if (city == null || config == null)
                return result;

            var list = new List<Candidate>(256);
            Collect(city, config, list, CollectPass.StrictPreferred);
            if (list.Count == 0)
                Collect(city, config, list, CollectPass.RelaxedDistricts);
            if (list.Count == 0)
                Collect(city, config, list, CollectPass.EmergencyAnyLot);

            if (list.Count == 0)
            {
                FillFallbackHub(city, result, metersPerPlanUnit, worldY);
                result.SpawnProfile = StartingGangSpawnProfile.FallbackStart;
                FillMemberRing(city.Seed, result.StartPlanPosition, result, metersPerPlanUnit, worldY);
                return result;
            }

            list.Sort((a, b) =>
            {
                int c = b.Score.CompareTo(a.Score);
                return c != 0 ? c : a.TieBreak.CompareTo(b.TieBreak);
            });

            int pool = Mathf.Max(1, config.gangSpawnCandidatePoolSize);
            int n = Mathf.Min(pool, list.Count);
            int pick = Mathf.Abs(DeterministicHash.Mix(DeterministicHash.Mix(city.Seed, unchecked((int)0x5A1D0U)), "gang_spawn_pick")) % n;
            Candidate win = list[pick];

            LotData lot = win.Lot;
            BuildingData b = win.Building;
            Vector2 plan = (lot.Min + lot.Max) * 0.5f;

            result.StartDistrictId = lot.DistrictId;
            result.StartDistrictKind = lot.DistrictKind;
            result.StartBlockId = lot.BlockId;
            result.StartLotId = lot.Id;
            result.StartPlanPosition = plan;
            result.StartWorldPosition = PlanToWorld(plan, metersPerPlanUnit, worldY);
            result.UsesBuildingBasedSpawn = b != null && !b.IsUndeveloped && BuildingSuitabilityScore(b.Kind) >= 8f;
            result.StartBuildingId = b?.Id ?? -1;
            result.SpawnProfile = ClassifyProfile(lot.DistrictKind, b);

            FillMemberRing(city.Seed, plan, result, metersPerPlanUnit, worldY);
            return result;
        }

        static void Collect(CityData city, CityGenerationConfig config, List<Candidate> list, CollectPass pass)
        {
            list.Clear();
            bool rejectAvoidDistricts = pass == CollectPass.StrictPreferred;
            bool requireRoad = pass != CollectPass.EmergencyAnyLot;
            float minScore = pass == CollectPass.StrictPreferred ? 14f : pass == CollectPass.RelaxedDistricts ? 2f : float.NegativeInfinity;
            bool skipUndesirableBuilding = pass == CollectPass.StrictPreferred;

            foreach (LotData lot in city.Lots)
            {
                if (lot.IsReserved)
                    continue;
                if (lot.AreaCells < 0.5f)
                    continue;
                if (requireRoad && !lot.TouchesRoad)
                    continue;

                if (rejectAvoidDistricts && IsAvoidDistrict(lot.DistrictKind))
                    continue;

                BuildingData b = FindBestBuildingOnLot(city, lot.Id);
                if (skipUndesirableBuilding && b != null && IsUndesirableBuilding(b.Kind))
                    continue;

                bool strictBuildingExpectation = pass == CollectPass.StrictPreferred;
                float s = ScoreLotAndBuilding(lot, b, config, strictBuildingExpectation);
                if (s < minScore)
                    continue;

                int tie = DeterministicHash.Mix(city.Seed, DeterministicHash.Mix(lot.Id, b?.Id ?? -712));
                list.Add(new Candidate { Lot = lot, Building = b, Score = s, TieBreak = tie });
            }
        }

        static bool IsAvoidDistrict(DistrictKind k) =>
            k == DistrictKind.Wealthy || k == DistrictKind.DowntownCommercial;

        static bool IsUndesirableBuilding(BuildingKind k) =>
            k == BuildingKind.FinanceOffice || k == BuildingKind.Office || k == BuildingKind.Clinic;

        static BuildingData FindBestBuildingOnLot(CityData city, int lotId)
        {
            BuildingData best = null;
            float bestScore = float.NegativeInfinity;
            foreach (BuildingData b in city.Buildings)
            {
                if (b.LotId != lotId)
                    continue;
                float s = BuildingSuitabilityScore(b.Kind);
                if (s > bestScore)
                {
                    bestScore = s;
                    best = b;
                }
            }

            return best;
        }

        static float DistrictBaseScore(DistrictKind k) =>
            k switch
            {
                DistrictKind.WorkingClass => 100f,
                DistrictKind.Residential => 98f,
                DistrictKind.FringeOuterEdge => 94f,
                DistrictKind.Industrial => 64f,
                DistrictKind.DocksPort => 50f,
                DistrictKind.Unknown => 38f,
                DistrictKind.Wealthy => 8f,
                DistrictKind.DowntownCommercial => 6f,
                _ => 30f
            };

        static float BuildingSuitabilityScore(BuildingKind k) =>
            k switch
            {
                BuildingKind.Tenement => 34f,
                BuildingKind.House => 32f,
                BuildingKind.ApartmentBuilding => 30f,
                BuildingKind.MixedUseCommercialResidential => 28f,
                BuildingKind.Workshop => 16f,
                BuildingKind.Garage => 14f,
                BuildingKind.BarTavern => 18f,
                BuildingKind.PawnShop => 16f,
                BuildingKind.CornerService => 15f,
                BuildingKind.GeneralStore => 12f,
                BuildingKind.Grocery => 11f,
                BuildingKind.Bakery => 10f,
                BuildingKind.Butcher => 10f,
                BuildingKind.Tailor => 9f,
                BuildingKind.SmallServiceOffice => 6f,
                BuildingKind.Warehouse => 4f,
                BuildingKind.StorageYard => 2f,
                BuildingKind.MachineShop => 8f,
                BuildingKind.DockFreight => 5f,
                BuildingKind.RailUtility => 5f,
                BuildingKind.EmptyLot => -8f,
                BuildingKind.VacantParcel => -6f,
                BuildingKind.Yard => -4f,
                BuildingKind.ReservedFutureParcel => -10f,
                _ => 4f
            };

        static float ScoreLotAndBuilding(LotData lot, BuildingData b, CityGenerationConfig config, bool strictBuildingExpectation)
        {
            float s = DistrictBaseScore(lot.DistrictKind);
            s += (1f - Mathf.Clamp01(lot.AccessibilityScore)) * 10f;
            s -= lot.HasMajorRoadFrontage ? config.gangSpawnMajorRoadExposurePenalty : 0f;
            s -= Mathf.Clamp01(lot.AccessibilityScore) * config.gangSpawnHighAccessibilityPenalty * 0.1f;

            float area = lot.AreaCells;
            if (area > 120f)
                s -= 5f;
            else if (area is > 8f and < 90f)
                s += 3f;

            if (b != null)
            {
                s += BuildingSuitabilityScore(b.Kind);
                if (b.CanSupportBackroom)
                    s += 5f;
            }
            else if (strictBuildingExpectation)
            {
                s -= 8f;
            }
            else
            {
                s -= 2f;
            }

            return s;
        }

        static StartingGangSpawnProfile ClassifyProfile(DistrictKind d, BuildingData b)
        {
            if (d == DistrictKind.FringeOuterEdge)
                return StartingGangSpawnProfile.EdgeSafeSpot;

            if (b != null && b.CanSupportBackroom &&
                b.Kind is BuildingKind.BarTavern or BuildingKind.PawnShop or BuildingKind.CornerService or BuildingKind.GeneralStore)
                return StartingGangSpawnProfile.BackRoomStart;

            if (b != null)
            {
                if (b.Kind is BuildingKind.BarTavern or BuildingKind.PawnShop or BuildingKind.CornerService)
                    return StartingGangSpawnProfile.BackRoomStart;
                if (b.Kind is BuildingKind.Tenement or BuildingKind.House)
                    return StartingGangSpawnProfile.WorkingClassFoothold;
                if (b.Kind == BuildingKind.ApartmentBuilding)
                    return StartingGangSpawnProfile.CheapLodging;
                if (b.Kind == BuildingKind.MixedUseCommercialResidential)
                    return StartingGangSpawnProfile.WorkingClassFoothold;
            }

            if (d is DistrictKind.WorkingClass or DistrictKind.Residential)
                return StartingGangSpawnProfile.WorkingClassFoothold;

            return StartingGangSpawnProfile.FallbackStart;
        }

        static void FillFallbackHub(CityData city, StartingGangSpawnData result, float metersPerPlanUnit, float worldY)
        {
            Vector2 hub = city.MacroBoundary.Vertices.Count >= 3
                ? city.MacroBoundary.Centroid()
                : city.Boundary.Center;
            result.StartPlanPosition = hub;
            result.StartWorldPosition = PlanToWorld(hub, metersPerPlanUnit, worldY);
            result.StartDistrictId = -1;
            result.StartDistrictKind = DistrictKind.Unknown;
            result.StartBlockId = -1;
            result.StartLotId = -1;
            result.UsesBuildingBasedSpawn = false;
            result.StartBuildingId = -1;
        }

        static void FillMemberRing(int citySeed, Vector2 anchor, StartingGangSpawnData result, float metersPerPlanUnit,
            float worldY)
        {
            result.GangMemberPlanPositions.Clear();
            result.GangMemberWorldPositions.Clear();

            float rot = (Mathf.Abs(DeterministicHash.Mix(citySeed, unchecked((int)0x4D454DU))) & 0xFFFF) / 65535f * Mathf.PI * 2f;
            float cos = Mathf.Cos(rot);
            float sin = Mathf.Sin(rot);
            Vector2 Rotate(Vector2 v) => new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);

            Vector2[] rel =
            {
                Vector2.zero,
                new Vector2(2.1f, 0.35f),
                new Vector2(-1.15f, 1.85f),
                new Vector2(0.65f, -1.7f)
            };

            for (int i = 0; i < rel.Length; i++)
            {
                Vector2 p = anchor + Rotate(rel[i]);
                result.GangMemberPlanPositions.Add(p);
                result.GangMemberWorldPositions.Add(PlanToWorld(p, metersPerPlanUnit, worldY));
            }
        }

        static Vector3 PlanToWorld(Vector2 plan, float metersPerPlanUnit, float worldY) =>
            new Vector3(plan.x * metersPerPlanUnit, worldY, plan.y * metersPerPlanUnit);
    }
}
