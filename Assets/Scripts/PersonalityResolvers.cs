using UnityEngine;
using System.Collections.Generic;

public static class PersonalityProgressionResolver
{
    static readonly Dictionary<PersonalityTraitType, PersonalityRuleTuning> Rules = BuildRules();
    static readonly Dictionary<PersonalityTraitType, PersonalityTraitType> Opposites = BuildOpposites();

    public static void ApplyObservedEvent(
        string characterId,
        PersonalityObservedEventType observedEvent,
        PersonalityContext context,
        int eventWeight = 1)
    {
        var deltas = PersonalityTriggerTable.GetDeltas(observedEvent);
        if (deltas == null || deltas.Length == 0) return;
        int ctxMultiplier = ContextMultiplier(context);
        for (int i = 0; i < deltas.Length; i++)
        {
            var d = deltas[i];
            int w = Mathf.Clamp(d.Weight * eventWeight * ctxMultiplier, -9, 12);
            if (w > 0)
            {
                if (d.IsMajorEvent) ApplyMajorEvent(characterId, d.TraitType, Mathf.Max(1, w / 2), PersonalityTraitSource.Behavior);
                else ApplyBehaviorPattern(characterId, d.TraitType, Mathf.Max(1, w), PersonalityTraitSource.Behavior);
            }
            else if (w < 0)
            {
                ApplyOppositeBehavior(characterId, d.TraitType, Mathf.Max(1, -w));
            }
        }
    }

    public static void ApplyBehaviorPattern(
        string characterId,
        PersonalityTraitType traitType,
        int behaviorWeight = 1,
        PersonalityTraitSource source = PersonalityTraitSource.Behavior)
    {
        var profile = PersonalityWorldState.EnsureProfile(characterId);
        var trait = PersonalityWorldState.EnsureTrait(profile, traitType);
        var progress = PersonalityWorldState.EnsureProgress(characterId, traitType);
        if (profile == null || trait == null || progress == null) return;

        PersonalityRuleTuning rule = GetRule(traitType);
        int gain = Mathf.Clamp(behaviorWeight * rule.RepeatedGain, 6, 70);
        progress.progressXp += gain;
        progress.repeatedBehaviorCount += 1;
        progress.lastTriggerInt = (int)PersonalityProgressTrigger.RepeatedBehavior;
        progress.trendDirectionInt = (int)PersonalityTrendDirection.Rising;
        trait.sourceInt = (int)source;
        trait.trendDirectionInt = (int)PersonalityTrendDirection.Rising;
        ApplyOppositeSuppression(characterId, traitType, Mathf.Max(1, gain / 14));
        RecomputeLevel(trait, progress);
    }

    public static void ApplyMajorEvent(
        string characterId,
        PersonalityTraitType traitType,
        int eventImpact = 1,
        PersonalityTraitSource source = PersonalityTraitSource.Trauma)
    {
        var profile = PersonalityWorldState.EnsureProfile(characterId);
        var trait = PersonalityWorldState.EnsureTrait(profile, traitType);
        var progress = PersonalityWorldState.EnsureProgress(characterId, traitType);
        if (profile == null || trait == null || progress == null) return;

        PersonalityRuleTuning rule = GetRule(traitType);
        int gain = Mathf.Clamp(eventImpact * rule.MajorEventGain, 25, 220);
        progress.progressXp += gain;
        progress.majorEventCount += 1;
        progress.lastTriggerInt = (int)PersonalityProgressTrigger.MajorEvent;
        progress.trendDirectionInt = (int)PersonalityTrendDirection.Rising;
        trait.sourceInt = (int)source;
        trait.trendDirectionInt = (int)PersonalityTrendDirection.Rising;
        ApplyOppositeSuppression(characterId, traitType, Mathf.Max(1, gain / 28));
        RecomputeLevel(trait, progress);
    }

    public static void ApplyTrainingAgainstTrait(
        string characterId,
        PersonalityTraitType traitType,
        int trainingWeight = 1)
    {
        var profile = PersonalityWorldState.EnsureProfile(characterId);
        var trait = PersonalityWorldState.EnsureTrait(profile, traitType);
        var progress = PersonalityWorldState.EnsureProgress(characterId, traitType);
        if (profile == null || trait == null || progress == null) return;

        PersonalityRuleTuning rule = GetRule(traitType);
        int decay = Mathf.Clamp(trainingWeight * rule.OppositeDecay, 8, 70);
        progress.decayXp += decay;
        progress.lastTriggerInt = (int)PersonalityProgressTrigger.Training;
        progress.trendDirectionInt = (int)PersonalityTrendDirection.Falling;
        trait.sourceInt = (int)PersonalityTraitSource.Training;
        trait.trendDirectionInt = (int)PersonalityTrendDirection.Falling;
        RecomputeLevel(trait, progress);
    }

    public static void ApplyOppositeBehavior(
        string characterId,
        PersonalityTraitType traitType,
        int oppositeWeight = 1)
    {
        var profile = PersonalityWorldState.EnsureProfile(characterId);
        var trait = PersonalityWorldState.EnsureTrait(profile, traitType);
        var progress = PersonalityWorldState.EnsureProgress(characterId, traitType);
        if (profile == null || trait == null || progress == null) return;

        PersonalityRuleTuning rule = GetRule(traitType);
        int decay = Mathf.Clamp(oppositeWeight * rule.OppositeDecay, 6, 80);
        progress.decayXp += decay;
        progress.lastTriggerInt = (int)PersonalityProgressTrigger.RepeatedBehavior;
        progress.trendDirectionInt = (int)PersonalityTrendDirection.Falling;
        trait.trendDirectionInt = (int)PersonalityTrendDirection.Falling;
        RecomputeLevel(trait, progress);
    }

    public static void ApplyEnvironmentPressure(
        string characterId,
        PersonalityTraitType traitType,
        int pressureSteps,
        PersonalityTraitSource source = PersonalityTraitSource.Environment)
    {
        var profile = PersonalityWorldState.EnsureProfile(characterId);
        var trait = PersonalityWorldState.EnsureTrait(profile, traitType);
        var progress = PersonalityWorldState.EnsureProgress(characterId, traitType);
        if (profile == null || trait == null || progress == null) return;

        PersonalityRuleTuning rule = GetRule(traitType);
        int gain = Mathf.Clamp(pressureSteps * rule.EnvironmentGainPerStep, -120, 120);
        progress.environmentPressure = Mathf.Clamp(progress.environmentPressure + gain, -200, 200);
        if (gain >= 0) progress.progressXp += gain;
        else progress.decayXp += -gain;
        progress.lastTriggerInt = (int)PersonalityProgressTrigger.Environment;
        progress.trendDirectionInt = gain >= 0 ? (int)PersonalityTrendDirection.Rising : (int)PersonalityTrendDirection.Falling;
        trait.sourceInt = (int)source;
        trait.trendDirectionInt = progress.trendDirectionInt;
        RecomputeLevel(trait, progress);
    }

    static void RecomputeLevel(PersonalityTraitInstance trait, PersonalityTraitProgress progress)
    {
        int net = Mathf.Max(0, progress.progressXp - progress.decayXp);
        int level = net >= 500 ? 3 : (net >= 250 ? 2 : (net >= 100 ? 1 : 0));
        if (level == 0 && progress.repeatedBehaviorCount >= 3) level = 1;
        // Level 3 cap: only with long pattern + strong events + high stability pressure.
        if (level >= 3)
        {
            bool longPattern = progress.repeatedBehaviorCount >= 18;
            bool major = progress.majorEventCount >= 2;
            bool stableBase = (35 + progress.repeatedBehaviorCount * 2 + progress.majorEventCount * 5) >= 72;
            bool extremeEnvironment = Mathf.Abs(progress.environmentPressure) >= 80;
            if (!(longPattern && (major || extremeEnvironment) && stableBase))
                level = 2;
        }
        trait.intensity = Mathf.Clamp(level <= 0 ? 1 : level, 1, 3);
        trait.stability = Mathf.Clamp(35 + progress.repeatedBehaviorCount * 2 + progress.majorEventCount * 5, 0, 100);
        trait.visibilityInt = DetermineVisibility(progress.repeatedBehaviorCount, progress.majorEventCount);
    }

    static PersonalityRuleTuning GetRule(PersonalityTraitType t)
    {
        if (Rules.TryGetValue(t, out var v)) return v;
        return new PersonalityRuleTuning(12, 65, 18, 10);
    }

    static Dictionary<PersonalityTraitType, PersonalityRuleTuning> BuildRules()
    {
        return new Dictionary<PersonalityTraitType, PersonalityRuleTuning>
        {
            { PersonalityTraitType.Disciplined, new PersonalityRuleTuning(14, 60, 20, 12) },
            { PersonalityTraitType.Loyal, new PersonalityRuleTuning(12, 70, 18, 10) },
            { PersonalityTraitType.Brave, new PersonalityRuleTuning(13, 75, 20, 8) },
            { PersonalityTraitType.Ideological, new PersonalityRuleTuning(10, 80, 16, 9) },
            { PersonalityTraitType.Patient, new PersonalityRuleTuning(12, 55, 20, 11) },
            { PersonalityTraitType.Calm, new PersonalityRuleTuning(12, 60, 18, 10) },
            { PersonalityTraitType.Curious, new PersonalityRuleTuning(12, 58, 16, 10) },
            { PersonalityTraitType.Creative, new PersonalityRuleTuning(12, 65, 14, 9) },
            { PersonalityTraitType.Ambitious, new PersonalityRuleTuning(13, 70, 18, 10) },
            { PersonalityTraitType.Charismatic, new PersonalityRuleTuning(11, 68, 15, 8) },
            { PersonalityTraitType.Protective, new PersonalityRuleTuning(12, 72, 18, 10) },
            { PersonalityTraitType.Impulsive, new PersonalityRuleTuning(13, 70, 22, 12) },
            { PersonalityTraitType.MoneyGreedy, new PersonalityRuleTuning(12, 74, 18, 11) },
            { PersonalityTraitType.Proud, new PersonalityRuleTuning(11, 68, 17, 9) },
            { PersonalityTraitType.Suspicious, new PersonalityRuleTuning(12, 76, 15, 10) },
            { PersonalityTraitType.Paranoid, new PersonalityRuleTuning(10, 85, 14, 12) },
            { PersonalityTraitType.Vengeful, new PersonalityRuleTuning(11, 82, 16, 10) },
            { PersonalityTraitType.Cruel, new PersonalityRuleTuning(11, 78, 18, 11) },
            { PersonalityTraitType.Cowardly, new PersonalityRuleTuning(11, 80, 20, 11) },
            { PersonalityTraitType.Sadistic, new PersonalityRuleTuning(10, 84, 15, 12) },
            { PersonalityTraitType.Treacherous, new PersonalityRuleTuning(12, 82, 16, 11) },
            { PersonalityTraitType.Calculated, new PersonalityRuleTuning(13, 65, 17, 10) },
            { PersonalityTraitType.Methodical, new PersonalityRuleTuning(14, 60, 18, 12) },
            { PersonalityTraitType.Undisciplined, new PersonalityRuleTuning(13, 66, 18, 11) },
            { PersonalityTraitType.Impatient, new PersonalityRuleTuning(12, 64, 18, 10) },
            { PersonalityTraitType.Reactive, new PersonalityRuleTuning(13, 68, 18, 10) },
            { PersonalityTraitType.Indifferent, new PersonalityRuleTuning(10, 58, 16, 8) },
            { PersonalityTraitType.Conventional, new PersonalityRuleTuning(11, 55, 15, 9) },
            { PersonalityTraitType.Complacent, new PersonalityRuleTuning(11, 60, 16, 10) },
            { PersonalityTraitType.Alienating, new PersonalityRuleTuning(12, 67, 15, 9) },
            { PersonalityTraitType.Predatory, new PersonalityRuleTuning(11, 72, 16, 10) },
            { PersonalityTraitType.Humble, new PersonalityRuleTuning(10, 56, 14, 8) },
            { PersonalityTraitType.Trusting, new PersonalityRuleTuning(10, 58, 15, 8) },
            { PersonalityTraitType.Secure, new PersonalityRuleTuning(10, 60, 15, 8) },
            { PersonalityTraitType.Forgiving, new PersonalityRuleTuning(10, 62, 15, 9) },
            { PersonalityTraitType.Compassionate, new PersonalityRuleTuning(11, 64, 16, 10) },
            { PersonalityTraitType.Merciful, new PersonalityRuleTuning(10, 66, 16, 10) },
            { PersonalityTraitType.Instinctive, new PersonalityRuleTuning(12, 62, 16, 9) },
            { PersonalityTraitType.Chaotic, new PersonalityRuleTuning(13, 70, 18, 11) }
        };
    }

    static Dictionary<PersonalityTraitType, PersonalityTraitType> BuildOpposites()
    {
        return new Dictionary<PersonalityTraitType, PersonalityTraitType>
        {
            { PersonalityTraitType.Disciplined, PersonalityTraitType.Undisciplined },
            { PersonalityTraitType.Undisciplined, PersonalityTraitType.Disciplined },
            { PersonalityTraitType.Loyal, PersonalityTraitType.Treacherous },
            { PersonalityTraitType.Treacherous, PersonalityTraitType.Loyal },
            { PersonalityTraitType.Brave, PersonalityTraitType.Cowardly },
            { PersonalityTraitType.Cowardly, PersonalityTraitType.Brave },
            { PersonalityTraitType.Ideological, PersonalityTraitType.MoneyGreedy },
            { PersonalityTraitType.MoneyGreedy, PersonalityTraitType.Ideological },
            { PersonalityTraitType.Patient, PersonalityTraitType.Impatient },
            { PersonalityTraitType.Impatient, PersonalityTraitType.Patient },
            { PersonalityTraitType.Calm, PersonalityTraitType.Reactive },
            { PersonalityTraitType.Reactive, PersonalityTraitType.Calm },
            { PersonalityTraitType.Curious, PersonalityTraitType.Indifferent },
            { PersonalityTraitType.Indifferent, PersonalityTraitType.Curious },
            { PersonalityTraitType.Creative, PersonalityTraitType.Conventional },
            { PersonalityTraitType.Conventional, PersonalityTraitType.Creative },
            { PersonalityTraitType.Ambitious, PersonalityTraitType.Complacent },
            { PersonalityTraitType.Complacent, PersonalityTraitType.Ambitious },
            { PersonalityTraitType.Charismatic, PersonalityTraitType.Alienating },
            { PersonalityTraitType.Alienating, PersonalityTraitType.Charismatic },
            { PersonalityTraitType.Protective, PersonalityTraitType.Predatory },
            { PersonalityTraitType.Predatory, PersonalityTraitType.Protective },
            { PersonalityTraitType.Impulsive, PersonalityTraitType.Calculated },
            { PersonalityTraitType.Calculated, PersonalityTraitType.Impulsive },
            { PersonalityTraitType.Proud, PersonalityTraitType.Humble },
            { PersonalityTraitType.Humble, PersonalityTraitType.Proud },
            { PersonalityTraitType.Suspicious, PersonalityTraitType.Trusting },
            { PersonalityTraitType.Trusting, PersonalityTraitType.Suspicious },
            { PersonalityTraitType.Paranoid, PersonalityTraitType.Secure },
            { PersonalityTraitType.Secure, PersonalityTraitType.Paranoid },
            { PersonalityTraitType.Vengeful, PersonalityTraitType.Forgiving },
            { PersonalityTraitType.Forgiving, PersonalityTraitType.Vengeful },
            { PersonalityTraitType.Cruel, PersonalityTraitType.Compassionate },
            { PersonalityTraitType.Compassionate, PersonalityTraitType.Cruel },
            { PersonalityTraitType.Sadistic, PersonalityTraitType.Merciful },
            { PersonalityTraitType.Merciful, PersonalityTraitType.Sadistic },
            { PersonalityTraitType.Methodical, PersonalityTraitType.Chaotic },
            { PersonalityTraitType.Chaotic, PersonalityTraitType.Methodical },
            { PersonalityTraitType.Instinctive, PersonalityTraitType.Calculated }
        };
    }

    static void ApplyOppositeSuppression(string characterId, PersonalityTraitType traitType, int oppositeWeight)
    {
        if (!Opposites.TryGetValue(traitType, out var opposite))
            return;
        var profile = PersonalityWorldState.EnsureProfile(characterId);
        var oppTrait = PersonalityWorldState.EnsureTrait(profile, opposite);
        var oppProgress = PersonalityWorldState.EnsureProgress(characterId, opposite);
        if (oppTrait == null || oppProgress == null) return;
        int decay = Mathf.Clamp(oppositeWeight * 16, 6, 80);
        oppProgress.decayXp += decay;
        oppProgress.trendDirectionInt = (int)PersonalityTrendDirection.Falling;
        oppTrait.trendDirectionInt = (int)PersonalityTrendDirection.Falling;
        RecomputeLevel(oppTrait, oppProgress);
    }

    static int ContextMultiplier(PersonalityContext context)
    {
        int m = 1;
        if (context.IsLeaderContext) m += 1;
        if (context.IsFederalContext || context.IsPoliceContext) m += 1;
        return Mathf.Clamp(m, 1, 3);
    }

    static int DetermineVisibility(int repeats, int events)
    {
        int score = repeats + events * 3;
        if (score >= 15) return (int)PersonalityVisibility.Confirmed;
        if (score >= 7) return (int)PersonalityVisibility.Known;
        if (score >= 3) return (int)PersonalityVisibility.Suspected;
        return (int)PersonalityVisibility.Unknown;
    }
}

public static class PersonalityEffectResolver
{
    public static PersonalityTraitEffect ResolveTraitEffect(
        PersonalityTraitType traitType,
        int intensity,
        PersonalityContext context)
    {
        int m = Mathf.Clamp(intensity, 1, 3);
        switch (traitType)
        {
            case PersonalityTraitType.Disciplined:
                return context.IsLeaderContext
                    ? new PersonalityTraitEffect(2 * m, 4 * m, -1 * m, -1 * m, 2 * m)
                    : new PersonalityTraitEffect(4 * m, 2 * m, -1 * m, -1 * m, 1 * m);
            case PersonalityTraitType.Creative:
                return context.IsLeaderContext
                    ? new PersonalityTraitEffect(-1 * m, -1 * m, 2 * m, 2 * m, 2 * m)
                    : new PersonalityTraitEffect(-1 * m, -1 * m, 1 * m, 2 * m, 2 * m);
            case PersonalityTraitType.Paranoid:
                return new PersonalityTraitEffect(-1 * m, -1 * m, -1 * m, 1 * m, 2 * m);
            case PersonalityTraitType.Calculated:
                return new PersonalityTraitEffect(2 * m, 3 * m, -2 * m, 2 * m, 3 * m);
            case PersonalityTraitType.Impulsive:
                return new PersonalityTraitEffect(-2 * m, -2 * m, 3 * m, -1 * m, -1 * m);
            case PersonalityTraitType.Treacherous:
                return new PersonalityTraitEffect(-3 * m, -2 * m, 0, 4 * m, -2 * m);
            case PersonalityTraitType.Cowardly:
                return new PersonalityTraitEffect(-2 * m, -3 * m, -2 * m, 1 * m, -1 * m);
            case PersonalityTraitType.Loyal:
                return new PersonalityTraitEffect(3 * m, 2 * m, 0, -2 * m, 1 * m);
            default:
                return new PersonalityTraitEffect(0, 0, 0, 0, 0);
        }
    }

    public static PersonalityTraitEffect ResolveProfileEffect(PersonalityProfile profile, PersonalityContext context)
    {
        if (profile == null || profile.traits == null || profile.traits.Count == 0)
            return new PersonalityTraitEffect(0, 0, 0, 0, 0);
        int c = 0, ls = 0, r = 0, d = 0, iq = 0;
        for (int i = 0; i < profile.traits.Count; i++)
        {
            var t = profile.traits[i];
            if (t == null) continue;
            var e = ResolveTraitEffect((PersonalityTraitType)t.traitTypeInt, t.intensity, context);
            c += e.ComplianceDelta;
            ls += e.LeadershipStabilityDelta;
            r += e.RiskTakingDelta;
            d += e.DeceptionDelta;
            iq += e.IntelligenceQualityDelta;
        }
        return new PersonalityTraitEffect(c, ls, r, d, iq);
    }
}

public static class TraitConflictResolver
{
    public static void NormalizeProfile(string characterId)
    {
        var profile = PersonalityWorldState.EnsureProfile(characterId);
        if (profile == null || profile.traits == null) return;
        // Keep only stronger side of opposite pairs for "no oxymoron" rule.
        for (int i = 0; i < profile.traits.Count; i++)
        {
            var a = profile.traits[i];
            if (a == null) continue;
            if (!PersonalityProgressionResolverOppositeMap.TryGet(a.traitTypeInt, out int oppType)) continue;
            var b = FindTrait(profile, oppType);
            if (b == null) continue;
            if (a.intensity == b.intensity && a.stability == b.stability) continue;
            bool aWins = a.intensity > b.intensity || (a.intensity == b.intensity && a.stability >= b.stability);
            if (aWins) { b.intensity = 1; b.stability = Mathf.Clamp(b.stability - 20, 0, 100); }
            else { a.intensity = 1; a.stability = Mathf.Clamp(a.stability - 20, 0, 100); }
        }
    }

    static PersonalityTraitInstance FindTrait(PersonalityProfile p, int tt)
    {
        for (int i = 0; i < p.traits.Count; i++)
        {
            var t = p.traits[i];
            if (t != null && t.traitTypeInt == tt) return t;
        }
        return null;
    }
}

static class PersonalityProgressionResolverOppositeMap
{
    static readonly Dictionary<int, int> _map = new Dictionary<int, int>
    {
        {(int)PersonalityTraitType.Disciplined, (int)PersonalityTraitType.Undisciplined},
        {(int)PersonalityTraitType.Undisciplined, (int)PersonalityTraitType.Disciplined},
        {(int)PersonalityTraitType.Loyal, (int)PersonalityTraitType.Treacherous},
        {(int)PersonalityTraitType.Treacherous, (int)PersonalityTraitType.Loyal},
        {(int)PersonalityTraitType.Brave, (int)PersonalityTraitType.Cowardly},
        {(int)PersonalityTraitType.Cowardly, (int)PersonalityTraitType.Brave},
        {(int)PersonalityTraitType.Ideological, (int)PersonalityTraitType.MoneyGreedy},
        {(int)PersonalityTraitType.MoneyGreedy, (int)PersonalityTraitType.Ideological},
        {(int)PersonalityTraitType.Patient, (int)PersonalityTraitType.Impatient},
        {(int)PersonalityTraitType.Impatient, (int)PersonalityTraitType.Patient},
        {(int)PersonalityTraitType.Calm, (int)PersonalityTraitType.Reactive},
        {(int)PersonalityTraitType.Reactive, (int)PersonalityTraitType.Calm},
        {(int)PersonalityTraitType.Curious, (int)PersonalityTraitType.Indifferent},
        {(int)PersonalityTraitType.Indifferent, (int)PersonalityTraitType.Curious},
        {(int)PersonalityTraitType.Creative, (int)PersonalityTraitType.Conventional},
        {(int)PersonalityTraitType.Conventional, (int)PersonalityTraitType.Creative},
        {(int)PersonalityTraitType.Ambitious, (int)PersonalityTraitType.Complacent},
        {(int)PersonalityTraitType.Complacent, (int)PersonalityTraitType.Ambitious},
        {(int)PersonalityTraitType.Charismatic, (int)PersonalityTraitType.Alienating},
        {(int)PersonalityTraitType.Alienating, (int)PersonalityTraitType.Charismatic},
        {(int)PersonalityTraitType.Protective, (int)PersonalityTraitType.Predatory},
        {(int)PersonalityTraitType.Predatory, (int)PersonalityTraitType.Protective},
        {(int)PersonalityTraitType.Impulsive, (int)PersonalityTraitType.Calculated},
        {(int)PersonalityTraitType.Calculated, (int)PersonalityTraitType.Impulsive},
        {(int)PersonalityTraitType.Proud, (int)PersonalityTraitType.Humble},
        {(int)PersonalityTraitType.Humble, (int)PersonalityTraitType.Proud},
        {(int)PersonalityTraitType.Suspicious, (int)PersonalityTraitType.Trusting},
        {(int)PersonalityTraitType.Trusting, (int)PersonalityTraitType.Suspicious},
        {(int)PersonalityTraitType.Paranoid, (int)PersonalityTraitType.Secure},
        {(int)PersonalityTraitType.Secure, (int)PersonalityTraitType.Paranoid},
        {(int)PersonalityTraitType.Vengeful, (int)PersonalityTraitType.Forgiving},
        {(int)PersonalityTraitType.Forgiving, (int)PersonalityTraitType.Vengeful},
        {(int)PersonalityTraitType.Cruel, (int)PersonalityTraitType.Compassionate},
        {(int)PersonalityTraitType.Compassionate, (int)PersonalityTraitType.Cruel},
        {(int)PersonalityTraitType.Sadistic, (int)PersonalityTraitType.Merciful},
        {(int)PersonalityTraitType.Merciful, (int)PersonalityTraitType.Sadistic},
        {(int)PersonalityTraitType.Methodical, (int)PersonalityTraitType.Chaotic},
        {(int)PersonalityTraitType.Chaotic, (int)PersonalityTraitType.Methodical},
    };

    public static bool TryGet(int tt, out int opp) => _map.TryGetValue(tt, out opp);
}

