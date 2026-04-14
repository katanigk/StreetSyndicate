using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Deterministic weather: same calendar month/day each year within a 4-year block uses the same <see cref="WeatherSnapshot.VariantIndex"/> template.
/// Block index = (calendarYear - 1924) / 4; <see cref="WeatherVariantCount"/> templates rotate: variant = block % 3.
/// After each 4-year block the template changes (re-shuffle to next of three annual sets).
/// Optional landmark events per variant (heat / fog windows). Most days use procedural weather.
/// Winter (snow/wind/fog), autumn (wet storms), spring (showers/unstable), summer (mostly fair + heat haze / rare storms).
/// Procedural: hash(variant, month, day) — same month/day each year while variant unchanged.
/// </summary>
public static class GameWeatherResolver
{
    public const int WeatherVariantCount = 3;
    public const int YearsPerWeatherBlock = 4;

    public static int GetFourYearBlockIndex(int calendarYear)
    {
        return Mathf.Max(0, (calendarYear - GameCalendarSystem.Epoch.Year) / YearsPerWeatherBlock);
    }

    /// <summary>0..2 — which of the three annual weather sets is active.</summary>
    public static int GetWeatherVariantIndex(int calendarYear)
    {
        return GetFourYearBlockIndex(calendarYear) % WeatherVariantCount;
    }

    public static WeatherSnapshot Resolve(int gameDay)
    {
        DateTime dt = GameCalendarSystem.GetDate(gameDay);
        return Resolve(dt);
    }

    public static WeatherSnapshot Resolve(DateTime date)
    {
        int variant = GetWeatherVariantIndex(date.Year);
        if (TryApplySpecialEvent(date, variant, out WeatherSnapshot special))
            return special;
        return ResolveProcedural(date, variant);
    }

    static bool TryApplySpecialEvent(DateTime date, int variant, out WeatherSnapshot w)
    {
        int m = date.Month;
        int d = date.Day;

        if (variant == 1 && m == 8 && d >= 10 && d <= 12)
        {
            w = HeatWaveEvent(variant);
            return true;
        }

        if (variant == 2 && m == 3 && d >= 5 && d <= 8)
        {
            w = FogBankEvent(variant);
            return true;
        }

        w = default;
        return false;
    }

    static WeatherSnapshot HeatWaveEvent(int variant)
    {
        return new WeatherSnapshot(
            hasGameplayEffects: true,
            lightRain: false,
            heavyRain: false,
            snow: false,
            wind: false,
            strongWind: false,
            fog: false,
            heatWave: true,
            severity01: 0.38f,
            variantIndex: variant);
    }

    static WeatherSnapshot FogBankEvent(int variant)
    {
        return new WeatherSnapshot(
            hasGameplayEffects: true,
            lightRain: true,
            heavyRain: false,
            snow: false,
            wind: false,
            strongWind: false,
            fog: true,
            heatWave: false,
            severity01: 0.42f,
            variantIndex: variant);
    }

    static bool IsWinterMonth(int month)
    {
        return month == 12 || month == 1 || month == 2;
    }

    static bool IsAutumnMonth(int month)
    {
        return month >= 9 && month <= 11;
    }

    static bool IsSpringMonth(int month)
    {
        return month >= 3 && month <= 5;
    }

    static bool IsSummerMonth(int month)
    {
        return month >= 6 && month <= 8;
    }

    static WeatherSnapshot ResolveProcedural(DateTime date, int variant)
    {
        int m = date.Month;
        int d = date.Day;
        int h = StableHash(variant, m, d);
        int roll = Mathf.Abs(h % 100);

        if (IsWinterMonth(m))
            return ResolveProceduralWinter(variant, m, d, h, roll);

        if (IsAutumnMonth(m))
            return ResolveProceduralAutumn(variant, m, d, h, roll);

        if (IsSpringMonth(m))
            return ResolveProceduralSpring(variant, m, d, h, roll);

        if (IsSummerMonth(m))
            return ResolveProceduralSummer(variant, m, d, h, roll);

        return WeatherSnapshot.Clear(variant);
    }

    /// <summary>
    /// Spring: March often wet/windy; April turns pleasant; May mostly fair (same month/day repeats yearly per variant).
    /// </summary>
    static WeatherSnapshot ResolveProceduralSpring(int variant, int m, int d, int h, int roll)
    {
        int clearThreshold = GetSpringClearThreshold(m);
        if (roll < clearThreshold)
            return WeatherSnapshot.Clear(variant);

        int h2 = Mathf.Abs(StableHash(variant + 2, m + d, 41)) % 100;

        if (h2 < 24)
            return LightRainOnly(variant, h);

        if (h2 < 42)
            return WindAndLightRain(variant, h);

        if (h2 < 56)
            return WindOnly(variant, h);

        if (h2 < 68)
            return FogOnly(variant, h);

        if (h2 < 82)
            return HeavyRainEvent(variant, h);

        if (h2 < 92)
            return LightRainOnly(variant, h ^ 19);

        return WindAndLightRain(variant, h ^ 7);
    }

    static int GetSpringClearThreshold(int month)
    {
        if (month == 3)
            return 44;
        if (month == 4)
            return 70;
        return 80;
    }

    /// <summary>
    /// Summer: June mostly bright. July–August: many “heat stress” days (hard on foot); otherwise clear or rare storms/haze.
    /// </summary>
    static WeatherSnapshot ResolveProceduralSummer(int variant, int m, int d, int h, int roll)
    {
        int hHeat = Mathf.Abs(StableHash(variant + 777, m, d * 31 + 9)) % 100;
        if (m == 7 || m == 8)
        {
            if (hHeat < 28)
                return SummerHeatStress(variant, h);
        }

        int clearThreshold = m == 6 ? 82 : 86;
        if (roll < clearThreshold)
            return WeatherSnapshot.Clear(variant);

        int h2 = Mathf.Abs(StableHash(variant + 41, m, d * 13)) % 100;

        if (h2 < 28)
            return SummerHeatHaze(variant, h);

        if (h2 < 48)
            return WindOnly(variant, h);

        if (h2 < 68)
            return LightRainOnly(variant, h);

        if (h2 < 88)
            return HeavyRainEvent(variant, h);

        return WindAndLightRain(variant, h);
    }

    /// <summary>Oppressive July/August heat — <see cref="WeatherSnapshot.HeatWave"/> + severity ≥ 0.44 for HUD “Heat stress”.</summary>
    static WeatherSnapshot SummerHeatStress(int variant, int h)
    {
        float sev = 0.46f + (Mathf.Abs(h % 9)) * 0.02f;
        return new WeatherSnapshot(true, false, false, false, false, false, false, true, Mathf.Clamp01(sev), variant);
    }

    /// <summary>Low-visibility haze / humidity shimmer — counts as fog for modifiers; labeled “Heat haze” in HUD.</summary>
    static WeatherSnapshot SummerHeatHaze(int variant, int h)
    {
        float sev = 0.11f + (Mathf.Abs(h % 5)) * 0.012f;
        return new WeatherSnapshot(true, false, false, false, false, false, true, false, sev, variant);
    }

    /// <summary>Winter: fewer “clear” days; bias to snow, wind, ice fog, mixed cold storms.</summary>
    static WeatherSnapshot ResolveProceduralWinter(int variant, int m, int d, int h, int roll)
    {
        const int clearThreshold = 38;
        if (roll < clearThreshold)
            return WeatherSnapshot.Clear(variant);

        int h2 = Mathf.Abs(StableHash(variant + 19, m, d * 17 + 3)) % 100;

        if (h2 < 24)
            return SnowEvent(variant, h);

        if (h2 < 40)
            return SnowAndWind(variant, h);

        if (h2 < 54)
            return FogOnly(variant, h);

        if (h2 < 66)
            return WindOnly(variant, h);

        if (h2 < 78)
            return LightSnowFlurries(variant, h);

        if (h2 < 88)
            return WindAndLightRain(variant, h);

        if (h2 < 94)
            return SnowEvent(variant, h ^ 91);

        return SnowAndWind(variant, h ^ 17);
    }

    /// <summary>Autumn (Sep–Nov): wet, windy, low sun — many “bad coat” days; favors driving over walking in sim.</summary>
    static WeatherSnapshot ResolveProceduralAutumn(int variant, int m, int d, int h, int roll)
    {
        const int clearThreshold = 30;
        if (roll < clearThreshold)
            return WeatherSnapshot.Clear(variant);

        int h2 = Mathf.Abs(StableHash(variant + 7, m * 3, d + 11)) % 100;

        if (h2 < 20)
            return HeavyRainEvent(variant, h);

        if (h2 < 36)
            return AutumnGaleRain(variant, h);

        if (h2 < 50)
            return WindAndLightRain(variant, h);

        if (h2 < 62)
            return HeavyRainAndFog(variant, h);

        if (h2 < 74)
            return FogOnly(variant, h);

        if (h2 < 84)
            return WindOnly(variant, h);

        if (h2 < 92)
            return LightRainOnly(variant, h);

        return HeavyRainEvent(variant, h ^ 53);
    }

    /// <summary>Heavy rain + strong wind — near-storm; high severity.</summary>
    static WeatherSnapshot AutumnGaleRain(int variant, int h)
    {
        float sev = 0.62f + (Mathf.Abs(h % 6)) * 0.02f;
        return new WeatherSnapshot(true, false, true, false, true, true, false, false, sev, variant);
    }

    static WeatherSnapshot HeavyRainAndFog(int variant, int h)
    {
        bool wind = (h % 4) != 0;
        float sev = 0.55f + (Mathf.Abs(h % 5)) * 0.03f;
        return new WeatherSnapshot(true, false, true, false, wind, false, true, false, sev, variant);
    }

    static WeatherSnapshot SnowAndWind(int variant, int h)
    {
        bool strong = (h % 6) < 2;
        float sev = strong ? 0.72f : 0.52f;
        return new WeatherSnapshot(true, false, false, true, true, strong, false, false, sev, variant);
    }

    static WeatherSnapshot LightSnowFlurries(int variant, int h)
    {
        float sev = 0.11f + (Mathf.Abs(h % 7)) * 0.015f;
        return new WeatherSnapshot(true, false, false, true, false, false, false, false, sev, variant);
    }

    static WeatherSnapshot WindOnly(int variant, int h)
    {
        bool strong = (h & 1) == 0;
        float sev = strong ? 0.28f : 0.18f;
        return new WeatherSnapshot(true, false, false, false, true, strong, false, false, sev, variant);
    }

    static WeatherSnapshot LightRainOnly(int variant, int h)
    {
        float sev = 0.14f + (Mathf.Abs(h % 7)) * 0.01f;
        return new WeatherSnapshot(true, true, false, false, false, false, false, false, sev, variant);
    }

    static WeatherSnapshot WindAndLightRain(int variant, int h)
    {
        bool strong = (h % 5) == 0;
        float sev = strong ? 0.55f : 0.40f;
        return new WeatherSnapshot(true, true, false, false, true, strong, false, false, sev, variant);
    }

    static WeatherSnapshot FogOnly(int variant, int h)
    {
        float sev = 0.22f + (Mathf.Abs(h % 5)) * 0.02f;
        return new WeatherSnapshot(true, false, false, false, false, false, true, false, sev, variant);
    }

    static WeatherSnapshot SnowEvent(int variant, int h)
    {
        bool wind = (h % 3) != 0;
        float sev = wind ? 0.48f : 0.35f;
        return new WeatherSnapshot(true, false, false, true, wind, false, false, false, sev, variant);
    }

    static WeatherSnapshot HeavyRainEvent(int variant, int h)
    {
        bool wind = (h % 2) == 0;
        float sev = wind ? 0.52f : 0.38f;
        return new WeatherSnapshot(true, false, true, false, wind, false, false, false, sev, variant);
    }

    static int StableHash(int a, int b, int c)
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + a;
            h = h * 31 + b;
            h = h * 31 + c;
            return h;
        }
    }

    /// <summary>Single HUD line in English; empty when <see cref="WeatherSnapshot.HasGameplayEffects"/> is false.</summary>
    public static string BuildHudWeatherLine(in WeatherSnapshot w)
    {
        if (!w.HasGameplayEffects)
            return string.Empty;

        if (w.HeatWave)
        {
            if (w.Severity01 >= 0.44f)
                return "Heat stress";
            return "Heat wave";
        }

        if (w.Fog && !w.LightRain && !w.HeavyRain && !w.Snow && !w.Wind && !w.StrongWind && w.Severity01 <= 0.17f)
            return "Heat haze";

        if (w.Snow && w.Severity01 < 0.18f && !w.Wind && !w.StrongWind && !w.Fog && !w.LightRain && !w.HeavyRain)
            return "Light snow";

        if (w.Snow && w.StrongWind && w.Wind)
            return "Heavy snow · strong wind";

        List<string> parts = new List<string>(6);
        if (w.HeavyRain)
            parts.Add("Heavy rain");
        else if (w.LightRain)
            parts.Add("Light rain");

        if (w.Snow)
            parts.Add("Snow");

        if (w.Fog)
            parts.Add("Fog");

        if (w.StrongWind)
            parts.Add("Strong wind");
        else if (w.Wind)
            parts.Add("Wind");

        if (parts.Count == 0)
            return string.Empty;

        string core = string.Join(" · ", parts);
        if (w.IsStormLike && !w.Snow)
            return core + " — storm risk";
        return core;
    }
}
