using UnityEngine;

/// <summary>
/// Northern Hemisphere, urban 1920s — abstract modifiers. Tune values in one place.
/// <list type="bullet">
/// <item><b>Vehicle travel</b> — roads / ice / traffic; milder than on-foot penalties.</item>
/// <item><b>Foot travel</b> — walking: heat breaks (summer), rain vs mud (autumn), ice (winter).</item>
/// <item><b>Foot slip hazard</b> — 0..1 for mud/ice (future: injury, noise, failed approach).</item>
/// <item><b>Execution</b> — heat, cold, crowds, daylight fatigue on the job site.</item>
/// <item><b>Street visibility exposure</b> — witnesses / patrol noticing you (existing ambient exposure).</item>
/// <item><b>Visual ID difficulty</b> — faces &amp; silhouettes at distance (winter dusk, fog season — harder to ID anyone).</item>
/// <item><b>Ranged &amp; thrown effectiveness</b> — firearms, thrown weapons, grenades / bottles: rain, wind, cold hands, breath steam.</item>
/// <item><b>Patrol presence</b> — beat density hook.</item>
/// </list>
/// <para>
/// <b>Design note:</b> Low visibility can help anonymity while <i>also</i> hurting shooting and throws for <i>everyone</i>
/// (storm, heavy fog, downpour). Future <c>WeatherOverlay</c> (fog, storm, heavy rain) should stack on these baselines.
/// </para>
/// </summary>
public static class SeasonGameplayModifiers
{
    /// <summary>Multiplies <b>vehicle</b> block-to-block travel hours (roads, not walking).</summary>
    public static float GetVehicleTravelDurationMultiplier(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 1.02f;
            case GameSeason.Summer:
                return 0.97f;
            case GameSeason.Autumn:
                return 1.05f;
            case GameSeason.Winter:
                return 1.09f;
            default:
                return 1f;
        }
    }

    /// <summary>Multiplies <b>on-foot</b> travel hours (walking / transit on foot).</summary>
    public static float GetFootTravelDurationMultiplier(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 1.00f;
            case GameSeason.Summer:
                return 1.18f;
            case GameSeason.Autumn:
                return GetAutumnFootTravelDurationMultiplierBlended();
            case GameSeason.Winter:
                return 1.32f;
            default:
                return 1f;
        }
    }

    /// <summary>Autumn: rain-slick and mud season — on foot is rough; car still pays for wet roads.</summary>
    static float GetAutumnFootTravelDurationMultiplierBlended()
    {
        const float rainStretchPace = 0.93f;
        const float mudStretchPace = 1.22f;
        const float mudWeight = 0.64f;
        return Mathf.Lerp(rainStretchPace, mudStretchPace, mudWeight);
    }

    /// <summary>0 = none, 1 = severe — mud (autumn) and ice (winter).</summary>
    public static float GetFootSlipHazard(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 0f;
            case GameSeason.Summer:
                return 0f;
            case GameSeason.Autumn:
                return 0.44f;
            case GameSeason.Winter:
                return 0.52f;
            default:
                return 0f;
        }
    }

    /// <summary>Multiplies on-site execution hours (weather fatigue, crowds, coordination).</summary>
    public static float GetExecutionDurationMultiplier(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 1.00f;
            case GameSeason.Summer:
                return 1.06f;
            case GameSeason.Autumn:
                return 1.03f;
            case GameSeason.Winter:
                return 1.08f;
            default:
                return 1f;
        }
    }

    /// <summary>
    /// 0 = low ambient exposure, 1 = high. How easy it is for witnesses / police to notice <b>routine street activity</b> (crowds, daylight, bustle).
    /// </summary>
    public static float GetAmbientVisibilityExposure(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 0.62f;
            case GameSeason.Summer:
                return 0.88f;
            case GameSeason.Autumn:
                return 0.58f;
            case GameSeason.Winter:
                return 0.42f;
            default:
                return 0.6f;
        }
    }

    /// <summary>
    /// 0 = crystal clear sight lines, 1 = very hard to resolve faces / clothing / ID at distance (silhouette, breath steam, low sun, gloom).
    /// Helps stealth and anonymity; <b>not</b> the same as ranged effectiveness (see <see cref="GetRangedAndThrownEffectivenessMultiplier"/>).
    /// </summary>
    public static float GetFaceAndSilhouetteObfuscation(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 0.45f;
            case GameSeason.Summer:
                return 0.28f;
            case GameSeason.Autumn:
                return 0.52f;
            case GameSeason.Winter:
                return 0.70f;
            default:
                return 0.5f;
        }
    }

    /// <summary>
    /// Multiplies effective accuracy, stable flight, and reliable placement for <b>firearms</b>, <b>thrown weapons</b>, and <b>tossed explosives</b>
    /// (wind, heavy rain, shivering, wet grips, mittens). Same bad weather that hides faces also throws off shots and arcs.
    /// </summary>
    public static float GetRangedAndThrownEffectivenessMultiplier(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 0.91f;
            case GameSeason.Summer:
                return 1.00f;
            case GameSeason.Autumn:
                return 0.86f;
            case GameSeason.Winter:
                return 0.79f;
            default:
                return 0.9f;
        }
    }

    /// <summary>
    /// Same as <see cref="GetRangedAndThrownEffectivenessMultiplier(GameSeason)"/>, with optional stacked weather (fog bank, thunderstorm, whiteout).
    /// <paramref name="weatherSeverity01"/>: 0 clear, 1 worst — further reduces effectiveness for everyone in the fight.
    /// </summary>
    public static float GetRangedAndThrownEffectivenessMultiplier(GameSeason s, float weatherSeverity01)
    {
        float b = GetRangedAndThrownEffectivenessMultiplier(s);
        float stormStack = Mathf.Lerp(1f, 0.74f, Mathf.Clamp01(weatherSeverity01));
        return Mathf.Clamp(b * stormStack, 0.3f, 1.05f);
    }

    /// <summary>Stacks fog / storm on face-ID difficulty (0 clear, 1 pea-soup / blizzard).</summary>
    public static float GetFaceAndSilhouetteObfuscation(GameSeason s, float weatherSeverity01)
    {
        float b = GetFaceAndSilhouetteObfuscation(s);
        float extra = Mathf.Lerp(0f, 0.22f, Mathf.Clamp01(weatherSeverity01));
        return Mathf.Clamp01(b + extra * (1f - b));
    }

    /// <summary>
    /// Multiplier for routine patrol / beat density (future: operation success penalty, random stops).
    /// </summary>
    public static float GetRoutinePatrolPresenceMultiplier(GameSeason s)
    {
        switch (s)
        {
            case GameSeason.Spring:
                return 1.00f;
            case GameSeason.Summer:
                return 1.10f;
            case GameSeason.Autumn:
                return 1.00f;
            case GameSeason.Winter:
                return 0.93f;
            default:
                return 1f;
        }
    }

    /// <summary>Clamp helper for rolling visibility into chance modifiers later.</summary>
    public static float GetVisibilityExposureClamped(GameSeason s)
    {
        return Mathf.Clamp01(GetAmbientVisibilityExposure(s));
    }
}
