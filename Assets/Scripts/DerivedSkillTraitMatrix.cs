using System;
using UnityEngine;

/// <summary>
/// Maps each <see cref="DerivedSkill"/> to one or more <see cref="CoreTrait"/> sources with weights summing to 1.
/// Fewer traits ⇒ each trait carries a larger share (full weight 1.0 for a single-trait skill).
/// Used to: estimate skill "level" from trait stars (until skill XP exists), and to route skill XP back into traits.
/// <para>XP conservation when splitting grants: see <see cref="XpProgressionRules"/>.</para>
/// </summary>
public static class DerivedSkillTraitMatrix
{
    public readonly struct Influence
    {
        public readonly CoreTrait Trait;
        public readonly float Weight;

        public Influence(CoreTrait trait, float weight)
        {
            Trait = trait;
            Weight = weight;
        }
    }

    private static readonly Influence[] Brawling =
    {
        new Influence(CoreTrait.Strength, 0.50f),
        new Influence(CoreTrait.Agility, 0.30f),
        new Influence(CoreTrait.Determination, 0.20f)
    };

    private static readonly Influence[] Firearms =
    {
        new Influence(CoreTrait.Agility, 0.30f),
        new Influence(CoreTrait.Intelligence, 0.30f),
        new Influence(CoreTrait.Determination, 0.40f)
    };

    private static readonly Influence[] Stealth =
    {
        new Influence(CoreTrait.Agility, 0.40f),
        new Influence(CoreTrait.Intelligence, 0.35f),
        new Influence(CoreTrait.MentalResilience, 0.25f)
    };

    private static readonly Influence[] Driving =
    {
        new Influence(CoreTrait.Agility, 0.40f),
        new Influence(CoreTrait.Intelligence, 0.50f),
        new Influence(CoreTrait.MentalResilience, 0.10f)
    };

    private static readonly Influence[] Lockpicking =
    {
        new Influence(CoreTrait.Intelligence, 0.55f),
        new Influence(CoreTrait.Agility, 0.45f)
    };

    private static readonly Influence[] Surveillance =
    {
        new Influence(CoreTrait.Intelligence, 0.30f),
        new Influence(CoreTrait.Agility, 0.15f),
        new Influence(CoreTrait.MentalResilience, 0.25f),
        new Influence(CoreTrait.Determination, 0.30f)
    };

    private static readonly Influence[] Negotiation =
    {
        new Influence(CoreTrait.Charisma, 0.45f),
        new Influence(CoreTrait.Intelligence, 0.40f),
        new Influence(CoreTrait.MentalResilience, 0.15f)
    };

    private static readonly Influence[] Intimidation =
    {
        new Influence(CoreTrait.Strength, 0.40f),
        new Influence(CoreTrait.Charisma, 0.45f),
        new Influence(CoreTrait.MentalResilience, 0.15f)
    };

    private static readonly Influence[] Deception =
    {
        new Influence(CoreTrait.Charisma, 0.40f),
        new Influence(CoreTrait.Intelligence, 0.40f),
        new Influence(CoreTrait.MentalResilience, 0.20f)
    };

    private static readonly Influence[] Logistics =
    {
        new Influence(CoreTrait.Intelligence, 0.40f),
        new Influence(CoreTrait.Determination, 0.40f),
        new Influence(CoreTrait.Agility, 0.20f)
    };

    private static readonly Influence[] Leadership =
    {
        new Influence(CoreTrait.Charisma, 0.40f),
        new Influence(CoreTrait.Determination, 0.35f),
        new Influence(CoreTrait.MentalResilience, 0.25f)
    };

    private static readonly Influence[] Medicine =
    {
        new Influence(CoreTrait.Intelligence, 0.40f),
        new Influence(CoreTrait.Determination, 0.60f)
    };

    private static readonly Influence[] Sabotage =
    {
        new Influence(CoreTrait.Intelligence, 0.40f),
        new Influence(CoreTrait.Determination, 0.60f)
    };

    /// <summary>Weights per trait for this skill; sum = 1 (within float tolerance).</summary>
    public static ReadOnlySpan<Influence> GetInfluences(DerivedSkill skill)
    {
        switch (skill)
        {
            case DerivedSkill.Brawling: return Brawling;
            case DerivedSkill.Firearms: return Firearms;
            case DerivedSkill.Stealth: return Stealth;
            case DerivedSkill.Driving: return Driving;
            case DerivedSkill.Lockpicking: return Lockpicking;
            case DerivedSkill.Surveillance: return Surveillance;
            case DerivedSkill.Negotiation: return Negotiation;
            case DerivedSkill.Intimidation: return Intimidation;
            case DerivedSkill.Deception: return Deception;
            case DerivedSkill.Logistics: return Logistics;
            case DerivedSkill.Leadership: return Leadership;
            case DerivedSkill.Medicine: return Medicine;
            case DerivedSkill.Sabotage: return Sabotage;
            default:
                return Array.Empty<Influence>();
        }
    }

    /// <summary>Initial skill seed from Physical / narrative bars (0–100 → steps 0–5), not from derived potential.</summary>
    public static int GetTraitDerivedStarLevelFromIntrinsicBars(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        if (profile == null)
            return 0;

        ReadOnlySpan<Influence> span = GetInfluences(skill);
        if (span.Length == 0)
            return 0;

        float acc = 0f;
        for (int i = 0; i < span.Length; i++)
        {
            Influence inf = span[i];
            float v = CoreTraitProgression.GetValue(profile, inf.Trait);
            int step = Mathf.Clamp(Mathf.RoundToInt(v / 20f), 0, 5);
            acc += inf.Weight * step;
        }

        return Mathf.Clamp(Mathf.RoundToInt(acc * 2f), 0, StarRubric.MaxLevel);
    }

    /// <summary>Backward-compatible name — uses intrinsic bars only.</summary>
    public static int GetTraitDerivedStarLevel(PlayerCharacterProfile profile, DerivedSkill skill)
    {
        return GetTraitDerivedStarLevelFromIntrinsicBars(profile, skill);
    }
}
