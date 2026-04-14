using FamilyBusiness.CityGen.Core;
using FamilyBusiness.CityGen.Data;
using FamilyBusiness.CityGen.Utils;
using UnityEngine;

namespace FamilyBusiness.CityGen.Generators
{
    /// <summary>
    /// Batch 8: assigns heuristic crime-facing metadata to each placed regular building (deterministic).
    /// </summary>
    public sealed class BuildingCrimePotentialGenerator
    {
        public void Generate(CityData city, CityGenerationConfig config, CitySeed stageSeed)
        {
            if (!config.computeBuildingCrimePotential || city.Buildings.Count == 0)
            {
                foreach (BuildingData b in city.Buildings)
                    b.Crime = null;
                return;
            }

            foreach (BuildingData b in city.Buildings)
            {
                LotData lot = FindLot(city, b.LotId);
                int mix = DeterministicHash.Mix(city.Seed, DeterministicHash.Mix(b.Id, unchecked((int)0xC81BEU)));
                System.Random rng = stageSeed.Fork(mix).CreateSystemRandom();

                var crime = new BuildingCrimeProfileData();
                ApplyBaseByKind(b.Kind, crime);
                ApplyDistrictModifiers(b.DistrictKind, b.Kind, crime);
                if (lot != null)
                    ApplyLotModifiers(lot, b.Kind, crime);
                ApplyDeterministicJitter(crime, rng, config.crimePotentialJitterAmplitude);
                ClampAll(crime);
                DeriveFlags(crime, b.DistrictKind, b.Kind);
                b.Crime = crime;
            }
        }

        static LotData FindLot(CityData city, int lotId)
        {
            foreach (LotData l in city.Lots)
            {
                if (l.Id == lotId)
                    return l;
            }

            return null;
        }

        static void ApplyBaseByKind(BuildingKind k, BuildingCrimeProfileData p)
        {
            switch (k)
            {
                case BuildingKind.BarTavern:
                    p.FrontBusinessPotential = 0.78f;
                    p.StoragePotential = 0.36f;
                    p.BackroomPotential = 0.52f;
                    p.LaunderingPotential = 0.46f;
                    p.ExtortionPotential = 0.56f;
                    p.BlackMarketSuitability = 0.42f;
                    p.MeetingPotential = 0.82f;
                    p.PoliceVisibility = 0.56f;
                    p.NeighborhoodInfluenceValue = 0.52f;
                    p.LogisticsValue = 0.36f;
                    break;

                case BuildingKind.Grocery:
                case BuildingKind.Bakery:
                case BuildingKind.Tailor:
                    p.FrontBusinessPotential = 0.66f;
                    p.StoragePotential = 0.34f;
                    p.BackroomPotential = 0.42f;
                    p.LaunderingPotential = 0.44f;
                    p.ExtortionPotential = 0.48f;
                    p.BlackMarketSuitability = 0.36f;
                    p.MeetingPotential = 0.42f;
                    p.PoliceVisibility = 0.48f;
                    p.NeighborhoodInfluenceValue = 0.46f;
                    p.LogisticsValue = 0.42f;
                    break;

                case BuildingKind.Butcher:
                    p.FrontBusinessPotential = 0.64f;
                    p.StoragePotential = 0.42f;
                    p.BackroomPotential = 0.44f;
                    p.LaunderingPotential = 0.43f;
                    p.ExtortionPotential = 0.5f;
                    p.BlackMarketSuitability = 0.38f;
                    p.MeetingPotential = 0.4f;
                    p.PoliceVisibility = 0.5f;
                    p.NeighborhoodInfluenceValue = 0.44f;
                    p.LogisticsValue = 0.4f;
                    break;

                case BuildingKind.PawnShop:
                    p.FrontBusinessPotential = 0.58f;
                    p.StoragePotential = 0.46f;
                    p.BackroomPotential = 0.52f;
                    p.LaunderingPotential = 0.52f;
                    p.ExtortionPotential = 0.78f;
                    p.BlackMarketSuitability = 0.88f;
                    p.MeetingPotential = 0.46f;
                    p.PoliceVisibility = 0.62f;
                    p.NeighborhoodInfluenceValue = 0.52f;
                    p.LogisticsValue = 0.42f;
                    break;

                case BuildingKind.GeneralStore:
                case BuildingKind.CornerService:
                    p.FrontBusinessPotential = 0.62f;
                    p.StoragePotential = 0.38f;
                    p.BackroomPotential = 0.46f;
                    p.LaunderingPotential = 0.42f;
                    p.ExtortionPotential = 0.46f;
                    p.BlackMarketSuitability = 0.4f;
                    p.MeetingPotential = 0.44f;
                    p.PoliceVisibility = 0.5f;
                    p.NeighborhoodInfluenceValue = 0.48f;
                    p.LogisticsValue = 0.44f;
                    break;

                case BuildingKind.Office:
                    p.FrontBusinessPotential = 0.56f;
                    p.StoragePotential = 0.26f;
                    p.BackroomPotential = 0.36f;
                    p.LaunderingPotential = 0.72f;
                    p.ExtortionPotential = 0.42f;
                    p.BlackMarketSuitability = 0.34f;
                    p.MeetingPotential = 0.58f;
                    p.PoliceVisibility = 0.54f;
                    p.NeighborhoodInfluenceValue = 0.56f;
                    p.LogisticsValue = 0.42f;
                    break;

                case BuildingKind.FinanceOffice:
                    p.FrontBusinessPotential = 0.54f;
                    p.StoragePotential = 0.24f;
                    p.BackroomPotential = 0.34f;
                    p.LaunderingPotential = 0.86f;
                    p.ExtortionPotential = 0.4f;
                    p.BlackMarketSuitability = 0.32f;
                    p.MeetingPotential = 0.52f;
                    p.PoliceVisibility = 0.58f;
                    p.NeighborhoodInfluenceValue = 0.58f;
                    p.LogisticsValue = 0.4f;
                    break;

                case BuildingKind.Warehouse:
                case BuildingKind.StorageYard:
                    p.FrontBusinessPotential = 0.2f;
                    p.StoragePotential = 0.94f;
                    p.BackroomPotential = 0.48f;
                    p.LaunderingPotential = 0.34f;
                    p.ExtortionPotential = 0.36f;
                    p.BlackMarketSuitability = 0.58f;
                    p.MeetingPotential = 0.34f;
                    p.PoliceVisibility = 0.46f;
                    p.NeighborhoodInfluenceValue = 0.4f;
                    p.LogisticsValue = 0.86f;
                    break;

                case BuildingKind.Workshop:
                case BuildingKind.MachineShop:
                case BuildingKind.Garage:
                    p.FrontBusinessPotential = 0.36f;
                    p.StoragePotential = 0.56f;
                    p.BackroomPotential = 0.76f;
                    p.LaunderingPotential = 0.38f;
                    p.ExtortionPotential = 0.44f;
                    p.BlackMarketSuitability = 0.66f;
                    p.MeetingPotential = 0.46f;
                    p.PoliceVisibility = 0.5f;
                    p.NeighborhoodInfluenceValue = 0.42f;
                    p.LogisticsValue = 0.58f;
                    break;

                case BuildingKind.RailUtility:
                    p.FrontBusinessPotential = 0.28f;
                    p.StoragePotential = 0.62f;
                    p.BackroomPotential = 0.44f;
                    p.LaunderingPotential = 0.32f;
                    p.ExtortionPotential = 0.34f;
                    p.BlackMarketSuitability = 0.52f;
                    p.MeetingPotential = 0.36f;
                    p.PoliceVisibility = 0.48f;
                    p.NeighborhoodInfluenceValue = 0.38f;
                    p.LogisticsValue = 0.78f;
                    break;

                case BuildingKind.DockFreight:
                    p.FrontBusinessPotential = 0.24f;
                    p.StoragePotential = 0.88f;
                    p.BackroomPotential = 0.46f;
                    p.LaunderingPotential = 0.4f;
                    p.ExtortionPotential = 0.38f;
                    p.BlackMarketSuitability = 0.82f;
                    p.MeetingPotential = 0.38f;
                    p.PoliceVisibility = 0.5f;
                    p.NeighborhoodInfluenceValue = 0.44f;
                    p.LogisticsValue = 0.9f;
                    break;

                case BuildingKind.House:
                    p.FrontBusinessPotential = 0.28f;
                    p.StoragePotential = 0.38f;
                    p.BackroomPotential = 0.52f;
                    p.LaunderingPotential = 0.4f;
                    p.ExtortionPotential = 0.32f;
                    p.BlackMarketSuitability = 0.34f;
                    p.MeetingPotential = 0.56f;
                    p.PoliceVisibility = 0.38f;
                    p.NeighborhoodInfluenceValue = 0.42f;
                    p.LogisticsValue = 0.28f;
                    break;

                case BuildingKind.ApartmentBuilding:
                    p.FrontBusinessPotential = 0.3f;
                    p.StoragePotential = 0.42f;
                    p.BackroomPotential = 0.54f;
                    p.LaunderingPotential = 0.44f;
                    p.ExtortionPotential = 0.4f;
                    p.BlackMarketSuitability = 0.36f;
                    p.MeetingPotential = 0.58f;
                    p.PoliceVisibility = 0.44f;
                    p.NeighborhoodInfluenceValue = 0.5f;
                    p.LogisticsValue = 0.32f;
                    break;

                case BuildingKind.Tenement:
                    p.FrontBusinessPotential = 0.32f;
                    p.StoragePotential = 0.44f;
                    p.BackroomPotential = 0.58f;
                    p.LaunderingPotential = 0.42f;
                    p.ExtortionPotential = 0.48f;
                    p.BlackMarketSuitability = 0.4f;
                    p.MeetingPotential = 0.6f;
                    p.PoliceVisibility = 0.46f;
                    p.NeighborhoodInfluenceValue = 0.55f;
                    p.LogisticsValue = 0.34f;
                    break;

                case BuildingKind.MixedUseCommercialResidential:
                    p.FrontBusinessPotential = 0.68f;
                    p.StoragePotential = 0.42f;
                    p.BackroomPotential = 0.58f;
                    p.LaunderingPotential = 0.52f;
                    p.ExtortionPotential = 0.48f;
                    p.BlackMarketSuitability = 0.46f;
                    p.MeetingPotential = 0.55f;
                    p.PoliceVisibility = 0.52f;
                    p.NeighborhoodInfluenceValue = 0.54f;
                    p.LogisticsValue = 0.44f;
                    break;

                case BuildingKind.Clinic:
                    p.FrontBusinessPotential = 0.52f;
                    p.StoragePotential = 0.3f;
                    p.BackroomPotential = 0.38f;
                    p.LaunderingPotential = 0.56f;
                    p.ExtortionPotential = 0.34f;
                    p.BlackMarketSuitability = 0.32f;
                    p.MeetingPotential = 0.42f;
                    p.PoliceVisibility = 0.62f;
                    p.NeighborhoodInfluenceValue = 0.48f;
                    p.LogisticsValue = 0.36f;
                    break;

                case BuildingKind.SmallServiceOffice:
                    p.FrontBusinessPotential = 0.5f;
                    p.StoragePotential = 0.28f;
                    p.BackroomPotential = 0.4f;
                    p.LaunderingPotential = 0.54f;
                    p.ExtortionPotential = 0.36f;
                    p.BlackMarketSuitability = 0.3f;
                    p.MeetingPotential = 0.46f;
                    p.PoliceVisibility = 0.55f;
                    p.NeighborhoodInfluenceValue = 0.44f;
                    p.LogisticsValue = 0.34f;
                    break;

                case BuildingKind.EmptyLot:
                case BuildingKind.VacantParcel:
                    p.FrontBusinessPotential = 0.12f;
                    p.StoragePotential = 0.28f;
                    p.BackroomPotential = 0.32f;
                    p.LaunderingPotential = 0.18f;
                    p.ExtortionPotential = 0.22f;
                    p.BlackMarketSuitability = 0.28f;
                    p.MeetingPotential = 0.36f;
                    p.PoliceVisibility = 0.22f;
                    p.NeighborhoodInfluenceValue = 0.18f;
                    p.LogisticsValue = 0.22f;
                    break;

                case BuildingKind.Yard:
                    p.FrontBusinessPotential = 0.14f;
                    p.StoragePotential = 0.48f;
                    p.BackroomPotential = 0.38f;
                    p.LaunderingPotential = 0.2f;
                    p.ExtortionPotential = 0.24f;
                    p.BlackMarketSuitability = 0.34f;
                    p.MeetingPotential = 0.42f;
                    p.PoliceVisibility = 0.26f;
                    p.NeighborhoodInfluenceValue = 0.2f;
                    p.LogisticsValue = 0.32f;
                    break;

                case BuildingKind.ReservedFutureParcel:
                    p.FrontBusinessPotential = 0.1f;
                    p.StoragePotential = 0.32f;
                    p.BackroomPotential = 0.34f;
                    p.LaunderingPotential = 0.16f;
                    p.ExtortionPotential = 0.2f;
                    p.BlackMarketSuitability = 0.26f;
                    p.MeetingPotential = 0.38f;
                    p.PoliceVisibility = 0.2f;
                    p.NeighborhoodInfluenceValue = 0.16f;
                    p.LogisticsValue = 0.26f;
                    break;

                default:
                    p.FrontBusinessPotential = 0.4f;
                    p.StoragePotential = 0.4f;
                    p.BackroomPotential = 0.4f;
                    p.LaunderingPotential = 0.4f;
                    p.ExtortionPotential = 0.4f;
                    p.BlackMarketSuitability = 0.4f;
                    p.MeetingPotential = 0.4f;
                    p.PoliceVisibility = 0.45f;
                    p.NeighborhoodInfluenceValue = 0.4f;
                    p.LogisticsValue = 0.4f;
                    break;
            }
        }

        static void ApplyDistrictModifiers(DistrictKind d, BuildingKind kind, BuildingCrimeProfileData p)
        {
            switch (d)
            {
                case DistrictKind.DowntownCommercial:
                    p.PoliceVisibility += 0.12f;
                    p.LaunderingPotential += 0.1f;
                    p.ExtortionPotential += 0.09f;
                    p.NeighborhoodInfluenceValue += 0.11f;
                    p.MeetingPotential += 0.05f;
                    p.FrontBusinessPotential += 0.06f;
                    break;

                case DistrictKind.Industrial:
                    p.StoragePotential += 0.1f;
                    p.LogisticsValue += 0.12f;
                    p.FrontBusinessPotential -= 0.06f;
                    p.NeighborhoodInfluenceValue -= 0.05f;
                    p.BlackMarketSuitability += 0.05f;
                    break;

                case DistrictKind.WorkingClass:
                    p.ExtortionPotential += 0.11f;
                    p.NeighborhoodInfluenceValue += 0.13f;
                    p.MeetingPotential += 0.06f;
                    break;

                case DistrictKind.Residential:
                    p.PoliceVisibility -= 0.08f;
                    p.BackroomPotential += 0.07f;
                    p.MeetingPotential += 0.05f;
                    p.FrontBusinessPotential -= 0.05f;
                    break;

                case DistrictKind.Wealthy:
                    p.LaunderingPotential += 0.13f;
                    p.FrontBusinessPotential += 0.08f;
                    p.PoliceVisibility += 0.07f;
                    p.NeighborhoodInfluenceValue += 0.09f;
                    break;

                case DistrictKind.DocksPort:
                    p.BlackMarketSuitability += 0.15f;
                    p.StoragePotential += 0.09f;
                    p.LogisticsValue += 0.12f;
                    p.MeetingPotential += 0.03f;
                    break;

                case DistrictKind.FringeOuterEdge:
                    p.NeighborhoodInfluenceValue -= 0.09f;
                    p.StoragePotential += 0.07f;
                    p.BackroomPotential += 0.06f;
                    p.PoliceVisibility -= 0.06f;
                    p.MeetingPotential += 0.04f;
                    break;
            }

            if (d == DistrictKind.Residential || d == DistrictKind.Wealthy)
            {
                if (kind == BuildingKind.Clinic || kind == BuildingKind.SmallServiceOffice)
                    p.PoliceVisibility += 0.06f;
            }
        }

        static void ApplyLotModifiers(LotData lot, BuildingKind kind, BuildingCrimeProfileData p)
        {
            if (lot.HasMajorRoadFrontage)
            {
                p.PoliceVisibility += 0.08f;
                p.FrontBusinessPotential += 0.06f;
                p.LogisticsValue += 0.05f;
            }

            float frontNorm = Mathf.Clamp01(lot.FrontageLength / 12f);
            p.FrontBusinessPotential += frontNorm * 0.05f;

            if (lot.AccessibilityScore >= 0.55f)
            {
                p.LogisticsValue += 0.08f;
                p.ExtortionPotential += 0.04f;
                p.PoliceVisibility += 0.04f;
            }
            else if (lot.AccessibilityScore <= 0.35f)
            {
                p.BackroomPotential += 0.06f;
                p.MeetingPotential += 0.04f;
                p.PoliceVisibility -= 0.03f;
            }

            if (lot.SupportsLargeBuilding)
                p.StoragePotential += 0.08f;

            if (lot.SupportsBackLotUse)
            {
                p.BackroomPotential += 0.1f;
                p.BlackMarketSuitability += 0.04f;
                p.StoragePotential += 0.04f;
            }

            if (lot.SizeClass == LotSizeClass.Medium)
            {
                p.StoragePotential += 0.04f;
            }

            if (lot.SizeClass == LotSizeClass.Large || lot.SizeClass == LotSizeClass.Oversize)
            {
                p.StoragePotential += 0.09f;
                p.LogisticsValue += 0.05f;
            }

            if (lot.TouchesRoad)
            {
                p.LogisticsValue += 0.04f;
                p.FrontBusinessPotential += 0.03f;
                p.PoliceVisibility += 0.03f;
            }

            if (lot.SupportsStreetFacingBuilding)
                p.FrontBusinessPotential += 0.03f;

            bool heavyLogistics = kind == BuildingKind.Warehouse || kind == BuildingKind.StorageYard ||
                                  kind == BuildingKind.DockFreight;
            if (heavyLogistics && !lot.TouchesRoad && lot.AccessibilityScore < 0.38f)
                p.PoliceVisibility += 0.06f;
        }

        static void ApplyDeterministicJitter(BuildingCrimeProfileData p, System.Random rng, float amplitude)
        {
            if (amplitude <= 0f)
                return;

            void J(ref float x)
            {
                x += (float)(rng.NextDouble() * 2.0 - 1.0) * amplitude;
            }

            J(ref p.FrontBusinessPotential);
            J(ref p.StoragePotential);
            J(ref p.BackroomPotential);
            J(ref p.LaunderingPotential);
            J(ref p.ExtortionPotential);
            J(ref p.BlackMarketSuitability);
            J(ref p.MeetingPotential);
            J(ref p.PoliceVisibility);
            J(ref p.NeighborhoodInfluenceValue);
            J(ref p.LogisticsValue);
        }

        static void ClampAll(BuildingCrimeProfileData p)
        {
            p.FrontBusinessPotential = Mathf.Clamp01(p.FrontBusinessPotential);
            p.StoragePotential = Mathf.Clamp01(p.StoragePotential);
            p.BackroomPotential = Mathf.Clamp01(p.BackroomPotential);
            p.LaunderingPotential = Mathf.Clamp01(p.LaunderingPotential);
            p.ExtortionPotential = Mathf.Clamp01(p.ExtortionPotential);
            p.BlackMarketSuitability = Mathf.Clamp01(p.BlackMarketSuitability);
            p.MeetingPotential = Mathf.Clamp01(p.MeetingPotential);
            p.PoliceVisibility = Mathf.Clamp01(p.PoliceVisibility);
            p.NeighborhoodInfluenceValue = Mathf.Clamp01(p.NeighborhoodInfluenceValue);
            p.LogisticsValue = Mathf.Clamp01(p.LogisticsValue);
        }

        static void DeriveFlags(BuildingCrimeProfileData p, DistrictKind district, BuildingKind kind)
        {
            p.CanActAsFront = p.FrontBusinessPotential >= 0.42f;
            p.CanStoreContraband = p.StoragePotential >= 0.45f;
            p.CanHostMeeting = p.MeetingPotential >= 0.42f;
            p.CanSupportLaundering = p.LaunderingPotential >= 0.45f;

            bool highScrutiny = p.PoliceVisibility >= 0.62f;
            bool heavyIllicit = p.LaunderingPotential >= 0.55f || p.BlackMarketSuitability >= 0.58f ||
                                p.ExtortionPotential >= 0.62f;
            p.IsHighRiskIfUsedIllegally = highScrutiny && heavyIllicit;

            if (district == DistrictKind.Wealthy && p.LaunderingPotential >= 0.52f && p.PoliceVisibility >= 0.48f)
                p.IsHighRiskIfUsedIllegally = true;

            if (kind == BuildingKind.Clinic || kind == BuildingKind.SmallServiceOffice)
            {
                if (p.PoliceVisibility >= 0.58f && p.LaunderingPotential >= 0.5f)
                    p.IsHighRiskIfUsedIllegally = true;
            }
        }
    }
}
