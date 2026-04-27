/// <summary>
/// Called whenever abstract planning week advances. Rent status is derived from days; extend here for bills and events.
/// </summary>
public static class MicroBlockDayAdvanceHooks
{
    public static void AfterDayIncremented(int previousDay, int newDay)
    {
        PoliceStreetPressureDaily.ProcessAfterDayAdvanced(previousDay, newDay);
        PoliceRuntimeDriver.OnCalendarDayAdvanced(previousDay, newDay);
        FederalBureauRuntimeDriver.OnCalendarDayAdvanced(previousDay, newDay);
    }
}
