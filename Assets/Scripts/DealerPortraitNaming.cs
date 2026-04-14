using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Portrait naming: <c>Dealer*</c> for merchants; <c>BossPortrait*</c> for the player boss.
/// Female portraits use <c>F</c> immediately after the prefix (<c>DealerF</c>, <c>BossPortraitF</c>, <c>BossPortraitF1</c>, …).
/// </summary>
public static class DealerPortraitNaming
{
    /// <summary>Merchant / roster keys: nine male <c>Dealer</c>…<c>Dealer9</c>, three female <c>DealerF</c>…</summary>
    public static readonly string[] PortraitResourceKeys =
    {
        "Dealer", "Dealer2", "Dealer3", "Dealer4", "Dealer5", "Dealer6", "Dealer7", "Dealer8", "Dealer9",
        "DealerF", "DealerF1", "DealerF2"
    };

    /// <summary>
    /// Player boss face carousel: nine male <c>BossPortrait</c>…<c>BossPortrait8</c>, three female <c>BossPortraitF</c>…
    /// Matches <c>BossPortraitF.png</c> style assets (F after <c>BossPortrait</c>).
    /// </summary>
    public static readonly string[] BossPlayerPortraitResourceKeys =
    {
        "BossPortrait", "BossPortrait1", "BossPortrait2", "BossPortrait3", "BossPortrait4",
        "BossPortrait5", "BossPortrait6", "BossPortrait7", "BossPortrait8",
        "BossPortraitF", "BossPortraitF1", "BossPortraitF2"
    };

    /// <summary>
    /// True when the resource key denotes a female portrait ( <c>DealerF*</c> or <c>BossPortraitF*</c> ).
    /// </summary>
    public static bool IsFemalePortraitResourceKey(string key)
    {
        string baseName = GetResourceBaseName(key);
        return IsFemaleDealerKey(baseName) || IsFemaleBossPortraitKey(baseName);
    }

    static bool IsFemaleDealerKey(string baseName)
    {
        if (baseName.Length < 7)
            return false;
        if (!baseName.StartsWith("Dealer", System.StringComparison.OrdinalIgnoreCase))
            return false;
        return baseName[6] == 'F' || baseName[6] == 'f';
    }

    static bool IsFemaleBossPortraitKey(string baseName)
    {
        const string prefix = "BossPortrait";
        if (baseName.Length < prefix.Length + 1)
            return false;
        if (!baseName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            return false;
        char c = baseName[prefix.Length];
        return c == 'F' || c == 'f';
    }

    /// <summary>
    /// Normalizes legacy or alternate keys to a canonical key used by thumb UV ( <c>Dealer*</c> / <c>DealerF*</c> ).
    /// </summary>
    public static string NormalizeToCarouselKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "Dealer";

        string baseName = GetResourceBaseName(key);
        const string legacy = "BossPortrait";
        if (baseName.Equals(legacy, System.StringComparison.OrdinalIgnoreCase))
            return "Dealer";

        // BossPortraitF, BossPortraitF1, BossPortraitF2 → DealerF*
        if (baseName.StartsWith(legacy, System.StringComparison.OrdinalIgnoreCase) && baseName.Length > legacy.Length)
        {
            string tail = baseName.Substring(legacy.Length);
            if (tail.Length > 0 && (tail[0] == 'F' || tail[0] == 'f'))
            {
                if (tail.Equals("F", System.StringComparison.OrdinalIgnoreCase))
                    return "DealerF";
                if (tail.Equals("F1", System.StringComparison.OrdinalIgnoreCase))
                    return "DealerF1";
                if (tail.Equals("F2", System.StringComparison.OrdinalIgnoreCase))
                    return "DealerF2";
                return "DealerF";
            }

            if (int.TryParse(tail, out int n))
            {
                if (n <= 0)
                    return "Dealer";
                if (n >= 9)
                    return "Dealer9";
                return "Dealer" + (n + 1);
            }
        }

        string dealerFromBoss = BossPortraitKeyToDealerKey(baseName);
        if (dealerFromBoss != null)
            return dealerFromBoss;

        return baseName;
    }

    /// <summary>
    /// Resolves the boss face carousel index: keeps <c>BossPortrait*</c> keys; maps legacy <c>Dealer*</c> saves to Boss keys.
    /// </summary>
    public static string NormalizeBossPlayerPickerKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return BossPlayerPortraitResourceKeys[0];

        string baseName = GetResourceBaseName(key);
        for (int i = 0; i < BossPlayerPortraitResourceKeys.Length; i++)
        {
            if (BossPlayerPortraitResourceKeys[i].Equals(baseName, System.StringComparison.OrdinalIgnoreCase))
                return BossPlayerPortraitResourceKeys[i];
        }

        string fromDealer = DealerKeyToBossPortraitKey(baseName);
        return fromDealer ?? BossPlayerPortraitResourceKeys[0];
    }

    /// <summary>
    /// Loads a portrait texture (Texture2D or Sprite sheet) from Resources, trying Boss and Dealer filename variants.
    /// </summary>
    public static Texture2D LoadPortraitTexture(string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
            resourceKey = "BossPortrait";

        string key = resourceKey.Trim();
        foreach (string cand in EnumerateLoadCandidates(key))
        {
            Texture2D t = TryLoadTexture(cand);
            if (t != null)
                return t;
        }

        return null;
    }

    static IEnumerable<string> EnumerateLoadCandidates(string key)
    {
        yield return key;

        string d = BossPortraitKeyToDealerKey(key);
        if (d != null && d != key)
            yield return d;

        string b = DealerKeyToBossPortraitKey(key);
        if (b != null && b != key)
            yield return b;

        if (key == "Dealer9")
            yield return "BossPortrait9";

        // Carousel may pick BossPortraitF1/F2 while Resources only has BossPortraitF.png (no separate F1/F2 files).
        // Same for parallel DealerF1/F2 keys — fall back to the shared female texture.
        string bn = GetResourceBaseName(key);
        if (IsFemalePortraitResourceKey(key) &&
            !bn.Equals("BossPortraitF", StringComparison.OrdinalIgnoreCase) &&
            !bn.Equals("DealerF", StringComparison.OrdinalIgnoreCase))
        {
            yield return "BossPortraitF";
            yield return "DealerF";
        }
    }

    /// <summary>Maps a BossPortrait resource name to the parallel Dealer key (for UV / fallbacks).</summary>
    public static string BossPortraitKeyToDealerKey(string keyOrBaseName)
    {
        if (string.IsNullOrEmpty(keyOrBaseName))
            return null;
        string k = GetResourceBaseName(keyOrBaseName);
        switch (k)
        {
            case "BossPortrait":
                return "Dealer";
            case "BossPortrait1":
                return "Dealer2";
            case "BossPortrait2":
                return "Dealer3";
            case "BossPortrait3":
                return "Dealer4";
            case "BossPortrait4":
                return "Dealer5";
            case "BossPortrait5":
                return "Dealer6";
            case "BossPortrait6":
                return "Dealer7";
            case "BossPortrait7":
                return "Dealer8";
            case "BossPortrait8":
                return "Dealer9";
            case "BossPortraitF":
                return "DealerF";
            case "BossPortraitF1":
                return "DealerF1";
            case "BossPortraitF2":
                return "DealerF2";
            default:
                return null;
        }
    }

    /// <summary>Maps a Dealer resource name to the parallel BossPortrait key (legacy files).</summary>
    public static string DealerKeyToBossPortraitKey(string keyOrBaseName)
    {
        if (string.IsNullOrEmpty(keyOrBaseName))
            return null;
        string k = GetResourceBaseName(keyOrBaseName);
        switch (k)
        {
            case "Dealer":
                return "BossPortrait";
            case "Dealer2":
                return "BossPortrait1";
            case "Dealer3":
                return "BossPortrait2";
            case "Dealer4":
                return "BossPortrait3";
            case "Dealer5":
                return "BossPortrait4";
            case "Dealer6":
                return "BossPortrait5";
            case "Dealer7":
                return "BossPortrait6";
            case "Dealer8":
                return "BossPortrait7";
            case "Dealer9":
                return "BossPortrait8";
            case "DealerF":
                return "BossPortraitF";
            case "DealerF1":
                return "BossPortraitF1";
            case "DealerF2":
                return "BossPortraitF2";
            default:
                return null;
        }
    }

    static Texture2D TryLoadTexture(string key)
    {
        Texture2D t = Resources.Load<Texture2D>(key);
        if (t == null)
        {
            Sprite s = Resources.Load<Sprite>(key);
            if (s != null)
                t = s.texture;
        }

        return t;
    }

    public static string GetResourceBaseName(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        string k = key.Trim().Replace('\\', '/');
        int slash = k.LastIndexOf('/');
        if (slash >= 0)
            k = k.Substring(slash + 1);

        return Path.GetFileNameWithoutExtension(k);
    }
}
