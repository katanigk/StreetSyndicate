using System;

/// <summary>
/// Vehicle archetypes for the late 1920s–1930s (US / international elite).
/// Ballistic and explosive resistance: 0–5; labels from <see cref="GetResistanceLabel"/>.
/// Display text in English — localize for Hebrew UI.
/// </summary>
public static class VehicleCatalog
{
    /// <summary>Typical roles a vehicle fits (bit flags).</summary>
    [Flags]
    public enum VehicleRoleMask
    {
        None = 0,
        CivilianRegular = 1 << 0,
        Gangster = 1 << 1,
        Luxury = 1 << 2,
        Commercial = 1 << 3,
        Police = 1 << 4,
        CivicElite = 1 << 5,
        FederalAgent = 1 << 6
    }

    public readonly struct VehicleArchetype
    {
        public string Id { get; }
        public string DisplayName { get; }
        public VehicleRoleMask Roles { get; }
        public int EraYearStart { get; }
        public int EraYearEnd { get; }
        public int CostUsdMin { get; }
        public int CostUsdMax { get; }
        public int SpeedMphMin { get; }
        public int SpeedMphMax { get; }
        public int PassengersMin { get; }
        public int PassengersMax { get; }
        public int CargoKgMin { get; }
        public int CargoKgMax { get; }
        /// <summary>0–5 ballistic resistance.</summary>
        public int BallisticResistance { get; }
        /// <summary>0–5 explosive / heavy fragmentation resistance.</summary>
        public int ExplosiveResistance { get; }
        public string Description { get; }

        public VehicleArchetype(
            string id,
            string displayName,
            VehicleRoleMask roles,
            int eraYearStart,
            int eraYearEnd,
            int costUsdMin,
            int costUsdMax,
            int speedMphMin,
            int speedMphMax,
            int passengersMin,
            int passengersMax,
            int cargoKgMin,
            int cargoKgMax,
            int ballisticResistance,
            int explosiveResistance,
            string description)
        {
            Id = id;
            DisplayName = displayName;
            Roles = roles;
            EraYearStart = eraYearStart;
            EraYearEnd = eraYearEnd;
            CostUsdMin = costUsdMin;
            CostUsdMax = costUsdMax;
            SpeedMphMin = speedMphMin;
            SpeedMphMax = speedMphMax;
            PassengersMin = passengersMin;
            PassengersMax = passengersMax;
            CargoKgMin = cargoKgMin;
            CargoKgMax = cargoKgMax;
            BallisticResistance = ballisticResistance;
            ExplosiveResistance = explosiveResistance;
            Description = description;
        }
    }

    /// <summary>Human-readable labels for resistance 0–5 (ballistic and explosive use same scale).</summary>
    public static string GetResistanceLabel(int level)
    {
        switch (Math.Max(0, Math.Min(5, level)))
        {
            case 0: return "Negligible";
            case 1: return "Very low";
            case 2: return "Low";
            case 3: return "Low–medium";
            case 4: return "High";
            default: return "Purpose-built protection";
        }
    }

    public static readonly VehicleArchetype[] All = BuildAll();

    private static VehicleArchetype[] BuildAll()
    {
        return new[]
        {
            new VehicleArchetype(
                "ford_model_a_sedan",
                "Ford Model A Sedan",
                VehicleRoleMask.CivilianRegular,
                1928, 1931,
                425, 635,
                45, 55,
                5, 5,
                100, 150,
                1, 0,
                "Classic everyday car for families, clerks, and shopkeepers. Some mass but no real armor."),

            new VehicleArchetype(
                "plymouth_pb_sedan_1932",
                "Plymouth PB Sedan (1932)",
                VehicleRoleMask.CivilianRegular | VehicleRoleMask.Gangster,
                1932, 1932,
                580, 640,
                50, 60,
                5, 5,
                110, 160,
                1, 0,
                "Clean, respectable early-1930s sedan; also used by hoods in coupe/sedan form — less threatening than a Ford V8."),

            new VehicleArchetype(
                "chevrolet_master_deluxe_sedan_1938",
                "Chevrolet Master Deluxe Sedan (1938)",
                VehicleRoleMask.CivilianRegular,
                1938, 1938,
                715, 815,
                55, 65,
                5, 5,
                120, 170,
                1, 0,
                "Late-1930s comfortable urban family car; senior clerks and agents."),

            new VehicleArchetype(
                "buick_series_40_special_1936",
                "Buick Series 40 Special Sedan (1936)",
                VehicleRoleMask.CivilianRegular | VehicleRoleMask.Gangster | VehicleRoleMask.CivicElite,
                1936, 1936,
                765, 885,
                60, 70,
                5, 5,
                130, 180,
                2, 0,
                "Heavier, more imposing — local bosses, shady businessmen, mid-tier mayors or judges. Slightly more mass vs. small arms, not real armor."),

            new VehicleArchetype(
                "ford_v8_model_18",
                "Ford V8 / Model 18 Sedan or Coupe (1932–1934)",
                VehicleRoleMask.Gangster | VehicleRoleMask.FederalAgent,
                1932, 1934,
                465, 655,
                65, 75,
                4, 5,
                100, 150,
                1, 0,
                "Chases and getaways — speed and power, not protection. The era's default for gangsters and G-men."),

            new VehicleArchetype(
                "packard_super_eight_1937",
                "Packard Super Eight (1937)",
                VehicleRoleMask.Luxury | VehicleRoleMask.CivicElite,
                1937, 1937,
                2300, 5000,
                70, 80,
                5, 5,
                150, 220,
                2, 1,
                "Serious money — senior judges, big-city mayors, governors. Very heavy; mass helps a little, not armor."),

            new VehicleArchetype(
                "chrysler_imperial_1933",
                "Chrysler Imperial (1933)",
                VehicleRoleMask.Luxury | VehicleRoleMask.CivicElite,
                1933, 1933,
                1275, 3575,
                75, 85,
                5, 8,
                160, 240,
                2, 1,
                "Flagship American sedan — motorcades, court presidents, important mayors."),

            new VehicleArchetype(
                "cadillac_v16_1930s",
                "Cadillac V-16 (1930s)",
                VehicleRoleMask.Luxury,
                1930, 1939,
                5000, 12000,
                70, 80,
                5, 7,
                160, 250,
                2, 1,
                "Peak American luxury — huge, heavy, expensive; mass vs. fire only, not protection."),

            new VehicleArchetype(
                "mercedes_benz_770_1930s",
                "Mercedes-Benz 770 (1930s)",
                VehicleRoleMask.Luxury,
                1930, 1939,
                0, 0,
                75, 90,
                6, 6,
                180, 260,
                2, 1,
                "Top-tier diplomats and rulers; price not a simple retail number. Rare armored variants exceed this profile."),

            new VehicleArchetype(
                "ford_model_aa",
                "Ford Model AA (1928–1932)",
                VehicleRoleMask.Commercial,
                1928, 1932,
                400, 900,
                25, 35,
                2, 3,
                200, 1500,
                1, 0,
                "Iconic light truck — heavier ladder frame; slightly less fragile than a car, still not durable."),

            new VehicleArchetype(
                "chevrolet_sedan_delivery_1938",
                "Chevrolet Sedan Delivery (1938)",
                VehicleRoleMask.Commercial,
                1938, 1938,
                650, 740,
                45, 55,
                2, 2,
                200, 230,
                1, 0,
                "Small urban delivery; closed body feels tougher, no real protection."),

            new VehicleArchetype(
                "mack_heavy_truck_1920s_30s",
                "Mack Heavy Truck (1920s–1930s)",
                VehicleRoleMask.Commercial,
                1920, 1939,
                2500, 8000,
                20, 30,
                2, 3,
                3600, 7600,
                2, 1,
                "Very heavy mass — not armor, but engine, nose, and frame absorb more than a sedan."),

            new VehicleArchetype(
                "ford_model_t_police",
                "Ford Model T (police, early 1920s)",
                VehicleRoleMask.Police,
                1920, 1927,
                260, 400,
                25, 35,
                4, 5,
                60, 100,
                0, 0,
                "Small-town and rural patrol — right for the first half of the decade."),

            new VehicleArchetype(
                "ford_model_a_police",
                "Ford Model A Police Sedan",
                VehicleRoleMask.Police,
                1928, 1931,
                425, 635,
                45, 55,
                5, 5,
                80, 120,
                1, 0,
                "Readable, available patrol car."),

            new VehicleArchetype(
                "plymouth_pb_police",
                "Plymouth PB (police / federal)",
                VehicleRoleMask.Police | VehicleRoleMask.FederalAgent,
                1932, 1932,
                500, 780,
                50, 60,
                4, 5,
                80, 120,
                1, 0,
                "State police or highway unit; also suits low-key federal work."),

            new VehicleArchetype(
                "ford_v8_police",
                "Ford V8 Police Sedan",
                VehicleRoleMask.Police,
                1932, 1934,
                465, 655,
                65, 75,
                4, 5,
                80, 120,
                1, 0,
                "Interception car — speed and response, not protection."),

            new VehicleArchetype(
                "chevrolet_master_police_federal",
                "Chevrolet Master Sedan (police / federal)",
                VehicleRoleMask.Police | VehicleRoleMask.FederalAgent,
                1938, 1938,
                715, 815,
                55, 65,
                5, 5,
                90, 130,
                1, 0,
                "Late-1930s — \"clean\" federal unit or modern city police."),
        };
    }
}
