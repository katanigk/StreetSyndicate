using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;

/// <summary>
/// Big Ops map only: one coarse zone image per block (all lots in that block share the same sprite).
/// Load order: (1) <see cref="OpsBigMapLotZoneVisualSet"/> at <c>Resources/OpsMap/BigMapLotZoneVisualSet</c> (drag sprites from anywhere),
/// (2) loose sprites under <c>Resources/OpsMap/BigMapLotZones/</c> named <c>Residential</c> or <c>residential</c> (same for other kinds).
/// <b>Assets/Art/… is not visible to Resources.Load</b> — use the ScriptableObject or copy PNGs into <c>Assets/Resources/OpsMap/BigMapLotZones/</c>.
/// Block zoom uses <see cref="BlockRoofVisualResolver"/> instead.
/// </summary>
public static class OpsBigMapLotZoneResolver
{
    const string ResourcesPathPrefix = "OpsMap/BigMapLotZones/";
    const string VisualSetResourcesPath = "OpsMap/BigMapLotZoneVisualSet";

    public enum ZoneKind
    {
        Residential = 0,
        Commercial = 1,
        Warehouse = 2,
        Police = 3
    }

    /// <summary>Value may be null after a failed load (cached so we do not hammer Resources).</summary>
    static readonly Dictionary<ZoneKind, Sprite> SpriteCache = new Dictionary<ZoneKind, Sprite>(4);
    static readonly HashSet<ZoneKind> WarnedMissingSprite = new HashSet<ZoneKind>();
    static OpsBigMapLotZoneVisualSet _visualSet;
    static bool _visualSetResolved;

    /// <summary>
    /// Sandbox: uses <see cref="MicroBlockWorldState.SandboxMacroZoneByBlockId"/> (random layout each new game; home block = Residential).
    /// Otherwise: police → warehouse → residential (crew/rooming) → commercial from façade spots.
    /// </summary>
    public static ZoneKind ClassifyBlock(CityData city, int blockId)
    {
        if (GameSessionState.SingleBlockSandboxEnabled
            && MicroBlockWorldState.TryGetSandboxMacroZone(blockId, out ZoneKind preset))
            return preset;

        bool police = false;
        bool warehouse = false;
        bool residential = false;
        if (city?.Lots == null || MicroBlockWorldState.Spots == null)
            return ZoneKind.Commercial;

        for (int i = 0; i < MicroBlockWorldState.Spots.Count; i++)
        {
            MicroBlockSpotRuntime sp = MicroBlockWorldState.Spots[i];
            if (sp == null || sp.AnchorLotId < 0)
                continue;
            LotData lot = FindLot(city, sp.AnchorLotId);
            if (lot == null || lot.BlockId != blockId)
                continue;

            switch (sp.Kind)
            {
                case MicroBlockSpotKind.PoliceBeatOffice:
                    police = true;
                    break;
                case MicroBlockSpotKind.Warehouse:
                case MicroBlockSpotKind.AutoGarage:
                    warehouse = true;
                    break;
                case MicroBlockSpotKind.CrewSharedRoom:
                case MicroBlockSpotKind.RoomingHouse:
                    residential = true;
                    break;
            }
        }

        if (police)
            return ZoneKind.Police;
        if (warehouse)
            return ZoneKind.Warehouse;
        if (residential)
            return ZoneKind.Residential;
        return ZoneKind.Commercial;
    }

    static LotData FindLot(CityData city, int lotId)
    {
        for (int i = 0; i < city.Lots.Count; i++)
        {
            LotData l = city.Lots[i];
            if (l.Id == lotId)
                return l;
        }

        return null;
    }

    public static Sprite TryGetSpriteForBlock(CityData city, int blockId)
    {
        ZoneKind k = ClassifyBlock(city, blockId);
        if (SpriteCache.TryGetValue(k, out Sprite cached))
            return cached;

        Sprite s = ResolveZoneSprite(k);
        SpriteCache[k] = s;
        if (s == null && WarnedMissingSprite.Add(k))
            Debug.LogWarning(
                "Ops big map: no sprite for zone \"" + k + "\". Either assign all four sprites on a " +
                nameof(OpsBigMapLotZoneVisualSet) + " asset at Resources path \"" + VisualSetResourcesPath +
                "\", or place PNGs (Sprite mode) under Assets/Resources/OpsMap/BigMapLotZones/ " +
                "using names Residential or residential, Commercial or commercial, Warehouse or warehouse, Police or police.");
        return s;
    }

    static Sprite ResolveZoneSprite(ZoneKind k)
    {
        if (!_visualSetResolved)
        {
            _visualSetResolved = true;
            _visualSet = Resources.Load<OpsBigMapLotZoneVisualSet>(VisualSetResourcesPath);
        }

        if (_visualSet != null)
        {
            Sprite fromSet = _visualSet.TryGet(k);
            if (fromSet != null)
                return fromSet;
        }

        string name = k.ToString();
        Sprite s = Resources.Load<Sprite>(ResourcesPathPrefix + name);
        if (s != null)
            return s;
        s = Resources.Load<Sprite>(ResourcesPathPrefix + name.ToLowerInvariant());
        if (s != null)
            return s;
        // Multi-sprite textures: Resources.Load<Sprite>(path) is often null; LoadAll returns the sheet slices.
        s = FirstSpriteInResourcesAsset(ResourcesPathPrefix + name);
        if (s != null)
            return s;
        return FirstSpriteInResourcesAsset(ResourcesPathPrefix + name.ToLowerInvariant());
    }

    static Sprite FirstSpriteInResourcesAsset(string resourcePathWithoutExtension)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePathWithoutExtension);
        return sprites != null && sprites.Length > 0 ? sprites[0] : null;
    }

    /// <summary>Editor / hot reload: clear cached Resources sprites.</summary>
    public static void ClearSpriteCache()
    {
        _visualSetResolved = false;
        _visualSet = null;
        SpriteCache.Clear();
        WarnedMissingSprite.Clear();
    }

    public static Color TintForLot(LotData lot, int crewHomeBlockId, int focusedLotId)
    {
        Color c = Color.white;
        if (crewHomeBlockId >= 0 && lot.BlockId == crewHomeBlockId)
            c = Color.Lerp(c, new Color(0.98f, 0.88f, 0.42f, 1f), 0.2f);
        if (focusedLotId >= 0 && lot.Id == focusedLotId)
            c = Color.Lerp(c, new Color(0.78f, 0.62f, 0.98f, 1f), 0.22f);
        c.a = 0.98f;
        return c;
    }

    /// <summary>
    /// Tint for one macro block cell. <paramref name="emphasizeCrewHomeGold"/> is for solid-color fallback only;
    /// when a zone sprite is shown, keep false so “Residential = home” is not double-marked as a full yellow tile.
    /// </summary>
    public static Color TintMacroBlock(BlockData block, int crewHomeBlockId, int focusedBlockId, bool emphasizeCrewHomeGold = true)
    {
        Color c = Color.white;
        if (emphasizeCrewHomeGold && crewHomeBlockId >= 0 && block.Id == crewHomeBlockId)
            c = Color.Lerp(c, new Color(0.98f, 0.88f, 0.42f, 1f), 0.52f);
        if (focusedBlockId >= 0 && block.Id == focusedBlockId)
            c = Color.Lerp(c, new Color(0.78f, 0.62f, 0.98f, 1f), 0.42f);
        c.a = 0.96f;
        return c;
    }
}
