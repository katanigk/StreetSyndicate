/// <summary>
/// Places in a single 1920s–30s poor urban block (Prohibition-era U.S. flavor). Expand later when linking to full CityGen.
/// </summary>
public enum MicroBlockSpotKind
{
    Unknown = 0,

    /// <summary>Shared room the crew rents; rent is weekly prepaid.</summary>
    CrewSharedRoom,

    /// <summary>Tenement / rooming house common halls, landlord contact.</summary>
    RoomingHouse,

    BarberShop,
    CornerGrocery,
    Laundromat,

    /// <summary>Small beat office, not a full precinct HQ.</summary>
    PoliceBeatOffice,

    PostOfficeBranch,
    ChurchParish,

    Warehouse,
    AutoGarage,

    /// <summary>Vacant lot, playground, or small green.</summary>
    NeighborhoodPark,

    /// <summary>Rumors, cards, smoke — social intel hub.</summary>
    PoolHall,

    /// <summary>Soda fountain / lunch counter.</summary>
    SodaLunchCounter,

    SmallClinic,
    Newsstand,

    /// <summary>Letterpress, job printing, handbills — ink and paper trails.</summary>
    PrintShop,

    /// <summary>Looks innocent from outside; truth is discoverable.</summary>
    SpeakeasyFront,

    /// <summary>Settlement house, mission — charity, English class, soup.</summary>
    MissionHall,

    /// <summary>Firehouse, horse or early motor.</summary>
    FirehouseSmall,

    /// <summary>Pawn / used goods — credit and secrets.</summary>
    PawnShop,

    /// <summary>Telegraph / telephone message desk at corner druggist, etc.</summary>
    TelegraphDesk
}
