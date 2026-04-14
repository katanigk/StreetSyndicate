namespace FamilyBusiness.CityGen.Discovery
{
    /// <summary>Total order for discovery tiers (Batch 9).</summary>
    public static class DiscoveryStateOrdering
    {
        public static int Rank(DiscoveryState s) =>
            s switch
            {
                DiscoveryState.Unknown => 0,
                DiscoveryState.Rumored => 1,
                DiscoveryState.Known => 2,
                DiscoveryState.PartiallyExposed => 3,
                DiscoveryState.DeeplyExposed => 4,
                DiscoveryState.Controlled => 5,
                DiscoveryState.Infiltrated => 6,
                _ => 0
            };

        public static bool IsAtLeast(DiscoveryState current, DiscoveryState minimum) =>
            Rank(current) >= Rank(minimum);

        /// <summary>Clamp promotion target to ceiling and monotonicity.</summary>
        public static DiscoveryState ClampPromotion(DiscoveryState current, DiscoveryState desired,
            DiscoveryState maxReachable)
        {
            int r = Rank(desired);
            r = System.Math.Min(r, Rank(maxReachable));
            r = System.Math.Max(r, Rank(current));
            return FromRank(r);
        }

        public static DiscoveryState FromRank(int r) =>
            r switch
            {
                <= 0 => DiscoveryState.Unknown,
                1 => DiscoveryState.Rumored,
                2 => DiscoveryState.Known,
                3 => DiscoveryState.PartiallyExposed,
                4 => DiscoveryState.DeeplyExposed,
                5 => DiscoveryState.Controlled,
                _ => DiscoveryState.Infiltrated
            };
    }
}
