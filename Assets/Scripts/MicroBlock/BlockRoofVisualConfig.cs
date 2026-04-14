using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps micro-block spot kinds and optional <see cref="MicroBlockSpotRuntime.RoofClusterKey"/> to roof sprites (top-down).
/// Place an asset at Resources path <c>BlockRoofs/BlockRoofVisualConfig</c> or assign via <see cref="BlockRoofVisualResolver"/>.
/// </summary>
[CreateAssetMenu(fileName = "BlockRoofVisualConfig", menuName = "Street Syndicate/Micro Block/Roof Visual Config")]
public sealed class BlockRoofVisualConfig : ScriptableObject
{
    [Tooltip("Used when a kind has no entry and cluster lookup fails.")]
    public Sprite defaultRoof;

    [Tooltip("CrewSharedRoom & RoomingHouse: pick one sprite by stable id (or shared RoofClusterKey for merged cells). Leave empty to use roofsByKind / default only. Ignored when a full 8-tile ring is set below.")]
    public List<Sprite> simpleResidentialRoofVariants = new List<Sprite>();

    [Tooltip("8 roof tiles for sandbox ring: TL, T, TR, L, R, BL, B, BR (+y = north). When all eight are set, block zoom uses position-based tiles for every lot.")]
    public Sprite[] denseResidentialRingTiles = new Sprite[8];

    [Tooltip("Center courtyard cell (3×3 hole). Used when no center variants are assigned; see denseResidentialCenterVariants.")]
    public Sprite denseResidentialCourtyardSprite;

    [Tooltip("Several top-down courtyard sprites (e.g. Center, Center2… in art). Each residential block picks one by block id (stable). When any entry is set, overrides denseResidentialCourtyardSprite.")]
    public List<Sprite> denseResidentialCenterVariants = new List<Sprite>();

    [Header("Synthesized ring (fewer assets than 8)")]
    [Tooltip("One corner tile: parapet along sprite bottom + left (southwest corner of the block). Reused at four corners with rotation.")]
    public Sprite denseResidentialCornerSouthWest;

    [Tooltip("Flat roof for ring edges + courtyard fallback. If empty, uses defaultRoof or residential variants by slot.")]
    public Sprite denseResidentialFlatRoof;

    public List<KindRoofEntry> roofsByKind = new List<KindRoofEntry>();
    public List<ClusterRoofEntry> roofsByCluster = new List<ClusterRoofEntry>();

    public Sprite GetRoofForCluster(string clusterKey)
    {
        if (string.IsNullOrEmpty(clusterKey) || roofsByCluster == null)
            return null;
        for (int i = 0; i < roofsByCluster.Count; i++)
        {
            ClusterRoofEntry e = roofsByCluster[i];
            if (e != null && e.clusterKey == clusterKey)
                return e.roof;
        }

        return null;
    }

    public Sprite GetRoofForKind(MicroBlockSpotKind kind)
    {
        if (roofsByKind == null)
            return null;
        for (int i = 0; i < roofsByKind.Count; i++)
        {
            KindRoofEntry e = roofsByKind[i];
            if (e != null && e.kind == kind)
                return e.roof;
        }

        return null;
    }

    /// <summary>True when all eight ring slots have a sprite (enables autotile layout).</summary>
    public bool HasCompleteDenseResidentialRing()
    {
        if (denseResidentialRingTiles == null || denseResidentialRingTiles.Length < 8)
            return false;
        for (int i = 0; i < 8; i++)
        {
            if (denseResidentialRingTiles[i] == null)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Courtyard for the empty 3×3 center: random-looking but <b>stable per block</b> from <see cref="denseResidentialCenterVariants"/> when populated; else legacy single sprite and fallbacks.
    /// </summary>
    /// <param name="blockId">City block id (same block always gets the same variant index).</param>
    public Sprite GetCourtyardSpriteForBlock(int blockId)
    {
        Sprite fromVariants = PickCenterVariantForBlock(blockId);
        if (fromVariants != null)
            return fromVariants;
        if (denseResidentialCourtyardSprite != null)
            return denseResidentialCourtyardSprite;
        if (denseResidentialFlatRoof != null)
            return denseResidentialFlatRoof;
        if (defaultRoof != null)
            return defaultRoof;
        if (simpleResidentialRoofVariants == null)
            return null;
        for (int i = 0; i < simpleResidentialRoofVariants.Count; i++)
        {
            if (simpleResidentialRoofVariants[i] != null)
                return simpleResidentialRoofVariants[i];
        }

        return null;
    }

    Sprite PickCenterVariantForBlock(int blockId)
    {
        if (denseResidentialCenterVariants == null || denseResidentialCenterVariants.Count == 0)
            return null;

        int total = 0;
        for (int i = 0; i < denseResidentialCenterVariants.Count; i++)
        {
            if (denseResidentialCenterVariants[i] != null)
                total++;
        }

        if (total == 0)
            return null;

        uint u = unchecked((uint)blockId);
        u ^= u << 13;
        u ^= u >> 17;
        u ^= u << 5;
        if (u == 0)
            u = 1;
        int pick = (int)(u % (uint)total);

        for (int i = 0; i < denseResidentialCenterVariants.Count; i++)
        {
            Sprite s = denseResidentialCenterVariants[i];
            if (s == null)
                continue;
            if (pick == 0)
                return s;
            pick--;
        }

        return null;
    }
}

[Serializable]
public sealed class KindRoofEntry
{
    public MicroBlockSpotKind kind;
    public Sprite roof;
}

[Serializable]
public sealed class ClusterRoofEntry
{
    [Tooltip("Matches MicroBlockSpotRuntime.RoofClusterKey (e.g. merge_0).")]
    public string clusterKey = string.Empty;
    public Sprite roof;
}
