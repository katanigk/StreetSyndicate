/// <summary>
/// Tentative social–economic strata (5 tiers) for the city — neighborhoods, map, NPCs, future real-estate pricing.
/// Incomes in period USD; ranges are relative to <see cref="EconomyBaseline.ReferenceUnskilledMonthlyIncomeUsd"/>.
/// Display text in English — localize for Hebrew UI.
/// </summary>
public static class SocialStratum
{
    public enum Tier
    {
        VeryLow = 0,
        Low = 1,
        Middle = 2,
        High = 3,
        VeryHigh = 4
    }

    public const int TierCount = 5;

    public readonly struct Definition
    {
        public Tier Tier { get; }
        public string Id { get; }
        public string Label { get; }
        /// <summary>Approximate monthly income range per person/household (design choice).</summary>
        public int TypicalMonthlyIncomeMinUsd { get; }
        public int TypicalMonthlyIncomeMaxUsd { get; }
        public string TypicalHousing { get; }
        public string DominantOccupations { get; }
        /// <summary>Hints for map build: density, streets, risk, building type.</summary>
        public string MapDesignNotes { get; }

        public Definition(
            Tier tier,
            string id,
            string label,
            int incomeMinUsd,
            int incomeMaxUsd,
            string typicalHousing,
            string dominantOccupations,
            string mapDesignNotes)
        {
            Tier = tier;
            Id = id;
            Label = label;
            TypicalMonthlyIncomeMinUsd = incomeMinUsd;
            TypicalMonthlyIncomeMaxUsd = incomeMaxUsd;
            TypicalHousing = typicalHousing;
            DominantOccupations = dominantOccupations;
            MapDesignNotes = mapDesignNotes;
        }
    }

    public static readonly Definition[] All = BuildAll();

    private static Definition[] BuildAll()
    {
        return new[]
        {
            new Definition(
                Tier.VeryLow,
                "very_low",
                "Very low",
                0, 35,
                "Room in a boarding house, one-room family flat, warehouse, night shelter.",
                "Day laborers, temp workers, drifters, new immigrants, occasional dock workers.",
                "Narrow streets, tenement blocks, inner courtyards, cheap food markets, low-to-mid police presence, survival crime and petty sabotage."),

            new Definition(
                Tier.Low,
                "low",
                "Low",
                35, 75,
                "Rental in a two-family house, row house, sometimes above a shop.",
                "Factory workers, domestic servants, truck drivers, junior clerks, small shopkeepers.",
                "Dense urban grid, power lines and roofs, grocery stores and bakeries, cheap bars; natural for small rackets and patrol police."),

            new Definition(
                Tier.Middle,
                "middle",
                "Middle",
                75, 180,
                "Comfortable two-family home, flat in a relatively quiet neighborhood, sometimes a small yard.",
                "Clerks, teachers, artisans, certified tradesmen, small agents, pharmacists.",
                "Wider streets, trees here and there, schools and churches/synagogues; less density, more family restaurants and clothing stores — good for \"everyday\" downtown neighborhoods."),

            new Definition(
                Tier.High,
                "high",
                "High",
                180, 450,
                "Small villa, large flat in a respectable building, partly gated neighborhood.",
                "Doctors, lawyers, small factory owners, merchants, junior bankers, accountants.",
                "Boulevards, driveways, less street retail and more small offices and upscale shops; respectable businesses and civic meetings."),

            new Definition(
                Tier.VeryHigh,
                "very_high",
                "Very high",
                400, 3000,
                "Estates, penthouses, service apartments with staff, hilltop or park-adjacent properties.",
                "Industrialists, politicians, judges, bank owners, cultural elite, diplomats.",
                "Large lots, gates, less density, views and privacy; fits map edges or a high urban island — big shots, private clubs, corporate offices. (Capital/rent income can exceed range — tentative.)")
        };
    }

    public static Definition Get(Tier tier)
    {
        int i = (int)tier;
        if (i < 0 || i >= All.Length)
            return All[0];
        return All[i];
    }

    public static string Label(Tier tier)
    {
        return Get(tier).Label;
    }

    /// <summary>Quick tier lookup from map layer (e.g. neighborhood id).</summary>
    public static bool TryParseTierId(string id, out Tier tier)
    {
        tier = Tier.VeryLow;
        if (string.IsNullOrEmpty(id))
            return false;
        string s = id.Trim().ToLowerInvariant();
        for (int i = 0; i < All.Length; i++)
        {
            if (All[i].Id == s)
            {
                tier = All[i].Tier;
                return true;
            }
        }
        return false;
    }
}
