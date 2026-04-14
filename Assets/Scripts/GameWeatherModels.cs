using System;

/// <summary>
/// One day’s resolved weather. Only <see cref="HasGameplayEffects"/> should drive HUD text (no filler).
/// </summary>
public readonly struct WeatherSnapshot
{
    public readonly bool HasGameplayEffects;

    public readonly bool LightRain;
    public readonly bool HeavyRain;
    public readonly bool Snow;
    public readonly bool Wind;
    public readonly bool StrongWind;
    public readonly bool Fog;
    public readonly bool HeatWave;

    /// <summary>0 = none, 1 = worst — stacks into ranged / travel / execution hooks.</summary>
    public readonly float Severity01;

    /// <summary>0, 1, or 2 — which annual template is active for this 4-year block.</summary>
    public readonly int VariantIndex;

    public WeatherSnapshot(
        bool hasGameplayEffects,
        bool lightRain,
        bool heavyRain,
        bool snow,
        bool wind,
        bool strongWind,
        bool fog,
        bool heatWave,
        float severity01,
        int variantIndex)
    {
        HasGameplayEffects = hasGameplayEffects;
        LightRain = lightRain;
        HeavyRain = heavyRain;
        Snow = snow;
        Wind = wind;
        StrongWind = strongWind;
        Fog = fog;
        HeatWave = heatWave;
        Severity01 = severity01;
        VariantIndex = variantIndex;
    }

    public bool IsStormLike => (HeavyRain && (Wind || StrongWind)) || (StrongWind && (LightRain || HeavyRain || Snow));

    public static WeatherSnapshot Clear(int variantIndex)
    {
        return new WeatherSnapshot(false, false, false, false, false, false, false, false, 0f, variantIndex);
    }
}
