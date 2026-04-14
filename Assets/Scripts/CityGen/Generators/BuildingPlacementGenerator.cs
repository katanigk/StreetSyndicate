using System.Collections.Generic;
using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 7: district-weighted, suitability-filtered regular buildings / undeveloped parcels on non-reserved lots.
    /// </summary>
    public sealed class BuildingPlacementGenerator
    {
        struct WeightEntry
        {
            public BuildingKind Kind;
            public int Weight;
        }

        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            city.Buildings.Clear();
            foreach (LotData lot in city.Lots)
                lot.RegularBuildingId = -1;

            if (!config.placeRegularBuildings || city.Lots.Count == 0)
                return;

            int nextBuildingId = 0;

            foreach (LotData lot in city.Lots)
            {
                if (lot.IsReserved)
                    continue;

                Vector2 center = (lot.Min + lot.Max) * 0.5f;
                float railD = MinDistToRail(city, center);
                float waterD = MinDistToWater(city, center);

                int mix = DeterministicHash.Mix(city.Seed, DeterministicHash.Mix(lot.Id, unchecked((int)0xB17D7U)));
                System.Random rng = stageSeed.Fork(mix).CreateSystemRandom();

                List<WeightEntry> table = GetDistrictWeightTable(lot.DistrictKind);
                ApplyUndevelopedScale(table, config.regularBuildingUndevelopedWeightScale);

                var filtered = new List<WeightEntry>(table.Count);
                for (int i = 0; i < table.Count; i++)
                {
                    WeightEntry e = table[i];
                    if (e.Weight <= 0)
                        continue;
                    if (!IsSuitable(e.Kind, lot, lot.DistrictKind, railD, waterD, config))
                        continue;
                    filtered.Add(e);
                }

                if (filtered.Count == 0)
                    filtered.Add(new WeightEntry { Kind = BuildingKind.EmptyLot, Weight = 1 });

                BuildingKind picked = PickWeighted(filtered, rng, out _);
                float placementScore = ComputePlacementScore(picked, lot, railD, waterD, config);

                BuildingData b = CreateBuildingData(nextBuildingId++, picked, lot, placementScore, rng, config);
                city.Buildings.Add(b);
                lot.RegularBuildingId = b.Id;
            }
        }

        static void ApplyUndevelopedScale(List<WeightEntry> table, float scale)
        {
            if (scale <= 0f || Mathf.Approximately(scale, 1f))
                return;
            for (int i = 0; i < table.Count; i++)
            {
                WeightEntry e = table[i];
                if (IsUndevelopedKind(e.Kind))
                {
                    e.Weight = Mathf.Max(0, Mathf.RoundToInt(e.Weight * scale));
                    table[i] = e;
                }
            }
        }

        static bool IsUndevelopedKind(BuildingKind k) =>
            k == BuildingKind.EmptyLot || k == BuildingKind.VacantParcel || k == BuildingKind.Yard ||
            k == BuildingKind.ReservedFutureParcel;

        static BuildingKind PickWeighted(List<WeightEntry> entries, System.Random rng, out int totalWeight)
        {
            totalWeight = 0;
            for (int i = 0; i < entries.Count; i++)
                totalWeight += entries[i].Weight;
            if (totalWeight <= 0)
                return BuildingKind.EmptyLot;
            int r = rng.Next(totalWeight);
            int acc = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                acc += entries[i].Weight;
                if (r < acc)
                    return entries[i].Kind;
            }

            return entries[^1].Kind;
        }

        static float ComputePlacementScore(BuildingKind kind, LotData lot, float railD, float waterD,
            CityGenerationConfig config)
        {
            float s = lot.AccessibilityScore * 12f + lot.FrontageLength * 0.55f + lot.AreaCells * 0.04f;
            if (lot.TouchesRoad)
                s += 4f;
            if (lot.HasMajorRoadFrontage)
                s += 6f;

            switch (kind)
            {
                case BuildingKind.RailUtility:
                    s += (1f - Mathf.Clamp01(railD / Mathf.Max(1f, config.regularBuildingRailContextMaxCells))) * 18f;
                    break;
                case BuildingKind.DockFreight:
                    s += (1f - Mathf.Clamp01(waterD / Mathf.Max(1f, config.regularBuildingWaterContextMaxCells))) * 16f;
                    break;
                case BuildingKind.StorageYard when lot.DistrictKind == DistrictKind.DocksPort:
                    s += (1f - Mathf.Clamp01(waterD / Mathf.Max(1f, config.regularBuildingWaterContextMaxCells))) * 16f;
                    break;
                case BuildingKind.Warehouse:
                case BuildingKind.Workshop:
                case BuildingKind.MachineShop:
                    s += lot.SupportsLargeBuilding ? 5f : 0f;
                    break;
            }

            return s;
        }

        static BuildingData CreateBuildingData(int id, BuildingKind kind, LotData lot, float placementScore,
            System.Random rng, CityGenerationConfig config)
        {
            Vector2 lotSize = lot.Max - lot.Min;
            float shrink = 0.72f + (float)rng.NextDouble() * 0.22f;
            if (IsUndevelopedKind(kind))
                shrink *= 0.35f + (float)rng.NextDouble() * 0.25f;
            if (kind == BuildingKind.House && lot.DistrictKind == DistrictKind.Wealthy)
                shrink = Mathf.Min(0.98f, shrink + 0.08f);

            var b = new BuildingData
            {
                Id = id,
                Kind = kind,
                Category = CategoryForKind(kind),
                LegalProfile = LegalForKind(kind),
                LotId = lot.Id,
                BlockId = lot.BlockId,
                DistrictId = lot.DistrictId,
                DistrictKind = lot.DistrictKind,
                FootprintCenter = (lot.Min + lot.Max) * 0.5f,
                FootprintSize = new Vector2(
                    Mathf.Max(0.5f, lotSize.x * shrink),
                    Mathf.Max(0.5f, lotSize.y * shrink)),
                FrontBusinessType = FrontLabel(kind),
                PlacementScore = placementScore
            };

            ApplyHiddenUseMetadata(b, kind, rng);
            return b;
        }

        static void ApplyHiddenUseMetadata(BuildingData b, BuildingKind kind, System.Random rng)
        {
            b.CanSupportFrontBusiness = kind is BuildingKind.BarTavern or BuildingKind.Grocery or BuildingKind.Butcher
                or BuildingKind.Bakery or BuildingKind.Tailor or BuildingKind.PawnShop or BuildingKind.GeneralStore
                or BuildingKind.MixedUseCommercialResidential or BuildingKind.CornerService;

            b.CanSupportStorage = kind is BuildingKind.Warehouse or BuildingKind.StorageYard or BuildingKind.DockFreight
                or BuildingKind.Workshop or BuildingKind.MachineShop or BuildingKind.Garage or BuildingKind.RailUtility
                or BuildingKind.Yard or BuildingKind.VacantParcel;

            b.CanSupportBackroom = kind is BuildingKind.BarTavern or BuildingKind.PawnShop or BuildingKind.Tailor
                or BuildingKind.GeneralStore or BuildingKind.Workshop or BuildingKind.MixedUseCommercialResidential
                or BuildingKind.CornerService or BuildingKind.FinanceOffice;

            float baseHidden = kind switch
            {
                BuildingKind.BarTavern => 0.55f,
                BuildingKind.PawnShop => 0.5f,
                BuildingKind.Warehouse => 0.35f,
                BuildingKind.DockFreight => 0.4f,
                BuildingKind.Workshop => 0.42f,
                BuildingKind.Tenement => 0.38f,
                BuildingKind.MixedUseCommercialResidential => 0.48f,
                BuildingKind.VacantParcel => 0.62f,
                BuildingKind.Yard => 0.45f,
                BuildingKind.EmptyLot => 0.25f,
                _ => 0.22f
            };

            b.HiddenUsePotential = Mathf.Clamp01(baseHidden + (float)rng.NextDouble() * 0.2f);
            b.GameplayTagsPlaceholder = $"batch7;kind:{kind};cat:{b.Category};legal:{b.LegalProfile}";
        }

        static List<WeightEntry> GetDistrictWeightTable(DistrictKind d)
        {
            var list = new List<WeightEntry>(48);
            void Add(BuildingKind k, int w)
            {
                if (w > 0)
                    list.Add(new WeightEntry { Kind = k, Weight = w });
            }

            switch (d)
            {
                case DistrictKind.DowntownCommercial:
                    Add(BuildingKind.EmptyLot, 5);
                    Add(BuildingKind.VacantParcel, 4);
                    Add(BuildingKind.Yard, 3);
                    Add(BuildingKind.ReservedFutureParcel, 2);
                    Add(BuildingKind.BarTavern, 14);
                    Add(BuildingKind.Office, 12);
                    Add(BuildingKind.Tailor, 9);
                    Add(BuildingKind.Bakery, 11);
                    Add(BuildingKind.Butcher, 8);
                    Add(BuildingKind.PawnShop, 8);
                    Add(BuildingKind.MixedUseCommercialResidential, 13);
                    Add(BuildingKind.ApartmentBuilding, 11);
                    Add(BuildingKind.FinanceOffice, 7);
                    Add(BuildingKind.GeneralStore, 10);
                    Add(BuildingKind.CornerService, 7);
                    Add(BuildingKind.Clinic, 5);
                    Add(BuildingKind.SmallServiceOffice, 6);
                    Add(BuildingKind.Grocery, 8);
                    break;

                case DistrictKind.Industrial:
                    Add(BuildingKind.EmptyLot, 4);
                    Add(BuildingKind.VacantParcel, 3);
                    Add(BuildingKind.Yard, 5);
                    Add(BuildingKind.ReservedFutureParcel, 2);
                    Add(BuildingKind.Warehouse, 18);
                    Add(BuildingKind.Workshop, 15);
                    Add(BuildingKind.MachineShop, 12);
                    Add(BuildingKind.Garage, 11);
                    Add(BuildingKind.StorageYard, 15);
                    Add(BuildingKind.RailUtility, 10);
                    Add(BuildingKind.DockFreight, 6);
                    Add(BuildingKind.House, 4);
                    Add(BuildingKind.Tenement, 3);
                    Add(BuildingKind.ApartmentBuilding, 4);
                    Add(BuildingKind.Office, 3);
                    Add(BuildingKind.BarTavern, 4);
                    break;

                case DistrictKind.WorkingClass:
                    Add(BuildingKind.EmptyLot, 6);
                    Add(BuildingKind.VacantParcel, 5);
                    Add(BuildingKind.Yard, 4);
                    Add(BuildingKind.ReservedFutureParcel, 2);
                    Add(BuildingKind.BarTavern, 13);
                    Add(BuildingKind.Grocery, 15);
                    Add(BuildingKind.Butcher, 9);
                    Add(BuildingKind.Workshop, 10);
                    Add(BuildingKind.Tenement, 13);
                    Add(BuildingKind.ApartmentBuilding, 9);
                    Add(BuildingKind.House, 11);
                    Add(BuildingKind.Garage, 10);
                    Add(BuildingKind.Bakery, 8);
                    Add(BuildingKind.CornerService, 7);
                    Add(BuildingKind.PawnShop, 6);
                    Add(BuildingKind.GeneralStore, 8);
                    Add(BuildingKind.Clinic, 5);
                    break;

                case DistrictKind.Residential:
                    Add(BuildingKind.EmptyLot, 8);
                    Add(BuildingKind.VacantParcel, 6);
                    Add(BuildingKind.Yard, 7);
                    Add(BuildingKind.ReservedFutureParcel, 3);
                    Add(BuildingKind.House, 17);
                    Add(BuildingKind.ApartmentBuilding, 11);
                    Add(BuildingKind.Clinic, 7);
                    Add(BuildingKind.Grocery, 9);
                    Add(BuildingKind.Bakery, 9);
                    Add(BuildingKind.CornerService, 8);
                    Add(BuildingKind.SmallServiceOffice, 5);
                    Add(BuildingKind.Tailor, 4);
                    Add(BuildingKind.Garage, 6);
                    break;

                case DistrictKind.Wealthy:
                    Add(BuildingKind.EmptyLot, 7);
                    Add(BuildingKind.VacantParcel, 5);
                    Add(BuildingKind.Yard, 8);
                    Add(BuildingKind.ReservedFutureParcel, 4);
                    Add(BuildingKind.House, 20);
                    Add(BuildingKind.MixedUseCommercialResidential, 10);
                    Add(BuildingKind.Office, 9);
                    Add(BuildingKind.FinanceOffice, 8);
                    Add(BuildingKind.Tailor, 7);
                    Add(BuildingKind.Bakery, 8);
                    Add(BuildingKind.GeneralStore, 6);
                    Add(BuildingKind.Clinic, 9);
                    Add(BuildingKind.CornerService, 5);
                    Add(BuildingKind.ApartmentBuilding, 6);
                    Add(BuildingKind.Grocery, 5);
                    break;

                case DistrictKind.DocksPort:
                    Add(BuildingKind.EmptyLot, 5);
                    Add(BuildingKind.VacantParcel, 4);
                    Add(BuildingKind.Yard, 6);
                    Add(BuildingKind.ReservedFutureParcel, 2);
                    Add(BuildingKind.Warehouse, 14);
                    Add(BuildingKind.StorageYard, 13);
                    Add(BuildingKind.DockFreight, 15);
                    Add(BuildingKind.BarTavern, 9);
                    Add(BuildingKind.Workshop, 11);
                    Add(BuildingKind.RailUtility, 7);
                    Add(BuildingKind.MixedUseCommercialResidential, 9);
                    Add(BuildingKind.Tenement, 8);
                    Add(BuildingKind.Garage, 9);
                    Add(BuildingKind.MachineShop, 8);
                    Add(BuildingKind.Grocery, 6);
                    Add(BuildingKind.PawnShop, 5);
                    break;

                case DistrictKind.FringeOuterEdge:
                    Add(BuildingKind.EmptyLot, 14);
                    Add(BuildingKind.VacantParcel, 12);
                    Add(BuildingKind.Yard, 11);
                    Add(BuildingKind.ReservedFutureParcel, 7);
                    Add(BuildingKind.House, 9);
                    Add(BuildingKind.Workshop, 7);
                    Add(BuildingKind.StorageYard, 10);
                    Add(BuildingKind.Garage, 6);
                    Add(BuildingKind.SmallServiceOffice, 3);
                    Add(BuildingKind.Grocery, 4);
                    Add(BuildingKind.Tenement, 5);
                    Add(BuildingKind.Warehouse, 5);
                    break;

                default:
                    Add(BuildingKind.EmptyLot, 8);
                    Add(BuildingKind.House, 10);
                    Add(BuildingKind.Grocery, 8);
                    Add(BuildingKind.Workshop, 6);
                    Add(BuildingKind.Garage, 6);
                    break;
            }

            return list;
        }

        static bool IsSuitable(BuildingKind k, LotData lot, DistrictKind district, float railD,
            float waterD, CityGenerationConfig config)
        {
            if (IsUndevelopedKind(k))
                return true;

            bool streetOk = lot.TouchesRoad || lot.SupportsStreetFacingBuilding ||
                            lot.FrontageLength >= config.lotMinStreetFrontageCells * 0.72f;
            bool mediumLot = lot.SizeClass == LotSizeClass.Medium || lot.SizeClass == LotSizeClass.Large ||
                              lot.SizeClass == LotSizeClass.Oversize;
            bool largeLot = lot.SizeClass == LotSizeClass.Large || lot.SizeClass == LotSizeClass.Oversize;
            float largeArea = config.lotLargeBuildingMinAreaCells * 0.88f;

            switch (k)
            {
                case BuildingKind.BarTavern:
                case BuildingKind.Grocery:
                case BuildingKind.Butcher:
                case BuildingKind.Bakery:
                case BuildingKind.Tailor:
                case BuildingKind.PawnShop:
                case BuildingKind.GeneralStore:
                case BuildingKind.CornerService:
                    return streetOk;

                case BuildingKind.Office:
                case BuildingKind.FinanceOffice:
                    if (district == DistrictKind.FringeOuterEdge &&
                        lot.AccessibilityScore < 0.38f && !lot.HasMajorRoadFrontage)
                        return false;
                    return streetOk || lot.AccessibilityScore >= 0.22f;

                case BuildingKind.MixedUseCommercialResidential:
                    return streetOk && (mediumLot || lot.AreaCells >= 32f);

                case BuildingKind.Warehouse:
                    if (district == DistrictKind.Wealthy)
                        return false;
                    return largeLot || lot.AreaCells >= largeArea || (lot.SupportsLargeBuilding && lot.AreaCells >= 38f);

                case BuildingKind.StorageYard:
                    if (district == DistrictKind.Wealthy)
                        return false;
                    return largeLot || lot.SupportsBackLotUse || lot.AreaCells >= config.lotBackUseMinAreaCells * 0.85f ||
                           district == DistrictKind.Industrial || district == DistrictKind.DocksPort ||
                           district == DistrictKind.FringeOuterEdge;

                case BuildingKind.Workshop:
                case BuildingKind.MachineShop:
                    if (district == DistrictKind.Wealthy && !lot.SupportsBackLotUse && lot.AreaCells < 40f)
                        return false;
                    return mediumLot || lot.AreaCells >= 28f || district == DistrictKind.Industrial;

                case BuildingKind.Garage:
                    return lot.AreaCells >= 18f || mediumLot;

                case BuildingKind.RailUtility:
                    return railD <= config.regularBuildingRailContextMaxCells * 1.15f ||
                           (district == DistrictKind.Industrial && railD <= 200f);

                case BuildingKind.DockFreight:
                    return district == DistrictKind.DocksPort ||
                           waterD <= config.regularBuildingWaterContextMaxCells * 1.05f ||
                           (district == DistrictKind.Industrial && waterD <= 160f);

                case BuildingKind.ApartmentBuilding:
                    return mediumLot || lot.AreaCells >= 36f ||
                           (district == DistrictKind.WorkingClass && lot.AreaCells >= 30f);

                case BuildingKind.Tenement:
                    return mediumLot || lot.AreaCells >= 26f ||
                           district == DistrictKind.WorkingClass || district == DistrictKind.DocksPort;

                case BuildingKind.House:
                    if (lot.SizeClass == LotSizeClass.Unknown && lot.AreaCells < 14f)
                        return false;
                    return lot.AreaCells >= 12f;

                case BuildingKind.Clinic:
                    return lot.AccessibilityScore >= 0.17f || (streetOk && lot.AccessibilityScore >= 0.12f);

                case BuildingKind.SmallServiceOffice:
                    return streetOk || lot.AccessibilityScore >= 0.18f;

                default:
                    return true;
            }
        }

        static BuildingCategory CategoryForKind(BuildingKind k)
        {
            if (IsUndevelopedKind(k))
                return BuildingCategory.Undeveloped;
            if (k == BuildingKind.MixedUseCommercialResidential)
                return BuildingCategory.MixedUse;
            if (k >= BuildingKind.BarTavern && k <= BuildingKind.GeneralStore)
                return BuildingCategory.Commercial;
            if (k >= BuildingKind.Warehouse && k <= BuildingKind.DockFreight)
                return BuildingCategory.Industrial;
            if (k >= BuildingKind.House && k <= BuildingKind.Tenement)
                return BuildingCategory.Residential;
            if (k >= BuildingKind.Clinic && k <= BuildingKind.CornerService)
                return BuildingCategory.Civic;
            return BuildingCategory.Commercial;
        }

        static BuildingLegalProfile LegalForKind(BuildingKind k)
        {
            if (IsUndevelopedKind(k))
                return BuildingLegalProfile.Vacant;
            if (k == BuildingKind.MixedUseCommercialResidential)
                return BuildingLegalProfile.MixedUse;
            if (k >= BuildingKind.Warehouse && k <= BuildingKind.DockFreight)
                return BuildingLegalProfile.Industrial;
            if (k >= BuildingKind.House && k <= BuildingKind.Tenement)
                return BuildingLegalProfile.Residential;
            if (k >= BuildingKind.Clinic && k <= BuildingKind.CornerService)
                return BuildingLegalProfile.CivicService;
            return BuildingLegalProfile.Commercial;
        }

        static string FrontLabel(BuildingKind k) =>
            k switch
            {
                BuildingKind.EmptyLot => "Empty lot",
                BuildingKind.VacantParcel => "Vacant parcel",
                BuildingKind.Yard => "Yard",
                BuildingKind.ReservedFutureParcel => "Future parcel",
                BuildingKind.BarTavern => "Bar / Tavern",
                BuildingKind.Grocery => "Grocery",
                BuildingKind.Butcher => "Butcher",
                BuildingKind.Bakery => "Bakery",
                BuildingKind.Tailor => "Tailor",
                BuildingKind.PawnShop => "Pawn shop",
                BuildingKind.Office => "Office",
                BuildingKind.FinanceOffice => "Finance office",
                BuildingKind.GeneralStore => "General store",
                BuildingKind.Warehouse => "Warehouse",
                BuildingKind.Workshop => "Workshop",
                BuildingKind.MachineShop => "Machine shop",
                BuildingKind.Garage => "Garage",
                BuildingKind.StorageYard => "Storage yard",
                BuildingKind.RailUtility => "Rail utility",
                BuildingKind.DockFreight => "Dock freight",
                BuildingKind.House => "House",
                BuildingKind.ApartmentBuilding => "Apartment building",
                BuildingKind.Tenement => "Tenement",
                BuildingKind.MixedUseCommercialResidential => "Mixed-use (shop + residence)",
                BuildingKind.Clinic => "Clinic",
                BuildingKind.SmallServiceOffice => "Service office",
                BuildingKind.CornerService => "Corner service",
                _ => "Building"
            };

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

            return best >= float.MaxValue - 1f ? CityDiagonal(city) : best;
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

            return best >= float.MaxValue - 1f ? CityDiagonal(city) : best;
        }

        static float CityDiagonal(CityData city)
        {
            Vector2 d = city.Boundary.Max - city.Boundary.Min;
            return Mathf.Max(1e-4f, Mathf.Sqrt(d.x * d.x + d.y * d.y));
        }
    }
}
