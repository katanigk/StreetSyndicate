using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

/// <summary>
/// Resolves roof sprites for block map UI. Loads <see cref="BlockRoofVisualConfig"/> from Resources unless overridden.
/// </summary>
public static class BlockRoofVisualResolver
{
    public const string DefaultResourcesPath = "BlockRoofs/BlockRoofVisualConfig";

    static BlockRoofVisualConfig _cachedFromResources;
    static bool _resourcesTried;

    /// <summary>Optional editor/runtime override (e.g. wired from a bootstrapper).</summary>
    public static BlockRoofVisualConfig OverrideConfig;

    public static void SetOverride(BlockRoofVisualConfig cfg) => OverrideConfig = cfg;

    internal static BlockRoofVisualConfig ActiveConfig => OverrideConfig != null ? OverrideConfig : LoadFromResources();

    /// <summary>Null if config not loaded (e.g. missing Resources asset).</summary>
    public static BlockRoofVisualConfig TryGetActiveConfig() => ActiveConfig;

    static BlockRoofVisualConfig LoadFromResources()
    {
        if (_resourcesTried)
            return _cachedFromResources;
        _resourcesTried = true;
        _cachedFromResources = Resources.Load<BlockRoofVisualConfig>(DefaultResourcesPath);
        return _cachedFromResources;
    }

    /// <summary>Clears cached Resources load (e.g. after hot reload in editor).</summary>
    public static void ClearCache()
    {
        _cachedFromResources = null;
        _resourcesTried = false;
    }

    public static Sprite ResolveRoof(MicroBlockSpotRuntime spot) => ResolveRoofUi(spot, null, null).Sprite;

    /// <param name="lot">Used with <paramref name="ringLots"/> for 8-tile autotiling when config ring is complete.</param>
    public static Sprite ResolveRoof(MicroBlockSpotRuntime spot, LotData lot, IReadOnlyList<LotData> ringLots) =>
        ResolveRoofUi(spot, lot, ringLots).Sprite;

    /// <summary>Sprite + Z rotation (for one corner sprite reused at four corners).</summary>
    public static RoofUiSpec ResolveRoofUi(MicroBlockSpotRuntime spot, LotData lot, IReadOnlyList<LotData> ringLots)
    {
        if (spot == null)
            return default;
        BlockRoofVisualConfig cfg = ActiveConfig;
        if (cfg == null)
            return default;

        if (!string.IsNullOrEmpty(spot.RoofClusterKey))
        {
            Sprite cluster = cfg.GetRoofForCluster(spot.RoofClusterKey);
            if (cluster != null)
                return new RoofUiSpec(cluster, 0f);
        }

        if (GameSessionState.SingleBlockSandboxEnabled
            && lot != null
            && TryBuildSandboxRingUi(cfg, lot, ringLots, out RoofUiSpec ringSpec))
            return ringSpec;

        if (TryPickSimpleResidentialRoof(cfg, spot, out Sprite residential))
            return new RoofUiSpec(residential, 0f);

        Sprite byKind = cfg.GetRoofForKind(spot.Kind);
        if (byKind != null)
            return new RoofUiSpec(byKind, 0f);

        return new RoofUiSpec(cfg.defaultRoof, 0f);
    }

    /// <summary>Full 8-tile ring, or corner+flat synthesis, or variant cycle by slot.</summary>
    static bool TryBuildSandboxRingUi(BlockRoofVisualConfig cfg, LotData lot, IReadOnlyList<LotData> ringLots, out RoofUiSpec spec)
    {
        spec = default;
        if (ringLots == null || ringLots.Count != 8)
            return false;

        int slot;
        if (!RoofRingGrid.TryGetRingSlotFromSandboxCreationOrder(lot, ringLots, out slot)
            && !RoofRingGrid.TryGetRingSlotIndex(lot, ringLots, out slot))
            return false;

        if (cfg.HasCompleteDenseResidentialRing())
        {
            Sprite s = cfg.denseResidentialRingTiles[slot];
            if (s == null)
                return false;
            spec = new RoofUiSpec(s, 0f);
            return true;
        }

        if (cfg.denseResidentialCornerSouthWest != null)
        {
            if (!TryPickFlatSprite(cfg, slot, out Sprite flat))
                return false;
            if (IsRingCornerSlot(slot))
            {
                spec = new RoofUiSpec(cfg.denseResidentialCornerSouthWest, CornerZRotationDegrees(slot));
                return true;
            }

            spec = new RoofUiSpec(flat, 0f);
            return true;
        }

        if (cfg.simpleResidentialRoofVariants == null || cfg.simpleResidentialRoofVariants.Count == 0)
            return false;

        int n = cfg.simpleResidentialRoofVariants.Count;
        int start = slot % n;
        for (int t = 0; t < n; t++)
        {
            Sprite s = cfg.simpleResidentialRoofVariants[(start + t) % n];
            if (s != null)
            {
                spec = new RoofUiSpec(s, 0f);
                return true;
            }
        }

        return false;
    }

    static bool TryPickFlatSprite(BlockRoofVisualConfig cfg, int ringSlot, out Sprite flat)
    {
        flat = null;
        if (cfg.denseResidentialFlatRoof != null)
        {
            flat = cfg.denseResidentialFlatRoof;
            return true;
        }

        if (cfg.defaultRoof != null)
        {
            flat = cfg.defaultRoof;
            return true;
        }

        if (cfg.simpleResidentialRoofVariants == null || cfg.simpleResidentialRoofVariants.Count == 0)
            return false;
        int n = cfg.simpleResidentialRoofVariants.Count;
        int idx = ringSlot % n;
        for (int t = 0; t < n; t++)
        {
            flat = cfg.simpleResidentialRoofVariants[(idx + t) % n];
            if (flat != null)
                return true;
        }

        return false;
    }

    static bool IsRingCornerSlot(int slot) => slot == 0 || slot == 2 || slot == 5 || slot == 7;

    /// <summary>
    /// Art is a southwest corner (parapet along sprite bottom + left). Map to TL / TR / BR via Z.
    /// Tune in Inspector if a pack uses a different winding.
    /// </summary>
    static float CornerZRotationDegrees(int slot)
    {
        return slot switch
        {
            5 => 0f,
            7 => -90f,
            0 => 90f,
            2 => 180f,
            _ => 0f
        };
    }

    /// <summary>
    /// <see cref="MicroBlockSpotKind.CrewSharedRoom"/> / <see cref="MicroBlockSpotKind.RoomingHouse"/> with
    /// <see cref="BlockRoofVisualConfig.simpleResidentialRoofVariants"/>: deterministic “random” per spot.
    /// Uses <see cref="MicroBlockSpotRuntime.RoofClusterKey"/> when set so merged adjacent cells share the same roof.
    /// </summary>
    static bool TryPickSimpleResidentialRoof(BlockRoofVisualConfig cfg, MicroBlockSpotRuntime spot, out Sprite sprite)
    {
        sprite = null;
        if (cfg.simpleResidentialRoofVariants == null || cfg.simpleResidentialRoofVariants.Count == 0)
            return false;
        if (spot.Kind != MicroBlockSpotKind.CrewSharedRoom && spot.Kind != MicroBlockSpotKind.RoomingHouse)
            return false;

        string key = !string.IsNullOrEmpty(spot.RoofClusterKey) ? spot.RoofClusterKey : spot.StableId;
        var list = cfg.simpleResidentialRoofVariants;
        int m = list.Count;
        int start = StableVariantIndex(key, m);
        for (int t = 0; t < m; t++)
        {
            Sprite s = list[(start + t) % m];
            if (s != null)
            {
                sprite = s;
                return true;
            }
        }

        return false;
    }

    static int StableVariantIndex(string key, int count)
    {
        if (count <= 0)
            return 0;
        if (string.IsNullOrEmpty(key))
            return 0;
        int h = key.GetHashCode();
        uint u = unchecked((uint)h);
        return (int)(u % (uint)count);
    }
}
