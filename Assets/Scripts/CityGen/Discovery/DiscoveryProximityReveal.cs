using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>
    /// Batch 10: shared distance-based reveal using <see cref="DiscoveryReveal"/> only (no duplicate promotion rules).
    /// </summary>
    public static class DiscoveryProximityReveal
    {
        public enum Mode
        {
            Starting,
            Movement
        }

        public static void ApplyAroundPlanPosition(CityData city, CityGenerationConfig config, Vector2 planCells, Mode mode)
        {
            if (city == null || config == null)
                return;

            float dRad = mode == Mode.Starting
                ? config.startingDistrictRevealRadiusCells
                : config.movementDistrictRevealRadiusCells;
            float bRad = mode == Mode.Starting
                ? config.startingBuildingRevealRadiusCells
                : config.movementBuildingRevealRadiusCells;
            float iRad = mode == Mode.Starting
                ? config.startingInstitutionRevealRadiusCells
                : config.movementInstitutionRevealRadiusCells;

            float kMul = Mathf.Max(0.05f, config.knownRevealDistanceMultiplier);
            float rMul = Mathf.Max(kMul, config.rumoredRevealDistanceMultiplier);

            LotData homeLot = CitySpatialDiscovery.FindLotContaining(city, planCells);
            int homeDistrictId = homeLot?.DistrictId ?? -1;
            DistrictData homeDistrict = CitySpatialDiscovery.FindDistrict(city, homeDistrictId);

            if (homeDistrict == null)
                homeDistrict = FindNearestDistrictForEntry(city, planCells, dRad * kMul * 1.15f);

            if (homeDistrict != null)
            {
                homeDistrictId = homeDistrict.Id;
                DiscoveryReveal.ApplyProximityRevealDistrict(homeDistrict);
            }

            foreach (DistrictData d in city.Districts)
            {
                if (d.Discovery == null || d.Id == homeDistrictId)
                    continue;
                float dist = Vector2.Distance(planCells, d.CenterPosition);
                if (dist <= dRad * kMul)
                    DiscoveryReveal.RevealDistrictTo(d, DiscoveryState.Known);
                else if (dist <= dRad * rMul)
                    DiscoveryReveal.ApplyRumorDistrict(d);
            }

            float bInner = bRad * kMul;
            float bOuter = bRad * rMul;
            foreach (BuildingData b in city.Buildings)
            {
                if (b.Discovery == null)
                    continue;
                float dist = Vector2.Distance(planCells, b.FootprintCenter);
                LotData lot = CitySpatialDiscovery.FindLot(city, b.LotId);
                bool streetFacing = lot != null && (lot.TouchesRoad || lot.SupportsStreetFacingBuilding);

                if (b.IsUndeveloped)
                {
                    if (dist <= bOuter * 0.55f)
                        DiscoveryReveal.ApplyRumorBuilding(b);
                    continue;
                }

                if (dist <= bInner && streetFacing)
                    DiscoveryReveal.ApplyProximityRevealBuilding(b);
                else if (dist <= bOuter && streetFacing)
                    DiscoveryReveal.ApplyRumorBuilding(b);
            }

            float iInner = iRad * kMul;
            float iOuter = iRad * rMul;
            float lowProfileScale = Mathf.Max(0.08f, config.lowProfileInstitutionKnownRadiusScale);
            foreach (InstitutionData inst in city.Institutions)
            {
                if (inst.Discovery == null)
                    continue;
                float dist = Vector2.Distance(planCells, inst.Position);
                if (dist > iOuter)
                    continue;

                bool low = IsLowProfileInstitution(inst.Kind);
                if (!low)
                {
                    if (dist <= iInner)
                        DiscoveryReveal.RevealInstitutionTo(inst, DiscoveryState.Known);
                    else
                        DiscoveryReveal.ApplyRumorInstitution(inst);
                    continue;
                }

                float vClose = iInner * lowProfileScale;
                if (dist <= vClose * 0.42f)
                    DiscoveryReveal.RevealInstitutionTo(inst, DiscoveryState.Known);
                else
                    DiscoveryReveal.ApplyRumorInstitution(inst);
            }
        }

        static bool IsLowProfileInstitution(InstitutionKind k) =>
            k == InstitutionKind.Prison || k == InstitutionKind.FederalOffice;

        /// <summary>When plan position is not inside any lot (gap / water), treat nearest district center within range as entry.</summary>
        static DistrictData FindNearestDistrictForEntry(CityData city, Vector2 planCells, float maxCenterDist)
        {
            foreach (DistrictData d in city.Districts)
            {
                if (d.Discovery == null)
                    continue;
                if (CitySpatialDiscovery.PointInDistrictOutline(d, planCells))
                    return d;
            }

            DistrictData nearest = null;
            float bestD = float.MaxValue;
            foreach (DistrictData d in city.Districts)
            {
                if (d.Discovery == null)
                    continue;
                float dd = Vector2.Distance(planCells, d.CenterPosition);
                if (dd < bestD)
                {
                    bestD = dd;
                    nearest = d;
                }
            }

            return nearest != null && bestD <= maxCenterDist ? nearest : null;
        }
    }

    /// <summary>Lot / district lookups in plan cell space.</summary>
    public static class CitySpatialDiscovery
    {
        public static LotData FindLotContaining(CityData city, Vector2 p)
        {
            foreach (LotData l in city.Lots)
            {
                if (p.x >= l.Min.x && p.x <= l.Max.x && p.y >= l.Min.y && p.y <= l.Max.y)
                    return l;
            }

            return null;
        }

        public static LotData FindLot(CityData city, int lotId)
        {
            foreach (LotData l in city.Lots)
            {
                if (l.Id == lotId)
                    return l;
            }

            return null;
        }

        public static DistrictData FindDistrict(CityData city, int districtId)
        {
            if (districtId < 0)
                return null;
            foreach (DistrictData d in city.Districts)
            {
                if (d.Id == districtId)
                    return d;
            }

            return null;
        }

        /// <summary>True if point lies inside district outline when valid, else false.</summary>
        public static bool PointInDistrictOutline(DistrictData d, Vector2 p)
        {
            if (d?.Outline == null || d.Outline.Count < 3)
                return false;
            return PolygonUtility.PointInPolygon(p, d.Outline);
        }
    }
}
