namespace FamilyBusiness.CityGen.Data
{
    /// <summary>Regular building / parcel outcome (Batch 7). Not anchor institutions.</summary>
    public enum BuildingKind
    {
        Unknown = 0,

        EmptyLot = 1,
        VacantParcel = 2,
        Yard = 3,
        ReservedFutureParcel = 4,

        BarTavern = 10,
        Grocery = 11,
        Butcher = 12,
        Bakery = 13,
        Tailor = 14,
        PawnShop = 15,
        Office = 16,
        FinanceOffice = 17,
        GeneralStore = 18,

        Warehouse = 30,
        Workshop = 31,
        MachineShop = 32,
        Garage = 33,
        StorageYard = 34,
        RailUtility = 35,
        DockFreight = 36,

        House = 50,
        ApartmentBuilding = 51,
        Tenement = 52,
        MixedUseCommercialResidential = 53,

        Clinic = 70,
        SmallServiceOffice = 71,
        CornerService = 72
    }
}
