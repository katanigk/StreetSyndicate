using System;
using UnityEngine;

/// <summary>
/// Gregorian calendar: game day 1 = July 1, 1924. Each planning→execution cycle advances one calendar day (<see cref="GameSessionState.CurrentDay"/>).
/// Month lengths and leap years follow the Gregorian rules (including 100/400 year exceptions).
/// Seasons by month (meteorological, Northern Hemisphere): Dec–Jan–Feb winter; Mar–Apr–May spring;
/// Jun–Jul–Aug summer; Sep–Oct–Nov autumn. (Gameplay hooks can read <see cref="GetSeason"/>.)
/// </summary>
public static class GameCalendarSystem
{
    /// <summary>First in-game calendar day.</summary>
    public static readonly DateTime Epoch = new DateTime(1924, 7, 1);

    /// <summary>Calendar date for a game day (1 = July 1, 1924).</summary>
    public static DateTime GetDate(int gameDay)
    {
        int d = Mathf.Max(1, gameDay);
        return Epoch.AddDays(d - 1);
    }

    public static int GetDayOfMonth(int gameDay)
    {
        return GetDate(gameDay).Day;
    }

    public static int GetMonth(int gameDay)
    {
        return GetDate(gameDay).Month;
    }

    /// <summary>Full calendar year (e.g. 1924).</summary>
    public static int GetCalendarYear(int gameDay)
    {
        return GetDate(gameDay).Year;
    }

    /// <summary>Campaign year for scaling: 1924 = 1, 1925 = 2, …</summary>
    public static int GetCampaignYear(int gameDay)
    {
        return GetCalendarYear(gameDay) - Epoch.Year + 1;
    }

    /// <summary>Law / rival pressure multiplier; baseline campaign year 1924 = 1.</summary>
    public static float GetOppositionMultiplier(int gameDay)
    {
        int y = GetCampaignYear(gameDay);
        float t = Mathf.Max(0, y - 1);
        return 1f + Mathf.Min(0.6f, t * 0.12f);
    }

    /// <summary>Subtracted from operation success chance (0..~0.16) as years pass.</summary>
    public static float GetOppositionChancePenalty(int gameDay)
    {
        float m = GetOppositionMultiplier(gameDay);
        return Mathf.Min(0.16f, (m - 1f) * 0.22f);
    }

    /// <summary>Season from month (Northern Hemisphere, meteorological).</summary>
    public static GameSeason GetSeason(int gameDay)
    {
        int m = GetMonth(gameDay);
        if (m == 12 || m == 1 || m == 2)
            return GameSeason.Winter;
        if (m >= 3 && m <= 5)
            return GameSeason.Spring;
        if (m >= 6 && m <= 8)
            return GameSeason.Summer;
        return GameSeason.Autumn;
    }

    public static string GetSeasonNameEn(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring: return "Spring";
            case GameSeason.Summer: return "Summer";
            case GameSeason.Autumn: return "Autumn";
            case GameSeason.Winter: return "Winter";
            default: return "Spring";
        }
    }

    private static readonly string[] MonthNamesEnShort =
    {
        "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    };

    private static readonly string[] MonthNamesEnFull =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    };

    private static readonly string[] WeekdayEn =
    {
        "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"
    };

    public static string GetMonthNameEn(int month)
    {
        int i = Mathf.Clamp(month, 1, 12) - 1;
        return MonthNamesEnShort[i];
    }

    public static string GetMonthNameEnFull(int month)
    {
        int i = Mathf.Clamp(month, 1, 12) - 1;
        return MonthNamesEnFull[i];
    }

    /// <summary>HUD: weekday + full date; optional weather line only when it has gameplay effects; then season.</summary>
    public static string FormatPlanningHudLine(int gameDay)
    {
        DateTime dt = GetDate(gameDay);
        int dow = (int)dt.DayOfWeek;
        string dayName = WeekdayEn[dow];
        string monthName = GetMonthNameEnFull(dt.Month);
        string line1 = dayName + ", " + monthName + " " + dt.Day + ", " + dt.Year;
        GameSeason se = GetSeason(gameDay);
        string seasonLine = GetSeasonNameEn(se);
        WeatherSnapshot wx = GameWeatherResolver.Resolve(gameDay);
        string weatherLine = GameWeatherResolver.BuildHudWeatherLine(wx);
        if (!string.IsNullOrEmpty(weatherLine))
            return line1 + "\n" + weatherLine + "\n" + seasonLine;
        return line1 + "\n" + seasonLine;
    }

    /// <summary>Compact one-line date + season; appends weather only when it has gameplay effects.</summary>
    public static string FormatPlanningHudLineShort(int gameDay)
    {
        DateTime dt = GetDate(gameDay);
        string monthShort = GetMonthNameEn(dt.Month);
        GameSeason se = GetSeason(gameDay);
        string core = monthShort + " " + dt.Day + ", " + dt.Year + " · " + GetSeasonNameEn(se);
        WeatherSnapshot wx = GameWeatherResolver.Resolve(gameDay);
        string w = GameWeatherResolver.BuildHudWeatherLine(wx);
        if (!string.IsNullOrEmpty(w))
            return core + " · " + w;
        return core;
    }

#if UNITY_EDITOR
    /// <summary>Editor check: en-US culture vs manual strings.</summary>
    public static string FormatPlanningHudLineCultureFallback(int gameDay)
    {
        DateTime dt = GetDate(gameDay);
        try
        {
            var ci = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            WeatherSnapshot wx = GameWeatherResolver.Resolve(gameDay);
            string w = GameWeatherResolver.BuildHudWeatherLine(wx);
            string mid = string.IsNullOrEmpty(w) ? string.Empty : w + "\n";
            return dt.ToString("dddd, MMMM d, yyyy", ci) + "\n" + mid + GetSeasonNameEn(GetSeason(gameDay));
        }
        catch
        {
            return FormatPlanningHudLine(gameDay);
        }
    }
#endif
}
