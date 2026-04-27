using System;
using System.Collections.Generic;
using UnityEngine;

public static class PersonalityWorldState
{
    public const int SnapshotVersion = 1;
    public static readonly List<PersonalityProfile> Profiles = new List<PersonalityProfile>();
    public static readonly List<PersonalityTraitProgress> Progress = new List<PersonalityTraitProgress>();

    public static void ClearAll()
    {
        Profiles.Clear();
        Progress.Clear();
    }

    public static PersonalityProfile EnsureProfile(string characterId)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        for (int i = 0; i < Profiles.Count; i++)
        {
            if (Profiles[i] != null && string.Equals(Profiles[i].characterId, characterId, StringComparison.OrdinalIgnoreCase))
                return Profiles[i];
        }
        var p = new PersonalityProfile
        {
            characterId = characterId,
            isCivilianContext = true,
            isSubordinateContext = true
        };
        Profiles.Add(p);
        return p;
    }

    public static PersonalityTraitInstance EnsureTrait(PersonalityProfile profile, PersonalityTraitType traitType)
    {
        if (profile == null) return null;
        for (int i = 0; i < profile.traits.Count; i++)
        {
            var t = profile.traits[i];
            if (t != null && t.traitTypeInt == (int)traitType)
                return t;
        }
        var nt = new PersonalityTraitInstance
        {
            traitTypeInt = (int)traitType,
            intensity = 1,
            stability = 40,
            visibilityInt = (int)PersonalityVisibility.Unknown,
            sourceInt = (int)PersonalityTraitSource.Birth,
            trendDirectionInt = (int)PersonalityTrendDirection.Stable
        };
        profile.traits.Add(nt);
        return nt;
    }

    public static PersonalityTraitProgress EnsureProgress(string characterId, PersonalityTraitType traitType)
    {
        if (string.IsNullOrEmpty(characterId)) return null;
        for (int i = 0; i < Progress.Count; i++)
        {
            var p = Progress[i];
            if (p != null && string.Equals(p.characterId, characterId, StringComparison.OrdinalIgnoreCase)
                && p.traitTypeInt == (int)traitType)
                return p;
        }
        var np = new PersonalityTraitProgress
        {
            characterId = characterId,
            traitTypeInt = (int)traitType,
            progressXp = 0,
            decayXp = 0,
            repeatedBehaviorCount = 0,
            majorEventCount = 0,
            environmentPressure = 0,
            trendDirectionInt = (int)PersonalityTrendDirection.Stable,
            lastTriggerInt = (int)PersonalityProgressTrigger.RepeatedBehavior
        };
        Progress.Add(np);
        return np;
    }

    public static string CaptureJson()
    {
        var s = new PersonalityWorldSnapshot
        {
            formatVersion = SnapshotVersion
        };
        s.profiles.AddRange(Profiles);
        s.progress.AddRange(Progress);
        return JsonUtility.ToJson(s, true);
    }

    public static void ApplyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;
        try
        {
            var s = JsonUtility.FromJson<PersonalityWorldSnapshot>(json);
            if (s == null || s.formatVersion <= 0)
                return;
            Profiles.Clear();
            if (s.profiles != null) Profiles.AddRange(s.profiles);
            Progress.Clear();
            if (s.progress != null) Progress.AddRange(s.progress);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Personality] Load snapshot failed: " + e.Message);
        }
    }
}

