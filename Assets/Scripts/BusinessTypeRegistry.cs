/// <summary>
/// Catalog of business archetypes (legitimate / illegal) for the 1920s setting and future logistics.
/// Stable <see cref="BusinessArchetype.Id"/> for saves; display strings are English defaults — localize via tables for Hebrew UI.
/// </summary>
public static class BusinessTypeRegistry
{
    public readonly struct BusinessArchetype
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Category { get; }

        public BusinessArchetype(string id, string displayName, string category = "")
        {
            Id = id;
            DisplayName = displayName;
            Category = category;
        }
    }

    /// <summary>Legitimate storefronts and services.</summary>
    public static readonly BusinessArchetype[] Legitimate = BuildLegitimate();

    /// <summary>Illegal operations / rackets (extortion fronts, heat, etc.).</summary>
    public static readonly BusinessArchetype[] Illegal = BuildIllegal();

    private static BusinessArchetype[] BuildLegitimate()
    {
        return new[]
        {
            new BusinessArchetype("barber_shop", "Barber shop", "Personal services"),
            new BusinessArchetype("beauty_salon", "Women's salon / beauty parlor", "Personal services"),
            new BusinessArchetype("hat_shop", "Hat shop", "Retail"),
            new BusinessArchetype("shoe_store", "Shoe store", "Retail"),
            new BusinessArchetype("shoe_repair", "Shoe repair", "Personal services"),
            new BusinessArchetype("tailor", "Tailor / suits", "Retail"),
            new BusinessArchetype("laundry", "Laundry", "Personal services"),
            new BusinessArchetype("photo_studio", "Photographer / portrait studio", "Personal services"),

            new BusinessArchetype("grocery", "Grocery / corner store", "Food"),
            new BusinessArchetype("bakery", "Bakery", "Food"),
            new BusinessArchetype("butcher", "Butcher shop", "Food"),
            new BusinessArchetype("fishmonger", "Fishmonger", "Food"),
            new BusinessArchetype("greengrocer", "Greengrocer", "Food"),
            new BusinessArchetype("deli", "Delicatessen", "Food"),
            new BusinessArchetype("street_diner", "Street diner / lunch counter", "Food"),
            new BusinessArchetype("neighborhood_restaurant", "Neighborhood restaurant", "Food"),
            new BusinessArchetype("fine_dining", "Fine dining", "Food"),
            new BusinessArchetype("coffee_house", "Coffee house", "Food"),
            new BusinessArchetype("tea_room", "Tea room", "Food"),
            new BusinessArchetype("candy_store", "Candy store", "Food"),

            new BusinessArchetype("bookstore", "Bookstore", "Retail"),
            new BusinessArchetype("newsstand", "Newsstand", "Retail"),
            new BusinessArchetype("tobacconist", "Tobacconist", "Retail"),
            new BusinessArchetype("pharmacy", "Pharmacy", "Health"),
            new BusinessArchetype("hardware_store", "Hardware store / ironmonger", "Retail"),
            new BusinessArchetype("general_store", "General store", "Retail"),
            new BusinessArchetype("department_store", "Department store", "Retail"),
            new BusinessArchetype("furniture_store", "Furniture store", "Retail"),
            new BusinessArchetype("dry_goods", "Dry goods / notions", "Retail"),
            new BusinessArchetype("jewelry_store", "Jewelry store", "Retail"),
            new BusinessArchetype("pawn_shop", "Pawn shop", "Finance"),
            new BusinessArchetype("antique_shop", "Antique shop", "Retail"),

            new BusinessArchetype("taxi_stand", "Taxi stand / cab rank", "Transport"),
            new BusinessArchetype("gas_station", "Gas station", "Transport"),
            new BusinessArchetype("garage", "Garage / truck shop", "Transport"),
            new BusinessArchetype("car_dealer", "Car dealership (early era)", "Transport"),
            new BusinessArchetype("stable", "Stable / horse and wagon hire", "Transport"),

            new BusinessArchetype("bank_branch", "Bank branch", "Finance"),
            new BusinessArchetype("insurance_office", "Insurance office", "Finance"),
            new BusinessArchetype("real_estate_office", "Real estate office", "Property"),
            new BusinessArchetype("accountant_office", "Accountant's office", "Finance"),
            new BusinessArchetype("law_office", "Law office", "Professional"),

            new BusinessArchetype("hotel", "Hotel", "Hospitality"),
            new BusinessArchetype("boarding_house", "Boarding house", "Hospitality"),
            new BusinessArchetype("warehouse", "Warehouse", "Logistics"),
            new BusinessArchetype("wholesale", "Wholesale office", "Logistics"),

            new BusinessArchetype("cinema", "Cinema", "Entertainment"),
            new BusinessArchetype("theater", "Theater", "Entertainment"),
            new BusinessArchetype("dance_hall", "Dance hall", "Entertainment"),
            new BusinessArchetype("pool_hall", "Pool hall", "Entertainment"),
            new BusinessArchetype("gym", "Gymnasium", "Sports"),
            new BusinessArchetype("boxing_gym", "Boxing gym / training hall", "Sports"),

            new BusinessArchetype("clinic", "Clinic", "Health"),
            new BusinessArchetype("dentist", "Dentist", "Health"),
            new BusinessArchetype("funeral_home", "Funeral home", "Services"),

            new BusinessArchetype("factory_small", "Small factory / packing plant", "Industry"),
            new BusinessArchetype("print_shop", "Print shop", "Industry"),
            new BusinessArchetype("lumber_yard", "Lumber yard", "Industry"),

            new BusinessArchetype("church", "Church", "Community"),
            new BusinessArchetype("synagogue", "Synagogue", "Community"),
            new BusinessArchetype("library", "Public library", "Education"),
            new BusinessArchetype("school_small", "Small school / classroom", "Education"),

            new BusinessArchetype("post_office_counter", "Post office counter (in store)", "Communications"),
            new BusinessArchetype("telegraph", "Telegraph office", "Communications"),
            new BusinessArchetype("radio_shop", "Radio and battery shop", "Communications"),

            new BusinessArchetype("soup_kitchen", "Soup kitchen / charity food line", "Public"),
            new BusinessArchetype("unemployment_office", "Unemployment office / labor hall", "Public"),
            new BusinessArchetype("docks_office", "Docks office / stevedoring", "Logistics"),
        };
    }

    private static BusinessArchetype[] BuildIllegal()
    {
        return new[]
        {
            new BusinessArchetype("speakeasy", "Speakeasy / hidden bar", "Alcohol"),
            new BusinessArchetype("bootleg_still", "Hidden still / liquor cook", "Alcohol"),
            new BusinessArchetype("smuggling_drop", "Smuggling drop / waterfront stash", "Alcohol"),
            new BusinessArchetype("brothel", "Brothel (narrative euphemism)", "Vice"),
            new BusinessArchetype("gambling_den", "Gambling den", "Gambling"),
            new BusinessArchetype("bookmaker", "Bookmaking / sports pool", "Gambling"),
            new BusinessArchetype("casino_backroom", "Backroom casino", "Gambling"),
            new BusinessArchetype("loan_shark", "Loan shark", "Black money"),
            new BusinessArchetype("numbers_racket", "Numbers racket", "Gambling"),
            new BusinessArchetype("opium_den", "Opium den", "Drugs"),
            new BusinessArchetype("fence", "Fence for stolen goods", "Theft"),
            new BusinessArchetype("chop_shop", "Chop shop", "Theft"),
            new BusinessArchetype("counterfeit_ring", "Counterfeiting ring", "Fraud"),
            new BusinessArchetype("extortion_hub", "Extortion collection point", "Violence"),
            new BusinessArchetype("labor_racket", "Labor racket / site shakedown", "Violence"),
            new BusinessArchetype("union_infiltration", "Union infiltration", "Politics"),
            new BusinessArchetype("insider_trading_ring", "Insider trading ring", "Fraud"),
            new BusinessArchetype("money_laundering_front", "Money laundering front (paired with legit business)", "Black money"),
            new BusinessArchetype("arms_cache", "Hidden arms cache", "Weapons"),
            new BusinessArchetype("illegal_warehouse", "Illegal warehouse (general goods)", "Logistics"),
        };
    }
}
