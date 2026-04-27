using System;
using System.Collections.Generic;
using FamilyBusiness.CityGen.Data;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Batch 15: builds the Operations tab map from <see cref="CityData"/> (lots + roads), replacing the legacy procedural grid.
/// </summary>
public static class OpsCityGenMapView
{
    /// <summary>
    /// Plan-cell units per layout pixel before viewport fit (<see cref="ScaleLayoutSizeToViewport"/>) is baked into <see cref="RectTransform.sizeDelta"/>
    /// and lot geometry (see <see cref="Rebuild"/>). Avoid relying on <c>localScale</c> for fit — it blurs uGUI.
    /// Strokes were tuned around legacy 1.35 plan-units/px; see <see cref="StrokeScale"/>.
    /// </summary>
    public const float PlanUnitsPerUiPixel = 0.55f;

    const float ReferencePlanUnitsPerUiPixel = 1.35f;

    /// <summary>Road/outline distances in layout space scale so on-screen weight stays similar when layout resolution changes.</summary>
    static float StrokeScale => ReferencePlanUnitsPerUiPixel / PlanUnitsPerUiPixel;

    /// <summary>
    /// <para><b>Cover</b> (<paramref name="cover"/> true): <see cref="Mathf.Max"/> — fills the viewport; may crop (fewer visible macro tiles on wide screens).</para>
    /// <para><b>Contain</b> (<paramref name="cover"/> false): <see cref="Mathf.Min"/> — entire map fits; may letterbox (sandbox macro 3×4 shows all 12 blocks).</para>
    /// </summary>
    static void ScaleLayoutSizeToViewport(ref float pxW, ref float pxH, Vector2 viewportLocalSize, bool cover)
    {
        if (viewportLocalSize.x <= 16f || viewportLocalSize.y <= 16f)
            return;
        float sFit = cover
            ? Mathf.Max(viewportLocalSize.x / pxW, viewportLocalSize.y / pxH)
            : Mathf.Min(viewportLocalSize.x / pxW, viewportLocalSize.y / pxH);
        sFit = Mathf.Clamp(sFit, 0.02f, 200f);
        pxW *= sFit;
        pxH *= sFit;
    }

    public sealed class BuildResult
    {
        public Vector2 PlanMin;
        public Vector2 PlanExtent;
        public int LotCount;
    }

    /// <summary>Clears <paramref name="mapContentRoot"/> and draws lots; optional <paramref name="onLotClicked"/>.
    /// Lots in <paramref name="crewHomeBlockId"/> (when ≥ 0) get a warm highlight.
    /// <paramref name="focusedLotId"/> (when ≥ 0) gets a second accent (selected micro-block spot).
    /// When <paramref name="mapViewportLocalSize"/> is large enough, layout geometry uses <b>cover</b> fit here.</summary>
    public static BuildResult Rebuild(RectTransform mapContentRoot, CityData city, Action<LotData> onLotClicked, int crewHomeBlockId = -1, int focusedLotId = -1, Vector2 mapViewportLocalSize = default)
    {
        if (mapContentRoot == null)
            return new BuildResult { PlanExtent = new Vector2(400f, 400f), LotCount = 0 };

        for (int i = mapContentRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(mapContentRoot.GetChild(i).gameObject);

        GridLayoutGroup gl = mapContentRoot.GetComponent<GridLayoutGroup>();
        if (gl != null)
            UnityEngine.Object.Destroy(gl);
        ContentSizeFitter cf = mapContentRoot.GetComponent<ContentSizeFitter>();
        if (cf != null)
            UnityEngine.Object.Destroy(cf);

        var empty = new BuildResult { PlanExtent = new Vector2(400f, 400f), LotCount = 0 };
        if (city == null || city.Lots.Count == 0)
        {
            float ew = 400f;
            float eh = 400f;
            ScaleLayoutSizeToViewport(ref ew, ref eh, mapViewportLocalSize, cover: true);

            mapContentRoot.anchorMin = mapContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            mapContentRoot.pivot = new Vector2(0.5f, 0.5f);
            mapContentRoot.sizeDelta = new Vector2(ew, eh);
            return empty;
        }

        Vector2 pMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 pMax = new Vector2(float.MinValue, float.MinValue);
        foreach (LotData lot in city.Lots)
        {
            pMin = Vector2.Min(pMin, lot.Min);
            pMax = Vector2.Max(pMax, lot.Max);
        }

        const float padCells = 6f;
        pMin -= new Vector2(padCells, padCells);
        pMax += new Vector2(padCells, padCells);
        Vector2 extent = pMax - pMin;
        extent.x = Mathf.Max(extent.x, 1f);
        extent.y = Mathf.Max(extent.y, 1f);

        float inv = 1f / PlanUnitsPerUiPixel;
        float pxW = extent.x * inv;
        float pxH = extent.y * inv;

        ScaleLayoutSizeToViewport(ref pxW, ref pxH, mapViewportLocalSize, cover: true);

        mapContentRoot.anchorMin = mapContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        mapContentRoot.pivot = new Vector2(0.5f, 0.5f);
        mapContentRoot.sizeDelta = new Vector2(pxW, pxH);

        float sx = pxW / extent.x;
        float sy = pxH / extent.y;

        DrawRoads(mapContentRoot, city, pMin, sx, sy);
        if (GameSessionState.SingleBlockSandboxEnabled)
            DrawSandboxCourtyards(mapContentRoot, city, pMin, sx, sy, null, crewHomeBlockId);
        bool tintKinds = GameSessionState.SingleBlockSandboxEnabled;
        DrawLots(mapContentRoot, city, pMin, sx, sy, onLotClicked, crewHomeBlockId, focusedLotId, null, tintKinds);

        return new BuildResult { PlanMin = pMin, PlanExtent = extent, LotCount = city.Lots.Count };
    }

    /// <summary>
    /// Macro OPS map: one clickable cell per city block (rectangles today; swap draw for hex meshes later without changing the shell).
    /// Single-block sandbox: uses contain-fit so all macro cells (12 on a 3×4 grid) stay visible on wide layouts (cover would crop rows).
    /// </summary>
    public static BuildResult RebuildBlockMacro(RectTransform mapContentRoot, CityData city, Action<BlockData> onBlockClicked,
        int crewHomeBlockId = -1, int focusedBlockId = -1, Vector2 mapViewportLocalSize = default)
    {
        if (mapContentRoot == null)
            return new BuildResult { PlanExtent = new Vector2(400f, 400f), LotCount = 0 };

        for (int i = mapContentRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(mapContentRoot.GetChild(i).gameObject);

        GridLayoutGroup gl = mapContentRoot.GetComponent<GridLayoutGroup>();
        if (gl != null)
            UnityEngine.Object.Destroy(gl);
        ContentSizeFitter cf = mapContentRoot.GetComponent<ContentSizeFitter>();
        if (cf != null)
            UnityEngine.Object.Destroy(cf);

        var empty = new BuildResult { PlanExtent = new Vector2(400f, 400f), LotCount = 0 };
        if (city == null || city.Blocks == null || city.Blocks.Count == 0)
        {
            float ew = 400f;
            float eh = 400f;
            // Sandbox macro: contain so placeholder still reads; wide modes use cover.
            ScaleLayoutSizeToViewport(ref ew, ref eh, mapViewportLocalSize,
                cover: !GameSessionState.SingleBlockSandboxEnabled);

            mapContentRoot.anchorMin = mapContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            mapContentRoot.pivot = new Vector2(0.5f, 0.5f);
            mapContentRoot.sizeDelta = new Vector2(ew, eh);
            return empty;
        }

        Vector2 pMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 pMax = new Vector2(float.MinValue, float.MinValue);
        foreach (BlockData b in city.Blocks)
        {
            pMin = Vector2.Min(pMin, b.Min);
            pMax = Vector2.Max(pMax, b.Max);
        }

        const float padCells = 6f;
        pMin -= new Vector2(padCells, padCells);
        pMax += new Vector2(padCells, padCells);
        Vector2 extent = pMax - pMin;
        extent.x = Mathf.Max(extent.x, 1f);
        extent.y = Mathf.Max(extent.y, 1f);

        float inv = 1f / PlanUnitsPerUiPixel;
        float pxW = extent.x * inv;
        float pxH = extent.y * inv;

        // Sandbox: "contain" the full 3×4 (12) grid so wide viewports do not vertical-crop to ~6 tiles.
        ScaleLayoutSizeToViewport(ref pxW, ref pxH, mapViewportLocalSize,
            cover: !GameSessionState.SingleBlockSandboxEnabled);

        mapContentRoot.anchorMin = mapContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        mapContentRoot.pivot = new Vector2(0.5f, 0.5f);
        mapContentRoot.sizeDelta = new Vector2(pxW, pxH);

        float sx = pxW / extent.x;
        float sy = pxH / extent.y;

        if (!GameSessionState.SingleBlockSandboxEnabled)
            DrawRoads(mapContentRoot, city, pMin, sx, sy);
        DrawBlocks(mapContentRoot, city, pMin, sx, sy, onBlockClicked, crewHomeBlockId, focusedBlockId);

        return new BuildResult { PlanMin = pMin, PlanExtent = extent, LotCount = city.Blocks.Count };
    }

    /// <summary>
    /// Inner block view: only lots belonging to <paramref name="blockId"/> (e.g. sandbox 8-lot ring), same road/lot styling as full map.
    /// </summary>
    public static BuildResult RebuildLotsForBlock(RectTransform mapContentRoot, CityData city, int blockId, Action<LotData> onLotClicked,
        int crewHomeBlockId = -1, int focusedLotId = -1, Vector2 mapViewportLocalSize = default, bool drawRoads = true)
    {
        if (mapContentRoot == null)
            return new BuildResult { PlanExtent = new Vector2(400f, 400f), LotCount = 0 };

        for (int i = mapContentRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(mapContentRoot.GetChild(i).gameObject);

        GridLayoutGroup gl = mapContentRoot.GetComponent<GridLayoutGroup>();
        if (gl != null)
            UnityEngine.Object.Destroy(gl);
        ContentSizeFitter cf = mapContentRoot.GetComponent<ContentSizeFitter>();
        if (cf != null)
            UnityEngine.Object.Destroy(cf);

        var empty = new BuildResult { PlanExtent = new Vector2(400f, 400f), LotCount = 0 };
        if (city == null || city.Lots == null || city.Lots.Count == 0)
        {
            mapContentRoot.anchorMin = mapContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            mapContentRoot.pivot = new Vector2(0.5f, 0.5f);
            mapContentRoot.sizeDelta = new Vector2(400f, 400f);
            return empty;
        }

        Vector2 pMin = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 pMax = new Vector2(float.MinValue, float.MinValue);
        int count = 0;
        foreach (LotData lot in city.Lots)
        {
            if (lot.BlockId != blockId)
                continue;
            pMin = Vector2.Min(pMin, lot.Min);
            pMax = Vector2.Max(pMax, lot.Max);
            count++;
        }

        if (count == 0)
        {
            mapContentRoot.anchorMin = mapContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
            mapContentRoot.pivot = new Vector2(0.5f, 0.5f);
            mapContentRoot.sizeDelta = new Vector2(400f, 400f);
            return empty;
        }

        const float padCells = 6f;
        pMin -= new Vector2(padCells, padCells);
        pMax += new Vector2(padCells, padCells);
        Vector2 extent = pMax - pMin;
        extent.x = Mathf.Max(extent.x, 1f);
        extent.y = Mathf.Max(extent.y, 1f);

        float inv = 1f / PlanUnitsPerUiPixel;
        float pxW = extent.x * inv;
        float pxH = extent.y * inv;

        ScaleLayoutSizeToViewport(ref pxW, ref pxH, mapViewportLocalSize, cover: true);

        mapContentRoot.anchorMin = mapContentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        mapContentRoot.pivot = new Vector2(0.5f, 0.5f);
        mapContentRoot.sizeDelta = new Vector2(pxW, pxH);

        float sx = pxW / extent.x;
        float sy = pxH / extent.y;

        if (drawRoads)
            DrawRoads(mapContentRoot, city, pMin, sx, sy);
        if (GameSessionState.SingleBlockSandboxEnabled)
            DrawSandboxCourtyards(mapContentRoot, city, pMin, sx, sy, blockId);
        bool tintKinds = GameSessionState.SingleBlockSandboxEnabled;
        DrawLots(mapContentRoot, city, pMin, sx, sy, onLotClicked, crewHomeBlockId, focusedLotId, blockId, tintKinds);

        return new BuildResult { PlanMin = pMin, PlanExtent = extent, LotCount = count };
    }

    /// <summary>
    /// One courtyard rect for the empty center of an 8-lot ring (3×3 minus middle). No-op if not sandbox or count ≠ 8.
    /// </summary>
    public static void TryDrawSandboxCourtyardForEightLots(RectTransform root, IReadOnlyList<LotData> ringLots, Vector2 planMin, float sx, float sy, string objectName = "Courtyard",
        CityData cityForFog = null, int crewHomeBlockIdForFog = -1)
    {
        if (!GameSessionState.SingleBlockSandboxEnabled || root == null || ringLots == null || ringLots.Count != 8)
            return;

        Vector2 gmin = ringLots[0].Min;
        Vector2 gmax = ringLots[0].Max;
        for (int i = 1; i < ringLots.Count; i++)
        {
            gmin = Vector2.Min(gmin, ringLots[i].Min);
            gmax = Vector2.Max(gmax, ringLots[i].Max);
        }

        float spanX = gmax.x - gmin.x;
        float spanY = gmax.y - gmin.y;
        if (spanX < 1e-4f || spanY < 1e-4f)
            return;

        float cw = spanX / 3f;
        float ch = spanY / 3f;
        Vector2 cMin = new Vector2(gmin.x + cw, gmin.y + ch);
        Vector2 cMax = new Vector2(gmin.x + 2f * cw, gmin.y + 2f * ch);

        Vector2 l0 = cMin - planMin;
        float w = (cMax.x - cMin.x) * sx;
        float h = (cMax.y - cMin.y) * sy;
        if (w < 0.5f || h < 0.5f)
            return;

        GameObject go = new GameObject(objectName);
        go.transform.SetParent(root, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        LayoutUIGridCellBottomLeft(rt, l0.x * sx, l0.y * sy, w, h);

        Image img = go.AddComponent<Image>();
        BlockRoofVisualConfig cfg = BlockRoofVisualResolver.TryGetActiveConfig();
        int blockId = ringLots[0].BlockId;
        bool courtyardFogged = false;
        if (cityForFog != null && crewHomeBlockIdForFog >= 0)
        {
            BuildSandboxGridAxes(cityForFog, out List<float> gxs, out List<float> gys);
            courtyardFogged = ShouldDrawSandboxFogOfWar(cityForFog, blockId, crewHomeBlockIdForFog, gxs, gys);
        }

        if (courtyardFogged)
        {
            Sprite courtyard = cfg != null ? cfg.GetCourtyardSpriteForBlock(blockId) : null;
            if (courtyard != null)
            {
                ApplyBlockMapRoofSprite(img, courtyard);
                Color veil = new Color(0.2f, 0.2f, 0.24f, 0.96f);
                img.color = Color.Lerp(Color.white, veil, 0.62f);
            }
            else
            {
                ApplyUnidentifiedFogBaseFill(img);
                AppendFogOfWarOverlay(rt);
            }
        }
        else
        {
            Sprite courtyard = cfg != null ? cfg.GetCourtyardSpriteForBlock(blockId) : null;
            if (courtyard != null)
            {
                ApplyBlockMapRoofSprite(img, courtyard);
                img.color = Color.white;
            }
            else
                img.color = new Color(0.24f, 0.32f, 0.22f, 0.92f);
        }

        img.raycastTarget = false;
    }

    /// <summary>
    /// Fills the missing 3×3 center cell for sandbox blocks that have exactly eight lots (courtyard).
    /// </summary>
    static void DrawSandboxCourtyards(RectTransform root, CityData city, Vector2 planMin, float sx, float sy, int? onlyBlockId,
        int crewHomeBlockId = -1)
    {
        if (root == null || city?.Lots == null)
            return;

        var byBlock = new Dictionary<int, List<LotData>>();
        for (int i = 0; i < city.Lots.Count; i++)
        {
            LotData lot = city.Lots[i];
            if (onlyBlockId.HasValue && lot.BlockId != onlyBlockId.Value)
                continue;
            if (!byBlock.TryGetValue(lot.BlockId, out List<LotData> list))
            {
                list = new List<LotData>(8);
                byBlock[lot.BlockId] = list;
            }

            list.Add(lot);
        }

        foreach (KeyValuePair<int, List<LotData>> kv in byBlock)
        {
            if (kv.Value.Count != 8)
                continue;
            if (onlyBlockId.HasValue)
                TryDrawSandboxCourtyardForEightLots(root, kv.Value, planMin, sx, sy, "Courtyard_" + kv.Key);
            else
                TryDrawSandboxCourtyardForEightLots(root, kv.Value, planMin, sx, sy, "Courtyard_" + kv.Key, city, crewHomeBlockId);
        }
    }

    static void DrawBlocks(RectTransform root, CityData city, Vector2 planMin, float sx, float sy, Action<BlockData> onBlockClicked,
        int crewHomeBlockId, int focusedBlockId)
    {
        BuildSandboxGridAxes(city, out List<float> gridXs, out List<float> gridYs);

        var blocks = new List<BlockData>(city.Blocks);
        blocks.Sort((a, b) => a.Id.CompareTo(b.Id));

        foreach (BlockData block in blocks)
        {
            Vector2 l0 = block.Min - planMin;
            float w = (block.Max.x - block.Min.x) * sx;
            float h = (block.Max.y - block.Min.y) * sy;
            if (w < 0.35f || h < 0.35f)
                continue;

            GameObject go = new GameObject("Block_" + block.Id);
            go.transform.SetParent(root, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            LayoutUIGridCellBottomLeft(rt, l0.x * sx, l0.y * sy, w, h);

            Image img = go.AddComponent<Image>();
            Sprite zoneSp = OpsBigMapLotZoneResolver.TryGetSpriteForBlock(city, block.Id);
            bool fogged = ShouldDrawSandboxFogOfWar(city, block.Id, crewHomeBlockId, gridXs, gridYs);
            if (fogged)
                ApplySandboxFogMacroBlockFill(img, rt, block, zoneSp, crewHomeBlockId, focusedBlockId);
            else if (zoneSp != null)
            {
                rt.localEulerAngles = Vector3.zero;
                ApplyBlockMapRoofSprite(img, zoneSp);
                img.color = OpsBigMapLotZoneResolver.TintMacroBlock(block, crewHomeBlockId, focusedBlockId, emphasizeCrewHomeGold: false);
            }
            else
            {
                img.sprite = null;
                img.color = BlockFillColor(block, crewHomeBlockId, focusedBlockId);
            }

            img.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            PlanningUiButtonStyle.ApplyColorTint(btn, img.color);

            bool focused = focusedBlockId >= 0 && block.Id == focusedBlockId;
            bool home = crewHomeBlockId >= 0 && block.Id == crewHomeBlockId;
            bool zoneArt = zoneSp != null;
            if (focused)
            {
                Outline hi = go.AddComponent<Outline>();
                hi.effectColor = new Color(0.72f, 0.48f, 0.98f, 1f);
                float f = StrokeScale;
                hi.effectDistance = new Vector2(2.5f * f, -2.5f * f);
            }
            else if (home && !zoneArt)
            {
                Outline hi = go.AddComponent<Outline>();
                hi.effectColor = new Color(0.98f, 0.86f, 0.35f, 1f);
                float f = StrokeScale;
                hi.effectDistance = new Vector2(2f * f, -2f * f);
            }
            else
            {
                PlanningUiButtonStyle.ApplyOutline(img);
                Outline o = go.GetComponent<Outline>();
                if (o != null)
                {
                    Vector2 d = PlanningUiButtonStyle.OutlineDistance;
                    float f = StrokeScale;
                    o.effectDistance = new Vector2(d.x * f, d.y * f);
                }
            }

            var bps = go.AddComponent<ButtonPressScale>();
            if (GameSessionState.SingleBlockSandboxEnabled)
                bps.SetScalePresets(0.94f, 1f);
            BlockData cap = block;
            if (onBlockClicked != null)
                btn.onClick.AddListener(() => onBlockClicked(cap));
        }
    }

    /// <summary>
    /// Sandbox FOW used to replace zone art with near-black void. Keep the same sprite / district fill under a dark veil
    /// so the grid never looks “empty” — only unexplored / low emphasis.
    /// </summary>
    static void ApplySandboxFogMacroBlockFill(Image img, RectTransform rt, BlockData block, Sprite zoneSp, int crewHomeBlockId, int focusedBlockId)
    {
        Color veil = new Color(0.2f, 0.2f, 0.24f, 0.96f);
        const float veilMix = 0.62f;
        if (zoneSp != null)
        {
            rt.localEulerAngles = Vector3.zero;
            ApplyBlockMapRoofSprite(img, zoneSp);
            Color t = OpsBigMapLotZoneResolver.TintMacroBlock(block, crewHomeBlockId, focusedBlockId, emphasizeCrewHomeGold: false);
            img.color = Color.Lerp(t, veil, veilMix);
        }
        else
        {
            img.sprite = null;
            Color c = BlockFillColor(block, crewHomeBlockId, focusedBlockId);
            img.color = Color.Lerp(c, veil, veilMix * 0.85f);
        }
    }

    static Color BlockFillColor(BlockData block, int crewHomeBlockId, int focusedBlockId)
    {
        Color c = DistrictFillColor(block.DistrictKind, false);
        float v = (block.Id * 793 % 7) * 0.014f;
        c = Color.Lerp(c, new Color(0.44f, 0.47f, 0.52f, 0.78f), v);
        if (crewHomeBlockId >= 0 && block.Id == crewHomeBlockId)
            c = Color.Lerp(c, new Color(0.95f, 0.78f, 0.28f, 0.94f), 0.62f);
        if (focusedBlockId >= 0 && block.Id == focusedBlockId)
            c = Color.Lerp(c, new Color(0.68f, 0.52f, 0.92f, 0.94f), 0.38f);
        return c;
    }

    static void DrawRoads(RectTransform root, CityData city, Vector2 planMin, float sx, float sy)
    {
        var nodeById = new Dictionary<int, RoadNode>(city.RoadNodes.Count);
        foreach (RoadNode n in city.RoadNodes)
            nodeById[n.Id] = n;

        foreach (RoadEdge e in city.RoadEdges)
        {
            if (!nodeById.TryGetValue(e.FromNodeId, out RoadNode a) || !nodeById.TryGetValue(e.ToNodeId, out RoadNode b))
                continue;
            Vector2 pa = a.Position - planMin;
            Vector2 pb = b.Position - planMin;
            Vector2 aUi = new Vector2(pa.x * sx, pa.y * sy);
            Vector2 bUi = new Vector2(pb.x * sx, pb.y * sy);
            Vector2 d = bUi - aUi;
            float len = d.magnitude;
            if (len < 0.5f)
                continue;

            GameObject go = new GameObject("Road_" + e.Id);
            go.transform.SetParent(root, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            Vector2 mid = (aUi + bUi) * 0.5f;
            rt.anchoredPosition = mid;
            float thick = RoadThicknessForKind(e.Kind) * StrokeScale;
            rt.sizeDelta = new Vector2(Mathf.Max(1.2f, len), thick);
            float ang = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, ang);

            Image img = go.AddComponent<Image>();
            img.color = RoadColor(e.Kind);
            img.raycastTarget = false;
        }
    }

    static float RoadThicknessForKind(RoadEdgeKind k) =>
        k switch
        {
            RoadEdgeKind.Major => 3.2f,
            RoadEdgeKind.Secondary => 2.4f,
            RoadEdgeKind.Alley => 1.4f,
            _ => 2f
        };

    static Color RoadColor(RoadEdgeKind k) =>
        k switch
        {
            RoadEdgeKind.Major => new Color(0.14f, 0.14f, 0.16f, 0.95f),
            RoadEdgeKind.Secondary => new Color(0.11f, 0.11f, 0.13f, 0.92f),
            RoadEdgeKind.Alley => new Color(0.09f, 0.09f, 0.1f, 0.88f),
            _ => new Color(0.1f, 0.1f, 0.12f, 0.9f)
        };

    /// <param name="onlyBlockId">When set, only lots in this block are drawn (inner block view).</param>
    /// <param name="useSpotKindTint">Sandbox: color lots from anchored <see cref="MicroBlockSpotRuntime.Kind"/> (no text on tiles).</param>
    static void DrawLots(RectTransform root, CityData city, Vector2 planMin, float sx, float sy, Action<LotData> onLotClicked, int crewHomeBlockId, int focusedLotId, int? onlyBlockId, bool useSpotKindTint)
    {
        BuildSandboxGridAxes(city, out List<float> gridXs, out List<float> gridYs);

        var lots = new List<LotData>(city.Lots);
        lots.Sort((a, b) =>
        {
            int c = a.IsReserved.CompareTo(b.IsReserved);
            return c != 0 ? c : a.Id.CompareTo(b.Id);
        });

        bool macroBigMapSurface = useSpotKindTint && GameSessionState.SingleBlockSandboxEnabled && !onlyBlockId.HasValue;

        Dictionary<int, List<LotData>> ringEightByBlock = null;
        if (useSpotKindTint && GameSessionState.SingleBlockSandboxEnabled && city?.Lots != null && !macroBigMapSurface)
        {
            ringEightByBlock = new Dictionary<int, List<LotData>>();
            var byBlock = new Dictionary<int, List<LotData>>();
            for (int i = 0; i < city.Lots.Count; i++)
            {
                LotData L = city.Lots[i];
                if (onlyBlockId.HasValue && L.BlockId != onlyBlockId.Value)
                    continue;
                if (!byBlock.TryGetValue(L.BlockId, out List<LotData> bl))
                {
                    bl = new List<LotData>(12);
                    byBlock[L.BlockId] = bl;
                }

                bl.Add(L);
            }

            foreach (KeyValuePair<int, List<LotData>> kv in byBlock)
            {
                if (kv.Value.Count != 8)
                    continue;
                kv.Value.Sort((a, b) => a.Id.CompareTo(b.Id));
                ringEightByBlock[kv.Key] = kv.Value;
            }
        }

        foreach (LotData lot in lots)
        {
            if (onlyBlockId.HasValue && lot.BlockId != onlyBlockId.Value)
                continue;
            Vector2 l0 = lot.Min - planMin;
            float w = (lot.Max.x - lot.Min.x) * sx;
            float h = (lot.Max.y - lot.Min.y) * sy;
            if (w < 0.35f || h < 0.35f)
                continue;

            GameObject go = new GameObject("Lot_" + lot.Id);
            go.transform.SetParent(root, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            LayoutUIGridCellBottomLeft(rt, l0.x * sx, l0.y * sy, w, h);

            // Same fog rule for full city and block-zoom strip so the strip matches the center map.
            bool lotFogged = ShouldDrawSandboxFogOfWar(city, lot.BlockId, crewHomeBlockId, gridXs, gridYs);

            Image img = go.AddComponent<Image>();
            MicroBlockSpotRuntime anchoredSpot = null;
            Sprite roofSp = null;
            if (lotFogged)
            {
                ApplySandboxFogLotFill(city, lot, img, rt, macroBigMapSurface, useSpotKindTint, crewHomeBlockId, focusedLotId,
                    ringEightByBlock, ref anchoredSpot, ref roofSp);
            }
            else if (macroBigMapSurface)
            {
                Sprite zoneSp = OpsBigMapLotZoneResolver.TryGetSpriteForBlock(city, lot.BlockId);
                if (zoneSp != null)
                {
                    rt.localEulerAngles = Vector3.zero;
                    ApplyBlockMapRoofSprite(img, zoneSp);
                    roofSp = zoneSp;
                    anchoredSpot = MicroBlockWorldState.FindSpotByAnchorLotId(lot.Id);
                }
            }

            if (!lotFogged && roofSp == null && useSpotKindTint && GameSessionState.SingleBlockSandboxEnabled)
            {
                anchoredSpot = MicroBlockWorldState.FindSpotByAnchorLotId(lot.Id);
                List<LotData> ringLots = null;
                if (ringEightByBlock != null && ringEightByBlock.TryGetValue(lot.BlockId, out List<LotData> ring))
                    ringLots = ring;
                RoofUiSpec roofUi = BlockRoofVisualResolver.ResolveRoofUi(anchoredSpot, lot, ringLots);
                ApplyRoofUi(rt, img, roofUi);
                roofSp = roofUi.Sprite;
            }

            if (!lotFogged)
            {
                img.color = macroBigMapSurface && roofSp != null
                    ? OpsBigMapLotZoneResolver.TintForLot(lot, crewHomeBlockId, focusedLotId)
                    : roofSp != null
                        ? SandboxRoofCellTint(lot, anchoredSpot, crewHomeBlockId, focusedLotId)
                        : useSpotKindTint
                            ? LotFillColorWithSpotKind(lot, crewHomeBlockId, focusedLotId)
                            : LotFillColor(lot, crewHomeBlockId, focusedLotId);
            }
            img.raycastTarget = true;

            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            // Do not use ApplyStandardRectButton — it forces RectFill and wipes district / home-block colors.
            PlanningUiButtonStyle.ApplyColorTint(btn, img.color);

            bool focused = focusedLotId >= 0 && lot.Id == focusedLotId;
            bool homeLot = crewHomeBlockId >= 0 && lot.BlockId == crewHomeBlockId;
            if (focused)
            {
                Outline hi = go.AddComponent<Outline>();
                hi.effectColor = new Color(0.72f, 0.48f, 0.98f, 1f);
                float f = StrokeScale;
                hi.effectDistance = new Vector2(2.5f * f, -2.5f * f);
            }
            else if (homeLot)
            {
                Outline hi = go.AddComponent<Outline>();
                hi.effectColor = new Color(0.98f, 0.86f, 0.35f, 1f);
                float f = StrokeScale;
                hi.effectDistance = new Vector2(2f * f, -2f * f);
            }
            else
            {
                PlanningUiButtonStyle.ApplyOutline(img);
                Outline o = go.GetComponent<Outline>();
                if (o != null)
                {
                    Vector2 d = PlanningUiButtonStyle.OutlineDistance;
                    float f = StrokeScale;
                    o.effectDistance = new Vector2(d.x * f, d.y * f);
                }
            }

            var bps = go.AddComponent<ButtonPressScale>();
            if (useSpotKindTint && GameSessionState.SingleBlockSandboxEnabled)
                bps.SetScalePresets(0.94f, 1f);
            LotData cap = lot;
            if (onLotClicked != null)
                btn.onClick.AddListener(() => onLotClicked(cap));
        }
    }

    static void ApplySandboxFogLotFill(
        CityData city,
        LotData lot,
        Image img,
        RectTransform rt,
        bool macroBigMapSurface,
        bool useSpotKindTint,
        int crewHomeBlockId,
        int focusedLotId,
        Dictionary<int, List<LotData>> ringEightByBlock,
        ref MicroBlockSpotRuntime anchoredSpot,
        ref Sprite roofSp)
    {
        Color veil = new Color(0.2f, 0.2f, 0.24f, 0.96f);
        const float veilMix = 0.62f;
        if (macroBigMapSurface)
        {
            Sprite zoneSp = OpsBigMapLotZoneResolver.TryGetSpriteForBlock(city, lot.BlockId);
            if (zoneSp != null)
            {
                rt.localEulerAngles = Vector3.zero;
                ApplyBlockMapRoofSprite(img, zoneSp);
                roofSp = zoneSp;
                anchoredSpot = MicroBlockWorldState.FindSpotByAnchorLotId(lot.Id);
                Color t = OpsBigMapLotZoneResolver.TintForLot(lot, crewHomeBlockId, focusedLotId);
                img.color = Color.Lerp(t, veil, veilMix);
            }
            else
            {
                img.sprite = null;
                Color c = LotFillColor(lot, crewHomeBlockId, focusedLotId);
                img.color = Color.Lerp(c, veil, veilMix * 0.85f);
            }

            return;
        }

        anchoredSpot = MicroBlockWorldState.FindSpotByAnchorLotId(lot.Id);
        List<LotData> ringLots = null;
        if (ringEightByBlock != null && ringEightByBlock.TryGetValue(lot.BlockId, out List<LotData> ring))
            ringLots = ring;

        RoofUiSpec roofUi = BlockRoofVisualResolver.ResolveRoofUi(anchoredSpot, lot, ringLots);
        if (roofUi.HasSprite)
        {
            ApplyRoofUi(rt, img, roofUi);
            roofSp = roofUi.Sprite;
            Color baseC = SandboxRoofCellTint(lot, anchoredSpot, crewHomeBlockId, focusedLotId);
            img.color = Color.Lerp(baseC, veil, veilMix);
        }
        else
        {
            img.sprite = null;
            Color c = useSpotKindTint
                ? LotFillColorWithSpotKind(lot, crewHomeBlockId, focusedLotId)
                : LotFillColor(lot, crewHomeBlockId, focusedLotId);
            img.color = Color.Lerp(c, veil, veilMix * 0.85f);
        }
    }

    static Color LotFillColor(LotData lot, int crewHomeBlockId, int focusedLotId)
    {
        Color c = DistrictFillColor(lot.DistrictKind, lot.IsReserved);
        if (crewHomeBlockId >= 0 && lot.BlockId == crewHomeBlockId)
            c = Color.Lerp(c, new Color(0.95f, 0.78f, 0.28f, 0.94f), 0.62f);
        if (focusedLotId >= 0 && lot.Id == focusedLotId)
            c = Color.Lerp(c, new Color(0.68f, 0.52f, 0.92f, 0.94f), 0.38f);
        return c;
    }

    static Color LotFillColorWithSpotKind(LotData lot, int crewHomeBlockId, int focusedLotId)
    {
        if (!TryGetSpotKindForLot(lot.Id, out MicroBlockSpotKind k) || k == MicroBlockSpotKind.Unknown)
            return LotFillColor(lot, crewHomeBlockId, focusedLotId);

        Color c = SpotKindAccentColor(k);
        float noise = (lot.Id % 5) * 0.018f;
        c = Color.Lerp(c, new Color(0.35f, 0.38f, 0.42f, 0.75f), noise);
        if (crewHomeBlockId >= 0 && lot.BlockId == crewHomeBlockId)
            c = Color.Lerp(c, new Color(0.95f, 0.82f, 0.35f, 0.92f), 0.48f);
        if (focusedLotId >= 0 && lot.Id == focusedLotId)
            c = Color.Lerp(c, new Color(0.78f, 0.62f, 0.98f, 0.92f), 0.42f);
        return c;
    }

    static bool TryGetSpotKindForLot(int lotId, out MicroBlockSpotKind kind)
    {
        kind = MicroBlockSpotKind.Unknown;
        MicroBlockSpotRuntime s = MicroBlockWorldState.FindSpotByAnchorLotId(lotId);
        if (s == null)
            return false;
        kind = s.Kind;
        return true;
    }

    static Color SandboxRoofCellTint(LotData lot, MicroBlockSpotRuntime spot, int crewHomeBlockId, int focusedLotId)
    {
        Color baseC = Color.white;
        if (spot != null)
            baseC = Color.Lerp(Color.white, SpotKindAccentColor(spot.Kind), 0.26f);
        if (crewHomeBlockId >= 0 && lot.BlockId == crewHomeBlockId)
            baseC = Color.Lerp(baseC, new Color(0.98f, 0.88f, 0.42f, 1f), 0.22f);
        if (focusedLotId >= 0 && lot.Id == focusedLotId)
            baseC = Color.Lerp(baseC, new Color(0.78f, 0.62f, 0.98f, 1f), 0.28f);
        baseC.a = 0.96f;
        return baseC;
    }

    /// <summary>
    /// uGUI cell in parent bottom-left space: <paramref name="bottomLeftX"/>/<paramref name="bottomLeftY"/> = lot corner.
    /// Uses <b>center pivot</b> so optional Z rotation (corner autotile) spins around the middle of the cell, not the corner — otherwise the 3×3 ring scatters.
    /// </summary>
    public static void LayoutUIGridCellBottomLeft(RectTransform rt, float bottomLeftX, float bottomLeftY, float w, float h)
    {
        float ww = Mathf.Max(1f, w);
        float hh = Mathf.Max(1f, h);
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(ww, hh);
        rt.anchoredPosition = new Vector2(bottomLeftX + ww * 0.5f, bottomLeftY + hh * 0.5f);
    }

    /// <summary>
    /// uGUI roof tiles: stretch to the full lot rect so the 3×3 ring stays visually aligned.
    /// (<c>preserveAspect</c> was leaving letterboxing inside each cell when scale sx/sy differed slightly, which looked like a broken grid.)
    /// </summary>
    public static void ApplyBlockMapRoofSprite(Image img, Sprite sprite)
    {
        if (img == null || sprite == null)
            return;
        img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.useSpriteMesh = false;
    }

    public static void ApplyRoofUi(RectTransform lotRect, Image img, RoofUiSpec spec)
    {
        if (lotRect != null)
            lotRect.localEulerAngles = spec.HasSprite ? new Vector3(0f, 0f, spec.ZRotationDegrees) : Vector3.zero;
        if (spec.HasSprite)
            ApplyBlockMapRoofSprite(img, spec.Sprite);
    }

    /// <summary>Shared with the left-column home block strip when sandbox tinting is on.</summary>
    public static Color AccentColorForSpotKind(MicroBlockSpotKind k) => SpotKindAccentColor(k);

    static Color SpotKindAccentColor(MicroBlockSpotKind k) =>
        k switch
        {
            MicroBlockSpotKind.CrewSharedRoom => new Color(0.42f, 0.55f, 0.38f, 0.82f),
            MicroBlockSpotKind.RoomingHouse => new Color(0.48f, 0.44f, 0.36f, 0.8f),
            MicroBlockSpotKind.CornerGrocery => new Color(0.38f, 0.52f, 0.44f, 0.82f),
            MicroBlockSpotKind.PrintShop => new Color(0.4f, 0.4f, 0.5f, 0.8f),
            MicroBlockSpotKind.BarberShop => new Color(0.45f, 0.42f, 0.48f, 0.8f),
            MicroBlockSpotKind.Laundromat => new Color(0.38f, 0.48f, 0.55f, 0.8f),
            MicroBlockSpotKind.SodaLunchCounter => new Color(0.52f, 0.45f, 0.36f, 0.8f),
            MicroBlockSpotKind.PoolHall => new Color(0.42f, 0.36f, 0.5f, 0.82f),
            MicroBlockSpotKind.SmallClinic => new Color(0.52f, 0.4f, 0.4f, 0.8f),
            MicroBlockSpotKind.PoliceBeatOffice => new Color(0.32f, 0.38f, 0.55f, 0.85f),
            MicroBlockSpotKind.PostOfficeBranch => new Color(0.4f, 0.45f, 0.52f, 0.8f),
            MicroBlockSpotKind.ChurchParish => new Color(0.45f, 0.42f, 0.5f, 0.78f),
            MicroBlockSpotKind.Warehouse => new Color(0.4f, 0.38f, 0.34f, 0.82f),
            MicroBlockSpotKind.AutoGarage => new Color(0.45f, 0.4f, 0.32f, 0.8f),
            MicroBlockSpotKind.NeighborhoodPark => new Color(0.34f, 0.48f, 0.36f, 0.78f),
            MicroBlockSpotKind.SpeakeasyFront => new Color(0.48f, 0.34f, 0.4f, 0.82f),
            MicroBlockSpotKind.MissionHall => new Color(0.44f, 0.46f, 0.5f, 0.78f),
            MicroBlockSpotKind.FirehouseSmall => new Color(0.52f, 0.34f, 0.3f, 0.82f),
            MicroBlockSpotKind.PawnShop => new Color(0.48f, 0.42f, 0.38f, 0.8f),
            MicroBlockSpotKind.TelegraphDesk => new Color(0.4f, 0.46f, 0.52f, 0.8f),
            MicroBlockSpotKind.Newsstand => new Color(0.46f, 0.44f, 0.4f, 0.8f),
            _ => new Color(0.36f, 0.38f, 0.42f, 0.78f)
        };

    static Color DistrictFillColor(DistrictKind k, bool reserved)
    {
        if (reserved)
            return new Color(0.42f, 0.22f, 0.28f, 0.55f);
        return k switch
        {
            DistrictKind.DowntownCommercial => new Color(0.38f, 0.35f, 0.48f, 0.72f),
            DistrictKind.Industrial => new Color(0.42f, 0.38f, 0.32f, 0.72f),
            DistrictKind.WorkingClass => new Color(0.32f, 0.36f, 0.42f, 0.72f),
            DistrictKind.Residential => new Color(0.30f, 0.40f, 0.34f, 0.72f),
            DistrictKind.Wealthy => new Color(0.44f, 0.42f, 0.36f, 0.72f),
            DistrictKind.DocksPort => new Color(0.28f, 0.36f, 0.44f, 0.72f),
            DistrictKind.FringeOuterEdge => new Color(0.36f, 0.34f, 0.30f, 0.68f),
            _ => new Color(0.28f, 0.28f, 0.30f, 0.65f)
        };
    }

    const float SandboxGridSnapEpsilon = 0.85f;

    public static void BuildSandboxGridAxes(CityData city, out List<float> xStarts, out List<float> yStarts)
    {
        xStarts = null;
        yStarts = null;
        if (city?.Blocks == null || city.Blocks.Count == 0)
            return;

        var xs = new SortedSet<float>();
        var ys = new SortedSet<float>();
        for (int i = 0; i < city.Blocks.Count; i++)
        {
            xs.Add(city.Blocks[i].Min.x);
            ys.Add(city.Blocks[i].Min.y);
        }

        xStarts = new List<float>(xs);
        yStarts = new List<float>(ys);
    }

    public static bool TryGetBlockGridCell(BlockData block, List<float> xStarts, List<float> yStarts, out int cx, out int cy)
    {
        cx = cy = -1;
        if (block == null || xStarts == null || yStarts == null)
            return false;
        for (int i = 0; i < xStarts.Count; i++)
        {
            if (Mathf.Abs(xStarts[i] - block.Min.x) < SandboxGridSnapEpsilon)
            {
                cx = i;
                break;
            }
        }

        for (int i = 0; i < yStarts.Count; i++)
        {
            if (Mathf.Abs(yStarts[i] - block.Min.y) < SandboxGridSnapEpsilon)
            {
                cy = i;
                break;
            }
        }

        return cx >= 0 && cy >= 0;
    }

    /// <summary>
    /// Sandbox full map: darken blocks outside Chebyshev distance 1 from crew home (8 neighbors + self clear),
    /// unless the block was revealed by travel/scout (<see cref="GameSessionState.IsSandboxBlockRevealed"/>).
    /// </summary>
    static bool ShouldDrawSandboxFogOfWar(CityData city, int blockId, int crewHomeBlockId, List<float> gridXs, List<float> gridYs)
    {
        if (!GameSessionState.SingleBlockSandboxEnabled || crewHomeBlockId < 0)
            return false;
        if (blockId == crewHomeBlockId)
            return false;
        if (GameSessionState.IsSandboxBlockRevealed(blockId))
            return false;
        if (gridXs == null || gridYs == null || city?.Blocks == null)
            return false;

        BlockData home = null;
        BlockData blk = null;
        for (int i = 0; i < city.Blocks.Count; i++)
        {
            if (city.Blocks[i].Id == crewHomeBlockId)
                home = city.Blocks[i];
            if (city.Blocks[i].Id == blockId)
                blk = city.Blocks[i];
        }

        if (home == null || blk == null)
            return false;
        if (!TryGetBlockGridCell(home, gridXs, gridYs, out int hx, out int hy))
            return false;
        if (!TryGetBlockGridCell(blk, gridXs, gridYs, out int bx, out int by))
            return false;

        int d = Mathf.Max(Mathf.Abs(bx - hx), Mathf.Abs(by - hy));
        return d > 1;
    }

    /// <summary>True when this block is fogged on the Ops sandbox map (not crew home and not within reveal radius).</summary>
    public static bool IsSandboxMacroBlockUnderFog(CityData city, int blockId)
    {
        if (!GameSessionState.SingleBlockSandboxEnabled || city == null)
            return false;
        int home = MicroBlockWorldState.CrewHomeBlockId;
        if (home < 0)
            return false;
        BuildSandboxGridAxes(city, out List<float> gxs, out List<float> gys);
        return ShouldDrawSandboxFogOfWar(city, blockId, home, gxs, gys);
    }

    /// <summary>Optional tiling smoke / noise on top of fog — place at <c>Resources/OpsMap/FogSmokeTile</c> (PNG, no extension in path).</summary>
    const string FogSmokeSpriteResourcesPath = "OpsMap/FogSmokeTile";

    static Sprite _fogSmokeSpriteCache;
    static bool _fogSmokeSpriteResolved;

    static Sprite TryGetFogSmokeSprite()
    {
        if (_fogSmokeSpriteResolved)
            return _fogSmokeSpriteCache;
        _fogSmokeSpriteResolved = true;
        _fogSmokeSpriteCache = Resources.Load<Sprite>(FogSmokeSpriteResourcesPath);
        return _fogSmokeSpriteCache;
    }

    /// <summary>Strip identifiable building art before drawing the fog veil (macro block / lot / courtyard).</summary>
    static void ApplyUnidentifiedFogBaseFill(Image img)
    {
        if (img == null)
            return;
        img.sprite = null;
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = new Color(0.07f, 0.069f, 0.074f, 1f);
    }

    static void AppendFogOfWarOverlay(RectTransform parent)
    {
        if (parent == null)
            return;

        GameObject fogGo = new GameObject("FogVeil");
        fogGo.transform.SetParent(parent, false);
        RectTransform frt = fogGo.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.pivot = new Vector2(0.5f, 0.5f);
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        Image fog = fogGo.AddComponent<Image>();
        fog.sprite = null;
        fog.color = new Color(0.015f, 0.016f, 0.028f, 0.94f);
        // Pass clicks to the macro block Button so fogged tiles stay selectable for dispatch.
        fog.raycastTarget = false;

        Sprite smoke = TryGetFogSmokeSprite();
        if (smoke == null)
            return;

        GameObject smGo = new GameObject("FogSmoke");
        smGo.transform.SetParent(parent, false);
        RectTransform srt = smGo.AddComponent<RectTransform>();
        srt.anchorMin = Vector2.zero;
        srt.anchorMax = Vector2.one;
        srt.pivot = new Vector2(0.5f, 0.5f);
        srt.offsetMin = Vector2.zero;
        srt.offsetMax = Vector2.zero;
        Image sm = smGo.AddComponent<Image>();
        sm.sprite = smoke;
        sm.type = Image.Type.Tiled;
        sm.color = new Color(0.5f, 0.48f, 0.52f, 0.38f);
        sm.raycastTarget = false;
    }
}
