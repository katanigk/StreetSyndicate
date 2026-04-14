using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 6: rule-based, deterministic anchor institution placement on scored, reserved lots.
    /// </summary>
    public sealed class AnchorInstitutionGenerator
    {
        static readonly InstitutionKind[] s_placementOrder =
        {
            InstitutionKind.CityHall,
            InstitutionKind.Courthouse,
            InstitutionKind.TaxOffice,
            InstitutionKind.FederalOffice,
            InstitutionKind.Bank,
            InstitutionKind.PoliceStation,
            InstitutionKind.Hospital,
            InstitutionKind.RailStation,
            InstitutionKind.DockAuthority,
            InstitutionKind.Prison
        };

        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            city.Institutions.Clear();
            if (!config.placeAnchorInstitutions || city.Lots.Count == 0)
                return;

            int nextId = 0;
            float cityDiag = CityDiagonal(city);
            Vector2 hub = CityHub(city);
            int maxPass = Mathf.Clamp(config.institutionPlacementRelaxationPasses, 1, 4);

            foreach (InstitutionKind kind in s_placementOrder)
            {
                if (kind == InstitutionKind.DockAuthority && !CitySupportsDockInstitution(city))
                {
                    UnityEngine.Debug.Log(
                        $"[Institutions] Skipping {kind}: no docks district or water macro feature (seed={city.Seed}).");
                    continue;
                }

                InstitutionData placed;
                if (!TryPlaceOne(city, config, kind, cityDiag, hub, maxPass, stageSeed, out placed) &&
                    !TryEmergencyPlaceAnchor(city, config, kind, out placed))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[Institutions] FAILED to place {kind} after {maxPass} relaxation pass(es) (seed={city.Seed}).");
                    continue;
                }

                placed.Id = nextId++;
                LotData l = FindLot(city, placed.LotId);
                if (l != null)
                    ReserveLot(l, placed.Id, kind);
                city.Institutions.Add(placed);
            }

            _ = stageSeed;
        }

        /// <summary>
        /// When scored placement finds no candidate (tight seeds, harsh lot filters), still reserve one lot so
        /// government extraction and gameplay have a stable anchor id (marked with low placement score).
        /// </summary>
        static bool TryEmergencyPlaceAnchor(CityData city, CityGenerationConfig config, InstitutionKind kind,
            out InstitutionData result)
        {
            result = null;
            LotData bestLot = null;
            DistrictData bestDist = null;
            float bestArea = -1f;
            float prisonMinArea = kind == InstitutionKind.Prison
                ? Mathf.Max(24f, config.institutionPrisonMinLotAreaCells * 0.45f)
                : 0f;

            foreach (LotData lot in city.Lots)
            {
                if (lot.IsReserved)
                    continue;
                DistrictData d = FindDistrictById(city, lot.DistrictId);
                if (d == null)
                    continue;
                if (HardDistrictBan(kind, d.Kind))
                    continue;
                if (kind == InstitutionKind.Prison && lot.AreaCells < prisonMinArea)
                    continue;

                if (lot.AreaCells > bestArea)
                {
                    bestArea = lot.AreaCells;
                    bestLot = lot;
                    bestDist = d;
                }
            }

            if (bestLot == null || bestDist == null)
                return false;

            result = new InstitutionData
            {
                Kind = kind,
                LotId = bestLot.Id,
                BlockId = bestLot.BlockId,
                DistrictId = bestDist.Id,
                DistrictKind = bestDist.Kind,
                Position = (bestLot.Min + bestLot.Max) * 0.5f,
                PlacementScore = -250f,
                DisplayName = DefaultDisplayName(kind),
                PreferredSizeClass = bestLot.SizeClass,
                TagsPlaceholder = null
            };

            UnityEngine.Debug.LogWarning(
                $"[Institutions] Emergency fallback placed {kind} on lot {bestLot.Id} ({bestDist.Kind}) — scored anchor pass found no seat (seed={city.Seed}).");
            return true;
        }

        static DistrictData FindDistrictById(CityData city, int districtId)
        {
            for (int i = 0; i < city.Districts.Count; i++)
            {
                if (city.Districts[i].Id == districtId)
                    return city.Districts[i];
            }

            return null;
        }

        static void ReserveLot(LotData lot, int institutionId, InstitutionKind kind)
        {
            lot.IsReserved = true;
            lot.ReservedByInstitutionId = institutionId;
            lot.ReservedForKind = kind;
        }

        static LotData FindLot(CityData city, int lotId)
        {
            for (int i = 0; i < city.Lots.Count; i++)
            {
                if (city.Lots[i].Id == lotId)
                    return city.Lots[i];
            }

            return null;
        }

        static bool CitySupportsDockInstitution(CityData city)
        {
            foreach (DistrictData d in city.Districts)
            {
                if (d.Kind == DistrictKind.DocksPort)
                    return true;
            }

            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind == MacroFeatureKind.River || f.Kind == MacroFeatureKind.Coastline)
                    return true;
            }

            return false;
        }

        static bool TryPlaceOne(CityData city, CityGenerationConfig config, InstitutionKind kind, float cityDiag,
            Vector2 hub, int maxPass, CitySeed stageSeed, out InstitutionData result)
        {
            result = null;
            Candidate? best = null;

            for (int pass = 0; pass < maxPass; pass++)
            {
                Candidate? passBest = null;
                foreach (DistrictData d in city.Districts)
                {
                    if (!DistrictAllowed(kind, d.Kind, pass, maxPass))
                        continue;
                    foreach (LotData lot in city.Lots)
                    {
                        if (lot.DistrictId != d.Id || lot.IsReserved)
                            continue;
                        if (LotRejected(kind, lot, d, pass, city, cityDiag, hub, config))
                            continue;
                        float s = ScoreLot(kind, lot, d, city, cityDiag, hub, pass, config);
                        if (s < -500f)
                            continue;
                        int tie = DeterministicHash.Mix(city.Seed,
                            DeterministicHash.Mix((int)kind + pass * 7919, lot.Id));
                        var cand = new Candidate { Lot = lot, District = d, Score = s, TieBreak = tie };
                        if (!passBest.HasValue || cand.Score > passBest.Value.Score + 1e-4f ||
                            (Mathf.Abs(cand.Score - passBest.Value.Score) < 1e-4f &&
                             cand.TieBreak < passBest.Value.TieBreak))
                            passBest = cand;
                    }
                }

                if (passBest.HasValue)
                {
                    best = passBest;
                    break;
                }
            }

            if (!best.HasValue)
                return false;

            LotData l = best.Value.Lot;
            DistrictData dist = best.Value.District;
            result = new InstitutionData
            {
                Kind = kind,
                LotId = l.Id,
                BlockId = l.BlockId,
                DistrictId = dist.Id,
                DistrictKind = dist.Kind,
                Position = (l.Min + l.Max) * 0.5f,
                PlacementScore = best.Value.Score,
                DisplayName = DefaultDisplayName(kind),
                PreferredSizeClass = l.SizeClass,
                TagsPlaceholder = null
            };
            return true;
        }

        struct Candidate
        {
            public LotData Lot;
            public DistrictData District;
            public float Score;
            public int TieBreak;
        }

        static float CityDiagonal(CityData city)
        {
            Vector2 d = city.Boundary.Max - city.Boundary.Min;
            return Mathf.Max(1e-4f, Mathf.Sqrt(d.x * d.x + d.y * d.y));
        }

        static Vector2 CityHub(CityData city)
        {
            if (city.MacroBoundary.Vertices.Count >= 3)
                return city.MacroBoundary.Centroid();
            return (city.Boundary.Min + city.Boundary.Max) * 0.5f;
        }

        /// <summary>pass 0 = strict, higher = looser. maxPass used to scale last pass as "anything legal".</summary>
        static bool DistrictAllowed(InstitutionKind k, DistrictKind d, int pass, int maxPass)
        {
            if (HardDistrictBan(k, d))
                return false;

            bool loose = pass >= maxPass - 1 && maxPass > 1;
            bool medium = pass >= 1;

            switch (k)
            {
                case InstitutionKind.CityHall:
                    return d == DistrictKind.DowntownCommercial || (medium && d == DistrictKind.Residential);

                case InstitutionKind.Courthouse:
                    if (d == DistrictKind.Industrial || d == DistrictKind.FringeOuterEdge)
                        return loose;
                    return d == DistrictKind.DowntownCommercial || (medium && d == DistrictKind.Residential);

                case InstitutionKind.TaxOffice:
                    if (d == DistrictKind.Industrial || d == DistrictKind.FringeOuterEdge)
                        return loose;
                    return d == DistrictKind.DowntownCommercial ||
                           (medium && (d == DistrictKind.WorkingClass || d == DistrictKind.Residential));

                case InstitutionKind.FederalOffice:
                    if (d == DistrictKind.FringeOuterEdge && !loose)
                        return false;
                    return d == DistrictKind.DowntownCommercial ||
                           (medium && (d == DistrictKind.WorkingClass || d == DistrictKind.Residential));

                case InstitutionKind.Bank:
                    if (d == DistrictKind.Industrial)
                        return medium;
                    return d == DistrictKind.DowntownCommercial || d == DistrictKind.Wealthy ||
                           (medium && d == DistrictKind.Residential);

                case InstitutionKind.PoliceStation:
                    if (pass == 0 && d == DistrictKind.FringeOuterEdge)
                        return false;
                    return d == DistrictKind.DowntownCommercial || d == DistrictKind.WorkingClass ||
                           d == DistrictKind.Residential || d == DistrictKind.Industrial ||
                           d == DistrictKind.DocksPort || (medium && d == DistrictKind.FringeOuterEdge);

                case InstitutionKind.Hospital:
                    if (pass == 0 && (d == DistrictKind.FringeOuterEdge || d == DistrictKind.Industrial))
                        return false;
                    return d == DistrictKind.Residential || d == DistrictKind.DowntownCommercial ||
                           d == DistrictKind.WorkingClass ||
                           (medium && (d == DistrictKind.Industrial || d == DistrictKind.DocksPort ||
                                       d == DistrictKind.Wealthy));

                case InstitutionKind.Prison:
                    return d == DistrictKind.FringeOuterEdge || d == DistrictKind.Industrial ||
                           (medium && (d == DistrictKind.WorkingClass || d == DistrictKind.DocksPort));

                case InstitutionKind.RailStation:
                    if (pass == 0)
                        return d == DistrictKind.Industrial || d == DistrictKind.WorkingClass ||
                               d == DistrictKind.DowntownCommercial || d == DistrictKind.DocksPort;
                    return true;

                case InstitutionKind.DockAuthority:
                    if (pass == 0)
                        return d == DistrictKind.DocksPort || d == DistrictKind.Industrial ||
                               d == DistrictKind.DowntownCommercial;
                    return d != DistrictKind.Wealthy || loose;

                default:
                    return false;
            }
        }

        static bool HardDistrictBan(InstitutionKind k, DistrictKind d)
        {
            if (k == InstitutionKind.Prison)
                return d == DistrictKind.Wealthy || d == DistrictKind.DowntownCommercial;
            if (k == InstitutionKind.FederalOffice && d == DistrictKind.DocksPort)
                return true;
            return false;
        }

        static bool LotRejected(InstitutionKind k, LotData lot, DistrictData d, int pass, CityData city,
            float cityDiag, Vector2 hub, CityGenerationConfig config)
        {
            float frontMin = pass == 0 ? config.institutionMinFrontageStrictCells : 0f;

            switch (k)
            {
                case InstitutionKind.Courthouse:
                case InstitutionKind.CityHall:
                    if (pass == 0 && !lot.SupportsStreetFacingBuilding && lot.FrontageLength < frontMin + 1f)
                        return true;
                    if (pass == 0 && lot.AccessibilityScore < config.institutionHospitalMinAccessibility * 0.95f)
                        return true;
                    break;

                case InstitutionKind.TaxOffice:
                case InstitutionKind.FederalOffice:
                    if (pass == 0 && !lot.TouchesRoad && lot.NearestRoadDistance > cityDiag * 0.08f)
                        return true;
                    break;

                case InstitutionKind.Bank:
                    if (pass == 0 && lot.FrontageLength < frontMin + 0.5f)
                        return true;
                    break;
            }

            return false;
        }

        static float ScoreLot(InstitutionKind k, LotData lot, DistrictData d, CityData city, float cityDiag,
            Vector2 hub, int pass, CityGenerationConfig config)
        {
            float inv = 1f / cityDiag;
            Vector2 c = (lot.Min + lot.Max) * 0.5f;
            float cw = config.institutionAccessibilityWeight;
            float cf = config.institutionFrontageWeight;
            float cc = config.institutionCentralityWeight;
            float cl = config.institutionLargeLotBonus;

            switch (k)
            {
                case InstitutionKind.CityHall:
                    return BaseCivicScore(lot, d, cw, cf) + Centrality(c, hub, inv, cc) * 22f +
                           d.CommercialValue * 20f * cf;

                case InstitutionKind.Courthouse:
                {
                    float v = BaseCivicScore(lot, d, cw, cf) + Centrality(c, hub, inv, cc) * 26f +
                              d.CommercialValue * 14f;
                    if (d.Kind == DistrictKind.Industrial)
                        v -= 40f;
                    if (d.Kind == DistrictKind.FringeOuterEdge)
                        v -= 35f;
                    if (lot.SupportsStreetFacingBuilding)
                        v += 18f;
                    return v;
                }

                case InstitutionKind.TaxOffice:
                {
                    float v = BaseCivicScore(lot, d, cw * 0.95f, cf) + Centrality(c, hub, inv, cc) * 16f +
                              d.CommercialValue * 16f;
                    if (d.Kind == DistrictKind.Industrial)
                        v -= 25f;
                    return v;
                }

                case InstitutionKind.FederalOffice:
                {
                    float v = BaseCivicScore(lot, d, cw * 0.9f, cf * 0.95f) + Centrality(c, hub, inv, cc) * 12f;
                    if (d.Kind == DistrictKind.DocksPort || d.Kind == DistrictKind.FringeOuterEdge)
                        v -= 50f;
                    v += d.PoliceBaseline * 10f;
                    return v;
                }

                case InstitutionKind.Bank:
                {
                    float v = BaseCivicScore(lot, d, cw, cf * 1.05f);
                    v += d.WealthLevel * 24f;
                    v += d.CommercialValue * 16f;
                    if (lot.SupportsStreetFacingBuilding)
                        v += 14f;
                    if (d.Kind == DistrictKind.Industrial)
                        v -= 30f;
                    if (d.Kind == DistrictKind.FringeOuterEdge)
                        v -= 25f;
                    return v;
                }

                case InstitutionKind.PoliceStation:
                {
                    float v = BaseCivicScore(lot, d, cw * 0.88f, cf);
                    if (d.Kind == DistrictKind.DowntownCommercial)
                        v += 20f;
                    if (d.Kind == DistrictKind.WorkingClass)
                        v += 14f;
                    if (d.Kind == DistrictKind.FringeOuterEdge)
                        v -= 22f;
                    v += d.CrimeBaseline * 12f;
                    v += Centrality(c, hub, inv, cc) * 6f;
                    if (lot.HasMajorRoadFrontage || lot.FrontageRoadKind == RoadEdgeKind.Secondary)
                        v += 10f;
                    return v;
                }

                case InstitutionKind.Hospital:
                {
                    if (lot.AccessibilityScore < config.institutionHospitalMinAccessibility)
                        return -1000f;
                    float v = BaseCivicScore(lot, d, cw, cf);
                    if (d.Kind == DistrictKind.Residential)
                        v += 16f;
                    if (d.Kind == DistrictKind.FringeOuterEdge)
                        v -= 22f;
                    v += Centrality(c, hub, inv, cc) * 14f;
                    v += (1f - Vector2.Distance(c, d.CenterPosition) * inv) * 10f;
                    return v;
                }

                case InstitutionKind.Prison:
                {
                    if (lot.SizeClass != LotSizeClass.Large && lot.SizeClass != LotSizeClass.Oversize &&
                        lot.AreaCells < config.institutionPrisonMinLotAreaCells)
                        return -1000f;
                    float v = lot.AreaCells * 0.08f * cl;
                    if (d.Kind == DistrictKind.FringeOuterEdge)
                        v += 35f;
                    if (d.Kind == DistrictKind.Industrial)
                        v += 28f;
                    v += (1f - Centrality(c, hub, inv, 1f)) * 12f;
                    v += (1f - d.WealthLevel) * 10f;
                    if (lot.TouchesRoad)
                        v -= 5f;
                    return v;
                }

                case InstitutionKind.RailStation:
                {
                    float railD = MinDistToRail(city, c);
                    float railW = config.institutionRailProximityWeight;
                    float railScore = (1f - Mathf.Clamp01(railD / Mathf.Max(1f, config.institutionRailMaxDistanceCells))) *
                                      42f * railW;
                    float v = lot.AreaCells * 0.06f * cl + d.LogisticsValue * 28f * config.institutionLogisticsWeight +
                              railScore;
                    if (lot.SizeClass == LotSizeClass.Large || lot.SizeClass == LotSizeClass.Oversize)
                        v += 15f;
                    v += lot.AccessibilityScore * 10f * cw;
                    if (d.Kind == DistrictKind.Industrial || d.Kind == DistrictKind.WorkingClass)
                        v += 12f;
                    return v;
                }

                case InstitutionKind.DockAuthority:
                {
                    float waterD = MinDistToWater(city, c);
                    float waterScore =
                        (1f - Mathf.Clamp01(waterD / Mathf.Max(1f, config.institutionWaterMaxDistanceCells))) * 45f *
                        config.institutionWaterProximityWeight;
                    float v = waterScore + d.LogisticsValue * 22f * config.institutionLogisticsWeight +
                              lot.AreaCells * 0.05f * cl;
                    if (d.Kind == DistrictKind.DocksPort)
                        v += 40f;
                    if (lot.TouchesRoad)
                        v += 6f;
                    return v;
                }

                default:
                    return 0f;
            }
        }

        static float Centrality(Vector2 lotCenter, Vector2 hub, float invDiag, float weight) =>
            (1f - Mathf.Clamp01(Vector2.Distance(lotCenter, hub) * invDiag * 1.05f)) * weight;

        static float BaseCivicScore(LotData lot, DistrictData d, float accessWeight, float frontageWeight)
        {
            float v = lot.AccessibilityScore * 35f * accessWeight;
            v += lot.FrontageLength * 1.8f * frontageWeight;
            if (lot.HasMajorRoadFrontage)
                v += 22f;
            else if (lot.FrontageRoadKind == RoadEdgeKind.Secondary)
                v += 12f;
            if (lot.SupportsStreetFacingBuilding)
                v += 14f;
            if (lot.SupportsLargeBuilding)
                v += 6f;
            if (lot.TouchesRoad)
                v += 8f;
            return v;
        }

        static float MinDistToRail(CityData city, Vector2 p)
        {
            float best = float.MaxValue;
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind != MacroFeatureKind.RailCorridor || f.Path == null || f.Path.Count < 2)
                    continue;
                float d = RoadGraphGeometry.MinDistancePointToPolyline(p, f.Path);
                if (d < best)
                    best = d;
            }

            return best >= float.MaxValue - 1f ? cityDiagFallback(city) : best;
        }

        static float MinDistToWater(CityData city, Vector2 p)
        {
            float best = float.MaxValue;
            foreach (MacroFeatureData f in city.MacroFeatures)
            {
                if (f.Kind != MacroFeatureKind.River && f.Kind != MacroFeatureKind.Coastline)
                    continue;
                if (f.Path == null || f.Path.Count < 2)
                    continue;
                float d = RoadGraphGeometry.MinDistancePointToPolyline(p, f.Path);
                if (d < best)
                    best = d;
            }

            return best >= float.MaxValue - 1f ? cityDiagFallback(city) : best;
        }

        static float cityDiagFallback(CityData city)
        {
            Vector2 d = city.Boundary.Max - city.Boundary.Min;
            return Mathf.Sqrt(d.x * d.x + d.y * d.y);
        }

        static string DefaultDisplayName(InstitutionKind k)
        {
            switch (k)
            {
                case InstitutionKind.PoliceStation: return "Police Station";
                case InstitutionKind.Courthouse: return "Courthouse";
                case InstitutionKind.Prison: return "Prison";
                case InstitutionKind.Hospital: return "Hospital";
                case InstitutionKind.CityHall: return "City Hall";
                case InstitutionKind.TaxOffice: return "Tax Office";
                case InstitutionKind.FederalOffice: return "Federal Office";
                case InstitutionKind.Bank: return "Bank";
                case InstitutionKind.RailStation: return "Rail Station";
                case InstitutionKind.DockAuthority: return "Port Authority";
                default: return "Institution";
            }
        }
    }
}
