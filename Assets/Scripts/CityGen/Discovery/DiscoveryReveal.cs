using FamilyBusiness.CityGen.Data;

namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Safe, monotonic reveal hooks for future gameplay (Batch 9). Truth data is never modified.</summary>
    public static class DiscoveryReveal
    {
        public static void RevealDistrictTo(DistrictData d, DiscoveryState target)
        {
            if (d?.Discovery == null)
                return;
            var ceiling = d.Discovery.Discoverability?.MaxReachableState ?? DiscoveryState.Infiltrated;
            d.Discovery.State = DiscoveryStateOrdering.ClampPromotion(d.Discovery.State, target, ceiling);
        }

        public static void ApplyRumorDistrict(DistrictData d)
        {
            if (d?.Discovery == null)
                return;
            if (d.Discovery.Discoverability != null && !d.Discovery.Discoverability.SupportsRumor)
                return;
            RevealDistrictTo(d, DiscoveryState.Rumored);
        }

        public static void MarkDistrictEntered(DistrictData d)
        {
            if (d?.Discovery == null)
                return;
            d.Discovery.HasBeenPhysicallyEntered = true;
        }

        /// <summary>Movement hook: marks physical entry and promotes district knowledge to at least <see cref="DiscoveryState.Known"/>.</summary>
        public static void ApplyProximityRevealDistrict(DistrictData d)
        {
            if (d?.Discovery == null)
                return;
            MarkDistrictEntered(d);
            RevealDistrictTo(d, DiscoveryState.Known);
        }

        /// <summary>Use when only <see cref="MarkDistrictEntered"/> was called elsewhere; promotes to Known if profile ties detailed knowledge to entry.</summary>
        public static void ApplyProximityAfterDistrictEntry(DistrictData d)
        {
            if (d?.Discovery == null || !d.Discovery.HasBeenPhysicallyEntered)
                return;
            var p = d.Discovery.Discoverability;
            if (p != null && p.RequiresProximityForKnown)
                RevealDistrictTo(d, DiscoveryState.Known);
        }

        public static void RevealInstitutionTo(InstitutionData i, DiscoveryState target)
        {
            if (i?.Discovery == null)
                return;
            var ceiling = i.Discovery.Discoverability?.MaxReachableState ?? DiscoveryState.Infiltrated;
            i.Discovery.State = DiscoveryStateOrdering.ClampPromotion(i.Discovery.State, target, ceiling);
            SyncInstitutionFlags(i);
        }

        public static void ApplyRumorInstitution(InstitutionData i)
        {
            if (i?.Discovery == null)
                return;
            if (i.Discovery.Discoverability != null && !i.Discovery.Discoverability.SupportsRumor)
                return;
            RevealInstitutionTo(i, DiscoveryState.Rumored);
        }

        public static void ApplyDocumentInstitutionHint(InstitutionData i)
        {
            if (i?.Discovery == null)
                return;
            RevealInstitutionTo(i, DiscoveryState.Known);
            i.Discovery.InstitutionKindKnown = true;
            i.Discovery.ExactLocationKnown = true;
        }

        public static void ApplyEspionageInstitutionDeep(InstitutionData i)
        {
            if (i?.Discovery == null)
                return;
            RevealInstitutionTo(i, DiscoveryState.DeeplyExposed);
            i.Discovery.InstitutionKindKnown = true;
            i.Discovery.ExactLocationKnown = true;
            i.Discovery.InternalDetailsKnown = true;
        }

        public static void RevealBuildingTo(BuildingData b, DiscoveryState target)
        {
            if (b?.Discovery == null)
                return;
            var ceiling = b.Discovery.Discoverability?.MaxReachableState ?? DiscoveryState.Infiltrated;
            b.Discovery.State = DiscoveryStateOrdering.ClampPromotion(b.Discovery.State, target, ceiling);
            SyncBuildingFlags(b);
        }

        public static void ApplyProximityBuildingFacade(BuildingData b)
        {
            if (b?.Discovery == null)
                return;
            RevealBuildingTo(b, DiscoveryState.Known);
            b.Discovery.FrontBusinessTypeKnown = true;
            b.Discovery.BuildingCategoryKnown = true;
        }

        /// <summary>Movement hook: facade reveal when close enough; respects <see cref="DiscoverabilityProfileData.RequiresProximityForKnown"/> (always satisfied when this is called).</summary>
        public static void ApplyProximityRevealBuilding(BuildingData b) => ApplyProximityBuildingFacade(b);

        public static void ApplyRumorBuilding(BuildingData b)
        {
            if (b?.Discovery == null)
                return;
            if (b.Discovery.Discoverability != null && !b.Discovery.Discoverability.SupportsRumor)
                return;
            RevealBuildingTo(b, DiscoveryState.Rumored);
        }

        public static void ApplyDeepExposureBuilding(BuildingData b)
        {
            if (b?.Discovery == null)
                return;
            RevealBuildingTo(b, DiscoveryState.DeeplyExposed);
            b.Discovery.FrontBusinessTypeKnown = true;
            b.Discovery.BuildingCategoryKnown = true;
            b.Discovery.CrimeProfileKnown = true;
        }

        /// <summary>Future hook: control of neighborhood asset unlocks sustained intel.</summary>
        public static void ApplyControlPromote(DistrictData d)
        {
            if (d?.Discovery == null)
                return;
            RevealDistrictTo(d, DiscoveryState.Controlled);
        }

        /// <summary>Future hook: infiltration / deep cover.</summary>
        public static void ApplyInfiltrationPromote(InstitutionData i)
        {
            if (i?.Discovery == null)
                return;
            RevealInstitutionTo(i, DiscoveryState.Infiltrated);
            i.Discovery.InternalDetailsKnown = true;
        }

        public static void PromoteDiscovery(DistrictData d, int rankDelta)
        {
            if (d?.Discovery == null || rankDelta <= 0)
                return;
            int r = DiscoveryStateOrdering.Rank(d.Discovery.State) + rankDelta;
            RevealDistrictTo(d, DiscoveryStateOrdering.FromRank(r));
        }

        public static void PromoteDiscovery(InstitutionData i, int rankDelta)
        {
            if (i?.Discovery == null || rankDelta <= 0)
                return;
            int r = DiscoveryStateOrdering.Rank(i.Discovery.State) + rankDelta;
            RevealInstitutionTo(i, DiscoveryStateOrdering.FromRank(r));
        }

        public static void PromoteDiscovery(BuildingData b, int rankDelta)
        {
            if (b?.Discovery == null || rankDelta <= 0)
                return;
            int r = DiscoveryStateOrdering.Rank(b.Discovery.State) + rankDelta;
            RevealBuildingTo(b, DiscoveryStateOrdering.FromRank(r));
        }

        /// <summary>Document / contact hook: partial building intel without full crime exposure.</summary>
        public static void ApplyDocumentBuildingHint(BuildingData b)
        {
            if (b?.Discovery == null)
                return;
            RevealBuildingTo(b, DiscoveryState.Known);
            b.Discovery.FrontBusinessTypeKnown = true;
        }

        static void SyncInstitutionFlags(InstitutionData i)
        {
            var s = i.Discovery.State;
            if (DiscoveryStateOrdering.IsAtLeast(s, DiscoveryState.Known))
            {
                i.Discovery.InstitutionKindKnown = true;
                i.Discovery.ExactLocationKnown = true;
            }

            if (DiscoveryStateOrdering.IsAtLeast(s, DiscoveryState.PartiallyExposed))
                i.Discovery.InternalDetailsKnown = true;
        }

        static void SyncBuildingFlags(BuildingData b)
        {
            var s = b.Discovery.State;
            if (DiscoveryStateOrdering.IsAtLeast(s, DiscoveryState.Known))
            {
                b.Discovery.FrontBusinessTypeKnown = true;
                b.Discovery.BuildingCategoryKnown = true;
            }

            if (DiscoveryStateOrdering.IsAtLeast(s, DiscoveryState.PartiallyExposed))
                b.Discovery.CrimeProfileKnown = true;
        }
    }
}
